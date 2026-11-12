using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    /// <summary>
    /// Grows a house out of the white void in rings around the user, and folds it back.
    /// </summary>
    public class HouseTransition : MonoBehaviour
    {
        public ProfileId profile;
        public Transform piecesRoot;
        public GameObject interactivesRoot;
        public float duration = 2.4f;
        public float riseDepth = 0.8f;
        public bool comfortEase = true;

        struct Piece
        {
            public Transform transform;
            public Vector3 position;
            public Vector3 scale;
        }

        readonly List<Piece> pieces = new List<Piece>();
        float[] delays;

        void CollectPieces()
        {
            if (pieces.Count > 0 || piecesRoot == null)
                return;
            foreach (Transform child in piecesRoot)
                pieces.Add(new Piece { transform = child, position = child.localPosition, scale = child.localScale });
            delays = new float[pieces.Count];
        }

        public IEnumerator Build(Vector3 origin)
        {
            CollectPieces();
            gameObject.SetActive(true);
            if (interactivesRoot != null)
                interactivesRoot.SetActive(false);

            // Devolve os objetos ao lugar ANTES de encolher as peças: com a peça em escala 0,
            // gravar posição de mundo num filho gera posição local degenerada (móveis amontoados).
            RestoreProps();

            // Rigidbody com interpolação reescreve o transform com a pose da física e "congela"
            // o objeto no meio do crescimento da peça. Desliga durante a animação.
            var bodies = GetComponentsInChildren<Rigidbody>(true);
            var interp = new RigidbodyInterpolation[bodies.Length];
            for (var i = 0; i < bodies.Length; i++)
            {
                interp[i] = bodies[i].interpolation;
                bodies[i].interpolation = RigidbodyInterpolation.None;
            }

            ComputeDelays(origin);
            for (var i = 0; i < pieces.Count; i++)
                Apply(i, 0f);

            yield return Animate(true);

            for (var i = 0; i < pieces.Count; i++)
                Apply(i, 1f);
            RestoreProps();
            Physics.SyncTransforms();
            for (var i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null)
                    bodies[i].interpolation = interp[i];
            }

            if (interactivesRoot != null)
                interactivesRoot.SetActive(true);
            GrabRest.SettleUnder(transform);
        }

        void RestoreProps()
        {
            foreach (var bounds in GetComponentsInChildren<BoundsReset>(true))
                bounds.Restore();
            foreach (var pose in GetComponentsInChildren<ResettablePose>(true))
            {
                if (pose.GetComponent<BoundsReset>() == null)
                    pose.ResetNow();
            }

            foreach (var body in GetComponentsInChildren<Rigidbody>(true))
            {
                body.position = body.transform.position;
                body.rotation = body.transform.rotation;
            }
        }

        public IEnumerator Dissolve(Vector3 origin)
        {
            CollectPieces();
            if (interactivesRoot != null)
                interactivesRoot.SetActive(false);

            ComputeDelays(origin);
            yield return Animate(false);

            gameObject.SetActive(false);
            for (var i = 0; i < pieces.Count; i++)
                Apply(i, 1f);
        }

        public void HideImmediate()
        {
            CollectPieces();
            for (var i = 0; i < pieces.Count; i++)
                Apply(i, 1f);
            gameObject.SetActive(false);
        }

        void ComputeDelays(Vector3 origin)
        {
            var max = 0.01f;
            for (var i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i].transform.position;
                var d = new Vector2(p.x - origin.x, p.z - origin.z).magnitude;
                delays[i] = d;
                if (d > max)
                    max = d;
            }

            for (var i = 0; i < pieces.Count; i++)
                delays[i] = delays[i] / max * duration * 0.6f;
        }

        IEnumerator Animate(bool building)
        {
            var span = duration * 0.4f;
            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                for (var i = 0; i < pieces.Count; i++)
                {
                    var u = Mathf.Clamp01((t - delays[i]) / span);
                    Apply(i, building ? u : 1f - u);
                }

                yield return null;
            }
        }

        void Apply(int i, float u)
        {
            var piece = pieces[i];
            if (piece.transform == null)
                return;
            var grow = comfortEase ? Mathf.SmoothStep(0f, 1f, u) : EaseOutBack(u);
            var rise = 1f - (1f - u) * (1f - u);
            piece.transform.localScale = piece.scale * Mathf.Max(0f, grow);
            piece.transform.localPosition = piece.position - Vector3.up * (riseDepth * (1f - rise));
            var active = u > 0.001f;
            if (piece.transform.gameObject.activeSelf != active)
                piece.transform.gameObject.SetActive(active);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.4f;
            const float c3 = c1 + 1f;
            var k = x - 1f;
            return 1f + c3 * k * k * k + c1 * k * k;
        }
    }
}
