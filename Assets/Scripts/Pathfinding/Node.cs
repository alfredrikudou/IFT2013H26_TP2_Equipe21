using UnityEngine;

namespace Pathfinding
{
    public class Node
    {
        public Vector2Int GridPos; // grid indices
        public Vector3 WorldPos; // actual point in space
        public bool Walkable; // can be traversed by the ai

        public float GCost; // distance from start
        public float HCost; // heuristic cost to reach the destination
        public float FCost => GCost + HCost;
        public Node Parent;

        public Node(Vector2Int gridPos, Vector3 worldPos, bool walkable)
        {
            GridPos = gridPos;
            WorldPos = worldPos;
            Walkable = walkable;
            GCost = float.MaxValue;
            HCost = 0f;
        }
    }
}
