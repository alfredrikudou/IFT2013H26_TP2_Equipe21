using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class PathfindController : MonoBehaviour
    {
        [SerializeField] private Transform _mapPlane;
        [SerializeField] private float _defaultPlaneSize = 10f;
        [SerializeField] private float _nodeSize = 1f;
        [SerializeField] private GameObject _walkerPrefab;
        [SerializeField] private LayerMask _wallLayer;

        public Pathfinder Pathfinder { get; private set; }

        private void Awake()
        {
            var capsule = _walkerPrefab.GetComponent<CapsuleCollider>();
            var sphere = _walkerPrefab.GetComponent<SphereCollider>();
            var box = _walkerPrefab.GetComponent<BoxCollider>();
            var col = _walkerPrefab.GetComponentInChildren<Collider>();

            var walkerRadius = capsule != null ? capsule.radius :
                sphere != null ? sphere.radius :
                box != null ? Mathf.Min(box.size.x, box.size.z) * 0.5f :
                col != null && col is CapsuleCollider cc ? cc.radius :
                col != null && col is SphereCollider sc ? sc.radius :
                0.5f;
            var grid = new PathGrid(_mapPlane, _defaultPlaneSize, _nodeSize, walkerRadius, _wallLayer);
            Pathfinder = new Pathfinder(grid);
        }

        public List<Vector3> GetPath(Vector3 from, Vector3 to)
        {
            return Pathfinder.FindPath(from, to);
        }
    }
}