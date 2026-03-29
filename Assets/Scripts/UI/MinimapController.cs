using System.Collections.Generic;
using Agents;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Minimap conforme aux critères type TP : (6) rendu par une caméra orthographique + RenderTexture ;
    /// (9) affichage des données environnementales via le <see cref="environmentCullingMask"/> (sol, murs, obstacles).
    /// Optionnel : repères UI par-dessus (joueurs, <see cref="MinimapPoi"/>).
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [Header("Repère monde (comme PathfindController)")]
        [SerializeField] private Transform mapPlane;
        [SerializeField] private float planeWorldSize = 10f;

        [Header("Rendu caméra — environnement (6) + données scène (9)")]
        [Tooltip("Si activé, une caméra orthographique rend la scène dans une RenderTexture affichée sur ce RawImage.")]
        [SerializeField] private bool useCameraRendering = true;
        [SerializeField] private RawImage environmentView;
        [Tooltip("Hauteur de la caméra au-dessus du centre du plan (axe Y monde).")]
        [SerializeField] private float cameraHeightAboveMap = 45f;
        [Tooltip("Hauteur en pixels de la RenderTexture (la largeur suit le ratio du terrain).")]
        [SerializeField] private int renderTextureShortSide = 256;
        [Tooltip("Calques visibles sur la minimap : sol (Default), Obstacle, Wall, etc. Exclure UI.")]
        [SerializeField] private LayerMask environmentCullingMask;

        [Header("Repères UI (optionnel, par-dessus la texture)")]
        [Tooltip("RectTransform enfant du panneau, stretch sur toute la zone, au-dessus du RawImage dans la hierarchy.")]
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

        private Camera _minimapCamera;
        private RenderTexture _renderTexture;
        private GameObject _cameraHost;

        private void Reset()
        {
            environmentCullingMask = LayerMask.GetMask("Default", "Obstacle", "Wall");
        }

        private void Awake()
        {
            if (environmentCullingMask.value == 0)
                environmentCullingMask = LayerMask.GetMask("Default", "Obstacle", "Wall");

            RecomputeWorldBounds();
            SetupEnvironmentCameraIfNeeded();
        }

        private void LateUpdate()
        {
            RecomputeWorldBounds();
            UpdateEnvironmentCameraIfNeeded();

            if (iconsRoot == null) return;
            SyncAgentIcons();
            SyncPoiIcons();
        }

        private void OnDestroy()
        {
            TeardownEnvironmentCamera();
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

        private void SetupEnvironmentCameraIfNeeded()
        {
            if (!useCameraRendering || environmentView == null || mapPlane == null)
                return;

            TeardownEnvironmentCamera();

            _cameraHost = new GameObject("MinimapEnvironmentCamera");
            _cameraHost.transform.SetParent(transform, false);

            _minimapCamera = _cameraHost.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.08f, 0.1f, 0.14f, 1f);
            _minimapCamera.cullingMask = environmentCullingMask;
            _minimapCamera.depth = -20f;
            _minimapCamera.nearClipPlane = 0.3f;
            _minimapCamera.farClipPlane = Mathf.Max(200f, cameraHeightAboveMap * 3f);
            _minimapCamera.allowHDR = false;
            _minimapCamera.allowMSAA = false;
            _minimapCamera.useOcclusionCulling = false;
            _minimapCamera.stereoTargetEye = StereoTargetEyeMask.None;

            var aud = _minimapCamera.GetComponent<AudioListener>();
            if (aud != null) Destroy(aud);

            BuildOrResizeRenderTexture();
            _minimapCamera.targetTexture = _renderTexture;
            environmentView.texture = _renderTexture;
        }

        private void BuildOrResizeRenderTexture()
        {
            Vector3 scale = mapPlane.lossyScale;
            float w = scale.x * planeWorldSize;
            float d = scale.z * planeWorldSize;
            float aspect = w / Mathf.Max(0.001f, d);

            int h = Mathf.Max(32, renderTextureShortSide);
            int rw = Mathf.Max(32, Mathf.RoundToInt(h * aspect));

            if (_renderTexture != null && _renderTexture.width == rw && _renderTexture.height == h)
                return;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }

            _renderTexture = new RenderTexture(rw, h, 16, RenderTextureFormat.ARGB32)
            {
                name = "MinimapRT",
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear
            };
            _renderTexture.Create();
        }

        private void UpdateEnvironmentCameraIfNeeded()
        {
            if (!useCameraRendering || _minimapCamera == null || mapPlane == null || environmentView == null)
                return;

            BuildOrResizeRenderTexture();
            if (_minimapCamera.targetTexture != _renderTexture)
                _minimapCamera.targetTexture = _renderTexture;
            if (environmentView.texture != _renderTexture)
                environmentView.texture = _renderTexture;

            Vector3 scale = mapPlane.lossyScale;
            float w = scale.x * planeWorldSize;
            float d = scale.z * planeWorldSize;
            Vector3 c = mapPlane.position;

            _minimapCamera.transform.SetPositionAndRotation(
                c + Vector3.up * cameraHeightAboveMap,
                Quaternion.Euler(90f, 0f, 0f));

            float aspect = w / Mathf.Max(0.001f, d);
            _minimapCamera.aspect = aspect;
            _minimapCamera.orthographicSize = d * 0.5f;
        }

        private void TeardownEnvironmentCamera()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            if (_cameraHost != null)
            {
                Destroy(_cameraHost);
                _cameraHost = null;
                _minimapCamera = null;
            }

            if (environmentView != null)
                environmentView.texture = null;
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
