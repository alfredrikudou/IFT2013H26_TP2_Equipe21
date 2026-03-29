using UnityEngine;

namespace Game.Rendering
{
    /// <summary>
    /// Présentation visuelle du culling logique (compteurs à l’écran).
    /// </summary>
    public class CullingDebugHud : MonoBehaviour
    {
        [SerializeField] private bool show = true;
        [SerializeField] private Vector2 screenOffset = new Vector2(12f, 12f);

        private GameplayLogicalCulling _culling;

        private void Awake()
        {
            _culling = FindFirstObjectByType<GameplayLogicalCulling>();
        }

        private void OnGUI()
        {
            if (!show || _culling == null) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = new Color(0.95f, 0.95f, 0.95f, 1f) }
            };

            string text =
                $"Culling logique — visibles: {_culling.CountVisible}   cullés: {_culling.CountCulled}\n" +
                "(frustum + distance ; IA ralentie si cullé ; silhouette si derrière obstacle)";

            GUI.Label(new Rect(screenOffset.x, screenOffset.y, 560f, 48f), text, style);
        }
    }
}
