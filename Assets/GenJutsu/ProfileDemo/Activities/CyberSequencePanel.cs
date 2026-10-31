using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Memory panel: watch the nodes light up, then repeat the sequence. Score: longest sequence repeated.
    /// </summary>
    public class CyberSequencePanel : ProfileActivity
    {
        public XRSimpleInteractable[] nodes;
        public Renderer[] nodeRenderers;
        public XRSimpleInteractable startButton;
        public TMPro.TMP_Text display;
        public Color idleColor = new Color(0.05f, 0.2f, 0.28f);
        public Color litColor = new Color(0.2f, 0.95f, 1f);
        public Color errorColor = new Color(1f, 0.15f, 0.2f);
        public float[] semitones = { 0f, 3f, 5f, 7f, 10f, 12f };

        enum Phase { Idle, Showing, Input }

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        readonly List<int> sequence = new List<int>();
        readonly List<System.Action<SelectEnterEventArgs>> handlers = new List<System.Action<SelectEnterEventArgs>>();
        MaterialPropertyBlock block;
        Phase phase;
        int inputIndex;
        int best;
        Coroutine routine;

        void Awake()
        {
            block = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            handlers.Clear();
            for (var i = 0; i < nodes.Length; i++)
            {
                var index = i;
                System.Action<SelectEnterEventArgs> handler = _ => OnNode(index);
                handlers.Add(handler);
                nodes[i].selectEntered.AddListener(handler.Invoke);
            }

            if (startButton != null)
                startButton.selectEntered.AddListener(OnStart);
        }

        void OnDisable()
        {
            for (var i = 0; i < nodes.Length; i++)
                nodes[i].selectEntered.RemoveAllListeners();
            if (startButton != null)
                startButton.selectEntered.RemoveListener(OnStart);
            routine = null;
            phase = Phase.Idle;
        }

        public override void ResetActivity()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            sequence.Clear();
            phase = Phase.Idle;
            best = 0;
            for (var i = 0; i < nodeRenderers.Length; i++)
                SetColor(i, idleColor, 0f);
            SetDisplay("TOQUE INICIAR");
        }

        void OnStart(SelectEnterEventArgs args)
        {
            if (phase != Phase.Idle)
                return;
            sequence.Clear();
            AddStep();
            Run(ShowSequence());
        }

        void OnNode(int index)
        {
            if (phase == Phase.Idle)
            {
                OnStart(null);
                return;
            }

            if (phase != Phase.Input)
                return;

            StartCoroutine(Flash(index, litColor, 0.2f));
            DemoSfx.Play(DemoSfx.Kind.Note, nodes[index].transform.position, 0.7f, DemoSfx.Semitones(semitones[index % semitones.Length] - 5f));
            if (index != sequence[inputIndex])
            {
                Run(Fail());
                return;
            }

            inputIndex++;
            if (inputIndex < sequence.Count)
                return;

            if (sequence.Count > best)
            {
                best = sequence.Count;
                Report(best, "passos");
            }

            Popup(transform.position + Vector3.up * 0.45f, "Nível " + sequence.Count + "!");
            DemoSfx.Play(DemoSfx.Kind.Success, transform.position, 0.6f);
            AddStep();
            Run(ShowSequence(0.9f));
        }

        void Run(IEnumerator coroutine)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(coroutine);
        }

        void AddStep()
        {
            var next = Random.Range(0, nodes.Length);
            if (sequence.Count > 0 && next == sequence[sequence.Count - 1])
                next = (next + 1 + Random.Range(0, nodes.Length - 1)) % nodes.Length;
            sequence.Add(next);
        }

        IEnumerator ShowSequence(float delay = 0.6f)
        {
            phase = Phase.Showing;
            SetDisplay("OBSERVE  " + sequence.Count);
            yield return new WaitForSeconds(delay);
            var step = Mathf.Lerp(0.55f, 0.3f, Mathf.InverseLerp(1, 12, sequence.Count));
            foreach (var index in sequence)
            {
                DemoSfx.Play(DemoSfx.Kind.Note, nodes[index].transform.position, 0.6f, DemoSfx.Semitones(semitones[index % semitones.Length] - 5f));
                yield return Flash(index, litColor, step);
                yield return new WaitForSeconds(0.12f);
            }

            inputIndex = 0;
            phase = Phase.Input;
            SetDisplay("REPITA  " + sequence.Count);
            routine = null;
        }

        IEnumerator Fail()
        {
            phase = Phase.Showing;
            DemoSfx.Play(DemoSfx.Kind.Error, transform.position, 0.7f);
            SetDisplay("ERRO  recorde " + best);
            for (var i = 0; i < nodeRenderers.Length; i++)
                SetColor(i, errorColor, 1.5f);
            yield return new WaitForSeconds(0.7f);
            for (var i = 0; i < nodeRenderers.Length; i++)
                SetColor(i, idleColor, 0f);
            sequence.Clear();
            phase = Phase.Idle;
            SetDisplay("TOQUE INICIAR");
            routine = null;
        }

        IEnumerator Flash(int index, Color color, float seconds)
        {
            SetColor(index, color, 2.5f);
            yield return new WaitForSeconds(seconds);
            SetColor(index, idleColor, 0f);
        }

        void SetColor(int index, Color color, float emission)
        {
            if (index >= nodeRenderers.Length || nodeRenderers[index] == null)
                return;
            nodeRenderers[index].GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(EmissionId, color * emission);
            nodeRenderers[index].SetPropertyBlock(block);
        }

        void SetDisplay(string text)
        {
            if (display != null)
                display.text = text;
        }
    }
}
