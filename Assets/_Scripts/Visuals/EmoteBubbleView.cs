using DG.Tweening;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>One pooled bubble quad. Follows its target, faces the camera and pops in and out.</summary>
    public class EmoteBubbleView : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");

        [SerializeField] private Renderer quad;

        private MaterialPropertyBlock block;
        private Transform target;
        private float height;
        private Camera cam;
        private Sequence sequence;

        public Transform Target => target;
        public bool IsFree => target == null;

        public void Play(Transform newTarget, Texture2D texture, float seconds, float worldSize, float heightOffset, float popSeconds)
        {
            target = newTarget;
            height = heightOffset;
            block ??= new MaterialPropertyBlock();
            quad.GetPropertyBlock(block);
            block.SetTexture(BaseMap, texture);
            quad.SetPropertyBlock(block);

            gameObject.SetActive(true);
            sequence?.Kill();
            transform.localScale = Vector3.zero;
            Follow();
            sequence = DOTween.Sequence()
                .Append(transform.DOScale(worldSize, popSeconds).SetEase(Ease.OutBack))
                .AppendInterval(Mathf.Max(0f, seconds - popSeconds * 2f))
                .Append(transform.DOScale(0f, popSeconds).SetEase(Ease.InBack))
                .OnComplete(Release)
                .SetLink(gameObject);
        }

        public void Release()
        {
            sequence?.Kill();
            sequence = null;
            target = null;
            gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (target == null) { Release(); return; }
            Follow();
        }

        private void Follow()
        {
            if (cam == null) cam = Camera.main;
            transform.position = target.position + Vector3.up * height;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
