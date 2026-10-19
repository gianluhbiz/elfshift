using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Crea e fornisce i materiali della tavolozza (come asset in Assets/Materials/Generated).
    /// Usa gli shader URP/Lit; se la pipeline non è URP ripiega su "Standard".
    /// </summary>
    public static class MaterialLibrary
    {
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static void ClearCache()
        {
            cache.Clear();
        }

        public static Shader LitShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Standard");
            return s;
        }

        public static Material Get(string key)
        {
            Material material;
            if (cache.TryGetValue(key, out material) && material != null) return material;

            MaterialSpec spec;
            if (!Palette.Specs.TryGetValue(key, out spec))
            {
                Debug.LogWarning("[Elfshift] Materiale sconosciuto nella tavolozza: " + key);
                spec = new MaterialSpec(Color.magenta, 0.2f, 0f, 0f);
            }

            string path = AssetUtil.GeneratedMaterials + "/M_" + key + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                AssetUtil.EnsureFolder(AssetUtil.GeneratedMaterials);
                material = new Material(LitShader());
                AssetDatabase.CreateAsset(material, path);
            }
            Configure(material, spec);
            EditorUtility.SetDirty(material);
            cache[key] = material;
            return material;
        }

        /// <summary>Materiale con una texture (es. pavimento di assi di legno).</summary>
        public static Material GetTextured(string key, Texture2D texture, Vector2 tiling, float smoothness)
        {
            Material material;
            if (cache.TryGetValue(key, out material) && material != null) return material;

            string path = AssetUtil.GeneratedMaterials + "/M_" + key + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                AssetUtil.EnsureFolder(AssetUtil.GeneratedMaterials);
                material = new Material(LitShader());
                AssetDatabase.CreateAsset(material, path);
            }
            Configure(material, new MaterialSpec(Color.white, smoothness, 0f, 0f));
            material.SetTexture("_BaseMap", texture);
            material.SetTexture("_MainTex", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTextureScale("_MainTex", tiling);
            EditorUtility.SetDirty(material);
            cache[key] = material;
            return material;
        }

        /// <summary>Materiale per le ombre morbide sotto i mobili (trasparente, senza luce).</summary>
        public static Material GetBlobShadow(Texture2D softCircle)
        {
            Material material;
            if (cache.TryGetValue("BlobShadow", out material) && material != null) return material;
            string path = AssetUtil.GeneratedMaterials + "/M_BlobShadow.mat";
            AssetUtil.EnsureFolder(AssetUtil.GeneratedMaterials);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = LitShader();
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetTexture("_MainTex", softCircle);
            material.SetTexture("_BaseMap", softCircle);
            material.SetColor("_Color", new Color(0.05f, 0.03f, 0.08f, 0.42f));
            material.SetColor("_BaseColor", new Color(0.05f, 0.03f, 0.08f, 0.42f));
            EditorUtility.SetDirty(material);
            cache["BlobShadow"] = material;
            return material;
        }

        private static void Configure(Material material, MaterialSpec spec)
        {
            material.SetColor("_BaseColor", spec.Color);
            material.SetColor("_Color", spec.Color);
            material.SetFloat("_Smoothness", spec.Smoothness);
            material.SetFloat("_Glossiness", spec.Smoothness);
            material.SetFloat("_Metallic", spec.Metallic);
            if (spec.Emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", spec.Color * spec.Emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
