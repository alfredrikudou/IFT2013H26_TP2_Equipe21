using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEngine;

namespace Agents
{
    public class AiController : Agent
    {
        [Header("Pathfinding")]
        [SerializeField] private GameObject pathfindingObject;
        [SerializeField] private string pathfindingSingletonName = "PathfindController";

        [Header("Mouvement")]
        [SerializeField] private float _arrivalThreshold = 0.45f;
        [SerializeField] private float _timeBetweenPathUpdates = 1.25f;
        [SerializeField] private float _targetSelectDelay = 0.35f;
        [SerializeField] private bool useHorizontalVelocityForMovement = true;
        [Tooltip("Lissage de la vélocité horizontale (évite les à-coups).")]
        [SerializeField] private float velocitySmoothTime = 0.14f;
        [Tooltip("Rotation douce du corps vers la direction de marche (XZ).")]
        [SerializeField] private float bodyTurnSpeed = 280f;

        [Header("Combat")]
        [SerializeField] private float _aiAimToleranceDegrees = 10f;
        [SerializeField] private float _aiMaxAimSeconds = 4f;
        [SerializeField] private float _aiChargeMin = 0.72f;
        [SerializeField] private float _aiChargeMax = 1f;
        [SerializeField] private float _aiComfortShotRange = 14f;

        private PathfindController _pathfinder;

        private Vector3[] _path = System.Array.Empty<Vector3>();
        private int _pathIndex;
        private bool _targetReached;
        private Vector3 _currentTargetPosition;
        private float _lastPathUpdateTime;
        private Vector3 _smoothVelocityXZ;
        private float _targetPickPhase;
        private AgentVisibilityState _visibilityState;

        protected override void Awake()
        {
            base.Awake();
            _visibilityState = GetComponent<AgentVisibilityState>();
            ResolvePathfinder();
        }

        private void ResolvePathfinder()
        {
            if (pathfindingObject != null)
            {
                _pathfinder = pathfindingObject.GetComponent<PathfindController>()
                              ?? pathfindingObject.GetComponentInChildren<PathfindController>(true);
            }

            if (_pathfinder == null && !string.IsNullOrWhiteSpace(pathfindingSingletonName))
            {
                foreach (var pc in FindObjectsByType<PathfindController>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (pc != null && pc.gameObject.name == pathfindingSingletonName)
                    {
                        _pathfinder = pc;
                        break;
                    }
                }
            }

            if (_pathfinder == null)
                _pathfinder = FindFirstObjectByType<PathfindController>();

            if (_pathfinder == null)
                Debug.LogError($"[AiController] {name} : aucun PathfindController — l’IA ne pourra pas calculer de chemin.");
        }

        private void Start()
        {
            StartCoroutine(RunBehaviour());
        }

        protected override void FixedUpdate()
        {
            if (GamePauseState.IsPaused || (GameManager.Instance != null && GameManager.Instance.IsMatchOver))
            {
                StopMovement();
                _smoothVelocityXZ = Vector3.zero;
                return;
            }

            FollowPath();
        }

        private IEnumerator RunBehaviour()
        {
            while (true)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                    yield break;

                while (!TryPickTarget())
                {
                    if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                        yield break;
                    yield return null;
                }

                yield return DelayWhilePlaying(_targetSelectDelay);
                if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                    yield break;

                // Ne s’arrêter que dans la « zone de tir » : la LOS seule (très loin) ne doit pas figer l’IA
                // (sinon le 4e slot, souvent excentré, reste planté en visée impossible).
                while (!IsCloseEnough())
                {
                    if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                        yield break;
                    float pathInterval = _timeBetweenPathUpdates;
                    if (_visibilityState != null && _visibilityState.IsLogicallyCulled)
                        pathInterval *= 4f;
                    if (_pathfinder != null &&
                        (_targetReached || Time.time - _lastPathUpdateTime > pathInterval))
                        UpdatePath();
                    yield return null;
                }

                float aimDeadline = Time.time + _aiMaxAimSeconds;
                while (Time.time < aimDeadline && !IsAimedAtTarget())
                {
                    if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                        yield break;
                    AimAtTarget();
                    yield return null;
                }

                float chargeGoal = Random.Range(_aiChargeMin, _aiChargeMax);
                yield return DelayWhilePlaying(chargeGoal);
                if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
                    yield break;

                FireShot(Mathf.Clamp01(chargeGoal));
            }
        }

        private static IEnumerator DelayWhilePlaying(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                if (!GamePauseState.IsPaused)
                    t += Time.deltaTime;
                yield return null;
            }
        }

        private void FollowPath()
        {
            if (_pathfinder == null || _targetReached || _path == null || _path.Length == 0 ||
                _pathIndex >= _path.Length)
            {
                ApplySmoothedHorizontalVelocity(Vector3.zero);
                return;
            }

            Vector3 waypoint = _path[_pathIndex];
            Vector3 self = transform.position;
            Vector3 flat = waypoint - self;
            flat.y = 0f;
            float distH = flat.magnitude;

            if (distH <= _arrivalThreshold)
            {
                _pathIndex++;
                if (_pathIndex >= _path.Length)
                    ApplySmoothedHorizontalVelocity(Vector3.zero);
                return;
            }

            flat /= distH;
            Vector3 desired = flat * moveSpeed;
            ApplySmoothedHorizontalVelocity(desired);

            if (bodyTurnSpeed > 1f && flat.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flat, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot,
                    bodyTurnSpeed * Time.fixedDeltaTime);
            }
        }

        private void ApplySmoothedHorizontalVelocity(Vector3 targetXZ)
        {
            if (!useHorizontalVelocityForMovement)
            {
                _rb.linearVelocity = new Vector3(targetXZ.x, _rb.linearVelocity.y, targetXZ.z);
                return;
            }

            Vector3 current = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 smoothed = Vector3.SmoothDamp(current, targetXZ, ref _smoothVelocityXZ, velocitySmoothTime,
                moveSpeed * 2f, Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector3(smoothed.x, _rb.linearVelocity.y, smoothed.z);
        }

        private bool TryPickTarget()
        {
            if (GameManager.Instance == null) return false;

            var positions = GameManager.Instance.GetOtherAliveAgentPositions(this);
            if (positions.Count == 0) return false;

            _currentTargetPosition = PickTargetPosition(positions);

            _targetReached = (_currentTargetPosition - transform.position).sqrMagnitude
                             < _arrivalThreshold * _arrivalThreshold;
            UpdatePath();
            return true;
        }

        /// <summary>
        /// Évite que toutes les IA visent toujours le même adversaire : mélange slot, temps et hasard.
        /// </summary>
        private Vector3 PickTargetPosition(List<Vector3> positions)
        {
            _targetPickPhase += Time.deltaTime * 0.22f + SlotIndex * 0.07f;
            var sorted = positions.OrderBy(p => HorizontalSqrDist(transform.position, p)).ToList();

            Vector3? losPreferred = null;
            foreach (var p in sorted)
            {
                if (HasLineOfSight(p) && HorizontalSqrDist(transform.position, p) <=
                    _aiComfortShotRange * _aiComfortShotRange)
                {
                    losPreferred = p;
                    break;
                }
            }

            if (losPreferred.HasValue && Random.value < 0.35f)
                return losPreferred.Value;

            int n = sorted.Count;
            int offset = (Mathf.Abs(SlotIndex) + Mathf.FloorToInt(_targetPickPhase)) % n;
            if (Random.value < 0.45f && n >= 2)
            {
                int second = (offset + 1) % n;
                return Random.value < 0.5f ? sorted[offset] : sorted[second];
            }

            return sorted[offset];
        }

        private static float HorizontalSqrDist(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void UpdatePath()
        {
            if (_pathfinder == null) return;

            _lastPathUpdateTime = Time.time;
            _pathIndex = 0;
            _targetReached = false;
            var path = _pathfinder.GetPath(transform.position, _currentTargetPosition);
            _path = path != null && path.Count > 0 ? path.ToArray() : System.Array.Empty<Vector3>();
        }

        private bool IsCloseEnough()
        {
            float r = _aiComfortShotRange;
            return HorizontalSqrDist(transform.position, _currentTargetPosition) <= r * r;
        }

        private bool HasLineOfSight(Vector3 target)
        {
            Vector3 origin = transform.position + Vector3.up * 0.55f;
            Vector3 direction = target + Vector3.up * 0.5f - origin;
            LayerMask mask = LayerMask.GetMask("Obstacle", "Wall");
            return !Physics.Raycast(origin, direction.normalized, direction.magnitude, mask,
                QueryTriggerInteraction.Ignore);
        }

        private void AimAtTarget()
        {
            if (Cannon == null || FirePoint == null) return;

            Vector3 aimPoint = _currentTargetPosition + Vector3.up * 0.5f;
            Vector3 dir = (aimPoint - FirePoint.position).normalized;
            Transform parent = Cannon.parent != null ? Cannon.parent : transform;
            Vector3 localDir = parent.InverseTransformDirection(dir);

            float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            float horiz = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
            float targetPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.0001f, horiz)) * Mathf.Rad2Deg;

            AimTowards(targetYaw, targetPitch);
        }

        private bool IsAimedAtTarget()
        {
            if (FirePoint == null) return false;
            Vector3 desired = (_currentTargetPosition + Vector3.up * 0.5f - FirePoint.position).normalized;
            return Vector3.Angle(FirePoint.forward, desired) <= _aiAimToleranceDegrees;
        }
    }
}
