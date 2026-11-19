# Envia uma varredura de inclinação para a maquete pelo MQTT público (sem precisar do Quest).
# Uso (PowerShell):  .\send-test.ps1            -> varre -40..+40 graus por ~20 s
#                    .\send-test.ps1 -Fixed 20  -> mantém 20 graus por 10 s
param(
  [string]$BrokerHost = "broker.emqx.io",
  [int]$Port = 1883,
  [string]$Topic = "genjutsu-9de75d60960b/maquete/gaze",
  [double]$Fixed = [double]::NaN,
  [int]$Seconds = 20
)

function Str([string]$s) { $b = [Text.Encoding]::UTF8.GetBytes($s); ,([byte[]](@([byte]($b.Length -shr 8), [byte]($b.Length -band 255)) + $b)) }
function Packet([byte]$h, [byte[]]$body) {
  $len = $body.Length; $rl = New-Object System.Collections.Generic.List[byte]
  do { $d = $len % 128; $len = [math]::Floor($len / 128); if ($len -gt 0) { $d = $d -bor 0x80 }; $rl.Add([byte]$d) } while ($len -gt 0)
  ,([byte[]](@($h) + $rl.ToArray() + $body))
}

$tcp = New-Object System.Net.Sockets.TcpClient($BrokerHost, $Port)
$s = $tcp.GetStream()
$id = "gj-pc-test-" + (Get-Random -Maximum 999999)
$conn = Packet 0x10 ([byte[]]((Str "MQTT") + @([byte]4, [byte]2, [byte]0, [byte]30) + (Str $id)))
$s.Write($conn, 0, $conn.Length)
$s.ReadTimeout = 5000
$ack = New-Object byte[] 4; $got = 0
while ($got -lt 4) { $r = $s.Read($ack, $got, 4 - $got); if ($r -le 0) { break }; $got += $r }
if ($ack[0] -ne 0x20 -or $ack[3] -ne 0) { throw "CONNACK falhou: $($ack -join ',')" }
Write-Host "MQTT conectado em ${BrokerHost}:$Port, topico $Topic"

$inv = [Globalization.CultureInfo]::InvariantCulture
$t0 = Get-Date; $n = 0
while (((Get-Date) - $t0).TotalSeconds -lt $Seconds) {
  $t = ((Get-Date) - $t0).TotalSeconds
  $deg = if ([double]::IsNaN($Fixed)) { 40 * [math]::Sin($t * 2 * [math]::PI / 6) } else { $Fixed }
  $n++
  $line = "Y,0.000,P,{0},S,90,PD,{1},N,{2}" -f ($deg / 57.2958).ToString("0.000", $inv), $deg.ToString("0.0", $inv), $n
  $pub = Packet 0x30 ([byte[]]((Str $Topic) + [Text.Encoding]::ASCII.GetBytes($line)))
  $s.Write($pub, 0, $pub.Length)
  if ($n % 12 -eq 0) { Write-Host $line }
  Start-Sleep -Milliseconds 83
}
$s.Write([byte[]](0xE0, 0), 0, 2); $tcp.Close()
Write-Host "enviados $n pacotes"
