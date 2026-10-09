using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GenJutsu.AITest
{
    public class HouseMqttClient : MonoBehaviour
    {
        public string StatusText { get; private set; } = "MQTT desconectado";

        TcpClient client;
        NetworkStream stream;
        Thread thread;
        volatile bool running;
        volatile bool connected;
        float nextPing;
        readonly object gate = new object();

        public void Connect(string host, int port)
        {
            Disconnect();
            StatusText = "MQTT conectando";
            thread = new Thread(() => ConnectBlocking(host, port)) { IsBackground = true, Name = "GenJutsuMqtt" };
            thread.Start();
        }

        public void Publish(string topic, string payload)
        {
            if (!connected || stream == null)
                return;
            try
            {
                lock (gate)
                {
                    var packet = BuildPublish(topic, payload);
                    stream.Write(packet, 0, packet.Length);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[GenJutsu] Etapa MQTT: publicação falhou. " + exception.Message);
                connected = false;
                StatusText = "MQTT desconectado";
            }
        }

        void Update()
        {
            if (!connected || Time.unscaledTime < nextPing)
                return;
            nextPing = Time.unscaledTime + 20f;
            try
            {
                lock (gate)
                {
                    if (stream != null)
                        stream.Write(new byte[] { 0xC0, 0x00 }, 0, 2);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[GenJutsu] Etapa MQTT: conexão perdida. " + exception.Message);
                connected = false;
                StatusText = "MQTT desconectado";
            }
        }

        void ConnectBlocking(string host, int port)
        {
            try
            {
                client = new TcpClient();
                var connect = client.ConnectAsync(host, port);
                if (!connect.Wait(2500) || !client.Connected)
                    throw new IOException("broker indisponível");
                stream = client.GetStream();
                stream.ReadTimeout = 2000;
                var packet = BuildConnect("genjutsu-ai-test");
                lock (gate)
                    stream.Write(packet, 0, packet.Length);
                var header = stream.ReadByte();
                var remaining = stream.ReadByte();
                if (header != 0x20 || remaining < 2)
                    throw new IOException("CONNACK inválido");
                var ack = new byte[remaining];
                stream.Read(ack, 0, ack.Length);
                if (ack[1] != 0)
                    throw new IOException("CONNACK " + ack[1]);
                connected = true;
                running = true;
                StatusText = "MQTT conectado";
                Debug.Log("[GenJutsu] MQTT conectado em " + host + ":" + port);
            }
            catch (Exception exception)
            {
                connected = false;
                StatusText = "MQTT desconectado";
                Debug.LogWarning("[GenJutsu] Etapa MQTT: " + exception.Message + ". O chat continua.");
                CloseSocket();
            }
        }

        public void Disconnect()
        {
            running = false;
            connected = false;
            CloseSocket();
            StatusText = "MQTT desconectado";
        }

        void CloseSocket()
        {
            try { if (stream != null) stream.Close(); } catch (Exception) { }
            try { if (client != null) client.Close(); } catch (Exception) { }
            stream = null;
            client = null;
        }

        void OnDestroy()
        {
            Disconnect();
        }

        static byte[] BuildConnect(string clientId)
        {
            using (var body = new MemoryStream())
            {
                WriteString(body, "MQTT");
                body.WriteByte(4);
                body.WriteByte(0x02);
                body.WriteByte(0);
                body.WriteByte(30);
                WriteString(body, clientId);
                return Finish(0x10, body.ToArray());
            }
        }

        static byte[] BuildPublish(string topic, string payload)
        {
            using (var body = new MemoryStream())
            {
                WriteString(body, topic);
                var bytes = Encoding.UTF8.GetBytes(payload ?? "");
                body.Write(bytes, 0, bytes.Length);
                return Finish(0x30, body.ToArray());
            }
        }

        static byte[] Finish(byte header, byte[] body)
        {
            using (var packet = new MemoryStream())
            {
                packet.WriteByte(header);
                WriteRemaining(packet, body.Length);
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

        static void WriteRemaining(Stream stream, int length)
        {
            do
            {
                var digit = length % 128;
                length /= 128;
                if (length > 0)
                    digit |= 0x80;
                stream.WriteByte((byte)digit);
            }
            while (length > 0);
        }
    }
}
