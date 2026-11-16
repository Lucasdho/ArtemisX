using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ArtemisX.UI.StartScreen
{
    /// <summary>
    /// 3D selectable astronaut on the Character Select lunar stage.
    /// Stands in staggered Idle until clicked, then reacts with Wave/Yes/Jump and lights up its spotlight.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SelectableAstronaut : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField, Range(0, 3)] private int characterIndex;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform headBone;
        [SerializeField] private Transform headEndBone;
        [SerializeField] private Light spotLight;
        [SerializeField] private Renderer padRingRenderer;
        [SerializeField] private Color accentColor = new Color(0.220f, 0.741f, 0.973f, 1f);
        [SerializeField] private float idleSpotIntensity = 1.5f;
        [SerializeField] private float selectedSpotIntensity = 34f;
        [SerializeField] private float faceCameraDistance = 1.35f;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string[] reactionStates = { "Wave", "Yes", "Jump" };
        [SerializeField] private float crossFadeSeconds = 0.15f;

        private Action<int> onClicked;
        private Vector3 baseScale;
        private Coroutine returnToIdleRoutine;
        private MaterialPropertyBlock propBlock;
        private int lastReactionIndex = -1;

        public int CharacterIndex => characterIndex;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            FindHeadBones();
            baseScale = transform.localScale;
            propBlock = new MaterialPropertyBlock();
            ApplyHighlightVisuals(false, instant: true);
        }

        private void Start()
        {
            PlayIdle(characterIndex * 0.27f);
        }

        public void Init(Action<int> clickCallback)
        {
            onClicked = clickCallback;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            onClicked?.Invoke(characterIndex);
        }

        /// <summary>Computes a clean front-facing close-up camera position and look target for the character's face.</summary>
        public void GetFaceCameraPose(out Vector3 cameraPos, out Vector3 lookTarget)
        {
            FindHeadBones();
            if (headBone != null && headEndBone != null)
                lookTarget = Vector3.Lerp(headBone.position, headEndBone.position, 0.55f);
            else if (headBone != null)
                lookTarget = headBone.position + Vector3.up * 0.14f;
            else
                lookTarget = transform.position + Vector3.up * 1.15f;

            Vector3 forward = transform.forward;
            cameraPos = lookTarget + forward * faceCameraDistance + Vector3.up * 0.03f;
        }

        /// <summary>Applies selected state: spotlight, gentle scale pop, and reaction animation.</summary>
        public void SetSelected(bool selected)
        {
            if (returnToIdleRoutine != null)
            {
                StopCoroutine(returnToIdleRoutine);
                returnToIdleRoutine = null;
            }

            ApplyHighlightVisuals(selected, instant: false);

            if (animator == null || !animator.isActiveAndEnabled) return;

            if (selected)
            {
                string state = PickReactionState();
                int hash = Animator.StringToHash(state);
                if (animator.HasState(0, hash))
                {
                    animator.CrossFadeInFixedTime(hash, crossFadeSeconds, 0, 0f);
                    returnToIdleRoutine = StartCoroutine(ReturnToIdleAfterDelay(1.75f));
                }
            }
            else
            {
                PlayIdle(0f);
            }
        }

        private void PlayIdle(float normalizedOffset)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            int hash = Animator.StringToHash(idleState);
            if (animator.HasState(0, hash))
            {
                animator.Play(hash, 0, normalizedOffset % 1f);
                animator.Update(0f);
            }
        }

        private IEnumerator ReturnToIdleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            int hash = Animator.StringToHash(idleState);
            if (animator != null && animator.isActiveAndEnabled && animator.HasState(0, hash))
                animator.CrossFadeInFixedTime(hash, 0.25f, 0, 0f);
            returnToIdleRoutine = null;
        }

        private string PickReactionState()
        {
            if (reactionStates == null || reactionStates.Length == 0) return idleState;
            int next = reactionStates.Length == 1
                ? 0
                : (lastReactionIndex + 1) % reactionStates.Length;
            lastReactionIndex = next;
            return reactionStates[next];
        }

        private void ApplyHighlightVisuals(bool selected, bool instant)
        {
            transform.DOKill();
            Vector3 targetScale = selected ? baseScale * 1.08f : baseScale;
            if (instant)
                transform.localScale = targetScale;
            else
                transform.DOScale(targetScale, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject);

            if (spotLight != null)
            {
                spotLight.DOKill();
                float targetIntensity = selected ? selectedSpotIntensity : idleSpotIntensity;
                if (instant)
                    spotLight.intensity = targetIntensity;
                else
                    spotLight.DOIntensity(targetIntensity, 0.25f).SetLink(gameObject);
            }

            if (padRingRenderer != null)
            {
                padRingRenderer.GetPropertyBlock(propBlock);
                Color emissive = selected ? accentColor * 2.2f : accentColor * 0.45f;
                propBlock.SetColor("_EmissionColor", emissive);
                propBlock.SetColor("_BaseColor", selected ? accentColor : new Color(accentColor.r * 0.4f, accentColor.g * 0.4f, accentColor.b * 0.4f, 1f));
                padRingRenderer.SetPropertyBlock(propBlock);
            }
        }

        private void FindHeadBones()
        {
            if (headBone != null && headEndBone != null) return;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (headBone == null && t.name == "Head") headBone = t;
                else if (headEndBone == null && t.name == "Head_end") headEndBone = t;
            }
        }
    }
}
