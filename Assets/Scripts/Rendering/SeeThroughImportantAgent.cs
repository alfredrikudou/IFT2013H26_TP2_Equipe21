using Agents;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    /// <summary>
    /// Affiche une silhouette semi-transparente lorsque l’agent est masqué par un mur / obstacle
    /// (voir <see cref="GameplayLogicalCulling"/> + shader ZTest Greater).
    /// À ajouter sur les prefabs joueur / IA (éléments importants).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class SeeThroughImportantAgent : MonoBehaviour
    {
        [SerializeField] private Vector3 quadOffset = new Vector3(0f, 0.55f, 0f);
        [SerializeField] private float quadScale = 1.15f;
        [SerializeField] private Color silhouetteColor = new Color(1f, 0.4f, 0.12f, 0.55f);

        private AgentVisibilityState _vis;
        private Transform _quadTransform;
        private MeshRenderer _quadRenderer;
        private Material _mat;
        private static Shader _cachedShader;

        private void Awake()
        {
            _vis = GetComponent<AgentVisibilityState>();

            if (_cachedShader == null)
                _cachedShader = Shader.Find("Game/SeeThroughSilhouette");
            if (_cachedShader == null)
            {
                Debug.LogWarning("[SeeThroughImportantAgent] Shader Game/SeeThroughSilhouette introuvable.");
                enabled = false;
                return;
            }

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SeeThroughSilhouette";
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = quadOffset;
            quad.transform.localScale = Vector3.one * quadScale;
            Destroy(quad.GetComponent<Collider>());

            _quadTransform = quad.transform;
            _quadRenderer = quad.GetComponent<MeshRenderer>();
            _mat = new Material(_cachedShader);
            _mat.SetColor("_BaseColor", silhouetteColor);
            _quadRenderer.sharedMaterial = _mat;
            _quadRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _quadRenderer.receiveShadows = false;
            _quadRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        private void LateUpdate()
        {
            if (_quadRenderer == null || _mat == null) return;
            if (_vis == null)
                _vis = GetComponent<AgentVisibilityState>();

            Camera cam = GetClosestPlayerCamera();
            if (cam == null || _vis == null)
            {
                _quadRenderer.enabled = false;
                return;
            }

            bool show = _vis.IsOccludedBehindObstacle && !_vis.IsLogicallyCulled;
            _quadRenderer.enabled = show;
            if (!show) return;

            Vector3 worldPos = transform.position + quadOffset;
            _quadTransform.position = worldPos;
            _quadTransform.LookAt(cam.transform.position, Vector3.up);
            _quadTransform.Rotate(0f, 180f, 0f);
        }

        private Camera GetClosestPlayerCamera()
        {
            var gm = GameManager.Instance;
            if (gm == null) return null;

            Camera best = null;
            float bestD2 = float.MaxValue;
            Vector3 p = transform.position;
            foreach (var a in gm.ActiveAgents)
            {
                if (a == null) continue;
                var c = a.ViewCamera;
                if (c == null || !c.isActiveAndEnabled) continue;
                float d2 = (c.transform.position - p).sqrMagnitude;
                if (d2 < bestD2)
                {
                    bestD2 = d2;
                    best = c;
                }
            }

            return best;
        }
    }
}
