using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Agents
{
    public class Animations : MonoBehaviour
    {
        private Transform _canonModel;
        private Transform _canon;
        private Transform _agentModel;
        private PlayerShooting _playerShooting;
        private Rigidbody _rigidbody;

        [SerializeField] private float recoilDistance = 0.5f;
        [SerializeField] private float stretchFactor = 0.05f;
        [SerializeField] private float maxStretch = 2f;
        [SerializeField] private float scaleSmoothing = 0.05f;

        private bool _hasStartedCharging = false;
        private bool _playShootingAnimation = false;

        void Start()
        {
            _agentModel = FindChildRecursive(transform, "Model");
            _canonModel = FindChildRecursive(transform, "CanonModel");
            _canon = FindChildRecursive(transform, "Canon");
            _playerShooting = GetComponent<PlayerShooting>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (!_hasStartedCharging)
                _hasStartedCharging = _playerShooting.IsCharging;
            if (_hasStartedCharging && !_playerShooting.IsCharging)
            {
                _playShootingAnimation = true;
                _hasStartedCharging = false;
            }
            if (_playShootingAnimation)
            {
                StartCoroutine(ShootAnimation());
                _playShootingAnimation = false;
            }

            AnimateMovement(Time.deltaTime);
        }

        private void AnimateMovement(float dt)
        {
            Vector3 velocity = _rigidbody.linearVelocity;
            float speed = velocity.magnitude;

            Vector3 targetScale;
            if (speed < 0.01f)
            {
                targetScale = Vector3.one;
            }
            else
            {
                Vector3 dir = velocity.normalized;
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
                Vector3 backPos = startPos - _canon.forward * -recoilDistance;
                _canonModel.localPosition = Vector3.Lerp(startPos, backPos, EaseOutElastic(t));
                time += Time.deltaTime;
                yield return null;
            }

            time = 0f;
            while (time < secondPhaseDuration)
            {
                float t = time / secondPhaseDuration;
                Vector3 startPos = _canon.localPosition;
                Vector3 backPos = startPos - _canon.forward * -recoilDistance;
                _canonModel.localPosition = Vector3.Lerp(backPos, startPos, t);
                time += Time.deltaTime;
                yield return null;
            }
        }

        private float EaseOutElastic(float t)
        {
            float c4 = (2f * Mathf.PI) / 3f;
            return t == 0f ? 0f :
                Mathf.Approximately(t, 1f) ? 1f :
                Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
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