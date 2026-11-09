using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Room-scale only: no snap/smooth turn, no thumbstick move, no teleport.
    /// Grab, poke/ray and B push-to-talk stay enabled.
    /// </summary>
    public static class LocomotionGuard
    {
        static readonly string[] TypeNames =
        {
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.SnapTurnProvider",
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning.ContinuousTurnProvider",
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider",
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.GrabMoveProvider",
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider",
            "UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController",
            "UnityEngine.XR.Interaction.Toolkit.ContinuousMoveProviderBase",
            "UnityEngine.XR.Interaction.Toolkit.SnapTurnProviderBase",
            "UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousMoveProvider",
            "UnityEngine.XR.Interaction.Toolkit.ActionBasedSnapTurnProvider",
            "UnityEngine.XR.Interaction.Toolkit.ActionBasedContinuousTurnProvider"
        };

        public static void Apply()
        {
            var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var b = 0; b < behaviours.Length; b++)
            {
                var mb = behaviours[b];
                if (mb == null)
                    continue;
                var full = mb.GetType().FullName ?? "";
                var name = mb.GetType().Name;
                var hit = false;
                for (var i = 0; i < TypeNames.Length; i++)
                {
                    if (full == TypeNames[i] || name == TypeNames[i].Substring(TypeNames[i].LastIndexOf('.') + 1))
                    {
                        hit = true;
                        break;
                    }
                }

                if (!hit && (name.IndexOf("SnapTurn", System.StringComparison.Ordinal) >= 0
                             || name.IndexOf("ContinuousTurn", System.StringComparison.Ordinal) >= 0
                             || name.IndexOf("ContinuousMove", System.StringComparison.Ordinal) >= 0
                             || name.IndexOf("GrabMove", System.StringComparison.Ordinal) >= 0
                             || name.IndexOf("TeleportationProvider", System.StringComparison.Ordinal) >= 0))
                    hit = true;
                if (hit)
                    mb.enabled = false;
            }

            var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var n = transforms[i].name;
                if (n.IndexOf("Teleport", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && n.IndexOf("Interactor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    transforms[i].gameObject.SetActive(false);
                if (n == "Teleport Area Setup" || n == "Teleportation" || n == "Locomotion")
                {
                    var comps = transforms[i].GetComponents<Behaviour>();
                    for (var b = 0; b < comps.Length; b++)
                    {
                        if (comps[b] is Transform)
                            continue;
                        var typeName = comps[b].GetType().Name;
                        if (typeName.IndexOf("Turn", System.StringComparison.Ordinal) >= 0
                            || typeName.IndexOf("Move", System.StringComparison.Ordinal) >= 0
                            || typeName.IndexOf("Teleport", System.StringComparison.Ordinal) >= 0)
                            comps[b].enabled = false;
                    }
                }
            }
        }

    }
}
