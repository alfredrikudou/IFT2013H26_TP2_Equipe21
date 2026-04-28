using System.Collections;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace Agents
{
    public class Animations : MonoBehaviour
    {
        private Transform _canonModel;
        private Transform _canon;

        private Transform _agentModel;

        private PlayerShooting _playerShooting;
        [SerializeField] private float _recoilDistance = 0.5f; 
        private readonly AnimationCurve _shootingAnimationCurve= new AnimationCurve(
            new Keyframe(0f, 1f, -4f, -4f),
            new Keyframe(0.2f, 0.6f),
            new Keyframe(0.5f, 1.2f),
            new Keyframe(1f, 1f)
        );
        [SerializeField] private float stretchFactor = 0.05f;
        [SerializeField] private float maxStretch = 2f;

        private bool _hasStartedCharging = false;

        private bool _playShootingAnimation = false;

        private Vector3 _lastPosition;
        private Vector3 _velocity;
        [SerializeField] private float scaleSmoothing = 0.05f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _agentModel = FindChildRecursive(transform, "Model");
            _canonModel = FindChildRecursive(transform, "CanonModel");
            _canon = FindChildRecursive(transform, "Canon");
            _playerShooting = GetComponent<PlayerShooting>();
            _lastPosition = transform.position;
            _velocity = Vector3.zero;

        }

        // Update is called once per frame
        void Update()
        {
            if (!_hasStartedCharging)
                _hasStartedCharging = _playerShooting.IsCharging;
            if (_hasStartedCharging && !_playerShooting.IsCharging)
            {
                _playShootingAnimation = true;
                _hasStartedCharging = false;
            }
            if(_playShootingAnimation)
            {
                StartCoroutine(ShootAnimation());
                _playShootingAnimation = false;
            }
            _velocity = (transform.position - _lastPosition) / Time.deltaTime;
            _lastPosition = transform.position;
            AnimateMovement(Time.deltaTime);
        }

        private void AnimateMovement(float dt)
        {
            float speed = _velocity.magnitude;

            Vector3 targetScale;
            if (speed < 0.01f)
            {
                targetScale = Vector3.one;
            }
            else
            {
                Vector3 dir = _velocity.normalized;
                float stretch = Mathf.Clamp(1f + speed * stretchFactor, 1f, maxStretch);
                float squash = 1f / Mathf.Sqrt(stretch);

                float scaleX = Mathf.Lerp(squash, stretch, dir.x * dir.x);
                float scaleZ = Mathf.Lerp(squash, stretch, dir.z * dir.z);
                targetScale = new Vector3(scaleX, 1f, scaleZ);
            }

            _agentModel.localScale = Vector3.Lerp(_agentModel.localScale, targetScale, dt / scaleSmoothing);
        }

        IEnumerator ShootAnimation()
        {
            float duration = _playerShooting.GetCooldown();
            float firstPhaseDuration = duration * 2.0f / 3;
            float secondPhaseDuration = duration - firstPhaseDuration;
            float time = 0f;

            while (time < firstPhaseDuration)
            {
                float t = time / firstPhaseDuration;
                Vector3 startPos = _canon.localPosition;
                Vector3 backPos = startPos - _canon.forward * -_recoilDistance;

                _canonModel.localPosition = Vector3.Lerp(startPos, backPos, EaseOutElastic(t));

                time += Time.deltaTime;
                yield return null;
            }

            time = 0f;
            while (time < secondPhaseDuration)
            {
                float t = time / secondPhaseDuration;

                Vector3 startPos = _canon.localPosition;
                Vector3 backPos = startPos - _canon.forward * -_recoilDistance;
                _canonModel.localPosition = Vector3.Lerp(backPos, startPos, t);

                time += Time.deltaTime;
                yield return null;
            }
        }
        private float EaseOutElastic(float t)
        {
            float c4 = (2f * Mathf.PI) / 3f;
            float elastic = t == 0f ? 0f :
                Mathf.Approximately(t, 1f) ? 1f :
                Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
            return elastic;
        }
        private Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                var result = FindChildRecursive(child, childName);
                if (result != null)
                    return result;
            }
            return null;
        }
    }
}
