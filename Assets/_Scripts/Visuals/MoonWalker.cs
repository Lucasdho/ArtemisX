using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Model-agnostic low-gravity walker. Put it on an empty root, drop any rigged model under it as the
    /// <see cref="visual"/> child, and give that model an Animator whose controller has the state names
    /// listed in the VisualsConfig (Idle, Walk and the action states). No code changes are needed to swap the model.
    /// </summary>
    public class MoonWalker : MonoBehaviour
    {
        private enum State { Walking, Pausing, Sheltered }

        [SerializeField] private VisualsConfig config;
        [SerializeField] private WaypointPath path;
        [SerializeField] private int startIndex;
        [SerializeField] private bool reverse;
        [Tooltip("Child that receives the hop offset and the landing squash. Usually the model root.")]
        [SerializeField] private Transform visual;
        [Tooltip("Optional. Found in children when left empty.")]
        [SerializeField] private Animator animator;
        [SerializeField] private ParticleSystem landingDust;

        private State state;
        private int targetIndex;
        private Vector3 target;
        private bool hasShelter;
        private Vector3 shelterPoint;
        private float hopClock;
        private bool airborne;
        private float pauseTimer;
        private Vector3 visualBasePosition;
        private Vector3 visualBaseScale;
        private string currentAnimation;

        /// <summary>Scales walking speed. AmbientMood lowers it at night and raises it in a storm.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        public bool IsSheltered => state == State.Sheltered;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null)
            {
                visualBasePosition = visual.localPosition;
                visualBaseScale = visual.localScale;
            }
        }

        private void Start()
        {
            if (config == null || path == null || path.Count < 2)
            {
                Debug.LogError("MoonWalker needs a VisualsConfig and a WaypointPath with at least 2 nodes.", this);
                enabled = false;
                return;
            }

            targetIndex = path.Wrap(startIndex);
            transform.position = path.GetPoint(targetIndex);
            hopClock = Random.value * config.hopAirSeconds;
            NextWaypoint();
        }

        /// <summary>Sends the walker to a point (e.g. the habitat door) where it waits until <see cref="LeaveShelter"/>.</summary>
        public void GoToShelter(Vector3 point)
        {
            hasShelter = true;
            shelterPoint = point;
            target = point;
            if (state != State.Walking) BeginWalking();
        }

        public void LeaveShelter()
        {
            if (!hasShelter) return;
            hasShelter = false;
            targetIndex = path.NearestIndex(transform.position);
            target = path.GetPoint(targetIndex);
            BeginWalking();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (state)
            {
                case State.Walking:
                    Walk(dt);
                    break;
                case State.Pausing:
                    pauseTimer -= dt;
                    if (pauseTimer <= 0f) NextWaypoint();
                    break;
            }
        }

        private void Walk(float dt)
        {
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            bool arrived = toTarget.magnitude <= config.arriveDistance;

            // Hop cycle: a long floaty arc, then a short squashy ground contact.
            float cycle = config.hopAirSeconds + config.hopGroundSeconds;
            hopClock += dt * Mathf.Lerp(1f, SpeedMultiplier, 0.5f);
            bool wasAirborne = airborne;
            if (hopClock >= cycle)
            {
                hopClock -= cycle;
                if (arrived) { Land(); Arrive(); return; }
            }
            airborne = hopClock < config.hopAirSeconds;
            if (wasAirborne && !airborne) Land();

            if (!arrived)
            {
                float speed = config.walkSpeed * SpeedMultiplier * (airborne ? 1f : config.groundSpeedFactor);
                Vector3 step = Vector3.ClampMagnitude(toTarget, speed * dt);
                transform.position += step;
                Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, config.walkerTurnDegreesPerSecond * dt);
            }

            ApplyHopPose();
        }

        private void ApplyHopPose()
        {
            if (visual == null) return;
            float height = 0f;
            Vector3 squash = Vector3.one;
            if (airborne)
            {
                float t = hopClock / config.hopAirSeconds;
                height = config.hopHeight * 4f * t * (1f - t);
                // Slight stretch on the way up sells the floatiness.
                float stretch = config.squashAmount * 0.35f * Mathf.Sin(t * Mathf.PI);
                squash = new Vector3(1f - stretch * 0.5f, 1f + stretch, 1f - stretch * 0.5f);
            }
            else
            {
                float g = (hopClock - config.hopAirSeconds) / config.hopGroundSeconds;
                float amount = config.squashAmount * Mathf.Sin(g * Mathf.PI);
                squash = new Vector3(1f + amount * 0.5f, 1f - amount, 1f + amount * 0.5f);
            }
            visual.localPosition = visualBasePosition + Vector3.up * height;
            visual.localScale = Vector3.Scale(visualBaseScale, squash);
        }

        private void ResetPose()
        {
            if (visual == null) return;
            visual.localPosition = visualBasePosition;
            visual.localScale = visualBaseScale;
        }

        private void Land()
        {
            if (landingDust != null && config.landingDustCount > 0) landingDust.Emit(config.landingDustCount);
        }

        private void Arrive()
        {
            airborne = false;
            hopClock = config.hopAirSeconds;
            ResetPose();

            if (hasShelter)
            {
                state = State.Sheltered;
                Play(config.idleState);
                return;
            }

            if (Random.value < config.pauseChance)
            {
                state = State.Pausing;
                bool act = config.actionStates.Length > 0 && Random.value < 0.7f;
                if (act)
                {
                    string action = config.actionStates[Random.Range(0, config.actionStates.Length)];
                    pauseTimer = Play(action);
                }
                else
                {
                    Play(config.idleState);
                    pauseTimer = Random.Range(config.pauseSeconds.x, config.pauseSeconds.y);
                }
                return;
            }

            NextWaypoint();
        }

        private void NextWaypoint()
        {
            targetIndex = path.Wrap(targetIndex + (reverse ? -1 : 1));
            target = path.GetPoint(targetIndex);
            BeginWalking();
        }

        private void BeginWalking()
        {
            state = State.Walking;
            Play(config.walkState);
        }

        /// <summary>Cross-fades to an Animator state by name and returns its length. Safe when the model has no such state.</summary>
        private float Play(string stateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName)) return config.fallbackActionSeconds;
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash)) return config.fallbackActionSeconds;
            if (currentAnimation != stateName || stateName != config.walkState)
            {
                animator.CrossFadeInFixedTime(hash, config.animationCrossFade, 0, 0f);
                currentAnimation = stateName;
            }
            return ClipLength(stateName);
        }

        private float ClipLength(string stateName)
        {
            RuntimeAnimatorController controller = animator.runtimeAnimatorController;
            if (controller != null)
            {
                AnimationClip[] clips = controller.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    // Convention: the state is named after its clip, or the clip name starts with the state name.
                    if (clips[i] != null && clips[i].name.StartsWith(stateName)) return clips[i].length;
                }
            }
            return config.fallbackActionSeconds;
        }
    }
}
