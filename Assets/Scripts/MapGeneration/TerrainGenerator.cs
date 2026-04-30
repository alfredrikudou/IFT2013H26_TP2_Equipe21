using System;
using System.Collections.Generic;
using System.Linq;
using CustomParticleSystem;
using Pathfinding;
using UnityEngine;
using Utils;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Meshing.Algorithm;
using Unity.VisualScripting;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MapGeneration
{
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] public int seed = 232232;
        [Header("Terrain settings")]
        [SerializeField] private int mapSamplingSize = 100;
        [SerializeField] private int mapMinVertices = 15;
        [SerializeField] private int mapMaxVertices = 45;
        [SerializeField] private float wallHeight = 5;
        [SerializeField] private float nodeDensity = 10f;
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private GameObject walkerPrefab;
        [SerializeField] private GameObject groundGo;
        [SerializeField] private GameObject wallGo;
        
        [Header("Heightmap settings")]
        [SerializeField] private float heightMaxNoiseScale = 0.07f;
        [SerializeField] private float heightMinNoiseScale = 0.02f;
        [SerializeField] private float heightMaxNoiseAmplification = 7;
        [SerializeField] private float heightMinNoiseAmplification = 1;
        
        [Header("Heightmap settings")]
        [SerializeField] private float humidityMaxNoiseScale = 0.03f;
        [SerializeField] private float humidityMinNoiseScale = 0.008f;
        [SerializeField] private float humidityMaxNoiseAmplification = 3;
        [SerializeField] private float humidityMinNoiseAmplification = 0.5f;
        
        [Header("Region settings")]
        [SerializeField] private bool useZoneRange = false;
        [SerializeField] private int maxRangeZoneCount = 10;
        [SerializeField] private int minRangeZoneCount = 7;
        [SerializeField] private int maxZoneSize = 40;
        [SerializeField] private int minZoneSize = 20;
        
        [Header("Obstacle settings")]
        [SerializeField] private bool useObstacleRange = false;
        [SerializeField] private int maxRangeObstacleCount = int.MaxValue;
        [SerializeField] private int minRangeObstacleCount = 0;
        [SerializeField] private int maxObstacleDistance = 20;
        [SerializeField] private int minObstacleDistance = 5;
        [SerializeField] private float maxObstacleSize = 2f;
        [SerializeField] private float minObstacleSize = 0.5f;
        [SerializeField] private GameObject obstaclePrefab;
        [SerializeField] private GameObject obstacleParent;
        
        
        [Header("Vegetation settings")]
        [SerializeField] private bool useVegetationRange = false;
        [SerializeField] private int maxRangeVegetationCount = int.MaxValue;
        [SerializeField] private int minRangeVegetationCount = 0;
        [SerializeField] private int maxVegetationDistance = 10;
        [SerializeField] private int minVegetationDistance = 3;
        [SerializeField] private GameObject[] vegetationPrefabs;
        [SerializeField] private GameObject vegetationParent;
        
        [Header("Biome settings")]
        [SerializeField] private int maxFogDistance = 50;
        [SerializeField] private int minFogDistance = 10;
        [SerializeField] private float rainThreshold = 0.75f;
        [SerializeField] private float fogThreshold = 0.50f;
        [SerializeField] private float scorchThreshold = 0.25f;
        
        
        
        private List<Vector2> _zones = new List<Vector2>();
        private List<ZoneInfo> _zoneInfos = new List<ZoneInfo>();
        private List<Vector2> _polygonVertices = new List<Vector2>();
        private readonly List<Vector3> points = new List<Vector3>();
        private Node[,] _mapGrid;

        private PerlinNoise2D _heightPerlin;
        private PerlinNoise2D _humidityPerlin;
        private float _checkSphereRadius;
        private LayerMask _wallLayer;

        public void Awake()
        {
            Random.InitState(seed);
            SetupHeightMap();
            SetupHumidityMap();
            GeneratePolygon();
            GenerateZones();
            SetWalkerSize();
            GenerateMapGrid();
            BuildPlane();
            BuildWalls();
            SpawnObstacles();
            SpawnVegetation();
        }

        public void Start()
        {
            SetupBiomeEvents();
        }

        public Node[,] GetMap() => _mapGrid;

        private void SetupHeightMap()
        {
            _heightPerlin = new PerlinNoise2D(Random.Range(heightMinNoiseScale, heightMaxNoiseScale), Random.Range(heightMinNoiseAmplification, heightMaxNoiseAmplification));
        }
        
        private void SetupHumidityMap()
        {
            _humidityPerlin = new PerlinNoise2D(Random.Range(humidityMinNoiseScale, humidityMaxNoiseScale), Random.Range(humidityMinNoiseAmplification, humidityMaxNoiseAmplification));
        }

        private void GenerateZones()
        {
            if(useZoneRange)
                _zones = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minZoneSize,
                maxZoneSize, Random.Range(minRangeZoneCount, maxRangeZoneCount));
            else
                _zones = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minZoneSize,
                maxZoneSize);
            _zones = _zones.Where(x => ConvexShapeGenerator.IsInsideConvex(_polygonVertices, x)).ToList();
            foreach (var zone in _zones)
            {
                var info = new ZoneInfo
                {
                    Color = new Color(Random.value, Random.value, Random.value),
                    Vegetation = Random.Range(0, vegetationPrefabs.Length)
                };
                _zoneInfos.Add(info);
            }
        }

        private void GeneratePolygon()
        {
            _polygonVertices = ConvexShapeGenerator.Generate2DConvexShape(mapSamplingSize, mapSamplingSize,
                Random.Range(mapMinVertices, mapMaxVertices));
        }

        private void GenerateMapGrid()
        {
            var nodeCount = Mathf.RoundToInt(mapSamplingSize * nodeDensity);
            var nodeSize = 1f / nodeDensity;
            _mapGrid = new Node[nodeCount, nodeCount];

            var origin = Vector3.zero + new Vector3(0, _heightPerlin.GetHeight(0, 0), 0);
            for (int x = 0; x < nodeCount; x++)
            for (int z = 0; z < nodeCount; z++)
            {
                Vector3 worldPos = origin + new Vector3(x * nodeSize, _heightPerlin.GetHeight(x * nodeSize, z * nodeSize), z * nodeSize);
                bool isInsidePolygon = ConvexShapeGenerator.IsInsideConvex(_polygonVertices, worldPos);
                bool walkable = !Physics.CheckSphere(worldPos, _checkSphereRadius, _wallLayer)
                                && isInsidePolygon;
                _mapGrid[x, z] = new Node(new Vector2Int(x, z), worldPos, walkable);
                if (isInsidePolygon)
                    points.Add(worldPos);
            }
        }

        private void SetWalkerSize()
        {
            var capsule = walkerPrefab.GetComponent<CapsuleCollider>();
            var sphere = walkerPrefab.GetComponent<SphereCollider>();
            var box = walkerPrefab.GetComponent<BoxCollider>();
            var col = walkerPrefab.GetComponentInChildren<Collider>();

            _checkSphereRadius = capsule != null ? capsule.radius :
                sphere != null ? sphere.radius :
                box != null ? Mathf.Min(box.size.x, box.size.z) * 0.5f :
                col != null && col is CapsuleCollider cc ? cc.radius :
                col != null && col is SphereCollider sc ? sc.radius :
                0.5f;
        }

        private void BuildWalls()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < _polygonVertices.Count; i++)
            {
                Vector2 v2a = _polygonVertices[i];
                Vector2 v2b = _polygonVertices[(i + 1) % _polygonVertices.Count];

                Vector3 a0 = new Vector3(v2a.x, _heightPerlin.GetHeight(v2a.x, v2a.y), v2a.y);
                Vector3 b0 = new Vector3(v2b.x, _heightPerlin.GetHeight(v2b.x, v2b.y), v2b.y);

                Vector3 a1 = a0 + Vector3.up * wallHeight;
                Vector3 b1 = b0 + Vector3.up * wallHeight;

                int start = vertices.Count;

                vertices.Add(a0);
                vertices.Add(b0);
                vertices.Add(b1);
                vertices.Add(a1);

                triangles.Add(start + 0);
                triangles.Add(start + 1);
                triangles.Add(start + 2);

                triangles.Add(start + 0);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
            }

            Mesh newMesh = new Mesh();
            newMesh.SetVertices(vertices);
            newMesh.SetTriangles(triangles, 0);

            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
            newMesh.colors = ColorizeZones(newMesh.vertices);

            var meshFilter = wallGo.GetComponent<MeshFilter>();
            var meshRenderer = wallGo.GetComponent<MeshRenderer>();
            var meshCollider = wallGo.GetComponent<MeshCollider>();
            meshFilter.mesh = newMesh;
            meshRenderer.material = new Material(wallMaterial);
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = newMesh;
        }

        private void BuildPlane()
        {
            Polygon polygon = new Polygon();
            foreach (var p in points)
            {
                polygon.Add(new Vertex(p.x, p.z));
            }

            var mesh = polygon.Triangulate();
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            Dictionary<int, int> indexMap = new Dictionary<int, int>();
            int index = 0;
            foreach (var v in mesh.Vertices)
            {
                indexMap[v.ID] = index++;
                vertices.Add(new Vector3((float)v.X, _heightPerlin.GetHeight((float)v.x, (float)v.y), (float)v.Y));
            }

            foreach (var tri in mesh.Triangles)
            {
                triangles.Add(indexMap[tri.GetVertexID(0)]);
                triangles.Add(indexMap[tri.GetVertexID(2)]);
                triangles.Add(indexMap[tri.GetVertexID(1)]);
            }

            _polygonVertices = GetOrderedBoundaryPoints(vertices, triangles);
            Mesh newMesh = new Mesh();
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            newMesh.vertices = vertices.ToArray();
            newMesh.triangles = triangles.ToArray();
            newMesh.colors = ColorizeZones(newMesh.vertices);
            newMesh.RecalculateNormals();
            newMesh.RecalculateBounds();
            
            var meshFilter = groundGo.GetComponent<MeshFilter>();
            var meshRenderer = groundGo.GetComponent<MeshRenderer>();
            var meshCollider = groundGo.GetComponent<MeshCollider>();
            meshFilter.mesh = newMesh;
            meshRenderer.material = new Material(terrainMaterial);
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = newMesh;
        }
        private List<Vector2> GetOrderedBoundaryPoints(List<Vector3> vertices, List<int> triangles)
        {
            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>();
            for (int i = 0; i < triangles.Count; i += 3)
            {
                CountEdge(edgeCount, triangles[i],     triangles[i + 1]);
                CountEdge(edgeCount, triangles[i + 1], triangles[i + 2]);
                CountEdge(edgeCount, triangles[i + 2], triangles[i]);
            }

            Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value != 1) continue;
                int a = kvp.Key.Item1, b = kvp.Key.Item2;

                if (!adjacency.ContainsKey(a)) adjacency[a] = new List<int>();
                if (!adjacency.ContainsKey(b)) adjacency[b] = new List<int>();
                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }

            List<Vector2> ordered = new List<Vector2>();
            HashSet<int> visited = new HashSet<int>();

            int current = adjacency.Keys.First();
            int prev = -1;

            while (true)
            {
                visited.Add(current);
                ordered.Add(new Vector2(vertices[current].x, vertices[current].z));

                int next = -1;
                foreach (var neighbor in adjacency[current])
                {
                    if (neighbor != prev && !visited.Contains(neighbor))
                    {
                        next = neighbor;
                        break;
                    }
                }

                if (next == -1) break;
                prev = current;
                current = next;
            }
            float area = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                Vector2 curr = ordered[i];
                Vector2 next = ordered[(i + 1) % ordered.Count];
                area += (next.x - curr.x) * (next.y + curr.y);
            }
            if (area > 0)
                ordered.Reverse();

            return ordered;
        }
        private void CountEdge(Dictionary<(int, int), int> dict, int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            dict[key] = dict.ContainsKey(key) ? dict[key] + 1 : 1;
        }

        private int FindNearestZoneIndex(Vector2 pos)
        {
            int nearest = 0;
            float minDist = float.MaxValue;
            for (int j = 0; j < _zones.Count; j++)
            {
                float dist = Vector2.Distance(pos, _zones[j]);
                if (dist < minDist) { minDist = dist; nearest = j; }
            }
            return nearest;
        }
        
        private Color[] ColorizeZones(Vector3[] vertices)
        {
            Color[] vertexColors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                int nearest = FindNearestZoneIndex(new Vector2(vertices[i].x, vertices[i].z));
                vertexColors[i] = _zoneInfos[nearest].Color;
            }
            return vertexColors;
        }

        private void SpawnObstacles()
        {
            List<Vector2> obstacles;
            if (useObstacleRange)
                obstacles = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minObstacleDistance,
                    maxObstacleDistance, Random.Range(minRangeObstacleCount, maxRangeObstacleCount));
            else
                obstacles = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minObstacleDistance,
                    maxObstacleDistance);
            obstacles = obstacles.Where(x => ConvexShapeGenerator.IsInsideConvex(_polygonVertices, x)).ToList();
            foreach (var obstacle in obstacles)
            {
                var scaleX = Random.Range(minObstacleSize, maxObstacleSize);
                var scaleY = Random.Range(minObstacleSize, maxObstacleSize);
                var scaleZ = Random.Range(minObstacleSize, maxObstacleSize);
                var spawnNodePos = new Vector3(obstacle.x, _heightPerlin.GetHeight(obstacle), obstacle.y);
                var spawnPos = spawnNodePos + scaleY * Vector3.up;
                var obj = Instantiate(obstaclePrefab, spawnPos, Quaternion.identity, obstacleParent.transform);
                obj.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
            }
        }
        private void SpawnVegetation()
        {
            List<Vector2> vegetationSpawns;
            if (useVegetationRange)
                vegetationSpawns = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minVegetationDistance,
                    maxVegetationDistance, Random.Range(minRangeVegetationCount, maxRangeVegetationCount));
            else
                vegetationSpawns = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, minVegetationDistance,
                    maxVegetationDistance);
            vegetationSpawns = vegetationSpawns.Where(x => ConvexShapeGenerator.IsInsideConvex(_polygonVertices, x)).ToList();
            foreach (var spawn in vegetationSpawns)
            {
                var spawnNodePos = new Vector3(spawn.x, _heightPerlin.GetHeight(spawn), spawn.y);
                int nearestZone = FindNearestZoneIndex(new Vector2(spawnNodePos.x, spawnNodePos.z));
                var vegetation = vegetationPrefabs[_zoneInfos[nearestZone].Vegetation];
                Instantiate(vegetation, spawnNodePos, Quaternion.identity, vegetationParent.transform);
            }
        }
        
        private void SetupBiomeEvents()
        {
            List<Vector2> biomeEventsSpawns = PoissonDiskSampling.Generate2DSampling(mapSamplingSize, mapSamplingSize, 
                minFogDistance, maxVegetationDistance);
            foreach (var spawn in biomeEventsSpawns)
            {
                var humidity = _humidityPerlin.GetHeight(spawn);
                var spawnNodePos = new Vector3(spawn.x, _heightPerlin.GetHeight(spawn), spawn.y);
                if (humidity >= rainThreshold) ParticleManager.Instance.Play(ParticleManager.EntryNames.Rain, spawnNodePos + Vector3.up * 10);
                else if (humidity >= fogThreshold) ParticleManager.Instance.Play(ParticleManager.EntryNames.Fog, spawnNodePos);
                else if (humidity < scorchThreshold) ParticleManager.Instance.Play(ParticleManager.EntryNames.Scorch, spawnNodePos);
            }
        }

        public List<Vector3> GetSpawnPoints()
        {
            List<int> numbers = Enumerable.Range(0, _zones.Count).ToList();
            ListUtil.Shuffle(numbers);
            return numbers.Select(index => new Vector3(_zones[index].x, _heightPerlin.GetHeight(_zones[index].x, _zones[index].y), _zones[index].y)).ToList();
        }

        private struct ZoneInfo
        {
            public Color Color;
            public int Vegetation;
        }
    }
}