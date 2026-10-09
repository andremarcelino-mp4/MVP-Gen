using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace GenJutsu.TableProps
{
    public class InkBrush : MonoBehaviour
    {
        [SerializeField] Transform tip;
        [SerializeField] float tipRadius = 0.005f;
        [SerializeField] float paintDistance = 0.04f;
        [SerializeField] LayerMask scrollMask = ~0;

        XRGrabInteractable grab;
        ScrollCanvas lastScroll;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (tip == null)
            {
                var tipPoint = transform.Find("TipPoint");
                if (tipPoint != null)
                    tip = tipPoint;
                else
                {
                    var tipGo = transform.Find("Tip");
                    tip = tipGo != null ? tipGo : transform;
                }
            }
        }

        void Update()
        {
            if (grab == null || !grab.isSelected || tip == null)
                return;

            var castDir = -tip.up;
            if (Physics.SphereCast(tip.position, tipRadius, castDir, out var hit, paintDistance, scrollMask, QueryTriggerInteraction.Ignore))
            {
                var scroll = hit.collider.GetComponentInParent<ScrollCanvas>();
                if (scroll == null)
                    scroll = hit.collider.GetComponent<ScrollCanvas>();
                if (scroll != null)
                {
                    lastScroll = scroll;
                    scroll.TryPaintWorldPoint(hit.point, tipRadius);
                    return;
                }
            }

            var nearby = Physics.OverlapSphere(tip.position, tipRadius * 2f, scrollMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < nearby.Length; i++)
            {
                var scroll = nearby[i].GetComponentInParent<ScrollCanvas>();
                if (scroll != null)
                {
                    lastScroll = scroll;
                    scroll.TryPaintWorldPoint(tip.position, tipRadius);
                    break;
                }
            }
        }
    }
}
