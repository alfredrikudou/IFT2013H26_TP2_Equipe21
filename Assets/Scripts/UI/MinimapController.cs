using System.Collections.Generic;
using Agents;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Minimap 2D : repères pour les joueurs (via <see cref="GameManager.ActiveAgents"/>)
    /// et pour les objets marqués <see cref="MinimapPoi"/>.
    /// Les limites du monde reprennent la même logique que <see cref="Pathfinding.PathGrid"/> (plan × taille).
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [Header("Repère monde (comme PathfindController)")]
        [SerializeField] private Transform mapPlane;
        [SerializeField] private float planeWorldSize = 10f;

        [Header("UI")]
        [Tooltip("RectTransform enfant du panneau minimap, ancré en bas-gauche (0,0) pleine zone, pour placer les icônes.")]
        [SerializeField] private RectTransform iconsRoot;
        [SerializeField] private float iconSizePixels = 10f;

        [Header("Couleurs joueurs (par slot 0–3)")]
        [SerializeField] private Color[] agentColors =
        {
            new Color(0.2f, 0.85f, 0.3f),
            new Color(0.25f, 0.55f, 1f),
            new Color(1f, 0.75f, 0.2f),
            new Color(0.95f, 0.35f, 0.85f)
        };

        private float _minX, _maxX, _minZ, _maxZ;
        private readonly List<Image> _agentIcons = new();
        private readonly List<Image> _poiIcons = new();
        private static Sprite _whiteSprite;

        private void Awake()
        {
            RecomputeWorldBounds();
        }

        private void LateUpdate()
        {
            if (iconsRoot == null) return;

            RecomputeWorldBounds();
            SyncAgentIcons();
            SyncPoiIcons();
        }

        private void RecomputeWorldBounds()
        {
            if (mapPlane == null)
            {
                _minX = _maxX = _minZ = _maxZ = 0f;
                return;
            }

            Vector3 scale = mapPlane.lossyScale;
            float w = scale.x * planeWorldSize;
            float d = scale.z * planeWorldSize;
            Vector3 c = mapPlane.position;
            _minX = c.x - w * 0.5f;
            _maxX = c.x + w * 0.5f;
            _minZ = c.z - d * 0.5f;
            _maxZ = c.z + d * 0.5f;
        }

        private void SyncAgentIcons()
        {
            var gm = GameManager.Instance;
            IReadOnlyList<Agent> agents = gm != null ? gm.ActiveAgents : null;
            int n = agents?.Count ?? 0;

            while (_agentIcons.Count < n)
                _agentIcons.Add(CreateIcon(iconsRoot, new Color(1f, 1f, 1f, 0.95f)));

            for (int i = n; i < _agentIcons.Count; i++)
                _agentIcons[i].enabled = false;

            if (n == 0 || Mathf.Approximately(_maxX - _minX, 0f) || Mathf.Approximately(_maxZ - _minZ, 0f))
                return;

            for (int i = 0; i < n; i++)
            {
                var img = _agentIcons[i];
                var a = agents![i];
                if (a == null || a.IsDead)
                {
                    img.enabled = false;
                    continue;
                }

                img.enabled = true;
                img.color = ColorForSlot(a.SlotIndex);
                PlaceIcon(img.rectTransform, a.transform.position);
            }
        }

        private void SyncPoiIcons()
        {
            int n = MinimapPoi.All.Count;

            while (_poiIcons.Count < n)
                _poiIcons.Add(CreateIcon(iconsRoot, Color.white));

            while (_poiIcons.Count > n)
            {
                int last = _poiIcons.Count - 1;
                Destroy(_poiIcons[last].gameObject);
                _poiIcons.RemoveAt(last);
            }

            if (n == 0 || Mathf.Approximately(_maxX - _minX, 0f) || Mathf.Approximately(_maxZ - _minZ, 0f))
                return;

            for (int i = 0; i < n; i++)
            {
                var img = _poiIcons[i];
                var poi = MinimapPoi.All[i];
                if (poi == null)
                {
                    img.enabled = false;
                    continue;
                }

                img.enabled = true;
                img.color = poi.Tint;
                PlaceIcon(img.rectTransform, poi.transform.position);
            }
        }

        private Color ColorForSlot(int slotIndex)
        {
            if (agentColors == null || agentColors.Length == 0)
                return Color.white;
            if (slotIndex < 0)
                return agentColors[0];
            return agentColors[slotIndex % agentColors.Length];
        }

        private void PlaceIcon(RectTransform rt, Vector3 world)
        {
            float nx = Mathf.InverseLerp(_minX, _maxX, world.x);
            float nz = Mathf.InverseLerp(_minZ, _maxZ, world.z);
            nx = Mathf.Clamp01(nx);
            nz = Mathf.Clamp01(nz);

            Rect r = iconsRoot.rect;
            float w = r.width;
            float h = r.height;

            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);
            rt.anchoredPosition = new Vector2(nx * w, nz * h);
        }

        private static Image CreateIcon(RectTransform parent, Color c)
        {
            var go = new GameObject("MinimapIcon", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = c;
            img.raycastTarget = false;
            img.preserveAspect = true;
            return img;
        }

        private static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                100f);
            return _whiteSprite;
        }
    }
}
