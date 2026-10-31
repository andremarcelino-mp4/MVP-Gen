using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Floating shapes that each play a note when struck by a mallet or touched. Score: distinct notes, melody as detail.
    /// </summary>
    public class ShapeXylophone : ProfileActivity
    {
        public Transform[] keys;
        public float keyRadius = 0.09f;
        public float[] semitones = { -5f, -3f, 0f, 2f, 4f, 7f, 9f };
        public string[] noteNames = { "Sol", "Lá", "Dó", "Ré", "Mi", "Sol", "Lá" };
        public Transform[] malletHeads;
        public float minStrikeSpeed = 0.4f;

        readonly List<string> melody = new List<string>();
        readonly HashSet<int> distinct = new HashSet<int>();
        XRSimpleInteractable[] touchables;
        Vector3[] previousHeads;
        float[] cooldowns;
        Vector3[] baseScales;
        int notesPlayed;

        void Awake()
        {
            touchables = new XRSimpleInteractable[keys.Length];
            cooldowns = new float[keys.Length];
            previousHeads = new Vector3[malletHeads.Length];
            baseScales = new Vector3[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                touchables[i] = keys[i].GetComponent<XRSimpleInteractable>();
                baseScales[i] = keys[i].localScale;
            }
        }

        void OnDisable()
        {
            StopAllCoroutines();
            for (var i = 0; i < keys.Length; i++)
                keys[i].localScale = baseScales[i];
            foreach (var touchable in touchables)
            {
                if (touchable != null)
                    touchable.selectEntered.RemoveAllListeners();
            }
        }

        void OnEnable()
        {
            for (var i = 0; i < touchables.Length; i++)
            {
                if (touchables[i] == null)
                    continue;
                var index = i;
                touchables[i].selectEntered.AddListener(_ => Play(index, 0.8f));
            }

            for (var m = 0; m < malletHeads.Length; m++)
                previousHeads[m] = malletHeads[m].position;
        }

        public override void ResetActivity()
        {
            melody.Clear();
            distinct.Clear();
            notesPlayed = 0;
        }

        void Update()
        {
            for (var k = 0; k < cooldowns.Length; k++)
                cooldowns[k] -= Time.deltaTime;

            for (var m = 0; m < malletHeads.Length; m++)
            {
                var head = malletHeads[m].position;
                var speed = Time.deltaTime > 0f ? (head - previousHeads[m]).magnitude / Time.deltaTime : 0f;
                previousHeads[m] = head;
                if (speed < minStrikeSpeed)
                    continue;
                for (var k = 0; k < keys.Length; k++)
                {
                    if (cooldowns[k] > 0f || Vector3.Distance(head, keys[k].position) > keyRadius + 0.03f)
                        continue;
                    Play(k, Mathf.Clamp(speed / 2f, 0.4f, 1f));
                }
            }
        }

        void Play(int index, float volume)
        {
            if (cooldowns[index] > 0f)
                return;
            cooldowns[index] = 0.18f;
            var key = keys[index];
            DemoSfx.Play(DemoSfx.Kind.Note, key.position, volume, DemoSfx.Semitones(semitones[index % semitones.Length]));
            StartCoroutine(Bounce(index));

            notesPlayed++;
            melody.Add(noteNames[index % noteNames.Length]);
            if (melody.Count > 16)
                melody.RemoveAt(0);
            var melodyText = string.Join(" ", melody);

            if (distinct.Add(index))
            {
                Report(distinct.Count, "notas diferentes", true, melodyText);
                Popup(key.position + Vector3.up * 0.18f, noteNames[index % noteNames.Length] + "!");
            }

            if (notesPlayed % 8 == 0)
                ReportAs(activityId + "_melodia", "Melodia tocada", notesPlayed, "notas", true, melodyText);
        }

        IEnumerator Bounce(int index)
        {
            var key = keys[index];
            var t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                key.localScale = baseScales[index] * (1f + Mathf.Sin(t / 0.25f * Mathf.PI) * 0.25f);
                yield return null;
            }

            key.localScale = baseScales[index];
        }
    }
}
