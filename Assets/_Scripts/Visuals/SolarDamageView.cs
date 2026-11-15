using System.Collections.Generic;
using ArtemisX.Simulation;
using DG.Tweening;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Exposed solar field visuals: panels fold down while Retracted, and storm damage drops lunar rocks on top of
    /// the panels with a blinking alert. Repair clears the rocks.
    /// </summary>
    public class SolarDamageView : MonoBehaviour
    {
        [SerializeField] private SolarFieldManager solar;
        [SerializeField] private GameplayVisualsConfig config;
        [SerializeField] private Transform[] panels = new Transform[0];
        [SerializeField] private GameObject[] rockPrefabs = new GameObject[0];
        [Tooltip("Blinking alert lamp shown while broken.")]
        [SerializeField] private GameObject alertLight;

        private readonly List<GameObject> rocks = new List<GameObject>();
        private Vector3[] panelScales;

        private void Awake()
        {
            panelScales = new Vector3[panels.Length];
            for (int i = 0; i < panels.Length; i++) panelScales[i] = panels[i] != null ? panels[i].localScale : Vector3.one;
            if (alertLight != null) alertLight.SetActive(false);
        }

        private void OnEnable()
        {
            if (solar == null) return;
            solar.SolarStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (solar == null) return;
            solar.SolarStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(SolarFieldState state)
        {
            bool retracted = state == SolarFieldState.Retracted;
            float scaleY = config != null ? config.retractedPanelScale : 0.35f;
            float seconds = config != null ? config.retractSeconds : 0.4f;
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                Vector3 target = panelScales[i];
                if (retracted) target.y *= scaleY;
                panels[i].DOKill();
                panels[i].DOScale(target, seconds).SetEase(Ease.InOutQuad).SetLink(panels[i].gameObject);
            }

            if (state == SolarFieldState.Broken) SpawnRocks();
            else ClearRocks();
            if (alertLight != null) alertLight.SetActive(state == SolarFieldState.Broken);
            if (state == SolarFieldState.Broken) EmoteBubble.Show(transform, EmoteKind.Alert, 3f, 2.5f);
        }

        private void SpawnRocks()
        {
            ClearRocks();
            if (rockPrefabs == null || rockPrefabs.Length == 0 || panels.Length == 0) return;
            int count = Mathf.Min(config != null ? config.damagedPanelCount : 4, panels.Length);
            Vector2 scaleRange = config != null ? config.rockScaleRange : new Vector2(0.65f, 0.9f);
            for (int i = 0; i < count; i++)
            {
                var panel = panels[(i * 2 + 1) % panels.Length];
                if (panel == null) continue;
                var r = panel.GetComponentInChildren<Renderer>();
                Bounds b = r != null ? r.bounds : new Bounds(panel.position, Vector3.one * 0.5f);
                var prefab = rockPrefabs[i % rockPrefabs.Length];
                if (prefab == null) continue;
                var rock = Instantiate(prefab, transform);
                rock.name = "StormRock_" + i;
                rock.transform.position = new Vector3(b.center.x, b.max.y, b.center.z);
                rock.transform.rotation = Quaternion.Euler(Random.Range(0f, 30f), Random.Range(0f, 360f), Random.Range(0f, 30f));
                float s = Random.Range(scaleRange.x, scaleRange.y);
                foreach (var c in rock.GetComponentsInChildren<Collider>()) Destroy(c);
                rock.transform.localScale = Vector3.zero;
                rock.transform.DOScale(Vector3.one * s, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.08f).SetLink(rock);
                rocks.Add(rock);
            }
        }

        private void ClearRocks()
        {
            foreach (var rock in rocks) if (rock != null) Destroy(rock);
            rocks.Clear();
        }
    }
}
