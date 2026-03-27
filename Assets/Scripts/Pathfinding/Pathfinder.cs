using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pathfinding
{
    public class Pathfinder
    {
        private PathGrid _grid;
        
        public Pathfinder(PathGrid grid)
        {
            _grid = grid;
        }

        public List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
        {
            Node start = _grid.NodeFromWorldPoint(startWorld);
            Node target = _grid.NodeFromWorldPoint(targetWorld);

            var open = new List<Node> { start };
            var closed = new HashSet<Node>();

            start.GCost = 0;
            start.HCost = Heuristic(start, target);

            while (open.Count > 0)
            {
                Node current = GetLowestFCost(open);

                if (current == target)
                    return RetracePath(start, target);

                open.Remove(current);
                closed.Add(current);

                foreach (var neighbour in _grid.GetNeighbours(current))
                {
                    if (!neighbour.Walkable || closed.Contains(neighbour)) continue;

                    float newG = current.GCost + Heuristic(current, neighbour);
                    if (newG < neighbour.GCost || !open.Contains(neighbour))
                    {
                        neighbour.GCost = newG;
                        neighbour.HCost = Heuristic(neighbour, target);
                        neighbour.Parent = current;
                        if (!open.Contains(neighbour))
                            open.Add(neighbour);
                    }
                }
            }

            return null;
        }

        private Node GetLowestFCost(List<Node> nodes)
        {
            Node best = nodes[0];
            foreach (var node in nodes)
                if (node.FCost < best.FCost)
                    best = node;
            return best;
        }

        private float Heuristic(Node a, Node b)
        {
            int dx = Mathf.Abs(a.GridPos.x - b.GridPos.x);
            int dz = Mathf.Abs(a.GridPos.y - b.GridPos.y);
            return 14 * Mathf.Min(dx, dz) + 10 * Mathf.Abs(dx - dz);
        }

        private List<Vector3> RetracePath(Node start, Node end)
        {
            var path = new List<Node>();
            Node current = end;
            while (current != start)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            return path.ConvertAll(n => n.WorldPos);
        }
    }
}