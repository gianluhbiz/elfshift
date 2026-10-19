using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Elfshift.EditorTools
{
    /// <summary>Particelle (neve, scintille, polvere di legno, coriandoli) e post-elaborazione (bagliore, colori, vignetta).</summary>
    public static class EffectsBuilder
    {
        /// <summary>Materiale per particelle: usa uno shader semplice e sempre disponibile (Sprites/Default).</summary>
        public static Material ParticleMaterial(Texture2D softCircle)
        {
            string path = AssetUtil.GeneratedMaterials + "/M_Particle.mat";
            AssetUtil.EnsureFolder(AssetUtil.GeneratedMaterials);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_MainTex", softCircle);
            mat.SetTexture("_BaseMap", softCircle);
            mat.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static ParticleSystem NewSystem(Transform parent, string name, Vector3 localPos, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // alcune impostazioni (es. durata) richiedono il sistema fermo
            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            return ps;
        }

        /// <summary>Neve che cade da un'area rettangolare (usata fuori dalle finestre e sulla banchina).</summary>
        public static ParticleSystem CreateSnow(Transform parent, string name, Vector3 center, Vector3 areaSize, float rate, Material mat)
        {
            ParticleSystem ps = NewSystem(parent, name, center, mat);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = 8f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.9f));
            main.gravityModifier = 0.04f;
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = areaSize;

            // Deriva laterale morbida, come vento leggero.
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.25f;
            return ps;
        }

        /// <summary>Cielo stellato: punti luminosi fissi su una grande cupola attorno alla scena.</summary>
        public static ParticleSystem CreateStars(Transform parent, string name, Material mat)
        {
            ParticleSystem ps = NewSystem(parent, name, Vector3.zero, mat);
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 1f;
            main.startLifetime = 1000000f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.92f, 0.95f), new Color(0.75f, 0.85f, 1f, 0.7f));
            main.maxParticles = 400;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)300) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 110f;
            return ps;
        }

        /// <summary>Scoppio di scintille dorate (prodotto finito).</summary>
        public static ParticleSystem CreateSparkle(Transform parent, string name, Vector3 localPos, Material mat)
        {
            ParticleSystem ps = NewSystem(parent, name, Vector3.zero, mat);
            ps.transform.localPosition = localPos;
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f, 1f), new Color(1f, 0.6f, 0.3f, 1f));
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)28) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            FadeOut(ps, true);
            return ps;
        }

        /// <summary>Polvere di legno mentre si costruisce (continua finché si tiene premuto E).</summary>
        public static ParticleSystem CreateWorkDust(Transform parent, string name, Vector3 localPos, Material mat)
        {
            ParticleSystem ps = NewSystem(parent, name, Vector3.zero, mat);
            ps.transform.localPosition = localPos;
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.96f, 0.82f, 0.58f, 0.85f), new Color(0.85f, 0.65f, 0.4f, 0.8f));
            main.gravityModifier = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 28f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.15f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            FadeOut(ps, false);
            return ps;
        }

        /// <summary>Coriandoli colorati alla consegna.</summary>
        public static ParticleSystem CreateConfetti(Transform parent, string name, Vector3 localPos, Material mat)
        {
            ParticleSystem ps = NewSystem(parent, name, Vector3.zero, mat);
            ps.transform.localPosition = localPos;
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.8f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);

            Gradient colors = new Gradient();
            colors.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.9f, 0.2f, 0.25f), 0f), new GradientColorKey(new Color(0.2f, 0.75f, 0.4f), 0.33f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.25f), 0.66f), new GradientColorKey(new Color(0.35f, 0.6f, 1f), 1f)
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            ParticleSystem.MinMaxGradient start = new ParticleSystem.MinMaxGradient(colors);
            start.mode = ParticleSystemGradientMode.RandomColor;
            main.startColor = start;
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)45) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            FadeOut(ps, true);
            return ps;
        }

        private static void FadeOut(ParticleSystem ps, bool shrink)
        {
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(fade);
            if (shrink)
            {
                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            }
        }

        // ------------------------------------------------------------------ Qualità grafica
        /// <summary>
        /// Alza la qualità del progetto URP: anti-aliasing 4x, ombre morbide ad alta risoluzione, più luci per oggetto.
        /// Usa SerializedObject: le impostazioni che non esistono in questa versione di URP vengono semplicemente ignorate.
        /// </summary>
        public static void ApplyQualitySettings()
        {
            RenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline;
            if (asset == null) return;
            SerializedObject so = new SerializedObject(asset);
            SetInt(so, "m_MSAA", 4);
            SetInt(so, "m_MainLightShadowmapResolution", 4096);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetInt(so, "m_SoftShadowQuality", 2);
            SetFloat(so, "m_ShadowDistance", 70f);
            SetInt(so, "m_ShadowCascadeCount", 4);
            SetBool(so, "m_SupportsHDR", true);
            SetInt(so, "m_AdditionalLightsPerObjectLimit", 8);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);

            SerializedProperty list = so.FindProperty("m_RendererDataList");
            if (list != null && list.isArray && list.arraySize > 0)
                EnsureAmbientOcclusion(list.GetArrayElementAtIndex(0).objectReferenceValue);
        }

        /// <summary>
        /// Aggiunge (una volta sola) l'effetto "Screen Space Ambient Occlusion" di URP al renderer:
        /// ombre morbide negli angoli, sotto i mobili e fra gli oggetti vicini. Fa sembrare la scena molto più "vera".
        /// È fatto via reflection perché la classe dell'effetto non è pubblica; se qualcosa non va,
        /// si limita a scrivere un avviso (il gioco funziona lo stesso, solo senza questo effetto).
        /// </summary>
        private static void EnsureAmbientOcclusion(Object rendererData)
        {
            if (rendererData == null) return;
            try
            {
                SerializedObject data = new SerializedObject(rendererData);
                SerializedProperty features = data.FindProperty("m_RendererFeatures");
                SerializedProperty map = data.FindProperty("m_RendererFeatureMap");
                if (features == null || map == null) return;

                Object feature = null;
                for (int i = 0; i < features.arraySize; i++)
                {
                    Object f = features.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion") { feature = f; break; }
                }

                if (feature == null)
                {
                    System.Type type = typeof(ScriptableRendererFeature).Assembly.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion");
                    if (type == null) { Debug.LogWarning("[Elfshift] SSAO non disponibile in questa versione di URP."); return; }
                    feature = ScriptableObject.CreateInstance(type);
                    feature.name = "ScreenSpaceAmbientOcclusion";
                    AssetDatabase.AddObjectToAsset(feature, rendererData);
                    string guid;
                    long localId;
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out guid, out localId)) return;
                    features.arraySize++;
                    features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                // impostazioni: effetto morbido e ampio, non "sporco"
                SerializedObject fso = new SerializedObject(feature);
                SetFloat(fso, "m_Settings.Intensity", 1.1f);
                SetFloat(fso, "m_Settings.Radius", 0.4f);
                SetFloat(fso, "m_Settings.DirectLightingStrength", 0.3f);
                SetInt(fso, "m_Settings.Source", 1);       // profondità + normali (più preciso)
                SetBool(fso, "m_Settings.Downsample", false);
                fso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(rendererData);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Elfshift] Non sono riuscito ad attivare l'ombreggiatura SSAO: " + e.Message);
            }
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.intValue = value;
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.boolValue = value;
        }

        private static void SetFloat(SerializedObject so, string name, float value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.floatValue = value;
        }

        // ------------------------------------------------------------------ Post-elaborazione URP
        /// <summary>Volume globale: bagliore morbido, colori caldi e vivaci, leggera vignetta.</summary>
        public static Volume CreatePostProcessing(Transform parent, string profileAssetPath)
        {
            AssetUtil.EnsureFolder(System.IO.Path.GetDirectoryName(profileAssetPath).Replace('\\', '/'));
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(profileAssetPath) != null) AssetDatabase.DeleteAsset(profileAssetPath);

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profileAssetPath);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.55f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(1f, 0.93f, 0.8f));
            AssetDatabase.AddObjectToAsset(bloom, profile);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            AssetDatabase.AddObjectToAsset(tonemapping, profile);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.25f);
            color.contrast.Override(8f);
            color.saturation.Override(14f);
            color.colorFilter.Override(new Color(1f, 0.97f, 0.93f));
            AssetDatabase.AddObjectToAsset(color, profile);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.26f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.05f, 0.04f, 0.12f));
            AssetDatabase.AddObjectToAsset(vignette, profile);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject go = new GameObject("PostProcessing");
            go.transform.SetParent(parent, false);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
            return volume;
        }
    }
}
