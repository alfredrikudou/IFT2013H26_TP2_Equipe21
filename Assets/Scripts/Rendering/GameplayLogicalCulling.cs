using System.Collections.Generic;
using Agents;
using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// Culling logique : pour chaque agent, détermine s’il est vu par au moins une caméra joueur
    /// (frustum + distance). Met à jour <see cref="AgentVisibilityState"/>.
    /// Compte les agents pour le HUD debug.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class GameplayLogicalCulling : MonoBehaviour
    {
        [SerializeField] private float maxRelevanceDistance = 120f;
        [SerializeField] private LayerMask obstacleLayers;
        [SerializeField] private bool raycastExcludeTriggers = true;

        public int CountVisible { get; private set; }
        public int CountCulled { get; private set; }

        private readonly List<Camera> _cameras = new();

        private void Awake()
        {
            if (obstacleLayers.value == 0)
                obstacleLayers = LayerMask.GetMask("Obstacle", "Wall");
        }

        private void LateUpdate()
        {
            CountVisible = 0;
            CountCulled = 0;

            var gm = GameManager.Instance;
            if (gm == null) return;

            CollectPlayerCameras(gm);
            IReadOnlyList<Agent> agents = gm.ActiveAgents;
            if (agents == null) return;

            foreach (var agent in agents)
            {
                if (agent == null || agent.IsDead) continue;

                var state = agent.GetComponent<AgentVisibilityState>();
                if (state == null)
                    state = agent.gameObject.AddComponent<AgentVisibilityState>();

                Bounds b = GetAgentBounds(agent);
                bool anyCameraSees = false;
                bool anyOccluded = false;

                foreach (var cam in _cameras)
                {
                    if (cam == null || !cam.isActiveAndEnabled) continue;

                    if (Vector3.Distance(cam.transform.position, b.center) > maxRelevanceDistance)
                        continue;

                    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                    if (!GeometryUtility.TestPlanesAABB(planes, b))
                        continue;

                    anyCameraSees = true;

                    if (IsOccludedSegment(cam.transform.position, b.center))
                        anyOccluded = true;
                }

                state.IsLogicallyCulled = !anyCameraSees;
                state.IsOccludedBehindObstacle = anyCameraSees && anyOccluded;

                if (state.IsLogicallyCulled) CountCulled++;
                else CountVisible++;
            }
        }

        private bool IsOccludedSegment(Vector3 origin, Vector3 target)
        {
            float dist = Vector3.Distance(origin, target);
            if (dist < 0.02f) return false;
            var q = raycastExcludeTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;
            if (!Physics.Linecast(origin, target, out RaycastHit hit, obstacleLayers, q))
                return false;
            return hit.distance < dist - 0.15f;
        }

        private void CollectPlayerCameras(GameManager gm)
        {
            _cameras.Clear();
            foreach (var a in gm.ActiveAgents)
            {
                if (a == null) continue;
                var cam = a.ViewCamera;
                if (cam != null && cam.enabled && !_cameras.Contains(cam))
                    _cameras.Add(cam);
            }
        }

        private static Bounds GetAgentBounds(Agent agent)
        {
            var c = agent.GetComponent<Collider>();
            if (c != null) return c.bounds;
            c = agent.GetComponentInChildren<Collider>();
            if (c != null) return c.bounds;
            return new Bounds(agent.transform.position, Vector3.one);
        }
    }
}
