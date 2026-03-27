using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class PathfindController : MonoBehaviour
    {
        [SerializeField] private Transform _mapPlane;
        [SerializeField] private float _defaultPlaneSize = 10f;
        [SerializeField] private float _nodeSize = 1f;
        [SerializeField] private LayerMask _wallLayer;
        
        public Pathfinder Pathfinder { get; private set; }

        private void Awake()
        {
            var grid  = new PathGrid(_mapPlane, _defaultPlaneSize, _nodeSize, _wallLayer);
            Pathfinder = new Pathfinder(grid);
        }

        public List<Vector3> GetPath(Vector3 from, Vector3 to)
        {
            return Pathfinder.FindPath(from, to);
        }
    }
}
