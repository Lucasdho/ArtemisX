using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// One resource bar. Listens to ResourceManager.ResourceChanged and resizes a masked fill
    /// (RectMask2D) so the bar's rounded art is clipped, not squashed.
    /// </summary>
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] private ResourceType type;
        [SerializeField] private ResourceManager resources;
        [Tooltip("RectMask2D container anchored to the left; its width is the fill fraction.")]
        [SerializeField] private RectTransform fillMask;

        private void OnEnable()
        {
            if (resources != null) resources.ResourceChanged += OnResourceChanged;
        }

        private void OnDisable()
        {
            if (resources != null) resources.ResourceChanged -= OnResourceChanged;
        }

        private void OnResourceChanged(ResourceType changed, int current, int max)
        {
            if (changed != type || fillMask == null) return;
            float fraction = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
            var anchor = fillMask.anchorMax;
            anchor.x = fraction;
            fillMask.anchorMax = anchor;
        }
    }
}
