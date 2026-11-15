using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Exposed solar field card: toggle Active / Retracted (Protected), or Repair when rocks broke it.</summary>
    public class SolarFieldInteractable : WorldInteractable
    {
        [SerializeField] private SolarFieldManager solar;
        [SerializeField] private GameConfig gameConfig;

        public override void OnWorldClicked()
        {
            if (solar == null) return;
            int fieldPower = gameConfig != null ? gameConfig.solarFieldPowerPerDay : 20;

            switch (solar.State)
            {
                case SolarFieldState.Active:
                    OpenCard("Solar Field: Active",
                        $"Making +{fieldPower} Power a day. Retract to protect the panels from storms (no field Power while retracted).",
                        "Retract", true, Toggle, null);
                    break;
                case SolarFieldState.Retracted:
                    OpenCard("Solar Field: Protected",
                        $"Panels are folded and safe. Deploy them to make +{fieldPower} Power a day again.",
                        "Deploy", true, Toggle, null);
                    break;
                case SolarFieldState.Broken:
                    var reason = solar.CanRepair();
                    OpenCard("Solar Field: Damaged",
                        reason == ActionBlockReason.NotEnoughParts
                            ? "Rocks hit the panels! You need more Parts to repair them."
                            : "Rocks hit the panels! Repair them to make Power again.",
                        $"Repair ({solar.RepairCost} Parts)", reason == ActionBlockReason.None, Repair,
                        new[] { new ContextConfirmPopupView.Cost(ResourceType.Parts, -solar.RepairCost) });
                    break;
            }
        }

        private void Toggle()
        {
            if (solar.ToggleRetract()) hud.PlaySfx(hud.Config != null ? hud.Config.confirmClip : null);
        }

        private void Repair()
        {
            var reason = solar.CanRepair();
            if (reason != ActionBlockReason.None) { hud.ShowBlockedTip(reason); return; }
            if (solar.TryRepairSolar()) hud.PlaySfx(hud.Config != null ? hud.Config.buildClip : null);
        }
    }
}
