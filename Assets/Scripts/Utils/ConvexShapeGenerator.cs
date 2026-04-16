using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utils
{
    public static class ConvexShapeGenerator
    {
        public static List<Vector2> Generate2DConvexShape(float maxWidth, float maxHeight, int nVertices)
        {
            
            List<float> xPool = new List<float>(nVertices);
            List<float> yPool = new List<float>(nVertices);
            
            for (int i = 0; i < nVertices; i++) {
                xPool.Add(Random.Range(0, maxWidth));
                yPool.Add(Random.Range(0, maxHeight));
            }
            xPool.Sort();
            yPool.Sort();
            float minX = xPool[0];
            float maxX = xPool[^1];
            float minY = yPool[0];
            float maxY = yPool[^1];
            List<float> xVec = new List<float>(nVertices);
            List<float> yVec = new List<float>(nVertices);

            float lastTop = minX, lastBot = minX;
            float lastLeft = minY, lastRight = minY;
            for (int i = 1; i < nVertices - 1; i++) {
                float curX = xPool[i];
                float curY = yPool[i];
                if (Random.value > 0.5f) {
                    xVec.Add(curX - lastTop);
                    lastTop = curX;
                } else {
                    xVec.Add(lastBot - curX);
                    lastBot = curX;
                }
                if (Random.value > 0.5f) {
                    yVec.Add(curY - lastLeft);
                    lastLeft = curY;
                } else {
                    yVec.Add(lastRight - curY);
                    lastRight = curY;
                }
            }
            xVec.Add(maxX - lastTop);
            xVec.Add(lastBot - maxX);
            yVec.Add(maxY - lastLeft);
            yVec.Add(lastRight - maxY);
            
            ListUtil.Shuffle(yPool);
            List<Vector2> vectors = new List<Vector2>(nVertices);
            for (int i = 0; i < nVertices; i++)
                vectors.Add(new Vector2(xVec[i], yVec[i]));
            vectors.Sort((a, b) =>
                Mathf.Atan2(a.y, a.x).CompareTo(Mathf.Atan2(b.y, b.x))
            );
            float x = 0, y = 0;
            float minPolygonX = 0;
            float minPolygonY = 0;
            var points = new List<Vector2>();

            for (int i = 0; i < nVertices; i++) {
                points.Add(new Vector2(x, y));

                x += vectors[i].x;
                y += vectors[i].y;

                minPolygonX = Mathf.Min(minPolygonX, x);
                minPolygonY = Mathf.Min(minPolygonY, y);
            }

            float xShift = minX - minPolygonX;
            float yShift = minY - minPolygonY;
            
            for (int i = 0; i < nVertices; i++) {
                Vector2 p = points[i];
                points[i] = new Vector2(p.x + xShift, p.y + yShift);
            }
            
            return points;
        }
        public static bool IsInsideConvex(List<Vector2> polygon, Vector2 point)
        {
            int n = polygon.Count;
            float sign = 0;

            for (int i = 0; i < n; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % n];

                float cross = (b.x - a.x) * (point.y - a.y) 
                              - (b.y - a.y) * (point.x - a.x);

                if (cross == 0) continue;

                if (sign == 0)
                    sign = cross;
                else if ((cross > 0) != (sign > 0))
                    return false;
            }

            return true;
        }
        public static bool IsInsideConvex(List<Vector2> polygon, Vector3 point) => IsInsideConvex(polygon, new Vector2(point.x, point.z));
    }
}
