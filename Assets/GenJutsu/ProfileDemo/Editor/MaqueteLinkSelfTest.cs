using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GenJutsu.ProfileDemo.EditorTools
{
    /// <summary>
    /// Sends a pitch sweep through the same MQTT + UDP-broadcast path the APK uses, so the
    /// maquete can be tested from the PC. Run with tools/watch.ps1 open to see it arrive.
    /// Batch: Unity.exe -batchmode -quit -projectPath . -executeMethod GenJutsu.ProfileDemo.EditorTools.MaqueteLinkSelfTest.Run
    /// </summary>
    public static class MaqueteLinkSelfTest
    {
        [MenuItem("GenJutsu/Maquete: enviar teste (MQTT + UDP)")]
        public static void Run()
        {
            var link = new MaqueteMqttLink(new[] { "broker.emqx.io", "broker.hivemq.com" }, 1883, GazePublisher.DefaultMqttTopic);
            link.Log += m => Debug.Log("[MaqueteSelfTest] " + m);
            link.Start();
            var sw = Stopwatch.StartNew();
            while (!link.Connected && sw.ElapsedMilliseconds < 15000)
                Thread.Sleep(50);
            Debug.Log("[MaqueteSelfTest] connected=" + link.Connected + " after " + sw.ElapsedMilliseconds + "ms");

            var udp = new UdpClient { EnableBroadcast = true };
            var ep = new IPEndPoint(IPAddress.Broadcast, 9000);
            var udpOk = 0;
            for (var i = 1; i <= 60; i++)
            {
                var deg = 40f * (float)System.Math.Sin(i * 2 * System.Math.PI / 30);
                var line = GazeLanCodec.FormatLine(0f, deg / 57.2958f, 90, deg, 900000 + i);
                link.Publish(line.TrimEnd('\n'));
                try
                {
                    var b = Encoding.ASCII.GetBytes(line);
                    udp.Send(b, b.Length, ep);
                    udpOk++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[MaqueteSelfTest] udp " + ex.Message);
                }
                Thread.Sleep(83);
            }
            Thread.Sleep(500);
            Debug.Log("[MaqueteSelfTest] done mqttPublished=" + link.Published + " udpSent=" + udpOk + " status=" + link.Status);
            link.Dispose();
            udp.Close();
        }
    }
}
