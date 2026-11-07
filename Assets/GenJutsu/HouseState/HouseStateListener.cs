using System.Collections;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GenJutsu.GenCore;
using GenJutsu.ProfileDemo;
using UnityEngine;

namespace GenJutsu.HouseState
{
    /// <summary>
    /// Polls GET /api/house/state every second. A C# Supabase Realtime client is not the MVP path on IL2CPP;
    /// the channel would be house:&lt;session_id&gt; / house_state_changed, and only a higher version is applied.
    /// </summary>
    public sealed class HouseStateListener : MonoBehaviour
    {
        static readonly HttpClient Http = NewClient();

        public string baseUrl = "https://genjutsuai.vercel.app";
        public float pollSeconds = 1f;
        public SessionGate gate;
        public HouseStateApplier applier;

        readonly HouseStateCursor cursor = new HouseStateCursor();
        Coroutine loop;
        string seenSession;

        public int AppliedVersion => cursor.Version;

        void OnEnable()
        {
            if (gate != null)
                gate.SessionEnded += OnSessionEnded;
            if (loop == null)
                loop = StartCoroutine(Poll());
        }

        void OnDisable()
        {
            if (gate != null)
                gate.SessionEnded -= OnSessionEnded;
            if (loop != null)
            {
                StopCoroutine(loop);
                loop = null;
            }
        }

        void OnSessionEnded()
        {
            cursor.Reset();
            seenSession = null;
            if (applier != null)
                applier.Release();
        }

        public void Ingest(string json)
        {
            var expected = gate != null ? gate.ActiveSessionKey : null;
            if (seenSession != expected)
            {
                cursor.Reset();
                seenSession = expected;
            }

            if (!cursor.Offer(json, expected))
                return;
            if (applier != null && cursor.HasPlan)
                applier.ApplyPlan(cursor.Plan);
        }

        IEnumerator Poll()
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(0.2f, pollSeconds));
            while (enabled)
            {
                if (gate != null && gate.suppressNetworkPoll)
                {
                    yield return wait;
                    continue;
                }

                if (gate == null || !gate.HasSession)
                {
                    yield return wait;
                    continue;
                }

                var url = (string.IsNullOrEmpty(baseUrl) ? "https://genjutsuai.vercel.app" : baseUrl.TrimEnd('/'))
                          + "/api/house/state";
                string body = null;
                var done = 0;
                Task.Run(() =>
                {
                    try
                    {
                        body = Http.GetStringAsync(url).GetAwaiter().GetResult();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("House state: " + e.GetType().Name);
                    }
                    finally
                    {
                        Interlocked.Exchange(ref done, 1);
                    }
                });
                while (Interlocked.CompareExchange(ref done, 0, 0) == 0)
                    yield return null;
                if (!string.IsNullOrEmpty(body))
                    Ingest(body);
                yield return wait;
            }
        }

        static HttpClient NewClient()
        {
            var client = new HttpClient();
            client.Timeout = System.TimeSpan.FromSeconds(8);
            return client;
        }
    }
}
