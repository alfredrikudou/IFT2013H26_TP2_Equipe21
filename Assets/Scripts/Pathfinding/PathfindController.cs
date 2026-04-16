using System.Collections.Generic;
using MapGeneration;
using UnityEngine;

namespace Pathfinding
{
    public class PathfindController : MonoBehaviour
    {
        public Pathfinder Pathfinder { get; private set; }

        private void Start()
        {
            TerrainGenerator terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
            Pathfinder = new Pathfinder(new PathGrid(terrainGenerator.GetMap()));
        }

        public List<Vector3> GetPath(Vector3 from, Vector3 to)
        {
            return Pathfinder.FindPath(from, to);
        }
    }
}