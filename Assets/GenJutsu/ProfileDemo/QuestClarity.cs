using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Quest 3S clarity: MSAA 4x and eye texture scale 1.15. Keeps 72–90 fps as the budget.
    /// </summary>
    public sealed class QuestClarity : MonoBehaviour
    {
        public const float EyeScale = 1.15f;
        public const int Msaa = 4;

        public static void Ensure(ProfileDemoDirector director)
        {
            if (director == null)
                return;
            var rig = director.GetComponent<QuestClarity>() ?? director.gameObject.AddComponent<QuestClarity>();
            rig.Apply();
        }

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            QualitySettings.antiAliasing = Msaa;
            XRSettings.eyeTextureResolutionScale = EyeScale;
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (asset != null)
            {
                asset.msaaSampleCount = Msaa;
                asset.renderScale = EyeScale;
            }

            Application.targetFrameRate = 90;
        }
    }
}
