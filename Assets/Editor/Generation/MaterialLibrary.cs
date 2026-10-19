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

        /// <summary>Chiave (EditorPrefs) con cui il menu Elfshift attiva/disattiva il shader stilizzato.</summary>
        public const string StylizedPrefKey = "Elfshift.StylizedShader";
        private static Shader stylized;
        private static bool stylizedChecked;

        public static void ClearShaderCache()
        {
            stylized = null;
            stylizedChecked = false;
        }

        /// <summary>Il shader "Elfshift/StylizedLit" se attivo e privo di errori, altrimenti null.</summary>
        public static Shader StylizedShader()
        {
            if (stylizedChecked) return stylized;
            stylizedChecked = true;
            if (!EditorPrefs.GetBool(StylizedPrefKey, true)) return null;

            Shader candidate = Shader.Find("Elfshift/StylizedLit");
            if (candidate == null)
            {
                Debug.LogWarning("[Elfshift] Shader stilizzato non trovato: uso il shader standard di URP.");
                return null;
            }
            if (ShaderHasErrors(candidate))
            {
                Debug.LogWarning("[Elfshift] Il shader stilizzato ha errori di compilazione: uso il shader standard di URP. " +
                                 "Apri Assets/Shaders/ElfshiftStylizedLit.shader nell'Inspector per leggere gli errori e mandameli.");
                return null;
            }
            stylized = candidate;
            return stylized;
        }

        /// <summary>Chiede a Unity se il shader ha errori (via reflection, per non dipendere da un'API interna).</summary>
        private static bool ShaderHasErrors(Shader shader)
        {
            try
            {
                System.Reflection.MethodInfo method = typeof(ShaderUtil).GetMethod(
                    "ShaderHasError", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (method == null) return false;
                return (bool)method.Invoke(null, new object[] { shader });
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public static Shader LitShader()
        {
            Shader styled = StylizedShader();
            if (styled != null) return styled;
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
            ConfigureStylized(material, key, spec);
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
            ConfigureStylized(material, key, new MaterialSpec(Color.white, smoothness, 0f, 0f));
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

        /// <summary>Dosa i dettagli del shader stilizzato in base al tipo di materiale (legno, neve, stoffa...).</summary>
        private static void ConfigureStylized(Material material, string key, MaterialSpec spec)
        {
            if (!material.HasProperty("_NoiseStrength")) return; // non è il shader stilizzato

            bool wood = key.StartsWith("Wood") || key.StartsWith("Bark") || key.StartsWith("Floor") || key.StartsWith("Dock");
            bool snow = key.StartsWith("Snow");
            bool plaster = key.StartsWith("Plaster") || key.StartsWith("Wallpaper");
            bool cloth = key.StartsWith("Cloth") || key.StartsWith("Rug") || key.StartsWith("Red") || key.StartsWith("Green");
            bool glowing = spec.Emission > 0f;

            float noise = wood ? 0.07f : snow ? 0.025f : plaster ? 0.045f : cloth ? 0.08f : 0.05f;
            float grain = key.StartsWith("Floor") || key.StartsWith("Dock") ? 0f : (wood ? 0.2f : 0f);
            material.SetFloat("_NoiseStrength", glowing ? 0f : noise);
            material.SetFloat("_NoiseScale", snow ? 6f : 4f);
            material.SetFloat("_GrainStrength", grain);
            material.SetFloat("_GroundShade", (glowing || snow && key == "SnowGround") ? 0f : 0.4f);
            material.SetFloat("_GroundHeight", 1.15f);
            material.SetFloat("_RimStrength", snow ? 0.35f : (glowing ? 0f : 0.1f));
            material.SetColor("_RimColor", snow ? new Color(0.7f, 0.82f, 1f) : new Color(1f, 0.88f, 0.7f));
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
