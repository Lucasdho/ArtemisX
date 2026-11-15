using System.Collections.Generic;
using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Visuals;
using DG.Tweening;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// One of the three linear build slots. Until built, the building's children are hidden; only the next slot shows
    /// its pulsing ghost ring. Confirming builds at once (BuildingManager.TryBuildNext) and the building pops in.
    /// </summary>
    public class GhostSlotInteractable : WorldInteractable
    {
        [SerializeField] private BuildingManager buildings;
        [Tooltip("0 = Water & O2 Plant, 1 = Greenhouse, 2 = Fuel Reserve.")]
        [SerializeField] private int buildingIndex;
        [SerializeField] private GameplayVisualsConfig visuals;
        [Tooltip("Pulsing ring shown while this is the next slot to build.")]
        [SerializeField] private GameObject ghostRing;
        [Tooltip("What the building reveals when built. Empty = every child except the ghost ring.")]
        [SerializeField] private List<Transform> buildingParts = new List<Transform>();

        private readonly List<Vector3> partScales = new List<Vector3>();
        private bool shownBuilt;

        public int BuildingIndex => buildingIndex;

        protected override void Awake()
        {
            base.Awake();
            if (buildingParts.Count == 0)
            {
                foreach (Transform child in transform)
                    if (ghostRing == null || child.gameObject != ghostRing) buildingParts.Add(child);
            }
            foreach (var part in buildingParts) partScales.Add(part != null ? part.localScale : Vector3.one);
        }

        private void OnEnable()
        {
            if (buildings == null) return;
            buildings.BuildingCompleted += OnBuildingCompleted;
        }

        private void OnDisable()
        {
            if (buildings == null) return;
            buildings.BuildingCompleted -= OnBuildingCompleted;
        }

        private void Start() => Refresh(false);

        private void OnBuildingCompleted(int index) => Refresh(index == buildingIndex);

        private void Refresh(bool animate)
        {
            bool built = buildings != null && buildings.IsBuilt(buildingIndex);
            bool isNext = buildings != null && !built && buildings.NextIndex == buildingIndex;
            if (ghostRing != null) ghostRing.SetActive(isNext);

            if (built == shownBuilt && !animate) { SetPartsActive(built); return; }
            shownBuilt = built;
            SetPartsActive(built);
            if (!built || !animate) return;

            float seconds = visuals != null ? visuals.buildPopSeconds : 0.5f;
            for (int i = 0; i < buildingParts.Count; i++)
            {
                var part = buildingParts[i];
                if (part == null) continue;
                part.DOKill();
                part.localScale = Vector3.zero;
                part.DOScale(partScales[i], seconds).SetEase(Ease.OutBack).SetDelay(i * 0.03f).SetLink(part.gameObject);
            }
            EmoteBubble.Show(transform, EmoteKind.Happy, 2.5f, 3f);
        }

        private void SetPartsActive(bool active)
        {
            foreach (var part in buildingParts)
                if (part != null && part.gameObject.activeSelf != active) part.gameObject.SetActive(active);
        }

        public override void OnWorldClicked()
        {
            if (buildings == null || buildings.Config == null) return;
            if (buildings.IsBuilt(buildingIndex)) return;

            if (buildings.NextIndex != buildingIndex)
            {
                if (hud.Config != null) hud.ShowTip(hud.Config.tipBuildOrder);
                return;
            }

            var reason = buildings.CanBuildNext();
            if (reason == ActionBlockReason.CrewRestricted)
            {
                hud.ShowBlockedTip(reason);
                return;
            }

            var b = buildings.Config.buildings[buildingIndex];
            string output = DescribeOutput(b.dailyOutput);
            string desc = $"Star {buildingIndex + 1} of 3. {output}";
            if (reason == ActionBlockReason.NotEnoughParts) desc += "\nNeed more Parts.";
            else if (reason == ActionBlockReason.NotEnoughPower) desc += "\nNeed more Power.";

            OpenCard("Build " + b.displayName, desc, "Build", reason == ActionBlockReason.None, Build,
                new[]
                {
                    new ContextConfirmPopupView.Cost(ResourceType.Parts, -b.partsCost),
                    new ContextConfirmPopupView.Cost(ResourceType.Power, -b.powerCost),
                });
        }

        private void Build()
        {
            var reason = buildings.CanBuildNext();
            if (reason != ActionBlockReason.None) { hud.ShowBlockedTip(reason); return; }
            if (buildings.TryBuildNext()) hud.PlaySfx(hud.Config != null ? hud.Config.buildClip : null);
        }

        private static string DescribeOutput(ResourceAmount[] output)
        {
            if (output == null || output.Length == 0) return "Stores rocket fuel to finish the base!";
            var parts = new List<string>();
            foreach (var o in output) parts.Add($"+{o.amount} {o.resource}");
            return "Makes " + string.Join(", ", parts) + " a day.";
        }
    }
}
