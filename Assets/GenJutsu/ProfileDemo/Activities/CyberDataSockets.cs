using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Plug every data cube into the socket of its colour as fast as possible. Score: time (lower is better).
    /// </summary>
    public class CyberDataSockets : ProfileActivity
    {
        public CyberSocket[] sockets;
        public ResettablePose[] cubes;
        public TMPro.TMP_Text display;

        float startTime = -1f;
        bool complete;
        float best = float.MaxValue;

        void OnEnable()
        {
            foreach (var cube in cubes)
            {
                var grab = cube.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.selectEntered.AddListener(OnCubeGrabbed);
            }

            foreach (var socket in sockets)
                socket.selectEntered.AddListener(OnSocketFilled);
        }

        void OnDisable()
        {
            foreach (var cube in cubes)
            {
                var grab = cube.GetComponent<XRGrabInteractable>();
                if (grab != null)
                    grab.selectEntered.RemoveListener(OnCubeGrabbed);
            }

            foreach (var socket in sockets)
                socket.selectEntered.RemoveListener(OnSocketFilled);
            StopAllCoroutines();
        }

        public override void ResetActivity()
        {
            StopAllCoroutines();
            foreach (var socket in sockets)
                socket.socketActive = true;
            startTime = -1f;
            complete = false;
            best = float.MaxValue;
            SetDisplay("CONECTE OS DADOS");
        }

        void OnCubeGrabbed(SelectEnterEventArgs args)
        {
            if (complete || startTime >= 0f)
                return;
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor)
                return;
            startTime = Time.time;
        }

        void OnSocketFilled(SelectEnterEventArgs args)
        {
            if (complete || startTime < 0f)
                return;
            DemoSfx.Play(DemoSfx.Kind.Pop, args.interactorObject.transform.position, 0.6f, 1.3f);
            foreach (var socket in sockets)
            {
                if (!socket.hasSelection)
                    return;
            }

            complete = true;
            var seconds = Mathf.Round((Time.time - startTime) * 10f) / 10f;
            best = Mathf.Min(best, seconds);
            Report(seconds, "s", false, "recorde " + best.ToString("0.0") + " s");
            Popup(transform.position + Vector3.up * 0.5f, "Sistema online " + seconds.ToString("0.0") + " s");
            DemoSfx.Play(DemoSfx.Kind.Success, transform.position, 0.8f);
            SetDisplay("ONLINE " + seconds.ToString("0.0") + " s");
            StartCoroutine(Rearm());
        }

        IEnumerator Rearm()
        {
            yield return new WaitForSeconds(3f);
            foreach (var socket in sockets)
                socket.socketActive = false;
            yield return null;
            foreach (var cube in cubes)
                cube.ReturnHome();
            yield return new WaitForSeconds(1f);
            foreach (var socket in sockets)
                socket.socketActive = true;
            startTime = -1f;
            complete = false;
            SetDisplay("CONECTE OS DADOS");
        }

        void Update()
        {
            if (complete || startTime < 0f)
                return;
            SetDisplay((Time.time - startTime).ToString("00.0") + " s");
        }

        void SetDisplay(string text)
        {
            if (display != null)
                display.text = text;
        }
    }
}
