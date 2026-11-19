using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Minimal MQTT 3.1.1 publisher (QoS 0, no credentials) for the physical maquete.
    /// Runs on its own thread with reconnect + exponential backoff and broker fallback, so the
    /// main thread only drops the newest line in a slot and never blocks on the network.
    /// </summary>
    public sealed class MaqueteMqttLink : IDisposable
    {
        readonly string[] hosts;
        readonly int port;
        readonly string topic;
        readonly string clientId;
        readonly object slotGate = new object();
        readonly AutoResetEvent wake = new AutoResetEvent(false);
        string pending;
        Thread thread;
        volatile bool running;

        public volatile bool Connected;
        public volatile string Status = "MQTT parado";
        public string CurrentHost { get; private set; }
        public long Published;

        public event Action<string> Log;

        public MaqueteMqttLink(string[] hosts, int port, string topic)
        {
            this.hosts = hosts;
            this.port = port;
            this.topic = topic;
            clientId = "gj-quest-" + Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        public void Start()
        {
            if (running)
                return;
            running = true;
            thread = new Thread(Run) { IsBackground = true, Name = "GenJutsuMaqueteMqtt" };
            thread.Start();
        }

        /// <summary>Queue the newest payload; older unsent payloads are replaced (newest wins).</summary>
        public void Publish(string payload)
        {
            lock (slotGate)
                pending = payload;
            wake.Set();
        }

        public void Dispose()
        {
            running = false;
            wake.Set();
        }

        void Emit(string msg)
        {
            var log = Log;
            if (log != null)
                log(msg);
        }

        void Run()
        {
            var backoffMs = 1000;
            var hostIndex = 0;
            while (running)
            {
                var host = hosts[hostIndex % hosts.Length];
                TcpClient client = null;
                try
                {
                    Status = "MQTT conectando " + host;
                    client = new TcpClient { NoDelay = true };
                    var connect = client.ConnectAsync(host, port);
                    if (!connect.Wait(5000) || !client.Connected)
                        throw new IOException("timeout TCP");
                    var stream = client.GetStream();
                    stream.ReadTimeout = 5000;
                    var packet = BuildConnect(clientId, 30);
                    stream.Write(packet, 0, packet.Length);
                    var header = stream.ReadByte();
                    var remaining = stream.ReadByte();
                    if (header != 0x20 || remaining < 2)
                        throw new IOException("CONNACK inválido");
                    var ack = new byte[remaining];
                    ReadFully(stream, ack);
                    if (ack[1] != 0)
                        throw new IOException("CONNACK rc=" + ack[1]);

                    CurrentHost = host;
                    Connected = true;
                    Status = "MQTT conectado " + host;
                    backoffMs = 1000;
                    Emit("MQTT connected " + host + ":" + port + " topic=" + topic + " id=" + clientId);
                    Pump(client, stream);
                }
                catch (Exception ex)
                {
                    if (running)
                        Emit("MQTT " + host + " falhou: " + ex.Message + " (nova tentativa em " + backoffMs / 1000 + "s)");
                }
                finally
                {
                    Connected = false;
                    try { if (client != null) client.Close(); } catch (Exception) { }
                }

                if (!running)
                    break;
                Status = "MQTT desconectado";
                hostIndex++;
                wake.WaitOne(backoffMs);
                backoffMs = Math.Min(backoffMs * 2, 30000);
            }
            Status = "MQTT parado";
        }

        void Pump(TcpClient client, NetworkStream stream)
        {
            var lastPing = DateTime.UtcNow;
            var lastRx = DateTime.UtcNow;
            var buf = new byte[256];
            while (running)
            {
                wake.WaitOne(50);
                string line;
                lock (slotGate)
                {
                    line = pending;
                    pending = null;
                }
                if (line != null)
                {
                    var pkt = BuildPublish(topic, line);
                    stream.Write(pkt, 0, pkt.Length);
                    Interlocked.Increment(ref Published);
                }

                while (client.Available > 0)
                {
                    var n = stream.Read(buf, 0, Math.Min(buf.Length, client.Available));
                    if (n <= 0)
                        throw new IOException("socket fechado");
                    lastRx = DateTime.UtcNow;
                }

                var now = DateTime.UtcNow;
                if ((now - lastPing).TotalSeconds >= 10)
                {
                    stream.Write(new byte[] { 0xC0, 0x00 }, 0, 2);
                    lastPing = now;
                }
                if ((now - lastRx).TotalSeconds > 35)
                    throw new IOException("sem PINGRESP");
                if (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0)
                    throw new IOException("broker fechou a conexão");
            }
        }

        static void ReadFully(Stream s, byte[] b)
        {
            var off = 0;
            while (off < b.Length)
            {
                var n = s.Read(b, off, b.Length - off);
                if (n <= 0)
                    throw new IOException("EOF");
                off += n;
            }
        }

        static byte[] BuildConnect(string id, int keepAliveSec)
        {
            using (var body = new MemoryStream())
            {
                WriteString(body, "MQTT");
                body.WriteByte(4);
                body.WriteByte(0x02);
                body.WriteByte((byte)(keepAliveSec >> 8));
                body.WriteByte((byte)keepAliveSec);
                WriteString(body, id);
                return Finish(0x10, body.ToArray());
            }
        }

        static byte[] BuildPublish(string t, string payload)
        {
            using (var body = new MemoryStream())
            {
                WriteString(body, t);
                var bytes = Encoding.ASCII.GetBytes(payload ?? "");
                body.Write(bytes, 0, bytes.Length);
                return Finish(0x30, body.ToArray());
            }
        }

        static byte[] Finish(byte header, byte[] body)
        {
            using (var packet = new MemoryStream())
            {
                packet.WriteByte(header);
                var length = body.Length;
                do
                {
                    var digit = length % 128;
                    length /= 128;
                    if (length > 0)
                        digit |= 0x80;
                    packet.WriteByte((byte)digit);
                }
                while (length > 0);
                packet.Write(body, 0, body.Length);
                return packet.ToArray();
            }
        }

        static void WriteString(Stream stream, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.WriteByte((byte)(bytes.Length >> 8));
            stream.WriteByte((byte)bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
