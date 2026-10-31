using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Socket that only accepts its matching data cube.
    /// </summary>
    public class CyberSocket : XRSocketInteractor
    {
        public Transform accepts;

        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return Matches(interactable.transform) && base.CanHover(interactable);
        }

        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return Matches(interactable.transform) && base.CanSelect(interactable);
        }

        bool Matches(Transform candidate)
        {
            return accepts == null || candidate == accepts;
        }
    }
}
