using UnityEngine;

namespace Agents
{
    /// <summary>
    /// État mis à jour par GameplayLogicalCulling : culling logique (frustum + distance)
    /// et indication d’occlusion pour le rendu « à travers » les obstacles.
    /// </summary>
    [DisallowMultipleComponent]
    public class AgentVisibilityState : MonoBehaviour
    {
        [Tooltip("Aucune caméra joueur ne voit ce personnage dans son frustum (réduction coût IA).")]
        public bool IsLogicallyCulled { get; internal set; }

        [Tooltip("Au moins une caméra a une LOS bloquée par un obstacle avant d’atteindre ce personnage.")]
        public bool IsOccludedBehindObstacle { get; internal set; }

        [SerializeField] private bool drawDebugGizmo = true;
        [SerializeField] private Color gizmoColorVisible = new Color(0f, 0.9f, 0.2f, 0.35f);
        [SerializeField] private Color gizmoColorCulled = new Color(0.95f, 0.15f, 0.1f, 0.45f);

        private void OnDrawGizmos()
        {
            if (!drawDebugGizmo || !Application.isPlaying) return;
            var col = GetComponent<Collider>();
            Bounds b = col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
            Gizmos.color = IsLogicallyCulled ? gizmoColorCulled : gizmoColorVisible;
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}
