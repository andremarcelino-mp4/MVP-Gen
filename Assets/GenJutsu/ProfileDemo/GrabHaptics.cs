using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>Controller impulse on hover and grab. Does not attach to UI or XR hands.</summary>
    [RequireComponent(typeof(XRBaseInteractable))]
    public sealed class GrabHaptics : MonoBehaviour
    {
        public float hoverAmp = 0.12f;
        public float grabAmp = 0.35f;
        public float duration = 0.05f;

        XRBaseInteractable ix;

        void Awake()
        {
            ix = GetComponent<XRBaseInteractable>();
            ix.hoverEntered.AddListener(OnHover);
            ix.selectEntered.AddListener(OnGrab);
        }

        void OnDestroy()
        {
            if (ix == null)
                return;
            ix.hoverEntered.RemoveListener(OnHover);
            ix.selectEntered.RemoveListener(OnGrab);
        }

        void OnHover(HoverEnterEventArgs args) => Pulse(hoverAmp);

        void OnGrab(SelectEnterEventArgs args) => Pulse(grabAmp);

        void Pulse(float amp)
        {
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Controller, devices);
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i].TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    devices[i].SendHapticImpulse(0u, amp, duration);
            }
        }
    }
}
