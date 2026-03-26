// using System.Collections;
// using UnityEngine;
//
// namespace Player
// {
//     public class AiController : Player
//     {
//         [Header("IA (si contrôle ordinateur)")]
//         [SerializeField] private float _aiAimToleranceDegrees = 10f;
//         [SerializeField] private float _aiMaxAimSeconds = 4f;
//         [SerializeField] private float _aiChargeMin = 0.72f;
//         [SerializeField] private float _aiChargeMax = 1f;
//         [Tooltip("Distance horizontale max pour commencer à charger / tirer. Au-delà, l’IA avance vers la cible (portée « confortable »).")]
//         [SerializeField] private float _aiComfortShotRange = 14f;
//         [Tooltip("Dès que la cible est plus loin que ça (horizontal), l’IA marche vers elle (même proche du joueur).")]
//         [SerializeField] private float _aiAlwaysMoveIfFartherThan = 2.5f;
//         [Tooltip("Petit déplacement latéral pour ne pas rester statique quand la cible est à portée.")]
//         [SerializeField] private float _aiStrafeAmplitude = 0.35f;
//         
//         private enum AiPhase { Idle, Aim, Charge }
//         private AiPhase _aiPhase = AiPhase.Idle;
//         private Player _aiTarget;
//         private float _aiChargeGoal;
//         private bool _aiNoTargetShotDone;
//         private float _aiAimStartTime;
//         
//         private void Start()
//         {
//             StartCoroutine(RunBehaviour());
//         }
//
//         private IEnumerator RunBehaviour()
//         {
//             while (true)
//             {
//                 // Select target
//                 PickTarget();
//                 yield return new WaitForSeconds(_targetSelectDelay);
//
//                 // Follow target
//                 while (!IsCloseEnough())
//                 {
//                     MoveTowardsTarget();
//                     yield return null;
//                 }
//
//                 // Aim
//                 yield return null;
//
//                 // Charge
//                 yield return null;
//
//                 // Fire
//             }
//         }
//         
//         private bool AiIsAimedAtTarget()
//         {
//             Vector3 aimPoint = _aiTarget.transform.position + Vector3.up * 0.5f;
//             Vector3 desired = (aimPoint - _firePoint.position).normalized;
//             return Vector3.Angle(_firePoint.forward, desired) <= _aiAimToleranceDegrees;
//         }
//         
//         private void PickAiTarget()
//         {
//             _aiTarget = null;
//             float best = float.MaxValue;
//             foreach (var p in FindObjectsOfType<Player>(false))
//             {
//                 if (p == this || p.IsDead) continue;
//                 float d = (p.transform.position - transform.position).sqrMagnitude;
//                 if (d < best)
//                 {
//                     best = d;
//                     _aiTarget = p;
//                 }
//             }
//         }
//
//         /// <summary>IA : priorité au plus proche (meilleure chance de toucher), puis visée + tir chargé.</summary>
//         private void RunComputerTurn()
//         {
//             if (_projectilePrefab == null || _cannon == null)
//                 return;
//
//             EnsureFirePoint();
//             if (_firePoint == null) return;
//
//             if (_aiTarget == null || _aiTarget.IsDead)
//                 PickAiTarget();
//
//             if (_aiTarget == null)
//             {
//                 _moveInput = Vector2.zero;
//                 if (!_aiNoTargetShotDone)
//                 {
//                     _aiNoTargetShotDone = true;
//                     FireShot(0.35f);
//                 }
//                 return;
//             }
//
//             switch (_aiPhase)
//             {
//                 case AiPhase.Aim:
//                     AiAimAtTarget();
//                     Vector3 flat = _aiTarget.transform.position - transform.position;
//                     flat.y = 0f;
//                     float distH = flat.magnitude;
//                     if (distH > 0.01f)
//                         flat /= distH;
//
//                     // Marche vers la cible si un peu loin ; strafe léger si déjà à portée pour ne pas rester figé.
//                     if (distH > _aiAlwaysMoveIfFartherThan)
//                         _moveInput = new Vector2(flat.x, flat.z);
//                     else if (_aiStrafeAmplitude > 0f)
//                     {
//                         Vector3 side = Vector3.Cross(Vector3.up, flat);
//                         if (side.sqrMagnitude > 0.0001f)
//                         {
//                             side.Normalize();
//                             float w = Mathf.Sin(Time.time * 2.1f) * _aiStrafeAmplitude;
//                             _moveInput = new Vector2(side.x, side.z) * w;
//                         }
//                         else
//                             _moveInput = Vector2.zero;
//                     }
//                     else
//                         _moveInput = Vector2.zero;
//
//                     bool aimReady = Time.time - _aiAimStartTime >= _aiMaxAimSeconds || AiIsAimedAtTarget();
//                     bool inComfortRange = distH <= _aiComfortShotRange;
//                     bool desperateShot = Time.time - _aiAimStartTime >= _aiMaxAimSeconds * 1.75f;
//                     if (aimReady && (inComfortRange || desperateShot))
//                         _aiPhase = AiPhase.Charge;
//                     break;
//
//                 case AiPhase.Charge:
//                     _moveInput = Vector2.zero;
//                     _charging = true;
//                     _charge01 += Time.deltaTime / Mathf.Max(0.01f, _chargeSecondsToMax);
//                     _charge01 = Mathf.Clamp01(_charge01);
//                     UpdatePowerUI();
//                     if (_charge01 >= _aiChargeGoal)
//                     {
//                         FireShot(_charge01);
//                         _charging = false;
//                         _charge01 = 0f;
//                         UpdatePowerUI();
//                         _aiPhase = AiPhase.Idle;
//                     }
//                     break;
//
//                 default:
//                     _moveInput = Vector2.zero;
//                     break;
//             }
//         }
//
//         /// <summary>Même convention que le joueur : Euler(pitch, yaw, 0) sur le canon (évite un blocage si LookRotation ≠ axe du mesh).</summary>
//         private void AiAimAtTarget()
//         {
//             Vector3 aimPoint = _aiTarget.transform.position + Vector3.up * 0.5f;
//             Vector3 dir = (aimPoint - _firePoint.position).normalized;
//             Transform parent = _cannon.parent != null ? _cannon.parent : transform;
//             Vector3 localDir = parent.InverseTransformDirection(dir);
//             float targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
//             float horiz = Mathf.Sqrt(localDir.x * localDir.x + localDir.z * localDir.z);
//             float targetPitch = -Mathf.Atan2(localDir.y, Mathf.Max(0.0001f, horiz)) * Mathf.Rad2Deg;
//             targetPitch = Mathf.Clamp(targetPitch, _pitchMin, _pitchMax);
//
//             float dt = Time.deltaTime;
//             _aimYaw = Mathf.MoveTowardsAngle(_aimYaw, targetYaw, _aimSpeed * dt);
//             _aimPitch = Mathf.MoveTowardsAngle(_aimPitch, targetPitch, _aimSpeed * dt);
//             _aimPitch = Mathf.Clamp(_aimPitch, _pitchMin, _pitchMax);
//             _cannon.localRotation = Quaternion.Euler(_aimPitch, _aimYaw, 0f);
//         }
//
//         
//     }
// }
