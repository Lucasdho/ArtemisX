using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Visuals;
using DG.Tweening;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// Supply landing pad: on a supply day the capsule drops from the sky onto the pad (after the morning cards),
    /// then the cargo pile appears. Clicking the pile collects it and flies Food and Parts icons to the HUD.
    /// </summary>
    public class SupplyPileInteractable : WorldInteractable
    {
        [SerializeField] private SupplyManager supply;
        [SerializeField] private MorningSequenceController morning;
        [SerializeField] private GameplayVisualsConfig visuals;
        [Tooltip("Cargo pile shown while a delivery waits.")]
        [SerializeField] private GameObject pile;
        [Tooltip("Capsule that drops onto the pad (the landed dropship).")]
        [SerializeField] private Transform capsule;

        private Vector3 capsuleRest;
        private Vector3 pileScale = Vector3.one;
        private bool dropPending;

        protected override void Awake()
        {
            base.Awake();
            if (capsule != null) capsuleRest = capsule.position;
            if (pile != null)
            {
                pileScale = pile.transform.localScale;
                pile.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (supply != null) supply.SupplyDropped += OnSupplyDropped;
            if (morning != null) morning.SequenceFinished += OnMorningFinished;
        }

        private void OnDisable()
        {
            if (supply != null) supply.SupplyDropped -= OnSupplyDropped;
            if (morning != null) morning.SequenceFinished -= OnMorningFinished;
        }

        private void OnSupplyDropped(int day, int food, int parts)
        {
            dropPending = true;
            if (morning == null || !morning.IsShowing) PlayDrop();
        }

        private void OnMorningFinished()
        {
            if (dropPending) PlayDrop();
        }

        /// <summary>Capsule falls onto the pad, then the pile pops in.</summary>
        public void PlayDrop()
        {
            dropPending = false;
            float height = visuals != null ? visuals.capsuleDropHeight : 14f;
            float seconds = visuals != null ? visuals.capsuleDropSeconds : 1.4f;
            var seq = DOTween.Sequence().SetLink(gameObject);
            if (capsule != null)
            {
                capsule.DOKill();
                capsule.position = capsuleRest + Vector3.up * height;
                seq.Append(capsule.DOMove(capsuleRest, seconds).SetEase(Ease.OutCubic));
                seq.Append(capsule.DOPunchScale(new Vector3(0.08f, -0.12f, 0.08f), 0.3f, 5, 0.5f));
            }
            seq.AppendCallback(ShowPile);
        }

        private void ShowPile()
        {
            if (pile == null || supply == null || !supply.HasPendingSupply) return;
            pile.SetActive(true);
            pile.transform.DOKill();
            pile.transform.localScale = Vector3.zero;
            pile.transform.DOScale(pileScale, 0.35f).SetEase(Ease.OutBack).SetLink(pile);
            EmoteBubble.Show(pile.transform, EmoteKind.Idea, 4f, 1.2f);
        }

        public override void OnWorldClicked()
        {
            if (supply == null) return;
            if (!supply.HasPendingSupply)
            {
                int interval = supply.Interval;
                OpenCard("Landing Pad", $"Supply capsules from Earth land here every {interval} days.", null, false, null, null);
                return;
            }

            Vector3 from = pile != null && pile.activeSelf ? pile.transform.position : AnchorWorldPosition;
            if (!supply.CollectSupply()) return;
            if (hud != null)
            {
                if (hud.Popup != null) hud.Popup.Close();
                hud.PlaySfx(hud.Config != null ? hud.Config.collectClip : null);
                if (hud.FlyFx != null)
                {
                    hud.FlyFx.Fly(ResourceType.Food, from, hud.ViewCamera);
                    hud.FlyFx.Fly(ResourceType.Parts, from, hud.ViewCamera);
                }
            }
            if (pile != null) pile.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack).SetLink(pile).OnComplete(() => pile.SetActive(false));
        }
    }
}
