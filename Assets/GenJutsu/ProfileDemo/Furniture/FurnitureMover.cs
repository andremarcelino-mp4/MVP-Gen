using System.Collections;
using GenJutsu.GenCore;
using UnityEngine;

namespace GenJutsu.ProfileDemo
{
    public sealed class FurnitureMover : MonoBehaviour
    {
        public float duration = 1f;
        Transform house;
        FurnitureMoveFx fx;
        Coroutine running;

        public void Bind(Transform root, FurnitureHouseCatalog houseCatalog)
        {
            house = root;
            if (fx == null)
            {
                var go = new GameObject("FurnitureMoveFx");
                go.transform.SetParent(transform, false);
                fx = go.AddComponent<FurnitureMoveFx>();
            }
        }

        public bool TryRun(FurnitureMoveIntent intent, out string reply)
        {
            reply = intent.Reply;
            if (!intent.Matched || house == null)
                return false;
            if (running != null)
                StopCoroutine(running);
            if (intent.Reset)
            {
                running = StartCoroutine(ResetAll());
                return true;
            }

            var piece = FindFurniture(intent.FurnitureId);
            var anchor = FindAnchor(intent.TargetId);
            if (piece == null || anchor == null)
            {
                reply = "Não achei esse móvel aqui.";
                return false;
            }

            running = StartCoroutine(MoveOne(piece, anchor.transform, anchor.Yaw));
            return true;
        }

        IEnumerator MoveOne(MovableFurniture piece, Transform target, float yaw)
        {
            piece.BeginDirectedMove();
            if (fx != null)
                fx.PlayAt(piece.transform.position);
            var fromPos = piece.transform.position;
            var fromRot = piece.transform.rotation;
            var toPos = target.position;
            toPos.y = fromPos.y;
            var toRot = Quaternion.Euler(0f, yaw, 0f);
            var t = 0f;
            var span = Mathf.Max(0.2f, duration);
            while (t < 1f)
            {
                t += Time.deltaTime / span;
                var u = Ease(Mathf.Clamp01(t));
                piece.transform.position = Vector3.Lerp(fromPos, toPos, u);
                piece.transform.rotation = Quaternion.Slerp(fromRot, toRot, u);
                yield return null;
            }

            piece.transform.position = toPos;
            piece.transform.rotation = toRot;
            piece.SettleAfterMove();
            if (fx != null)
                fx.PlayAt(piece.transform.position);
            running = null;
        }

        IEnumerator ResetAll()
        {
            var pieces = house.GetComponentsInChildren<MovableFurniture>(true);
            var fromPos = new Vector3[pieces.Length];
            var fromRot = new Quaternion[pieces.Length];
            for (var i = 0; i < pieces.Length; i++)
            {
                pieces[i].BeginDirectedMove();
                fromPos[i] = pieces[i].transform.localPosition;
                fromRot[i] = pieces[i].transform.localRotation;
            }

            if (fx != null && pieces.Length > 0)
                fx.PlayAt(pieces[0].transform.position);
            var t = 0f;
            var span = Mathf.Max(0.2f, duration);
            while (t < 1f)
            {
                t += Time.deltaTime / span;
                var u = Ease(Mathf.Clamp01(t));
                for (var i = 0; i < pieces.Length; i++)
                {
                    pieces[i].transform.localPosition = Vector3.Lerp(fromPos[i], pieces[i].HomeLocal, u);
                    pieces[i].transform.localRotation = Quaternion.Slerp(fromRot[i], pieces[i].HomeRot, u);
                }

                yield return null;
            }

            for (var i = 0; i < pieces.Length; i++)
            {
                pieces[i].transform.localPosition = pieces[i].HomeLocal;
                pieces[i].transform.localRotation = pieces[i].HomeRot;
                pieces[i].SettleAfterMove();
            }

            running = null;
        }

        static float Ease(float t)
        {
            return t * t * (3f - 2f * t);
        }

        MovableFurniture FindFurniture(string id)
        {
            var all = house.GetComponentsInChildren<MovableFurniture>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].FurnitureId == id)
                    return all[i];
            }

            return null;
        }

        FurnitureAnchor FindAnchor(string id)
        {
            var all = house.GetComponentsInChildren<FurnitureAnchor>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].AnchorId == id)
                    return all[i];
            }

            return null;
        }
    }
}
