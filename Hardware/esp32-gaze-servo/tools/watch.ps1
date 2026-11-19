# Mostra o que chega no tópico da maquete (o que o Quest e o ESP32 publicam). Ctrl+C para sair.
# Uso: powershell -ExecutionPolicy Bypass -File tools\watch.ps1 [-Seconds 60]
param(
  [string]$BrokerHost = "broker.emqx.io",
  [int]$Port = 1883,
  [string]$Filter = "genjutsu-9de75d60960b/maquete/#",
  [int]$Seconds = 0,
  [int]$Every = 12
)

function Str([string]$s) { $b = [Text.Encoding]::UTF8.GetBytes($s); ,([byte[]](@([byte]($b.Length -shr 8), [byte]($b.Length -band 255)) + $b)) }
function Packet([byte]$h, [byte[]]$body) {
  $len = $body.Length; $rl = New-Object System.Collections.Generic.List[byte]
  do { $d = $len % 128; $len = [math]::Floor($len / 128); if ($len -gt 0) { $d = $d -bor 0x80 }; $rl.Add([byte]$d) } while ($len -gt 0)
  ,([byte[]](@($h) + $rl.ToArray() + $body))
}
function ReadN($s, [int]$n) { $b = New-Object byte[] $n; $o = 0; while ($o -lt $n) { $r = $s.Read($b, $o, $n - $o); if ($r -le 0) { throw "conexao fechada" }; $o += $r }; ,$b }

$tcp = New-Object System.Net.Sockets.TcpClient($BrokerHost, $Port)
$s = $tcp.GetStream(); $s.ReadTimeout = 15000
$id = "gj-pc-watch-" + (Get-Random -Maximum 999999)
$conn = Packet 0x10 ([byte[]]((Str "MQTT") + @([byte]4, [byte]2, [byte]0, [byte]60) + (Str $id)))
$s.Write($conn, 0, $conn.Length)
$ack = ReadN $s 4
if ($ack[0] -ne 0x20 -or $ack[3] -ne 0) { throw "CONNACK falhou" }
$sub = Packet 0x82 ([byte[]](@([byte]0, [byte]1) + (Str $Filter) + @([byte]0)))
$s.Write($sub, 0, $sub.Length)
Write-Host "assinando $Filter em ${BrokerHost}:$Port"

$t0 = Get-Date; $lastPing = Get-Date; $count = 0; $tick = 0
while ($Seconds -le 0 -or ((Get-Date) - $t0).TotalSeconds -lt $Seconds) {
  if (((Get-Date) - $lastPing).TotalSeconds -gt 20) { $s.Write([byte[]](0xC0, 0), 0, 2); $lastPing = Get-Date }
  if (-not $tcp.Client.Poll(200000, [System.Net.Sockets.SelectMode]::SelectRead)) { continue }
  $h = (ReadN $s 1)[0]
  $len = 0; $mul = 1
  do { $d = (ReadN $s 1)[0]; $len += ($d -band 127) * $mul; $mul *= 128 } while ($d -band 128)
  $body = if ($len -gt 0) { ReadN $s $len } else { [byte[]]@() }
  if (($h -band 0xF0) -eq 0x30) {
    $tl = $body[0] * 256 + $body[1]
    $topic = [Text.Encoding]::UTF8.GetString($body, 2, $tl)
    $off = 2 + $tl; if (($h -band 6) -ne 0) { $off += 2 }
    $msg = [Text.Encoding]::UTF8.GetString($body, $off, $body.Length - $off)
    if ($topic -like "*/gaze") { $count++; if ($count -eq 1 -or $count % $Every -eq 0) { Write-Host ("{0:HH:mm:ss} #{1} {2} {3}" -f (Get-Date), $count, $topic, $msg) } }
    else { Write-Host ("{0:HH:mm:ss} {1} {2}" -f (Get-Date), $topic, $msg) }
  }
}
$tcp.Close()
Write-Host "total gaze msgs: $count"
