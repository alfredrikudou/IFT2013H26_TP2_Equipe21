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

            ResetSearchState();

            var open = new List<Node>();
            var closed = new HashSet<Node>();

            if (!start.Walkable || !target.Walkable)
                return null;

            if (start == target)
                return new List<Vector3> { targetWorld };

            start.GCost = 0;
            start.HCost = Heuristic(start, target);
            open.Add(start);

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

        /// <summary>
        /// Indispensable : les mêmes instances <see cref="Node"/> sont réutilisées à chaque recherche.
        /// Sans reset, l’A* est corrompu et les chemins échouent (IA immobile).
        /// </summary>
        private void ResetSearchState()
        {
            var g = _grid.Grid;
            int w = g.GetLength(0);
            int h = g.GetLength(1);
            for (int x = 0; x < w; x++)
            for (int z = 0; z < h; z++)
            {
                Node n = g[x, z];
                n.GCost = float.MaxValue;
                n.HCost = 0f;
                n.Parent = null;
            }
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
            while (current != start && current != null)
            {
                path.Add(current);
                current = current.Parent;
            }

            path.Reverse();
            var world = path.ConvertAll(n => n.WorldPos);
            if (world.Count == 0)
                world.Add(end.WorldPos);
            return world;
        }
    }
}