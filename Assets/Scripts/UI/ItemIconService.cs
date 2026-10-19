using System.Collections.Generic;
using Elfshift.Core;
using UnityEngine;

namespace Elfshift.UI
{
    /// <summary>
    /// Crea al volo le icone degli oggetti fotografando il loro modello 3D in un piccolo
    /// "studio fotografico" nascosto lontano dalla scena. Così, quando sostituisci un modello
    /// provvisorio con uno definitivo, anche le icone si aggiornano da sole.
    /// </summary>
    public sealed class ItemIconService : MonoBehaviour
    {
        [SerializeField] private int iconSize = 256;
        [Tooltip("Posizione dello studio: deve essere lontana dal laboratorio.")]
        [SerializeField] private Vector3 studioPosition = new Vector3(0f, -400f, 0f);

        private readonly Dictionary<ItemDefinition, RenderTexture> cache = new Dictionary<ItemDefinition, RenderTexture>();
        private Camera studioCamera;
        private Light studioLight;
        private Transform studioRoot;

        private void Awake()
        {
            BuildStudio();
        }

        /// <summary>Restituisce la texture dell'icona (creandola la prima volta). Null se non possibile.</summary>
        public Texture GetIcon(ItemDefinition item)
        {
            if (studioCamera == null) BuildStudio(); // sicurezza se chiamato prima di Awake
            if (item == null || item.VisualPrefab == null || studioCamera == null) return null;

            RenderTexture existing;
            if (cache.TryGetValue(item, out existing) && existing != null) return existing;

            RenderTexture rt = new RenderTexture(iconSize, iconSize, 24, RenderTextureFormat.ARGB32);
            rt.name = "Icona_" + item.DisplayName;
            rt.Create();

            GameObject instance = Instantiate(item.VisualPrefab, studioRoot);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.Euler(0f, -30f, 0f);

            Bounds bounds = ComputeBounds(instance);
            float radius = Mathf.Max(0.1f, bounds.extents.magnitude);
            float fov = studioCamera.fieldOfView * Mathf.Deg2Rad;
            float dist = radius / Mathf.Sin(fov * 0.5f) * 1.05f;
            Vector3 dir = new Vector3(0.45f, 0.55f, -1f).normalized;
            studioCamera.transform.position = bounds.center + dir * dist;
            studioCamera.transform.LookAt(bounds.center);
            studioCamera.nearClipPlane = Mathf.Max(0.05f, dist - radius * 1.5f);
            studioCamera.farClipPlane = dist + radius * 2f;
            Vector3 lightOffset = new Vector3(1.2f, 2.2f, -2.2f) * radius * 1.2f;
            studioLight.transform.position = bounds.center + lightOffset;
            studioLight.range = radius * 14f;
            // L'intensità cresce con il quadrato della distanza, così l'icona ha sempre la stessa luminosità.
            studioLight.intensity = 1.1f * lightOffset.sqrMagnitude;

            studioCamera.targetTexture = rt;
            studioCamera.Render();
            studioCamera.targetTexture = null;

            Destroy(instance);
            cache[item] = rt;
            return rt;
        }

        private void BuildStudio()
        {
            GameObject root = new GameObject("StudioIcone");
            root.transform.SetParent(transform, false);
            root.transform.position = studioPosition;
            studioRoot = root.transform;

            GameObject camObject = new GameObject("CameraIcone");
            camObject.transform.SetParent(root.transform, false);
            studioCamera = camObject.AddComponent<Camera>();
            studioCamera.enabled = false; // renderizza solo quando lo chiediamo noi
            studioCamera.clearFlags = CameraClearFlags.SolidColor;
            studioCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            studioCamera.fieldOfView = 24f;
            studioCamera.allowHDR = false;
            studioCamera.allowMSAA = false;

            GameObject lightObject = new GameObject("LuceIcone");
            lightObject.transform.SetParent(root.transform, false);
            studioLight = lightObject.AddComponent<Light>();
            studioLight.type = LightType.Point;
            studioLight.color = new Color(1f, 0.96f, 0.88f);
            studioLight.shadows = LightShadows.None;
        }

        private static Bounds ComputeBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.one * 0.5f);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<ItemDefinition, RenderTexture> pair in cache)
            {
                if (pair.Value != null)
                {
                    pair.Value.Release();
                    Destroy(pair.Value);
                }
            }
            cache.Clear();
        }
    }
}
