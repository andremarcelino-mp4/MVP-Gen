using System.Collections;
using System.Collections.Generic;
using GenJutsu.TableProps;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Grabbable instant camera. Trigger shoots right away; with hand tracking a 3 s timer shoots after each grab.
    /// Each photo is saved to the session and pops out as a physical print.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class InstantCamera : MonoBehaviour
    {
        public ProfileId profile;
        public Camera lens;
        public Transform photoSlot;
        public Material photoTemplate;
        public Material frameTemplate;
        public float timerSeconds = 3f;
        public int maxPrints = 5;
        public int photoWidth = 640;
        public int photoHeight = 480;

        XRGrabInteractable grab;
        Coroutine timer;
        float lastShot = -10f;
        readonly Queue<(GameObject go, Material material, Texture2D texture)> prints = new Queue<(GameObject, Material, Texture2D)>();

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (lens != null)
                lens.enabled = false;
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
            grab.activated.AddListener(OnActivated);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            grab.activated.RemoveListener(OnActivated);
            timer = null;
            while (prints.Count > 0)
                DestroyPrint(prints.Dequeue());
        }

        static void DestroyPrint((GameObject go, Material material, Texture2D texture) print)
        {
            if (print.go != null)
                Destroy(print.go);
            if (print.material != null)
                Destroy(print.material);
            if (print.texture != null)
                Destroy(print.texture);
        }

        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (timer != null)
                StopCoroutine(timer);
            timer = StartCoroutine(TimerRoutine());
        }

        void OnReleased(SelectExitEventArgs args)
        {
            if (timer != null)
            {
                StopCoroutine(timer);
                timer = null;
            }
        }

        void OnActivated(ActivateEventArgs args)
        {
            if (timer != null)
            {
                StopCoroutine(timer);
                timer = null;
            }

            TakePhoto();
        }

        IEnumerator TimerRoutine()
        {
            for (var i = 0; i < Mathf.RoundToInt(timerSeconds); i++)
            {
                DemoSfx.Play(DemoSfx.Kind.Note, transform.position, 0.35f, 2f);
                yield return new WaitForSeconds(1f);
            }

            timer = null;
            TakePhoto();
        }

        public void TakePhoto()
        {
            if (lens == null || Time.time - lastShot < 0.5f)
                return;
            lastShot = Time.time;

            var texture = DemoCapture.Render(lens, photoWidth, photoHeight);
            var recorder = DemoSessionRecorder.Instance;
            if (recorder != null)
                recorder.SaveCapture(profile, "foto", texture);
            DemoSfx.Play(DemoSfx.Kind.Shutter, transform.position, 1f, 1f);
            ScorePopup.Spawn(transform.position + Vector3.up * 0.12f, "Foto salva", Profiles.Get(profile).Accent);
            SpawnPrint(texture);
        }

        void SpawnPrint(Texture2D texture)
        {
            var slot = photoSlot != null ? photoSlot : transform;
            var print = GameObject.CreatePrimitive(PrimitiveType.Cube);
            print.name = "Polaroid";
            print.transform.SetPositionAndRotation(slot.position, slot.rotation);
            print.transform.localScale = new Vector3(0.1f, 0.12f, 0.004f);
            if (frameTemplate != null)
                print.GetComponent<Renderer>().sharedMaterial = frameTemplate;

            var image = GameObject.CreatePrimitive(PrimitiveType.Quad);
            image.name = "Image";
            DestroyImmediate(image.GetComponent<Collider>());
            image.transform.SetParent(print.transform, false);
            image.transform.localPosition = new Vector3(0f, 0.08f, -0.6f);
            image.transform.localRotation = Quaternion.identity;
            image.transform.localScale = new Vector3(0.86f, 0.62f, 1f);
            var material = photoTemplate != null ? new Material(photoTemplate) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            image.GetComponent<Renderer>().sharedMaterial = material;

            var body = print.AddComponent<Rigidbody>();
            body.mass = 0.02f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var printCollider = print.GetComponent<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(printCollider, c);
            body.linearVelocity = slot.up * 0.6f + slot.forward * 0.2f;

            var printGrab = print.AddComponent<XRGrabInteractable>();
            printGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            printGrab.useDynamicAttach = true;

            prints.Enqueue((print, material, texture));
            while (prints.Count > maxPrints)
                DestroyPrint(prints.Dequeue());
        }
    }
}
