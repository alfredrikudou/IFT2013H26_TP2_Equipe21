using UnityEngine;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class PathGrid
    {
        private Transform _mapPlane;
        private float _defaultPlaneSize = 10f;
        private float _nodeSize = 1 / 10f;
        private LayerMask _wallLayer;

        public Node[,] Grid { get; private set; }
        private int _nNodeWidth, _nNodeHeight;
        private Vector3 _origin;

        public PathGrid(Transform mapPlane, float defaultPlaneSize, float nodeSize, LayerMask wallLayer)
        {
            _mapPlane     = mapPlane;
            _defaultPlaneSize = defaultPlaneSize;
            _nodeSize     = nodeSize;
            _wallLayer = wallLayer;
            BuildGrid();
        }

        public void BuildGrid()
        {
            float worldWidth = _mapPlane.localScale.x * _defaultPlaneSize;
            float worldHeight = _mapPlane.localScale.z * _defaultPlaneSize;

            _nNodeWidth = Mathf.RoundToInt(worldWidth / _nodeSize);
            _nNodeHeight = Mathf.RoundToInt(worldHeight / _nodeSize);
            Grid = new Node[_nNodeWidth, _nNodeHeight];

            _origin = _mapPlane.position - new Vector3(worldWidth * 0.5f, 0f, worldHeight * 0.5f); //bottom-left

            for (int x = 0; x < _nNodeWidth; x++)
                for (int z = 0; z < _nNodeHeight; z++)
                {
                    Vector3 worldPos = _origin + new Vector3(x * _nodeSize, 0f, z * _nodeSize);
                    bool walkable = !Physics.CheckSphere(worldPos, _nodeSize * 0.5f, _wallLayer);
                    Grid[x, z] = new Node(new Vector2Int(x, z), worldPos, walkable);
                }
        }

        public Node NodeFromWorldPoint(Vector3 worldPos)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((worldPos.x - _origin.x) / _nodeSize), 0, _nNodeWidth - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((worldPos.z - _origin.z) / _nodeSize), 0, _nNodeHeight - 1);
            return Grid[x, z];
        }

        public List<Node> GetNeighbours(Node node)
        {
            var neighbours = new List<Node>();
            for (int x = -1; x <= 1; x++)
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0) continue;
                    int nx = node.GridPos.x + x;
                    int nz = node.GridPos.y + z;
                    if (nx >= 0 && nx < _nNodeWidth && nz >= 0 && nz < _nNodeHeight)
                        neighbours.Add(Grid[nx, nz]);
                }

            return neighbours;
        }
    }
}