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
            if (host == null || string.IsNullOrEmpty(deviceId))
                return;
            host.StartCoroutine(Send(baseUrl, deviceId, on));
        }

        static IEnumerator Send(string baseUrl, string deviceId, bool on)
        {
            var root = string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/');
            var url = root + "/api/house/device";
            var json = "{\"device_id\":\"" + deviceId + "\",\"on\":" + (on ? "true" : "false") + "}";
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
