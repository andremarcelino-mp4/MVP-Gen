# Escuta o UDP broadcast da maquete (porta 9000) no PC, para conferir o caminho local.
param([int]$Port = 9000, [int]$Seconds = 30)
$u = New-Object System.Net.Sockets.UdpClient
$u.Client.SetSocketOption([System.Net.Sockets.SocketOptionLevel]::Socket, [System.Net.Sockets.SocketOptionName]::ReuseAddress, $true)
$u.Client.Bind((New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, $Port)))
$u.Client.ReceiveTimeout = 1000
$ep = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
$t0 = Get-Date; $n = 0
while (((Get-Date) - $t0).TotalSeconds -lt $Seconds) {
  try { $b = $u.Receive([ref]$ep); $n++; if ($n -eq 1 -or $n % 12 -eq 0) { Write-Host ("#{0} de {1}: {2}" -f $n, $ep, [Text.Encoding]::ASCII.GetString($b).Trim()) } } catch { }
}
$u.Close(); Write-Host "total udp: $n"
