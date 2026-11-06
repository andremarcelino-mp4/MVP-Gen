using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Bring the warm milk to the cat and pet it. Scores: deliveries and seconds of petting.
    /// </summary>
    public class ServeTheCat : ProfileActivity
    {
        public Transform catMouth;
        public ResettablePose mug;
        public XRSimpleInteractable catHead;
        public Transform tail;
        public Transform[] eyes;
        public float deliverDistance = 0.3f;
        public float petDistance = 0.35f;

        int deliveries;
        float petSeconds;
        float nextPetReport;
        float purrTimer;
        float happyUntil;
        float nextBlink;
        float blinkEnd;
        Quaternion tailBase;
        Vector3[] eyeScales;

        void Awake()
        {
            tailBase = tail != null ? tail.localRotation : Quaternion.identity;
            eyeScales = new Vector3[eyes.Length];
            for (var i = 0; i < eyes.Length; i++)
                eyeScales[i] = eyes[i].localScale;
        }

        public override void ResetActivity()
        {
            deliveries = 0;
            petSeconds = 0f;
            nextPetReport = 3f;
            happyUntil = 0f;
        }

        void Update()
        {
            var now = Time.time;
            var happy = now < happyUntil;

            if (!mug.IsFlying && Vector3.Distance(mug.transform.position, catMouth.position) < deliverDistance)
                Deliver();

            var petting = IsPetting();
            if (petting)
            {
                petSeconds += Time.deltaTime;
                happy = true;
                purrTimer -= Time.deltaTime;
                if (purrTimer <= 0f)
                {
                    purrTimer = 1.3f;
                    DemoSfx.Play(DemoSfx.Kind.Purr, catMouth.position, 0.7f);
                }

                if (petSeconds >= nextPetReport)
                {
                    nextPetReport += 3f;
                    var seconds = Mathf.Floor(petSeconds);
                    ReportAs(activityId + "_carinho", "Carinho no gato", seconds, "s");
                    Popup(catHead.transform.position + Vector3.up * 0.25f, "Carinho " + seconds + " s");
                }
            }

            if (tail != null)
            {
                var speed = happy ? 2.2f : 0.8f;
                var amplitude = happy ? 30f : 14f;
                tail.localRotation = tailBase * Quaternion.Euler(0f, Mathf.Sin(now * speed * Mathf.PI) * amplitude, 0f);
            }

            UpdateEyes(now, petting);
        }

        bool IsPetting()
        {
            if (catHead == null || !catHead.isHovered)
                return false;
            var head = catHead.transform.position;
            foreach (var interactor in catHead.interactorsHovering)
            {
                if (Vector3.Distance(interactor.transform.position, head) < petDistance)
                    return true;
            }

            return false;
        }

        void Deliver()
        {
            deliveries++;
            happyUntil = Time.time + 4f;
            Report(deliveries, "entregas");
            Popup(catMouth.position + Vector3.up * 0.25f, "Miau! +1");
            DemoSfx.Play(DemoSfx.Kind.Purr, catMouth.position, 0.9f, 1.1f);
            mug.ReturnHome();
        }

        void UpdateEyes(float now, bool squint)
        {
            if (now >= nextBlink)
            {
                blinkEnd = now + 0.12f;
                nextBlink = now + Random.Range(2.5f, 5f);
            }

            var closed = squint ? 0.25f : (now < blinkEnd ? 0.1f : 1f);
            for (var i = 0; i < eyes.Length; i++)
            {
                var s = eyeScales[i];
                eyes[i].localScale = new Vector3(s.x, s.y * closed, s.z);
            }
        }
    }
}
