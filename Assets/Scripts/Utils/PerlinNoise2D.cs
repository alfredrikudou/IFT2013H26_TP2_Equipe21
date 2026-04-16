using UnityEngine;

namespace Utils
{
    public class PerlinNoise2D
    {
        private readonly float _seedX;
        private readonly float _seedY;
        private readonly float _scale;
        private readonly float _amplification;
        public PerlinNoise2D(float scale, float amplification)
        {
            _seedX = Random.value * 99999f;
            _seedY = Random.value * 99999f;
            _scale = scale;
            _amplification = amplification;
        }
        public float GetHeight(float x, float z) => Mathf.PerlinNoise(_seedX + x * _scale, _seedY + z * _scale) * _amplification;
        public float GetHeight(Vector2 v) => GetHeight(v.x, v.y);
        public float GetHeight(Vector3 v) => GetHeight(v.x, v.z);
    }
}
