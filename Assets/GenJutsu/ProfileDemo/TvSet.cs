using GenJutsu.GenCore;
using GenJutsu.HouseState;
using UnityEngine;
using UnityEngine.Video;

namespace GenJutsu.ProfileDemo
{
    /// <summary>TV screen: house_state.media, remote power, mini-game when the pad is grabbed.</summary>
    public sealed class TvSet : MonoBehaviour
    {
        public Renderer screen;
        public AudioSource speaker;
        public VideoPlayer video;
        public string baseUrl = "https://genjutsuai.vercel.app";
        public bool Power { get; private set; } = true;

        Texture2D gameTex;
        Color[] gamePx;
        float pulse;
        bool game;
        string playingUrl = "";
        string appliedKey = "";
        HouseMediaState media;

        public static TvSet Ensure(Transform home)
        {
            var stand = home.Find("TvStand");
            if (stand == null)
                return null;
            var existing = stand.GetComponent<TvSet>();
            if (existing != null)
                return existing;
            var tv = stand.gameObject.AddComponent<TvSet>();
            var screenT = stand.Find("Screen");
            if (screenT != null)
                tv.screen = screenT.GetComponent<Renderer>();
            tv.speaker = stand.gameObject.AddComponent<AudioSource>();
            tv.speaker.spatialBlend = 1f;
            tv.speaker.minDistance = 0.4f;
            tv.speaker.maxDistance = 6f;
            tv.speaker.rolloffMode = AudioRolloffMode.Linear;
            tv.speaker.playOnAwake = false;
            tv.video = stand.gameObject.AddComponent<VideoPlayer>();
            tv.video.playOnAwake = false;
            tv.video.renderMode = VideoRenderMode.MaterialOverride;
            if (tv.screen != null)
                tv.video.targetMaterialRenderer = tv.screen;
            tv.video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            tv.video.SetTargetAudioSource(0, tv.speaker);
            return tv;
        }

        public void ApplyMedia(HouseMediaState next)
        {
            var key = (next.Url ?? "") + "|" + next.Playing + "|" + next.Type;
            if (key == appliedKey)
                return;
            appliedKey = key;
            media = next;
            if (!string.IsNullOrEmpty(next.Url) && next.Playing)
            {
                Power = true;
                PlayUrl(next.Url, next.Type == "video");
            }
            else if (!next.Playing)
                StopMedia();
        }

        public void SetPower(bool on, bool report)
        {
            Power = on;
            if (!on)
                StopMedia();
            else if (!string.IsNullOrEmpty(media.Url))
                PlayUrl(media.Url, media.Type == "video");
            if (screen != null)
            {
                var mat = screen.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", on ? new Color(0.2f, 0.35f, 0.55f) : Color.black);
            }

            if (report)
                HouseDeviceClient.Post(this, baseUrl, "tv", on);
        }

        public void TogglePower()
        {
            SetPower(!Power, true);
        }

        public void Next()
        {
            game = true;
            PlayMiniGame();
        }

        public void PlayMiniGame()
        {
            game = true;
            StopMedia();
            Power = true;
            if (gameTex == null)
            {
                gameTex = new Texture2D(64, 36, TextureFormat.RGB24, false);
                gameTex.filterMode = FilterMode.Point;
                gamePx = new Color[64 * 36];
            }

            if (screen != null)
                screen.material.mainTexture = gameTex;
        }

        void PlayUrl(string url, bool isVideo)
        {
            if (playingUrl == url && ((isVideo && video.isPlaying) || (!isVideo && speaker.isPlaying)))
                return;
            playingUrl = url;
            game = false;
            if (isVideo)
            {
                video.url = url;
                video.Play();
            }
            else
                StartCoroutine(StreamAudio(url));
        }

        System.Collections.IEnumerator StreamAudio(string url)
        {
            using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    yield break;
                var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
                speaker.clip = clip;
                speaker.loop = true;
                speaker.spatialBlend = 1f;
                speaker.Play();
            }
        }

        void StopMedia()
        {
            playingUrl = "";
            if (video != null)
                video.Stop();
            if (speaker != null)
                speaker.Stop();
        }

        void Update()
        {
            if (!game || gameTex == null)
                return;
            pulse += Time.deltaTime;
            var px = 8 + (int)(Mathf.PingPong(pulse * 18f, 48));
            var py = 10 + (int)(Mathf.Sin(pulse * 3f) * 8f);
            for (var i = 0; i < gamePx.Length; i++)
                gamePx[i] = new Color(0.04f, 0.08f, 0.12f);
            Stamp(px, py, Color.cyan);
            Stamp(px + 2, py, Color.yellow);
            Stamp(40, 6, Color.red);
            gameTex.SetPixels(gamePx);
            gameTex.Apply(false);
        }

        void Stamp(int x, int y, Color c)
        {
            for (var oy = 0; oy < 4; oy++)
            for (var ox = 0; ox < 4; ox++)
            {
                var xx = Mathf.Clamp(x + ox, 0, 63);
                var yy = Mathf.Clamp(y + oy, 0, 35);
                gamePx[yy * 64 + xx] = c;
            }
        }
    }
}
