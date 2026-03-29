using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// À ajouter sur les obstacles ou objets importants à montrer sur la minimap (caisses, zones, etc.).
    /// </summary>
    public sealed class MinimapPoi : MonoBehaviour
    {
        private static readonly List<MinimapPoi> Active = new();

        public static IReadOnlyList<MinimapPoi> All => Active;

        [SerializeField] private Color _tint = new Color(0.65f, 0.65f, 0.65f, 0.95f);

        public Color Tint => _tint;

        private void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }
    }
}
