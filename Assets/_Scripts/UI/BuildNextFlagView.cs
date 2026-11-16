using System;
using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Floating "build here next" Toy UI flag + ground shadow over the next empty build slot.
    /// Visual-only: tapping it delegates straight to <see cref="GhostSlotInteractable.OnWorldClicked"/>.
    /// </summary>
    public class BuildNextFlagView : MonoBehaviour
    {
        [Serializable]
        public class CostStickerSlot
        {
            public GameObject root;
            public Image icon;
            public Text amount;
        }

        [Header("Scene & Managers")]
        [SerializeField] private BuildingManager buildings;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private UIStyle style;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private GhostSlotInteractable[] slots = Array.Empty<GhostSlotInteractable>();

        [Header("Flag UI")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RectTransform pillRect;
        [SerializeField] private Image pillImage;
        [SerializeField] private Button button;
        [SerializeField] private Image buildingIcon;
        [SerializeField] private Text nameLabel;
        [SerializeField] private CostStickerSlot[] costStickers = new CostStickerSlot[2];

        [Header("Building Icons (256px)")]
        [SerializeField] private Sprite[] buildingIcons = Array.Empty<Sprite>();
        [SerializeField] private Sprite fallbackHammerIcon;

        [Header("World Shadow")]
        [SerializeField] private GameObject groundShadow;

        [Header("Motion")]
        [SerializeField] private float heightAboveGround = 1.5f;
        [SerializeField] private float bobAmplitudePx = 4f;
        [SerializeField] private float bobFrequencyHz = 0.8f;

        private static readonly Color UnaffordableTint = new Color(0.690f, 0.690f, 0.690f, 1f); // #B0B0B0

        private RectTransform canvasRect;
        private GhostSlotInteractable currentSlot;
        private int lastSlotIndex = -2;
        private bool lastAffordable = true;

        private void Awake()
        {
            canvasRect = GetComponentInParent<Canvas>()?.rootCanvas.GetComponent<RectTransform>();
            if (button != null) button.onClick.AddListener(OnFlagClicked);
        }

        private void OnEnable()
        {
            if (buildings != null) buildings.BuildingCompleted += OnBuildingCompleted;
            if (resources != null) resources.ResourceChanged += OnResourceChanged;
            RefreshTarget();
        }

        private void OnDisable()
        {
            if (buildings != null) buildings.BuildingCompleted -= OnBuildingCompleted;
            if (resources != null) resources.ResourceChanged -= OnResourceChanged;
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnFlagClicked);
        }

        private void Start() => RefreshTarget();

        private void OnBuildingCompleted(int _) => RefreshTarget();

        private void OnResourceChanged(ResourceType _, int __, int ___) => UpdateAffordability();

        public void RefreshTarget()
        {
            currentSlot = ResolveNextSlot();
            DisableOldRingOn(currentSlot);

            if (currentSlot == null || buildings == null || buildings.Config == null ||
                currentSlot.BuildingIndex < 0 || currentSlot.BuildingIndex >= buildings.Config.Count)
            {
                lastSlotIndex = -1;
                SetVisible(false);
                return;
            }

            int idx = currentSlot.BuildingIndex;
            var b = buildings.Config.buildings[idx];
            if (idx != lastSlotIndex)
            {
                lastSlotIndex = idx;
                if (nameLabel != null) nameLabel.text = b.displayName;
                if (buildingIcon != null)
                {
                    Sprite sp = buildingIcons != null && idx < buildingIcons.Length && buildingIcons[idx] != null
                        ? buildingIcons[idx]
                        : fallbackHammerIcon;
                    buildingIcon.sprite = sp;
                    buildingIcon.enabled = sp != null;
                    buildingIcon.color = Color.white;
                }
                ApplyCosts(b);
                PositionGroundShadow(currentSlot);
            }

            UpdateAffordability();
            UpdateScreenPosition();
        }

        private void LateUpdate()
        {
            bool hide = currentSlot == null || (hud != null && hud.IsOverlayCoveringHud);
            if (hide)
            {
                SetVisible(false);
                return;
            }
            UpdateScreenPosition();
        }

        private void UpdateScreenPosition()
        {
            if (currentSlot == null || canvasRect == null) { SetVisible(false); return; }
            Camera cam = viewCamera != null ? viewCamera : (hud != null ? hud.ViewCamera : Camera.main);
            if (cam == null) { SetVisible(false); return; }

            Vector3 worldPos = GetSlotAnchor(currentSlot) + Vector3.up * heightAboveGround;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) { SetVisible(false); return; }

            Canvas rootCanvas = canvasRect.GetComponent<Canvas>();
            Camera uiCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPos))
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            float bob = Mathf.Sin(Time.unscaledTime * bobFrequencyHz * Mathf.PI * 2f) * bobAmplitudePx;
            if (pillRect != null)
                pillRect.anchoredPosition = localPos + new Vector2(0f, bob);
            else if (transform is RectTransform rt)
                rt.anchoredPosition = localPos + new Vector2(0f, bob);
        }

        private void SetVisible(bool visible)
        {
            if (visualRoot != null && visualRoot.activeSelf != visible)
                visualRoot.SetActive(visible);
            if (groundShadow != null)
            {
                bool showShadow = visible && currentSlot != null;
                if (groundShadow.activeSelf != showShadow) groundShadow.SetActive(showShadow);
            }
        }

        private void UpdateAffordability()
        {
            if (currentSlot == null || buildings == null || buildings.Config == null) return;
            var b = buildings.Config.buildings[currentSlot.BuildingIndex];
            bool affordable = resources == null ||
                (resources.Has(ResourceType.Parts, b.partsCost) && resources.Has(ResourceType.Power, b.powerCost));
            lastAffordable = affordable;
            if (pillImage != null)
                pillImage.color = affordable ? Color.white : UnaffordableTint;
        }

        private void ApplyCosts(BuildingConfig.Building b)
        {
            var list = new (ResourceType type, int amt)[]
            {
                (ResourceType.Parts, b.partsCost),
                (ResourceType.Power, b.powerCost)
            };
            int slotIdx = 0;
            for (int i = 0; i < list.Length && costStickers != null && slotIdx < costStickers.Length; i++)
            {
                if (list[i].amt <= 0) continue;
                var slot = costStickers[slotIdx++];
                if (slot == null || slot.root == null) continue;
                slot.root.SetActive(true);
                if (slot.icon != null && style != null && style.TryGetLook(list[i].type, out var look))
                {
                    slot.icon.sprite = look.icon;
                    slot.icon.color = Color.white;
                }
                if (slot.amount != null)
                    slot.amount.text = "\u2212" + list[i].amt;
            }
            if (costStickers != null)
            {
                for (int i = slotIdx; i < costStickers.Length; i++)
                    if (costStickers[i] != null && costStickers[i].root != null)
                        costStickers[i].root.SetActive(false);
            }
        }

        private GhostSlotInteractable ResolveNextSlot()
        {
            if (buildings == null || buildings.AllBuilt || slots == null || slots.Length == 0) return null;

            int nextIdx = buildings.NextIndex;
            foreach (var s in slots)
                if (s != null && s.BuildingIndex == nextIdx && !buildings.IsBuilt(s.BuildingIndex))
                    return s;

            if (buildings.Config != null && resources != null)
            {
                foreach (var s in slots)
                {
                    if (s == null || buildings.IsBuilt(s.BuildingIndex)) continue;
                    var b = buildings.Config.buildings[s.BuildingIndex];
                    if (resources.Has(ResourceType.Parts, b.partsCost) && resources.Has(ResourceType.Power, b.powerCost))
                        return s;
                }
            }

            foreach (var s in slots)
                if (s != null && !buildings.IsBuilt(s.BuildingIndex))
                    return s;

            return null;
        }

        private static void DisableOldRingOn(GhostSlotInteractable slot)
        {
            if (slot == null) return;
            var ring = slot.transform.Find("GhostRing");
            if (ring == null) return;
            var pulse = ring.GetComponent<Visuals.GhostRingPulse>();
            if (pulse != null) pulse.enabled = false;
            var lr = ring.GetComponent<LineRenderer>();
            if (lr != null) lr.enabled = false;
        }

        private void PositionGroundShadow(GhostSlotInteractable slot)
        {
            if (groundShadow == null || slot == null) return;
            Vector3 anchor = GetSlotAnchor(slot);
            groundShadow.transform.position = new Vector3(anchor.x, anchor.y + 0.26f, anchor.z);
        }

        private static Vector3 GetSlotAnchor(GhostSlotInteractable slot)
        {
            var col = slot.GetComponent<Collider>();
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.min.y, b.center.z);
            }
            return slot.transform.position;
        }

        private void OnFlagClicked()
        {
            if (currentSlot == null) return;
            if (hud != null && hud.IsWorldInputBlocked) return;
            currentSlot.OnWorldClicked();
        }
    }
}
