using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.HouseState
{
    /// <summary>
    /// Named lights and emissive meshes keyed to site house_state device ids.
    /// </summary>
    public sealed class HouseDeviceRig : MonoBehaviour
    {
        public Light sala;
        public Light quarto;
        public Light abajur;
        public Light fita;
        public Light tv;
        public Renderer[] salaGlow;
        public Renderer[] quartoGlow;
        public Renderer[] abajurGlow;
        public Renderer[] fitaGlow;
        public Renderer[] tvGlow;

        public void Apply(HouseApplyPlan plan, float k)
        {
            if (plan.Devices == null)
                return;
            for (var i = 0; i < plan.Devices.Length; i++)
                Drive(plan.Devices[i], k);
        }

        public void Drive(HouseDeviceState device, float k)
        {
            if (string.IsNullOrEmpty(device.Id))
                return;
            switch (device.Id)
            {
                case "sala_luz":
                    Push(sala, salaGlow, device, k, true);
                    break;
                case "quarto_luz":
                    Push(quarto, quartoGlow, device, k, true);
                    break;
                case "abajur":
                    Push(abajur, abajurGlow, device, k, true);
                    break;
                case "fita_led":
                    Push(fita, fitaGlow, device, k, true);
                    break;
                case "tv":
                    Push(tv, tvGlow, device, k, false);
                    break;
                case "ambiente":
                    if (device.On)
                    {
                        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, device.Color * (0.25f + device.Intensity * 0.55f), k);
                        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, 0.4f + device.Intensity * 0.7f, k);
                    }
                    break;
            }
        }

        static void Push(Light light, Renderer[] glow, HouseDeviceState device, float k, bool point)
        {
            var on = device.On && device.Intensity > 0.02f;
            var color = device.Color;
            var intensity = HouseStateLogic.DriveIntensity(device.Intensity, point);
            if (light != null)
            {
                light.enabled = on;
                if (on)
                {
                    light.color = Color.Lerp(light.color, color, k);
                    light.intensity = Mathf.Lerp(light.intensity, intensity, k);
                }
            }

            var emission = on ? color * Mathf.Max(0.4f, device.Intensity * 3.4f) : Color.black;
            if (glow == null)
                return;
            for (var i = 0; i < glow.Length; i++)
            {
                var renderer = glow[i];
                if (renderer == null)
                    continue;
                var mat = renderer.material;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.Lerp(mat.GetColor("_EmissionColor"), emission, k));
                }

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", Color.Lerp(mat.GetColor("_BaseColor"), on ? color : Color.black, k));
            }
        }
    }
}
