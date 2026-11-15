using System;
using ArtemisX.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Flies little resource icons from a world point to that resource's HUD pill.</summary>
    public class ResourceFlyFx : MonoBehaviour
    {
        [Serializable]
        public struct Target
        {
            public ResourceType type;
            public RectTransform pill;
        }

        [SerializeField] private GameplayUIConfig config;
        [SerializeField] private UIStyle style;
        [SerializeField] private RectTransform layer;
        [SerializeField] private Target[] targets = new Target[0];
        [SerializeField] private Vector2 iconSize = new Vector2(44f, 44f);

        public void Fly(ResourceType type, Vector3 worldFrom, Camera cam)
        {
            if (layer == null || cam == null) return;
            RectTransform pill = FindPill(type);
            if (pill == null) return;

            Vector2 from = ToLocal(cam.WorldToScreenPoint(worldFrom));
            Vector2 to = ToLocal(RectTransformUtility.WorldToScreenPoint(null, pill.TransformPoint(pill.rect.center)));

            Sprite sprite = null;
            Color color = Color.white;
            if (style != null && style.TryGetLook(type, out var look))
            {
                sprite = look.icon;
                if (sprite == null) color = look.accent;
            }

            int count = config != null ? config.flyIconCount : 5;
            float seconds = config != null ? config.flyIconSeconds : 0.7f;
            float stagger = config != null ? config.flyIconStagger : 0.06f;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("FlyIcon_" + type, typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(layer, false);
                rt.sizeDelta = iconSize;
                var img = go.GetComponent<Image>();
                img.sprite = sprite;
                img.color = color;
                img.raycastTarget = false;
                Vector2 start = from + UnityEngine.Random.insideUnitCircle * 40f;
                rt.anchoredPosition = start;
                rt.localScale = Vector3.zero;
                float delay = i * stagger;
                DOTween.Sequence().SetLink(go)
                    .AppendInterval(delay)
                    .Append(rt.DOScale(1f, 0.15f).SetEase(Ease.OutBack))
                    .Append(rt.DOAnchorPos(to, seconds).SetEase(Ease.InCubic))
                    .Join(rt.DOScale(0.6f, seconds).SetEase(Ease.InCubic))
                    .OnComplete(() =>
                    {
                        pill.DOKill(true);
                        pill.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f).SetLink(pill.gameObject);
                        Destroy(go);
                    });
            }
        }

        private RectTransform FindPill(ResourceType type)
        {
            foreach (var t in targets) if (t.type == type) return t.pill;
            return null;
        }

        private Vector2 ToLocal(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out Vector2 local);
            return local;
        }
    }
}
