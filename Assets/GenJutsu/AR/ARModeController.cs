using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

namespace GenJutsu.AR
{
    public class ARModeController : MonoBehaviour
    {
        [SerializeField] Camera targetCamera;
        [SerializeField] ARCameraManager cameraManager;
        [SerializeField] GhostSystem ghostSystem;
        [SerializeField] ARRoomController roomController;
        [SerializeField] GravityProvider gravityProvider;
        [SerializeField] GameObject[] vrOnlyObjects;
        [SerializeField] bool startInAR;

        public bool IsAR { get; private set; }

        bool prevPrimary;
        bool prevSecondary;
        CameraClearFlags originalClearFlags;
        Color originalBackgroundColor;

        void Start()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            originalClearFlags = CameraClearFlags.Skybox;
            originalBackgroundColor = Color.clear;
            if (targetCamera != null && !startInAR)
                targetCamera.clearFlags = originalClearFlags;
            if (ghostSystem != null)
                ghostSystem.SetMode(false);
            if (startInAR)
                SetAR();
        }

        void Update()
        {
            bool primary = ReadButton(true);
            bool secondary = ReadButton(false);

            if (primary && !prevPrimary)
            {
                if (IsAR)
                    SetVR();
                else
                    SetAR();
            }

            if (secondary && !prevSecondary && IsAR && roomController != null)
                roomController.Toggle();

            prevPrimary = primary;
            prevSecondary = secondary;
        }

        bool ReadButton(bool primary)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (primary && kb.mKey.wasPressedThisFrame)
                    return true;
                if (!primary && kb.fKey.wasPressedThisFrame)
                    return true;
            }

            bool value = false;
            value |= ReadDeviceButton(XRNode.LeftHand, primary);
            value |= ReadDeviceButton(XRNode.RightHand, primary);
            return value;
        }

        static bool ReadDeviceButton(XRNode node, bool primary)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid)
                return false;
            bool pressed;
            if (primary && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out pressed))
                return pressed;
            if (!primary && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out pressed))
                return pressed;
            return false;
        }

        public void SetAR()
        {
            if (IsAR)
                return;
            IsAR = true;
            if (gravityProvider != null)
                gravityProvider.enabled = false;
            if (targetCamera != null)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
                targetCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            if (cameraManager != null)
                cameraManager.enabled = true;
            SetVrObjectsActive(false);
            if (roomController != null)
                roomController.ForceHide();
            if (ghostSystem != null)
                ghostSystem.SetMode(true);
        }

        public void SetVR()
        {
            if (!IsAR)
                return;
            IsAR = false;
            if (gravityProvider != null)
                gravityProvider.enabled = true;
            if (cameraManager != null)
                cameraManager.enabled = false;
            if (targetCamera != null)
            {
                targetCamera.clearFlags = originalClearFlags;
                targetCamera.backgroundColor = originalBackgroundColor;
            }
            SetVrObjectsActive(true);
            if (roomController != null)
                roomController.ForceHide();
            if (ghostSystem != null)
                ghostSystem.SetMode(false);
        }

        void SetVrObjectsActive(bool active)
        {
            if (vrOnlyObjects == null)
                return;
            for (int i = 0; i < vrOnlyObjects.Length; i++)
            {
                if (vrOnlyObjects[i] != null)
                    vrOnlyObjects[i].SetActive(active);
            }
        }
    }
}
