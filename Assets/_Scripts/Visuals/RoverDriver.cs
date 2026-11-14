using System.Collections.Generic;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Drives a rover around a closed waypoint loop with smooth turns, stops and a little suspension wobble.</summary>
    public class RoverDriver : MonoBehaviour
    {
        private const int SamplesPerSegment = 14;

        [SerializeField] private VisualsConfig config;
        [SerializeField] private WaypointPath path;
        [Tooltip("Child that receives the bounce and wobble. The root only moves along the path.")]
        [SerializeField] private Transform body;
        [SerializeField] private ParticleSystem wheelDust;

        private readonly List<Vector3> samples = new List<Vector3>();
        private readonly List<float> distances = new List<float>();
        private float loopLength;
        private float travelled;
        private float speed;
        private float stopTimer;
        private int nextNode = 1;
        private int segment;
        private Vector3 bodyBasePosition;
        private Quaternion bodyBaseRotation;
        private float noiseSeed;

        private void Start()
        {
            if (config == null || path == null || path.Count < 3)
            {
                Debug.LogError("RoverDriver needs a VisualsConfig and a WaypointPath with at least 3 nodes.", this);
                enabled = false;
                return;
            }

            path.BuildSmoothLoop(SamplesPerSegment, samples, distances);
            loopLength = distances[distances.Count - 1];
            if (body != null)
            {
                bodyBasePosition = body.localPosition;
                bodyBaseRotation = body.localRotation;
            }
            noiseSeed = Random.value * 100f;
            transform.position = samples[0];
            transform.rotation = Quaternion.LookRotation(Flat(samples[1] - samples[0]), Vector3.up);
            stopTimer = path.GetStopSeconds(0);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float previousSpeed = speed;

            if (stopTimer > 0f)
            {
                stopTimer -= dt;
                speed = 0f;
            }
            else
            {
                Advance(dt);
            }

            ApplyBody(dt > 0f ? (speed - previousSpeed) / dt : 0f);
            SetDust(speed > config.roverSpeed * 0.2f);
        }

        private void Advance(float dt)
        {
            float nodeDistance = NodeDistance(nextNode);
            float remaining = nodeDistance - travelled;
            float nextStop = path.GetStopSeconds(nextNode);

            float targetSpeed = config.roverSpeed;
            if (nextStop > 0f)
            {
                // Brake so the rover reaches the stop node at rest.
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(2f * config.roverAcceleration * Mathf.Max(remaining, 0f)) + 0.05f);
            }
            speed = Mathf.MoveTowards(speed, targetSpeed, config.roverAcceleration * dt);
            travelled += speed * dt;

            if (travelled >= nodeDistance)
            {
                if (nextStop > 0f)
                {
                    travelled = nodeDistance;
                    stopTimer = nextStop;
                    speed = 0f;
                }
                nextNode++;
                if (nextNode > path.Count)
                {
                    nextNode = 1;
                    travelled -= loopLength;
                    segment = 0;
                }
            }

            Vector3 position = Sample(Mathf.Max(travelled, 0f), out Vector3 tangent);
            transform.position = position;
            tangent = Flat(tangent);
            if (tangent.sqrMagnitude > 0.0001f)
            {
                Quaternion look = Quaternion.LookRotation(tangent, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-config.roverTurnSharpness * dt));
            }
        }

        private float NodeDistance(int node) => node >= path.Count ? loopLength : distances[node * SamplesPerSegment];

        private Vector3 Sample(float distance, out Vector3 tangent)
        {
            while (segment < samples.Count - 2 && distances[segment + 1] < distance) segment++;
            while (segment > 0 && distances[segment] > distance) segment--;
            float span = distances[segment + 1] - distances[segment];
            float t = span > 0f ? (distance - distances[segment]) / span : 0f;
            tangent = samples[segment + 1] - samples[segment];
            return Vector3.Lerp(samples[segment], samples[segment + 1], t);
        }

        private void ApplyBody(float acceleration)
        {
            if (body == null) return;
            float ratio = Mathf.Clamp01(speed / config.roverSpeed);
            float t = Time.time * config.roverBounceFrequency + noiseSeed;
            float bounce = (Mathf.PerlinNoise(t, 0.3f) - 0.5f) * 2f * config.roverBounceHeight * ratio;
            float roll = (Mathf.PerlinNoise(0.7f, t * 0.8f) - 0.5f) * 2f * config.roverWobbleDegrees * ratio;
            float pitch = (Mathf.PerlinNoise(t * 0.6f, 5.1f) - 0.5f) * 2f * config.roverWobbleDegrees * ratio;
            pitch += Mathf.Clamp(-acceleration / config.roverAcceleration, -1f, 1f) * config.roverBrakePitchDegrees;

            body.localPosition = bodyBasePosition + Vector3.up * bounce;
            Quaternion target = bodyBaseRotation * Quaternion.Euler(pitch, 0f, roll);
            body.localRotation = Quaternion.Slerp(body.localRotation, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        private void SetDust(bool on)
        {
            if (wheelDust == null) return;
            ParticleSystem.EmissionModule emission = wheelDust.emission;
            if (emission.enabled != on) emission.enabled = on;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}
