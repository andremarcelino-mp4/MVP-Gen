using GenJutsu.GenCore;
using GenJutsu.HouseState;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Wall split AC: display, swinging louver, airflow, poke remote. Device id ar_condicionado.</summary>
    public sealed class AirConUnit : MonoBehaviour
    {
        public string baseUrl = "https://genjutsuai.vercel.app";
        public bool Power { get; private set; }
        public int Temp { get; private set; } = 24;
        public string Mode { get; private set; } = "frio";
        public string Fan { get; private set; } = "auto";

        Transform louver;
        TextMesh display;
        ParticleSystem air;
        AudioSource hum;
        string appliedKey = "";
        float swing;

        public static AirConUnit Ensure(Transform home)
        {
            if (home == null)
                return null;
            var existing = home.GetComponentInChildren<AirConUnit>(true);
            if (existing != null)
                return existing;
            var root = new GameObject("AirCon");
            var stand = home.Find("TvStand");
            root.transform.SetParent(stand != null ? stand : home, false);
            root.transform.localPosition = stand != null ? new Vector3(0f, 1.18f, 0.02f) : new Vector3(-0.15f, 1.55f, -1.05f);
            root.transform.localRotation = Quaternion.identity;
            var unit = root.AddComponent<AirConUnit>();
            unit.Build();
            return unit;
        }

        public void Apply(HouseDeviceState device)
        {
            var key = device.On + "|" + device.Temp + "|" + (device.Mode ?? "") + "|" + (device.Fan ?? "");
            if (key == appliedKey)
                return;
            appliedKey = key;
            Power = device.On;
            if (device.Temp >= 16)
                Temp = Mathf.Clamp(device.Temp, 16, 30);
            Mode = HouseStateLogic.NormalizeMode(device.Mode);
            if (!string.IsNullOrEmpty(device.Fan))
                Fan = device.Fan;
            Refresh();
        }

        public void TogglePower()
        {
            Set(!Power, Temp, Mode, Fan, true);
        }

        public void NudgeTemp(int delta)
        {
            Set(true, Mathf.Clamp(Temp + delta, 16, 30), Mode, Fan, true);
        }

        public void CycleMode()
        {
            var next = Mode == "frio" ? "ventilar" : Mode == "ventilar" ? "seco" : "frio";
            Set(true, Temp, next, Fan, true);
        }

        public void Set(bool on, int temp, string mode, string fan, bool report)
        {
            Power = on;
            Temp = Mathf.Clamp(temp, 16, 30);
            Mode = HouseStateLogic.NormalizeMode(mode);
            Fan = string.IsNullOrEmpty(fan) ? "auto" : fan;
            appliedKey = Power + "|" + Temp + "|" + Mode + "|" + Fan;
            Refresh();
            if (report)
                HouseDeviceClient.PostAc(this, baseUrl, Power, Temp, Mode, Fan);
        }

        void Build()
        {
            var shell = Mat(new Color(0.92f, 0.93f, 0.94f), 0.25f);
            var dark = Mat(new Color(0.12f, 0.14f, 0.16f), 0.2f);
            Prim(transform, "Body", Vector3.zero, new Vector3(0.92f, 0.28f, 0.22f), shell, false);
            Prim(transform, "Vent", new Vector3(0f, -0.08f, 0.08f), new Vector3(0.78f, 0.04f, 0.04f), dark, false);
            louver = Prim(transform, "Louver", new Vector3(0f, -0.1f, 0.1f), new Vector3(0.74f, 0.012f, 0.08f), dark, false).transform;

            var screen = Prim(transform, "Display", new Vector3(0.32f, 0.04f, 0.115f), new Vector3(0.18f, 0.08f, 0.01f), Glow(new Color(0.15f, 0.85f, 0.45f)), false);
            var label = new GameObject("TempText");
            label.transform.SetParent(screen.transform, false);
            label.transform.localPosition = new Vector3(0f, 0f, -0.6f);
            label.transform.localScale = Vector3.one * 0.08f;
            display = label.AddComponent<TextMesh>();
            display.fontSize = 48;
            display.anchor = TextAnchor.MiddleCenter;
            display.alignment = TextAlignment.Center;
            display.color = new Color(0.3f, 1f, 0.55f);
            display.characterSize = 0.12f;

            var psGo = new GameObject("Airflow");
            psGo.transform.SetParent(transform, false);
            psGo.transform.localPosition = new Vector3(0f, -0.14f, 0.12f);
            psGo.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            air = psGo.AddComponent<ParticleSystem>();
            var main = air.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.35f;
            main.startSize = 0.04f;
            main.startColor = new Color(0.7f, 0.9f, 1f, 0.35f);
            main.maxParticles = 80;
            var em = air.emission;
            em.rateOverTime = 0f;
            var sh = air.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(0.7f, 0.04f, 0.04f);
            var renderer = air.GetComponent<ParticleSystemRenderer>();
            var pshader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (pshader != null)
            {
                var pmat = new Material(pshader);
                pmat.color = new Color(0.75f, 0.92f, 1f, 0.4f);
                renderer.material = pmat;
            }

            hum = gameObject.AddComponent<AudioSource>();
            hum.playOnAwake = false;
            hum.loop = true;
            hum.spatialBlend = 1f;
            hum.minDistance = 0.4f;
            hum.maxDistance = 6f;
            hum.clip = HumClip();
            hum.volume = 0f;

            Remote();
            Refresh();
        }

        void Remote()
        {
            var panel = new GameObject("AcRemote");
            var home = transform.parent != null && transform.parent.name == "TvStand"
                ? transform.parent.parent
                : transform.parent;
            panel.transform.SetParent(home != null ? home : transform.parent, false);
            panel.transform.localPosition = new Vector3(1.55f, 1.15f, -2.18f);
            panel.transform.localRotation = Quaternion.identity;
            Prim(panel.transform, "Plate", Vector3.zero, new Vector3(0.16f, 0.28f, 0.03f), Mat(new Color(0.18f, 0.18f, 0.2f), 0.3f), false);
            Btn(panel.transform, "Power", new Vector3(0f, 0.08f, 0.02f), new Color(0.85f, 0.2f, 0.2f), TogglePower);
            Btn(panel.transform, "TempUp", new Vector3(-0.035f, 0.02f, 0.02f), new Color(0.25f, 0.55f, 0.9f), () => NudgeTemp(1));
            Btn(panel.transform, "TempDn", new Vector3(0.035f, 0.02f, 0.02f), new Color(0.2f, 0.45f, 0.8f), () => NudgeTemp(-1));
            Btn(panel.transform, "Mode", new Vector3(0f, -0.06f, 0.02f), new Color(0.3f, 0.75f, 0.45f), CycleMode);
        }

        void Btn(Transform parent, string name, Vector3 pos, Color color, System.Action act)
        {
            var go = Prim(parent, name, pos, new Vector3(0.05f, 0.04f, 0.02f), Mat(color, 0.2f), true);
            var ix = go.AddComponent<XRSimpleInteractable>();
            var filter = go.AddComponent<XRPokeFilter>();
            filter.pokeConfiguration = new PokeThresholdDatumProperty(new PokeThresholdData
            {
                pokeDirection = PokeAxis.None,
                enablePokeAngleThreshold = false,
                interactionDepthOffset = 0f
            });
            ix.selectEntered.AddListener(_ => act());
        }

        void Refresh()
        {
            if (display != null)
                display.text = Power ? Temp + "° " + ModeShort() : "OFF";
            if (hum != null)
            {
                if (Power && !hum.isPlaying)
                    hum.Play();
                if (!Power && hum.isPlaying)
                    hum.Stop();
                hum.volume = Power ? 0.08f + HouseStateLogic.FanScale(Fan) * 0.08f : 0f;
                hum.pitch = Mode == "ventilar" ? 1.05f : 0.92f;
            }

            if (air == null)
                return;
            var em = air.emission;
            em.rateOverTime = Power ? 12f * HouseStateLogic.FanScale(Fan) : 0f;
            var main = air.main;
            main.startSpeed = Power ? 0.25f + HouseStateLogic.FanScale(Fan) * 0.35f : 0f;
            main.startColor = Mode == "seco"
                ? new Color(0.95f, 0.92f, 0.75f, 0.3f)
                : new Color(0.7f, 0.9f, 1f, 0.35f);
        }

        string ModeShort()
        {
            if (Mode == "ventilar")
                return "VEN";
            if (Mode == "seco")
                return "DRY";
            return "FRIO";
        }

        void Update()
        {
            if (louver == null)
                return;
            if (Power)
            {
                swing += Time.deltaTime * (0.7f + HouseStateLogic.FanScale(Fan));
                louver.localRotation = Quaternion.Euler(Mathf.Sin(swing) * 22f, 0f, 0f);
            }
            else
                louver.localRotation = Quaternion.Euler(8f, 0f, 0f);
        }

        static GameObject Prim(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                    Object.Destroy(col);
            }

            return go;
        }

        static Material Mat(Color color, float smooth)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smooth);
            return mat;
        }

        static Material Glow(Color color)
        {
            var mat = Mat(color, 0.1f);
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", color * 1.4f);
            return mat;
        }

        static AudioClip HumClip()
        {
            const int rate = 22050;
            var n = rate * 2;
            var data = new float[n];
            var phase = 0.0;
            for (var i = 0; i < n; i++)
            {
                phase += 0.012;
                var n1 = Mathf.Sin((float)phase * 18f) * 0.12f;
                var n2 = (Mathf.PerlinNoise(i * 0.004f, 0.2f) - 0.5f) * 0.22f;
                data[i] = (n1 + n2) * 0.35f;
            }

            var clip = AudioClip.Create("AcHum", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
