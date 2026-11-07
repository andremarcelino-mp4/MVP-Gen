using GenJutsu.GenCore;
using GenJutsu.ProfileDemo;
using UnityEngine;

namespace GenJutsu.HouseState
{
    /// <summary>
    /// Smooth light and ambience changes. switch_dojo is a visual morph only and does not write GuestSession.
    /// </summary>
    public sealed class HouseStateApplier : MonoBehaviour
    {
        public ProfileDemoDirector director;
        public ProceduralMusic music;
        public AmbienceBed ambience;
        public float blendSeconds = 0.7f;

        bool hasPlan;
        HouseApplyPlan plan;
        string switchedDojo;
        string appliedPreset;
        float appliedVolume = -1f;
        Color ambientFrom;
        bool ambientCaptured;
        readonly System.Collections.Generic.Dictionary<Renderer, Color> surfaceBase = new System.Collections.Generic.Dictionary<Renderer, Color>();

        public void ApplyPlan(HouseApplyPlan next)
        {
            plan = next;
            hasPlan = true;
            switchedDojo = null;
            appliedPreset = null;
            appliedVolume = -1f;
        }

        public void Release()
        {
            hasPlan = false;
            switchedDojo = null;
            appliedPreset = null;
            appliedVolume = -1f;
        }

        void LateUpdate()
        {
            if (!hasPlan || director == null)
                return;
            if (director.Current == ProfileDemoDirector.State.WhiteSpace ||
                director.Current == ProfileDemoDirector.State.Dissolving)
                return;

            TrySwitch();
            ApplyAmbience();
            if (!plan.HasLights)
                return;
            var ms = plan.MainTransitionMs > 0 ? plan.MainTransitionMs : plan.LanternTransitionMs;
            var seconds = ms > 0 ? ms / 1000f : blendSeconds;
            var k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, seconds * 0.45f));
            if (plan.HasMain)
            {
                TintMain(k);
                TintLargeSurfaces(k);
            }
            if (plan.HasLanterns)
                TintLanterns(k);
        }

        void TintMain(float k)
        {
            var intensity = HouseStateLogic.DriveIntensity(plan.MainIntensity, false);
            if (director.mainLight != null)
            {
                director.mainLight.color = Color.Lerp(director.mainLight.color, plan.MainColor, k);
                director.mainLight.intensity = Mathf.Lerp(director.mainLight.intensity, intensity, k);
            }

            if (LightmapSettings.lightmaps != null && LightmapSettings.lightmaps.Length > 0)
            {
                RenderSettings.ambientIntensity = Mathf.Lerp(
                    RenderSettings.ambientIntensity,
                    0.55f + plan.MainIntensity * 0.45f,
                    k);
            }
            else
            {
                if (!ambientCaptured)
                {
                    ambientFrom = RenderSettings.ambientLight;
                    ambientCaptured = true;
                }

                var targetAmbient = Color.Lerp(new Color(0.04f, 0.04f, 0.04f), plan.MainColor, 0.92f);
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, k);
            }

            if (RenderSettings.fog)
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, plan.MainColor, k);
        }

        void TrySwitch()
        {
            if (!plan.SwitchDojo || director.Current != ProfileDemoDirector.State.InHouse)
                return;
            if (!Profiles.TryParse(plan.Dojo, false, out var id))
                return;
            if (director.Active == id)
            {
                switchedDojo = plan.Dojo;
                return;
            }

            if (switchedDojo == plan.Dojo)
                return;
            if (director.SwitchVisualDojo(id))
                switchedDojo = plan.Dojo;
        }

        void ApplyAmbience()
        {
            if (!plan.HasAmbience || music == null)
                return;
            if (appliedPreset == (plan.Preset ?? "") && Mathf.Abs(appliedVolume - plan.Volume) < 0.001f)
                return;
            appliedPreset = plan.Preset ?? "";
            appliedVolume = plan.Volume;
            var preset = appliedPreset.Trim().ToLowerInvariant();
            if (ambience == null)
                ambience = GetComponent<AmbienceBed>() ?? gameObject.AddComponent<AmbienceBed>();
            var fade = plan.AmbienceTransitionMs > 0 ? plan.AmbienceTransitionMs / 1000f : 0.8f;
            if (music != null && preset != "off" && preset != "silence" && preset != "mute")
                music.SetLevel(0f);
            ambience.Play(preset, plan.Volume, fade);
        }

        void TintLargeSurfaces(float k)
        {
            var house = director.VisualHouse;
            if (house == null)
                return;
            var renderers = house.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;
                var name = renderer.name;
                if (name != "PaperL" && name != "PaperR" && name != "Tatami" && name != "TatamiField")
                    continue;
                var mat = renderer.material;
                if (!mat.HasProperty("_BaseColor"))
                    continue;
                if (!surfaceBase.TryGetValue(renderer, out var original))
                {
                    original = mat.GetColor("_BaseColor");
                    surfaceBase[renderer] = original;
                }

                var target = Color.Lerp(original, plan.MainColor, 0.62f);
                mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), target, k));
            }
        }

        void TintLanterns(float k)
        {
            var house = director.VisualHouse;
            if (house == null)
                return;
            var lights = house.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (light == null || light.type != LightType.Point)
                    continue;
                light.color = Color.Lerp(light.color, plan.LanternColor, k);
                light.intensity = Mathf.Lerp(light.intensity, HouseStateLogic.DriveIntensity(plan.LanternIntensity, true), k);
            }

            var emission = plan.LanternColor * Mathf.Max(0.35f, plan.LanternIntensity * 3.2f);
            var renderers = house.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer.name != "Shade")
                    continue;
                var mat = renderer.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.Lerp(mat.GetColor("_EmissionColor"), emission, k));
                }

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), plan.LanternColor, k));
            }
        }
    }
}
