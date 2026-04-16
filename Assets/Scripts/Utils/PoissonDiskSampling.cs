using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class PoissonDiskSampling
    {
        public static List<Vector2> Generate2DSampling(float width, float height, float minimumRadius, float maximumRadius,
            int maxSampleCount = int.MaxValue, int samplingResolution = 30)
        {
            int N = 2;
            List<Vector2> points = new List<Vector2>();
            float cellSize = Mathf.Floor(minimumRadius / Mathf.Sqrt(N));
            int ncells_width = Mathf.CeilToInt(width / cellSize) + 1;
            int ncells_height = Mathf.CeilToInt(height / cellSize) + 1;
            Vector2?[,] grid =  new Vector2?[ncells_width,ncells_height];
            for(int i = 0; i < ncells_width; i++)
            for (int j = 0; j < ncells_height; j++)
                grid[i, j] = null;
            void insertPoint(Vector2 point) {
                int xindex = (int)(point.x / cellSize);
                int yindex = (int)(point.y / cellSize);
                grid[xindex, yindex] = point;
            }

            bool isValidPoint(Vector2 point)
            {
                if (point.x < 0 || point.x >= width || point.y < 0 || point.y >= height) return false;
                int xindex = (int)(point.x / cellSize);
                int yindex = (int)(point.y / cellSize);
                int i0 = Mathf.Max(xindex - 1, 0);
                int i1 = Mathf.Min(xindex + 1, ncells_width - 1);
                int j0 = Mathf.Max(yindex - 1, 0);
                int j1 = Mathf.Min(yindex + 1, ncells_height - 1);

                for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                    if (grid[i,j] != null)
                        if (Vector2.Distance((Vector2)grid[i,j], point) < minimumRadius)
                            return false;
                return true;
            }
            
            List<Vector2> actives =  new List<Vector2>();
            Vector2 p0 =  new Vector2(Random.Range(0f, width), Random.Range(0f, height));
            insertPoint(p0);
            points.Add(p0);
            actives.Add(p0);

            while (actives.Count > 0 && points.Count <= maxSampleCount)
            {
                Vector2 p = actives[Random.Range(0, actives.Count)];
                bool found  = false;
                for (int k = 0; k < samplingResolution; k++)
                {
                    float theta = Random.Range(0f, 360f);
                    float newRadius = Random.Range(minimumRadius, maximumRadius);
                    float newX = p.x + newRadius * Mathf.Cos(theta);
                    float newY = p.y + newRadius * Mathf.Sin(theta);
                    Vector2 newP = new Vector2(newX, newY);
                    if (!isValidPoint(newP))
                        continue;
                    points.Add(newP);
                    insertPoint(newP);
                    actives.Add(newP);
                    found = true;
                    break;
                }

                if (!found)
                    actives.Remove(p);
            }

            return points;
        }
    }
}