using System.Collections;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GenJutsu.HouseState
{
    /// <summary>POST /api/house/device with no secrets. Server holds the keys.</summary>
    public static class HouseDeviceClient
    {
        static readonly HttpClient Http = new HttpClient();

        public static void Post(MonoBehaviour host, string baseUrl, string deviceId, bool on)
        {
            PostJson(host, baseUrl, "{\"device_id\":\"" + Esc(deviceId) + "\",\"on\":" + (on ? "true" : "false") + "}");
        }

        public static void PostAc(MonoBehaviour host, string baseUrl, bool on, int temp, string mode, string fan)
        {
            var t = Mathf.Clamp(temp, 16, 30);
            var json = "{\"device_id\":\"ar_condicionado\",\"on\":" + (on ? "true" : "false")
                       + ",\"temp\":" + t
                       + ",\"mode\":\"" + Esc(string.IsNullOrEmpty(mode) ? "frio" : mode)
                       + "\",\"fan\":\"" + Esc(string.IsNullOrEmpty(fan) ? "auto" : fan) + "\"}";
            PostJson(host, baseUrl, json);
        }

        public static void PostJson(MonoBehaviour host, string baseUrl, string json)
        {
            if (host == null || string.IsNullOrEmpty(json))
                return;
            host.StartCoroutine(Send(baseUrl, json));
        }

        static string Esc(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        static IEnumerator Send(string baseUrl, string json)
        {
            var root = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            var url = root + "/api/house/device";
            var done = 0;
            Task.Run(() =>
            {
                try
                {
                    Http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"))
                        .GetAwaiter().GetResult();
                }
                catch (System.Exception)
                {
                    /* poll will catch up */
                }
                finally
                {
                    Interlocked.Exchange(ref done, 1);
                }
            });
            while (Interlocked.CompareExchange(ref done, 0, 0) == 0)
                yield return null;
        }
    }
}
