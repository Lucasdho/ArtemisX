using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Slowly rotates an antenna or radar dish around a local axis.</summary>
    public class SlowSpin : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private Vector3 localAxis = Vector3.up;
        [SerializeField] private float speedScale = 1f;

        private void Update()
        {
            if (config == null) return;
            transform.Rotate(localAxis, config.antennaDegreesPerSecond * speedScale * Time.deltaTime, Space.Self);
        }
    }
}
