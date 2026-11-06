using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Head-forward yaw/pitch. LAN UDP drives the physical maquete; Supabase Broadcast stays stubbed.
    /// See docs/GAZE-PUBLISHER.md.
    /// </summary>
    public interface IGazePublisher
    {
        void StartSession(string code, ProfileId profile);
        void StopSession();
    }

    public class GazePublisher : MonoBehaviour, IGazePublisher
    {
        public bool broadcasting;
        public string channel;
        public float yaw;
        public float pitch;
        public float sendHz = 12f;

        [Header("Maquete Wi-Fi (ESP32)")]
        public bool sendUdpLan = true;
        [Tooltip("If true, UDP starts in Play even before a session code.")]
        public bool sendBeforeSession = true;
        public string udpHost = "192.168.1.80";
        public int udpPort = 9000;
        public bool invertYaw;
        [Range(0.2f, 1.6f)]
        public float yawLimitRad = GazeLanCodec.DefaultYawLimitRad;
        public int servoMin = GazeLanCodec.DefaultServoMin;
        public int servoMax = GazeLanCodec.DefaultServoMax;
        [Range(0f, 1f)]
        public float smooth = 0.35f;

        float nextSend;
        float yawSmooth;
        string code;
        ProfileId profile;
        bool helloSent;
        bool warnedNet;
        UdpClient udp;
        IPEndPoint endpoint;

        public void StartSession(string sessionCode, ProfileId id)
        {
            code = sessionCode;
            profile = id;
            channel = "gaze:" + sessionCode;
            broadcasting = sendUdpLan;
            helloSent = false;
            Debug.Log("[GazePublisher] session " + channel + " profile=" + id + " udp=" + sendUdpLan + " " + udpHost + ":" + udpPort);
        }

        public void StopSession()
        {
            broadcasting = sendBeforeSession && sendUdpLan;
            channel = null;
        }

        void OnDisable()
        {
            CloseUdp();
        }

        void OnDestroy()
        {
            CloseUdp();
        }

        void Update()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            var f = cam.transform.forward;
            yaw = Mathf.Atan2(f.x, f.z);
            pitch = Mathf.Asin(Mathf.Clamp(f.y, -1f, 1f));
            yawSmooth = Mathf.Lerp(yawSmooth, yaw, 1f - Mathf.Clamp01(smooth));

            var live = sendUdpLan && (broadcasting || sendBeforeSession);
            if (!live)
                return;
            if (string.IsNullOrWhiteSpace(udpHost) || udpPort <= 0)
                return;
            if (Time.unscaledTime < nextSend)
                return;
            nextSend = Time.unscaledTime + 1f / Mathf.Max(10f, sendHz);
            if (!helloSent)
                helloSent = true;
            SendLan();
        }

        void SendLan()
        {
            try
            {
                EnsureEndpoint();
                if (udp == null || endpoint == null)
                    return;
                var servo = GazeLanCodec.ServoFromYaw(yawSmooth, yawLimitRad, servoMin, servoMax, invertYaw);
                var line = GazeLanCodec.FormatLine(yawSmooth, pitch, servo);
                var bytes = Encoding.ASCII.GetBytes(line);
                udp.Send(bytes, bytes.Length, endpoint);
            }
            catch (Exception ex)
            {
                if (warnedNet)
                    return;
                warnedNet = true;
                Debug.LogWarning("[GazePublisher] UDP send failed (maquete offline?): " + ex.Message);
            }
        }

        void EnsureEndpoint()
        {
            if (udp == null)
                udp = new UdpClient();
            if (endpoint != null && endpoint.Port == udpPort && endpoint.Address.ToString() == udpHost.Trim())
                return;
            if (!IPAddress.TryParse(udpHost.Trim(), out var ip))
            {
                if (!warnedNet)
                {
                    warnedNet = true;
                    Debug.LogWarning("[GazePublisher] udpHost must be an IPv4 address, got '" + udpHost + "'");
                }
                endpoint = null;
                return;
            }
            endpoint = new IPEndPoint(ip, udpPort);
        }

        void CloseUdp()
        {
            if (udp == null)
                return;
            try
            {
                udp.Close();
            }
            catch (Exception)
            {
                // ignore
            }
            udp = null;
            endpoint = null;
        }
    }
}
