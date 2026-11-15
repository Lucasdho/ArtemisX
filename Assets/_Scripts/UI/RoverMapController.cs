using ArtemisX.Core;
using ArtemisX.Simulation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Top-down 3D exploration view: flies the base camera up over the five sites, shows a pin per site
    /// (-Power, +Water / +Parts, risk %) and dispatches the rover with one click. Back returns to the diorama.
    /// </summary>
    public class RoverMapController : MonoBehaviour
    {
        [SerializeField] private GameplayHud hud;
        [SerializeField] private GameplayUIConfig config;
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private SpaceWeatherManager weather;
        [Tooltip("The camera rig the base camera follows (CM_BaseCamera).")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private CameraPanController panController;
        [SerializeField] private GameObject mapRoot;
        [SerializeField] private RectTransform markerLayer;
        [SerializeField] private RoverSiteMarkerView[] markers = new RoverSiteMarkerView[0];
        [SerializeField] private Button backButton;
        [SerializeField] private Text titleLabel;

        private Vector3 savedPosition;
        private Quaternion savedRotation;
        private Sequence flight;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(Close);
            foreach (var m in markers) if (m != null) m.Bind(OnSiteClicked);
            if (mapRoot != null) mapRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Close);
        }

        public void Open()
        {
            if (IsOpen || cameraRig == null) return;
            IsOpen = true;
            if (panController != null) panController.enabled = false;
            savedPosition = cameraRig.position;
            savedRotation = cameraRig.rotation;

            RefreshMarkers();
            if (titleLabel != null && rover != null) titleLabel.text = "Send " + rover.RoverName + " to a site";

            ComputeMapPose(out Vector3 pos, out Quaternion rot);
            Fly(pos, rot, () => { if (mapRoot != null && IsOpen) mapRoot.SetActive(true); });
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (mapRoot != null) mapRoot.SetActive(false);
            Fly(savedPosition, savedRotation, () =>
            {
                IsOpen = false;
                if (panController != null) panController.enabled = true;
            });
        }

        private void Fly(Vector3 pos, Quaternion rot, TweenCallback done)
        {
            float seconds = config != null ? config.mapTransitionSeconds : 0.8f;
            flight?.Kill();
            flight = DOTween.Sequence().SetLink(gameObject)
                .Join(cameraRig.DOMove(pos, seconds).SetEase(Ease.InOutCubic))
                .Join(cameraRig.DORotateQuaternion(rot, seconds).SetEase(Ease.InOutCubic))
                .OnComplete(done);
        }

        private void ComputeMapPose(out Vector3 position, out Quaternion rotation)
        {
            var bounds = new Bounds();
            bool any = false;
            foreach (var m in markers)
            {
                if (m == null || m.WorldTarget == null) continue;
                if (!any) { bounds = new Bounds(m.WorldTarget.position, Vector3.zero); any = true; }
                else bounds.Encapsulate(m.WorldTarget.position);
            }
            if (rover != null) bounds.Encapsulate(rover.transform.position);

            float margin = config != null ? config.mapMargin : 5f;
            float pitch = config != null ? config.mapPitch : 80f;
            Camera cam = hud != null ? hud.ViewCamera : Camera.main;
            float vFov = (cam != null ? cam.fieldOfView : 30f) * Mathf.Deg2Rad;
            float aspect = cam != null ? Mathf.Max(0.5f, cam.aspect) : 16f / 9f;
            float halfZ = bounds.extents.z + margin;
            float halfX = bounds.extents.x + margin;
            float distZ = halfZ / Mathf.Tan(vFov * 0.5f);
            float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * aspect);
            float distX = halfX / Mathf.Tan(hFov * 0.5f);
            float distance = Mathf.Max(distZ, distX);

            rotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 center = new Vector3(bounds.center.x, 0f, bounds.center.z);
            position = center - rotation * Vector3.forward * distance;
        }

        private void RefreshMarkers()
        {
            if (rover == null || rover.Config == null) return;
            bool storm = weather != null && weather.TodayWeather == WeatherEventType.SolarStorm;
            foreach (var m in markers)
            {
                if (m == null) continue;
                if (!rover.Config.TryGetSite(m.SiteId, out var site)) { m.gameObject.SetActive(false); continue; }
                float chance = storm ? rover.Config.stormBreakdownChance : site.calmBreakdownChance;
                bool affordable = resources == null || resources.Has(ResourceType.Power, site.powerCost);
                m.Show(site.displayName, site.powerCost, site.waterReward, site.partsReward, Mathf.RoundToInt(chance * 100f), affordable);
            }
        }

        private void LateUpdate()
        {
            if (!IsOpen || mapRoot == null || !mapRoot.activeSelf || markerLayer == null) return;
            Camera cam = hud != null ? hud.ViewCamera : Camera.main;
            if (cam == null) return;
            float height = config != null ? config.markerHeight : 1.5f;
            foreach (var m in markers)
            {
                if (m == null || m.WorldTarget == null) continue;
                Vector3 screen = cam.WorldToScreenPoint(m.WorldTarget.position + Vector3.up * height);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(markerLayer, screen, null, out Vector2 local))
                    m.Rect.anchoredPosition = local;
            }
        }

        private void OnSiteClicked(string siteId)
        {
            if (rover == null) return;
            var reason = rover.CanDispatch(siteId);
            if (reason != ActionBlockReason.None)
            {
                if (hud != null) hud.ShowBlockedTip(reason);
                return;
            }
            if (rover.TryDispatchRover(siteId))
            {
                if (hud != null) hud.PlaySfx(hud.Config != null ? hud.Config.confirmClip : null);
                Close();
            }
        }
    }
}
