using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace GenJutsu.Gen
{
    /// <summary>
    /// Runs Unity API calls on the player loop. Socket and HTTP work stays on worker threads and posts here.
    /// </summary>
    public sealed class MainThread : MonoBehaviour
    {
        static readonly ConcurrentQueue<Action> Queue = new ConcurrentQueue<Action>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<MainThread>() != null)
                return;
            var go = new GameObject("GenMainThread");
            DontDestroyOnLoad(go);
            go.AddComponent<MainThread>();
        }

        public static void Post(Action action)
        {
            if (action != null)
                Queue.Enqueue(action);
        }

        void Update()
        {
            var guard = 0;
            while (guard++ < 64 && Queue.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
