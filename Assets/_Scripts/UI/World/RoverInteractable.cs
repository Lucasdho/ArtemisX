using ArtemisX.Core;
using ArtemisX.Simulation;

namespace ArtemisX.UI
{
    /// <summary>
    /// Rover actions, driven only by the HUD Rover button (the 3D rover itself is not clickable, GDD §6):
    /// Ready opens the top-down site map, Broken opens the repair card, otherwise Bauzer explains.
    /// </summary>
    public class RoverInteractable : WorldInteractable
    {
        [UnityEngine.SerializeField] private RoverExpeditionManager rover;
        [UnityEngine.SerializeField] private ResourceManager resources;

        /// <summary>Clicks on the 3D rover do nothing; the HUD Rover button is the only way to operate it.</summary>
        public override void OnWorldClicked() { }

        /// <summary>Called by the HUD Rover button.</summary>
        public void Activate()
        {
            if (rover == null || hud == null || hud.IsWorldInputBlocked) return;

            switch (rover.Status)
            {
                case RoverStatus.Ready:
                    if (resources != null && resources.IsCrewRestricted) { hud.ShowBlockedTip(ActionBlockReason.CrewRestricted); return; }
                    if (hud.Popup != null) hud.Popup.Close();
                    if (hud.RoverMap != null) hud.RoverMap.Open();
                    break;
                case RoverStatus.Broken:
                    var reason = rover.CanRepair();
                    OpenCard(rover.RoverName + " is broken",
                        reason == ActionBlockReason.NotEnoughParts ? "You need more Parts to fix it." : "Fix it to send it on trips again.",
                        $"Repair ({rover.RepairCost} Parts)", reason == ActionBlockReason.None, Repair,
                        new[] { new ContextConfirmPopupView.Cost(ResourceType.Parts, -rover.RepairCost) });
                    break;
                case RoverStatus.OnMission:
                    hud.ShowBlockedTip(ActionBlockReason.RoverOnMission);
                    break;
                case RoverStatus.LockedByRadiation:
                    hud.ShowBlockedTip(ActionBlockReason.RoverLockedByRadiation);
                    break;
            }
        }

        private void Repair()
        {
            var reason = rover.CanRepair();
            if (reason != ActionBlockReason.None) { hud.ShowBlockedTip(reason); return; }
            if (rover.TryRepairRover()) hud.PlaySfx(hud.Config != null ? hud.Config.buildClip : null);
        }
    }
}
