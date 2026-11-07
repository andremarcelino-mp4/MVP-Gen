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
        public float blendSeconds = 0.7f;

        bool hasPlan;
        HouseApplyPlan plan;
        string switchedDojo;
        string appliedPreset;
        float appliedVolume = -1f;

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
            var k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.05f, blendSeconds));
            if (plan.HasMain && director.mainLight != null)
            {
                director.mainLight.color = Color.Lerp(director.mainLight.color, plan.MainColor, k);
                director.mainLight.intensity = Mathf.Lerp(director.mainLight.intensity, plan.MainIntensity, k);
            }

            if (plan.HasLanterns)
                TintLanterns(k);
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
            if (preset == "off" || preset == "silence" || preset == "mute")
            {
                music.Stop();
                return;
            }

            var level = Mathf.Clamp01(plan.Volume);
            if (!music.IsPlaying && director.Current == ProfileDemoDirector.State.InHouse)
            {
                music.musicVolume = level;
                music.Play(director.Active);
                return;
            }

            music.SetLevel(level);
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
                light.intensity = Mathf.Lerp(light.intensity, plan.LanternIntensity, k);
            }

            var emission = plan.LanternColor * Mathf.Max(0.2f, plan.LanternIntensity);
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
