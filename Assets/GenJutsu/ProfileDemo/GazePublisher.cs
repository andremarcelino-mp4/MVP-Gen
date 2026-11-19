using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Head-forward yaw/pitch → physical maquete (ESP32 + MG996R + MPU6050 tilt).
    /// Two transports, no fixed IP: MQTT over the internet (works on any network) and
    /// UDP broadcast on the local network (works without internet). The ESP32 dedups by
    /// sequence number, so the newest packet wins. Supabase Broadcast stays stubbed.
    /// See docs/GAZE-PUBLISHER.md.
    /// </summary>
    public interface IGazePublisher
    {
        void StartSession(string code, ProfileId profile);
        void StopSession();
    }

    public class GazePublisher : MonoBehaviour, IGazePublisher
    {
        public const string DefaultMqttTopic = "genjutsu-9de75d60960b/maquete/gaze";

        public bool broadcasting;
        public string channel;
        public float yaw;
        public float pitch;
        public float pitchDeg;
        public float sendHz = 12f;
        [Tooltip("If true, packets go out in Play even before a session code.")]
        public bool sendBeforeSession = true;

        [Header("Maquete pitch (cima/baixo)")]
        [Range(10f, 89f)]
        public float pitchLimitDeg = GazeLanCodec.DefaultPitchLimitDeg;
        public bool invertPitch;
        [Range(0f, 1f)]
        public float smooth = 0.35f;

        [Header("Maquete via internet (MQTT)")]
        public bool sendMqtt = true;
        public string mqttHost = "broker.emqx.io";
        public string mqttFallbackHost = "broker.hivemq.com";
        public int mqttPort = 1883;
        public string mqttTopic = DefaultMqttTopic;

        [Header("Maquete via rede local (UDP broadcast)")]
        public bool sendUdpLan = true;
        public int udpPort = 9000;
        [Tooltip("Optional extra unicast IPv4 (leave empty: broadcast is enough).")]
        public string udpExtraHost = "";

        [Header("Legacy yaw servo field (S) kept for old firmware")]
        public bool invertYaw;
        [Range(0.2f, 1.6f)]
        public float yawLimitRad = GazeLanCodec.DefaultYawLimitRad;
        public int servoMin = GazeLanCodec.DefaultServoMin;
        public int servoMax = GazeLanCodec.DefaultServoMax;

        float nextSend;
        float nextStat;
        float yawSmooth;
        float pitchSmooth;
        int seq;
        long udpSent;
        string code;
        ProfileId profile;
        bool warnedUdp;
        UdpClient udp;
        IPEndPoint broadcastEp;
        IPEndPoint extraEp;
        MaqueteMqttLink mqtt;
        readonly System.Collections.Concurrent.ConcurrentQueue<string> logs = new System.Collections.Concurrent.ConcurrentQueue<string>();

        public string MqttStatus => mqtt != null ? mqtt.Status : "MQTT off";

        public void StartSession(string sessionCode, ProfileId id)
        {
            code = sessionCode;
            profile = id;
            channel = "gaze:" + sessionCode;
            broadcasting = true;
            Debug.Log("[GazePublisher] session " + channel + " profile=" + id + " mqtt=" + sendMqtt + " " + mqttTopic + " udpBroadcast=" + sendUdpLan + " :" + udpPort);
        }

        public void StopSession()
        {
            broadcasting = false;
            channel = null;
        }

        void OnEnable()
        {
            Debug.Log("[GazePublisher] maquete pitch: mqtt=" + sendMqtt + " " + mqttHost + "/" + mqttFallbackHost + ":" + mqttPort + " topic=" + mqttTopic + " udp=" + sendUdpLan + " 255.255.255.255:" + udpPort);
        }

        void OnDisable()
        {
            CloseNet();
        }

        void OnDestroy()
        {
            CloseNet();
        }

        void Update()
        {
            while (logs.TryDequeue(out var msg))
                Debug.Log("[GazePublisher] " + msg);

            var cam = Camera.main;
            if (cam == null)
                return;
            var f = cam.transform.forward;
            yaw = Mathf.Atan2(f.x, f.z);
            pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f));
            var k = 1f - Mathf.Clamp01(smooth);
            yawSmooth = Mathf.Lerp(yawSmooth, yaw, k);
            pitchSmooth = Mathf.Lerp(pitchSmooth, pitch, k);
            pitchDeg = GazeLanCodec.PitchDegrees(pitchSmooth, pitchLimitDeg, invertPitch);

            var live = broadcasting || sendBeforeSession;
            if (!live || (!sendMqtt && !sendUdpLan))
                return;
            if (Time.unscaledTime < nextSend)
                return;
            nextSend = Time.unscaledTime + 1f / Mathf.Clamp(sendHz, 5f, 30f);

            var servo = GazeLanCodec.ServoFromYaw(yawSmooth, yawLimitRad, servoMin, servoMax, invertYaw);
            seq = (seq + 1) & 0x3FFFFFFF;
            var line = GazeLanCodec.FormatLine(yawSmooth, pitchSmooth, servo, pitchDeg, seq);

            if (sendMqtt)
                SendMqtt(line);
            if (sendUdpLan)
                SendUdp(line);

            if (Time.unscaledTime >= nextStat)
            {
                nextStat = Time.unscaledTime + 5f;
                Debug.Log("[GazePublisher] pitch=" + pitchDeg.ToString("0.0") + "deg seq=" + seq
                          + " mqtt=" + MqttStatus + " pub=" + (mqtt != null ? mqtt.Published : 0)
                          + " udp=" + udpSent);
            }
        }

        void SendMqtt(string line)
        {
            if (mqtt == null)
            {
                if (string.IsNullOrWhiteSpace(mqttHost) || string.IsNullOrWhiteSpace(mqttTopic))
                    return;
                var hosts = string.IsNullOrWhiteSpace(mqttFallbackHost)
                    ? new[] { mqttHost.Trim() }
                    : new[] { mqttHost.Trim(), mqttFallbackHost.Trim() };
                mqtt = new MaqueteMqttLink(hosts, mqttPort, mqttTopic.Trim());
                mqtt.Log += m => logs.Enqueue(m);
                mqtt.Start();
            }
            mqtt.Publish(line.TrimEnd('\n'));
        }

        void SendUdp(string line)
        {
            try
            {
                if (udp == null)
                {
                    udp = new UdpClient();
                    udp.EnableBroadcast = true;
                    broadcastEp = new IPEndPoint(IPAddress.Broadcast, udpPort);
                    extraEp = null;
                    if (!string.IsNullOrWhiteSpace(udpExtraHost) && IPAddress.TryParse(udpExtraHost.Trim(), out var ip))
                        extraEp = new IPEndPoint(ip, udpPort);
                }
                var bytes = Encoding.ASCII.GetBytes(line);
                udp.Send(bytes, bytes.Length, broadcastEp);
                if (extraEp != null)
                    udp.Send(bytes, bytes.Length, extraEp);
                udpSent++;
            }
            catch (Exception ex)
            {
                if (!warnedUdp)
                {
                    warnedUdp = true;
                    Debug.LogWarning("[GazePublisher] UDP broadcast failed (sem Wi-Fi?): " + ex.Message);
                }
                try { if (udp != null) udp.Close(); } catch (Exception) { }
                udp = null;
            }
        }

        void CloseNet()
        {
            if (udp != null)
            {
                try { udp.Close(); } catch (Exception) { }
                udp = null;
            }
            if (mqtt != null)
            {
                mqtt.Dispose();
                mqtt = null;
            }
        }
    }
}
