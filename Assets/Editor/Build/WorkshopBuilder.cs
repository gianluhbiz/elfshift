using System;
using System.Collections.Generic;
using Elfshift.Core;
using Elfshift.Interaction;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.Production;
using Elfshift.UI;
using Elfshift.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Costruisce da zero tutta la Fase 1: texture, materiali, modelli 3D, prefab, dati di gioco,
    /// scena del laboratorio, luci, effetti e interfaccia. È ripetibile: puoi rilanciarlo quando vuoi.
    /// </summary>
    public static class WorkshopBuilder
    {
        public const string ScenePath = AssetUtil.SceneFolder + "/Workshop.unity";

        // Posizioni principali del laboratorio (metri). Il pavimento va da x=-8 a 8 e da z=-6 a 6.
        private static readonly Vector3 PlayerStart = new Vector3(-1.2f, 0f, -1.5f);
        private static readonly Vector3 ConstructionBenchPos = new Vector3(-3.0f, 0f, 2.6f);
        private static readonly Vector3 PaintBenchPos = new Vector3(0.9f, 0f, 2.6f);
        private static readonly Vector3 WrapBenchPos = new Vector3(4.7f, 0f, 2.6f);
        private static readonly Vector3 SleighPos = new Vector3(11.0f, 0f, 0f);

        private static GameData data;
        private static Material particleMaterial;
        private static readonly List<Renderer> BulbRenderers = new List<Renderer>();
        private static readonly List<Color> BulbColors = new List<Color>();

        public static string BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "Operazione annullata.";
            try
            {
                Step("Preparazione cartelle...", 0.02f);
                PrepareFolders();
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                ResetGeneratedAssets();

                Step("Texture e materiali...", 0.08f);
                Texture2D planks = SaveTexture(ProceduralTextures.WoodPlanks(512), AssetUtil.GeneratedTextures + "/floor_planks.png");
                Material floorMat = MaterialLibrary.GetTextured("FloorPlanks", planks, Vector2.one, 0.3f);
                Material dockMat = MaterialLibrary.GetTextured("DockPlanks", planks, Vector2.one, 0.2f);
                dockMat.SetColor("_BaseColor", new Color(0.8f, 0.84f, 0.95f));
                dockMat.SetColor("_Color", new Color(0.8f, 0.84f, 0.95f));
                Texture2D soft = SaveTexture(ProceduralTextures.SoftCircle(64), AssetUtil.GeneratedTextures + "/soft_circle.png");
                particleMaterial = EffectsBuilder.ParticleMaterial(soft);

                Step("Oggetti di gioco (legno, trenino)...", 0.18f);
                GameObject woodPrefab = SaveAsPrefab(ItemModels.BuildWood(null), AssetUtil.GeneratedPrefabs + "/Items/Item_Wood.prefab");
                GameObject trainPrefab = SaveAsPrefab(ItemModels.BuildTrain(null), AssetUtil.GeneratedPrefabs + "/Items/Item_Train.prefab");
                data = DataBuilder.Build(woodPrefab, trainPrefab);

                Step("Sistemi di gioco...", 0.28f);
                GameObject systems = new GameObject("_Game");
                GameSession session = systems.AddComponent<GameSession>();
                InputReader input = systems.AddComponent<InputReader>();
                OrderManager orders = systems.AddComponent<OrderManager>();
                Wire.Ref(orders, "session", session);
                Wire.Refs(orders, "pool", new UnityEngine.Object[] { data.TrainOrder });
                Wire.Int(orders, "maxActive", 1);
                GameObject iconsObject = new GameObject("ItemIcons");
                ItemIconService icons = iconsObject.AddComponent<ItemIconService>();

                Step("Pavimento, pareti e finestre...", 0.38f);
                GameObject environment = new GameObject("Environment");
                ShellModels.Build(environment.transform, floorMat, dockMat);
                BuildWindowsAndWallDecor(environment.transform);

                Step("Arredamento e postazioni...", 0.5f);
                GameObject furniture = new GameObject("Furniture");
                BuildShelves(furniture.transform);
                CraftingStation bench = BuildBenches(furniture.transform);
                Sleigh sleigh = BuildSleigh(furniture.transform);
                BuildDecor(furniture.transform);

                Step("Luci ed effetti...", 0.65f);
                Light sun = BuildLighting(environment.transform);
                GameObject weather = BuildWeather();

                Step("Elfo e telecamera...", 0.75f);
                Camera cam = BuildCamera();
                GameObject player = BuildPlayer(input, cam, out PlayerInteractor interactor, out Carrier carrier);
                Wire.Ref(cam.GetComponent<TopDownCamera>(), "target", player.transform);
                cam.GetComponent<TopDownCamera>().SnapToTarget();

                // Collegamenti tra la scena e gli oggetti di gioco
                Wire.Ref(sleigh, "orders", orders);
                Wire.Ref(player.GetComponent<PlayerController>(), "cameraTransform", cam.transform);
                if (bench == null || sun == null || weather == null) Debug.LogWarning("[Elfshift] Elementi mancanti nella scena.");

                Step("Interfaccia...", 0.88f);
                HudSources hudSources = new HudSources();
                hudSources.Session = session;
                hudSources.Orders = orders;
                hudSources.Interactor = interactor;
                hudSources.Carrier = carrier;
                hudSources.Icons = icons;
                HudBuilder.Build(hudSources);

                Step("Salvataggio scena...", 0.96f);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.Refresh();
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError("[Elfshift] Errore durante la costruzione: " + e);
                return "Errore: " + e.Message + "\n\nControlla la finestra Console per i dettagli.";
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ============================================================================ preparazione
        private static void Step(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("Elfshift - costruzione del laboratorio", message, progress);
        }

        private static void PrepareFolders()
        {
            string[] folders =
            {
                "Assets/Scenes", "Assets/Prefabs", "Assets/Models", "Assets/Materials", "Assets/Textures",
                "Assets/Animations", "Assets/Audio", "Assets/Settings", AssetUtil.DataFolder
            };
            for (int i = 0; i < folders.Length; i++) AssetUtil.EnsureFolder(folders[i]);
        }

        /// <summary>Svuota le cartelle "Generated": tutto ciò che contengono viene rigenerato.</summary>
        private static void ResetGeneratedAssets()
        {
            AssetUtil.ResetFolder(AssetUtil.GeneratedModels);
            AssetUtil.ResetFolder(AssetUtil.GeneratedMaterials);
            AssetUtil.ResetFolder(AssetUtil.GeneratedTextures);
            AssetUtil.ResetFolder(AssetUtil.GeneratedPrefabs);
            AssetUtil.EnsureFolder(AssetUtil.GeneratedPrefabs + "/Items");
            AssetUtil.EnsureFolder(AssetUtil.GeneratedPrefabs + "/Stations");
            MaterialLibrary.ClearCache();
            BulbRenderers.Clear();
            BulbColors.Clear();
        }

        private static Texture2D SaveTexture(Texture2D texture, string path)
        {
            Texture2D saved = ProceduralTextures.SavePng(texture, path, false, Vector4.zero);
            UnityEngine.Object.DestroyImmediate(texture);
            return saved;
        }

        /// <summary>Salva un oggetto come prefab e lo toglie dalla scena (resta solo l'asset).</summary>
        private static GameObject SaveAsPrefab(GameObject go, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Salva come prefab e lascia nella scena un'istanza collegata.</summary>
        private static GameObject SaveAsConnectedPrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
            return go;
        }

        private static void Place(Transform t, Transform parent, Vector3 position, float yawDegrees)
        {
            t.SetParent(parent, false);
            t.position = position;
            t.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        // ============================================================================ finestre e decorazioni a parete
        private static void BuildWindowsAndWallDecor(Transform parent)
        {
            Transform group = new GameObject("WallDecor").transform;
            group.SetParent(parent, false);
            float wallInnerZ = ShellModels.HalfDepth;

            for (int i = 0; i < ShellModels.WindowCenters.Length; i++)
            {
                GameObject window = PropModels.BuildWindow(null, ShellModels.WindowWidth, ShellModels.WindowHeight);
                window.name = "Window_" + (i + 1);
                // finestra rivolta verso l'interno (-Z)
                Place(window.transform, group, new Vector3(ShellModels.WindowCenters[i], ShellModels.WindowBottom, wallInnerZ + 0.12f), 180f);
            }

            GameObject board = PropModels.BuildCorkBoard(null);
            board.name = "OrderBoard";
            Place(board.transform, group, new Vector3(-2.0f, 2.35f, wallInnerZ - 0.08f), 180f);

            GameObject wreath = PropModels.BuildWreath(null);
            Place(wreath.transform, group, new Vector3(2.55f, 2.45f, wallInnerZ - 0.1f), 180f);

            // Luci di Natale appese lungo le pareti
            Transform lights = new GameObject("LightStrings").transform;
            lights.SetParent(group, false);
            float y = 3.95f;
            for (int i = 0; i < 4; i++)
            {
                float x0 = -8f + i * 4f;
                PropModels.BuildLightString(lights, "BackWall_" + i, new Vector3(x0 + 0.1f, y, wallInnerZ - 0.12f), new Vector3(x0 + 3.9f, y, wallInnerZ - 0.12f), 0.4f, 9, BulbRenderers, BulbColors);
            }
            for (int i = 0; i < 3; i++)
            {
                float z0 = -6f + i * 4f;
                PropModels.BuildLightString(lights, "LeftWall_" + i, new Vector3(-ShellModels.HalfWidth + 0.12f, y, z0 + 0.1f), new Vector3(-ShellModels.HalfWidth + 0.12f, y, z0 + 3.9f), 0.4f, 9, BulbRenderers, BulbColors);
            }
        }

        // ============================================================================ scaffali
        private static void BuildShelves(Transform parent)
        {
            Transform group = new GameObject("Shelves").transform;
            group.SetParent(parent, false);
            float x = -ShellModels.HalfWidth + FurnitureModels.ShelfDepth / 2f + 0.06f;
            float yaw = 90f; // il fronte (+Z locale) guarda verso +X, cioè verso il centro della stanza

            // Scaffale del legno: l'unico attivo nella Fase 1
            GameObject wood = FurnitureModels.BuildShelf(null, "Wood");
            ItemShelf shelf = wood.AddComponent<ItemShelf>();
            Wire.Ref(shelf, "item", data.Wood);
            Wire.Ref(shelf, "spawnOrigin", wood.transform.Find("SpawnOrigin"));
            Wire.Ref(shelf, "bounceTarget", wood.transform.Find("Goods"));
            Wire.Ref(shelf, "interactionPoint", wood.transform.Find("InteractionPoint"));
            Wire.Float(shelf, "radius", 1.9f);
            SaveAsConnectedPrefab(wood, AssetUtil.GeneratedPrefabs + "/Stations/Shelf_Wood.prefab");
            Place(wood.transform, group, new Vector3(x, 0f, 4.0f), yaw);

            // Scaffali di stoffa e metallo: per ora solo scenografia (si attiveranno nella Fase 2)
            GameObject cloth = FurnitureModels.BuildShelf(null, "Cloth");
            Place(cloth.transform, group, new Vector3(x, 0f, 1.2f), yaw);
            GameObject metal = FurnitureModels.BuildShelf(null, "Metal");
            Place(metal.transform, group, new Vector3(x, 0f, -1.6f), yaw);
        }

        // ============================================================================ banchi
        private static CraftingStation BuildBenches(Transform parent)
        {
            Transform group = new GameObject("Workbenches").transform;
            group.SetParent(parent, false);

            // Banco di costruzione (attivo)
            GameObject construction = FurnitureModels.BuildWorkbench(null, "Construction", true);
            CraftingStation station = construction.AddComponent<CraftingStation>();
            Wire.Enum(station, "stationType", (int)StationType.Construction);
            Wire.Refs(station, "recipes", new UnityEngine.Object[] { data.TrainRecipe });
            Wire.Refs(station, "inputSlots", new UnityEngine.Object[] { construction.transform.Find("Input1"), construction.transform.Find("Input2") });
            Wire.Ref(station, "outputSlot", construction.transform.Find("Output"));
            Wire.Ref(station, "interactionPoint", construction.transform.Find("InteractionPoint"));
            Wire.Float(station, "radius", 1.8f);
            ParticleSystem dust = EffectsBuilder.CreateWorkDust(construction.transform.Find("WorkFx"), "WorkDust", Vector3.zero, particleMaterial);
            ParticleSystem sparkle = EffectsBuilder.CreateSparkle(construction.transform.Find("CompleteFx"), "CompleteSparkle", Vector3.zero, particleMaterial);
            Wire.Ref(station, "workEffect", dust);
            Wire.Ref(station, "completeEffect", sparkle);
            SaveAsConnectedPrefab(construction, AssetUtil.GeneratedPrefabs + "/Stations/Bench_Construction.prefab");
            Place(construction.transform, group, ConstructionBenchPos, 180f);

            // Banco di verniciatura e banco dei pacchi: scenografia (si attiveranno nella Fase 2)
            GameObject paint = FurnitureModels.BuildWorkbench(null, "Paint", false);
            Place(paint.transform, group, PaintBenchPos, 180f);
            GameObject wrap = FurnitureModels.BuildWorkbench(null, "Wrap", false);
            Place(wrap.transform, group, WrapBenchPos, 180f);
            return station;
        }

        // ============================================================================ slitta
        private static Sleigh BuildSleigh(Transform parent)
        {
            Transform group = new GameObject("DeliveryZone").transform;
            group.SetParent(parent, false);

            GameObject sleighObject = FurnitureModels.BuildSleigh(null);
            Sleigh sleigh = sleighObject.AddComponent<Sleigh>();
            Wire.Ref(sleigh, "dropPoint", sleighObject.transform.Find("DropPoint"));
            Wire.Ref(sleigh, "bounceTarget", sleighObject.transform.Find("Body"));
            Wire.Ref(sleigh, "interactionPoint", sleighObject.transform.Find("InteractionPoint"));
            Wire.Float(sleigh, "radius", 2.2f);
            ParticleSystem confetti = EffectsBuilder.CreateConfetti(sleighObject.transform.Find("DeliveryFx"), "Confetti", Vector3.zero, particleMaterial);
            Wire.Ref(sleigh, "deliveryEffect", confetti);
            SaveAsConnectedPrefab(sleighObject, AssetUtil.GeneratedPrefabs + "/Stations/Sleigh.prefab");
            Place(sleighObject.transform, group, SleighPos, 0f);

            // Lanterne e staccionata della banchina
            float dockX = ShellModels.HalfWidth + ShellModels.DockLength / 2f + 0.2f;
            for (int sx = 0; sx < 2; sx++)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    GameObject post = PropModels.BuildLanternPost(null);
                    float px = sx == 0 ? ShellModels.HalfWidth + 0.7f : ShellModels.HalfWidth + ShellModels.DockLength - 0.1f;
                    Place(post.transform, group, new Vector3(px, 0f, sz * (ShellModels.DockHalfDepth - 0.45f)), 0f);
                    AddPointLight(post.transform, "Light", new Vector3(0f, 2.02f, 0f), new Color(1f, 0.8f, 0.5f), 1.8f, 6f, true);
                }
            }
            for (int sz = -1; sz <= 1; sz += 2)
            {
                GameObject fence = PropModels.BuildFence(null, ShellModels.DockLength);
                Place(fence.transform, group, new Vector3(dockX, 0f, sz * (ShellModels.DockHalfDepth - 0.1f)), 0f);
            }
            GameObject endFence = PropModels.BuildFence(null, ShellModels.DockHalfDepth * 2f);
            Place(endFence.transform, group, new Vector3(ShellModels.HalfWidth + ShellModels.DockLength + 0.3f, 0f, 0f), 90f);
            return sleigh;
        }

        // ============================================================================ decorazioni
        private static void BuildDecor(Transform parent)
        {
            Transform group = new GameObject("Decor").transform;
            group.SetParent(parent, false);

            // tappeto al centro
            GameObject rug = PropModels.BuildRug(null, 6.4f, 4.0f);
            Place(rug.transform, group, new Vector3(0f, 0f, -0.2f), 0f);

            // albero di Natale nell'angolo in fondo a destra
            List<Renderer> treeBulbs;
            List<Color> treeColors;
            GameObject tree = PropModels.BuildChristmasTree(null, out treeBulbs, out treeColors);
            Place(tree.transform, group, new Vector3(6.4f, 0f, 4.2f), 0f);
            BulbRenderers.AddRange(treeBulbs);
            BulbColors.AddRange(treeColors);
            AddPointLight(tree.transform, "TreeGlow", new Vector3(0f, 2.0f, 1.4f), new Color(1f, 0.85f, 0.55f), 1.4f, 5f, false);

            // casse e barili
            GameObject crate1 = PropModels.BuildCrate(null, new Vector3(0.9f, 0.7f, 0.9f), false);
            Place(crate1.transform, group, new Vector3(-6.7f, 0f, -4.9f), 8f);
            GameObject crate2 = PropModels.BuildCrate(null, new Vector3(0.75f, 0.6f, 0.75f), false);
            Place(crate2.transform, group, new Vector3(-6.6f, 0.7f, -4.85f), -14f);
            GameObject crate3 = PropModels.BuildCrate(null, new Vector3(0.8f, 0.65f, 0.8f), false);
            Place(crate3.transform, group, new Vector3(-5.6f, 0f, -5.1f), -5f);
            GameObject barrel1 = PropModels.BuildBarrel(null, true);
            Place(barrel1.transform, group, new Vector3(6.6f, 0f, -5.0f), 0f);
            GameObject barrel2 = PropModels.BuildBarrel(null, false);
            Place(barrel2.transform, group, new Vector3(7.3f, 0f, -4.2f), 30f);

            // pila di regali vicino alla porta
            MeshBuilder gifts = new MeshBuilder();
            ModelKit.Begin("GiftPile");
            FurnitureModels.AddGift(gifts, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.45f, 0.5f), "RedSanta", "Gold", 12f);
            FurnitureModels.AddGift(gifts, new Vector3(0.62f, 0f, 0.1f), new Vector3(0.42f, 0.34f, 0.42f), "ClothBlue", "Cream", -18f);
            FurnitureModels.AddGift(gifts, new Vector3(0.1f, 0.45f, 0f), new Vector3(0.36f, 0.3f, 0.36f), "GreenElf", "Gold", 35f);
            GameObject giftPile = new GameObject("GiftPile");
            ModelKit.Part(giftPile.transform, "Mesh", gifts);
            Place(giftPile.transform, group, new Vector3(7.0f, 0f, -1.6f), 20f);

            // lampade appese sopra i banchi (ognuna con la sua luce calda)
            float[] lampX = { ConstructionBenchPos.x, PaintBenchPos.x, WrapBenchPos.x };
            for (int i = 0; i < lampX.Length; i++)
            {
                GameObject lamp = PropModels.BuildHangingLamp(null, 1.0f);
                Place(lamp.transform, group, new Vector3(lampX[i], 4.0f, 2.6f), 0f);
                AddPointLight(lamp.transform, "LampLight", new Vector3(0f, -1.0f - 0.28f, 0f), new Color(1f, 0.78f, 0.5f), 2.4f, 8f, false);
            }

            // pupazzo di neve e abeti sullo sfondo
            GameObject snowman = PropModels.BuildSnowman(null);
            Place(snowman.transform, group, new Vector3(10.2f, 0f, 5.4f), 200f);
            float[][] pines =
            {
                new[] { -9f, 9.5f, 5.5f }, new[] { -5f, 11f, 6.5f }, new[] { -1f, 9.8f, 5f }, new[] { 3f, 12f, 7f }, new[] { 7f, 10f, 5.8f },
                new[] { 11f, 9.2f, 6.2f }, new[] { 15f, 8f, 5.4f }, new[] { 18f, 5f, 6.5f }, new[] { 17.5f, -3f, 5f }, new[] { 15f, -7f, 6f },
                new[] { -12f, 2f, 6f }, new[] { -13f, -4f, 5f }, new[] { -11.5f, 7f, 5.5f }, new[] { 20f, 1f, 7f }, new[] { 6f, 15f, 8f }, new[] { -8f, 14f, 7.5f }
            };
            for (int i = 0; i < pines.Length; i++)
            {
                GameObject pine = PropModels.BuildPine(null, pines[i][2]);
                Place(pine.transform, group, new Vector3(pines[i][0], 0f, pines[i][1]), i * 47f);
            }

            // lampeggio delle lucine
            GameObject twinkleObject = new GameObject("TwinkleLights");
            twinkleObject.transform.SetParent(group, false);
            TwinkleLights twinkle = twinkleObject.AddComponent<TwinkleLights>();
            twinkle.EditorSetup(BulbRenderers.ToArray(), BulbColors.ToArray());
            EditorUtility.SetDirty(twinkle);
        }

        private static Light AddPointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range, bool flicker)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            if (flicker) go.AddComponent<FlickerLight>();
            return light;
        }

        // ============================================================================ luci, neve, volume
        private static Light BuildLighting(Transform parent)
        {
            GameObject sunObject = new GameObject("MainLight");
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.87f, 0.7f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.55f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.38f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.02f;
            RenderSettings.fogColor = new Color(0.1f, 0.15f, 0.3f);

            EffectsBuilder.CreatePostProcessing(parent, "Assets/Settings/WorkshopVolumeProfile.asset");
            return sun;
        }

        private static GameObject BuildWeather()
        {
            GameObject weather = new GameObject("Weather");
            // Neve fuori dalle finestre (dietro il muro di fondo), sulla banchina e a sinistra del laboratorio.
            EffectsBuilder.CreateSnow(weather.transform, "Snow_Back", new Vector3(4f, 11f, 13f), new Vector3(46f, 1f, 16f), 170f, particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_Dock", new Vector3(14f, 11f, 0f), new Vector3(14f, 1f, 22f), 90f, particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_Left", new Vector3(-15f, 11f, 0f), new Vector3(14f, 1f, 26f), 80f, particleMaterial);
            return weather;
        }

        // ============================================================================ telecamera e giocatore
        private static Camera BuildCamera()
        {
            GameObject camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            Camera cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.1f, 0.22f);
            cam.fieldOfView = 36f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;
            camObject.AddComponent<AudioListener>();

            UniversalAdditionalCameraData extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = true;
            extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            extra.antialiasingQuality = AntialiasingQuality.Medium;

            camObject.AddComponent<TopDownCamera>();
            return cam;
        }

        private static GameObject BuildPlayer(InputReader input, Camera cam, out PlayerInteractor interactor, out Carrier carrier)
        {
            GameObject player = new GameObject("Player");
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.92f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.04f;
            controller.slopeLimit = 45f;

            GameObject visual;
            Transform hand = CharacterModels.BuildElf(player.transform, out visual);

            carrier = player.AddComponent<Carrier>();
            Wire.Ref(carrier, "handSocket", hand);
            PlayerController movement = player.AddComponent<PlayerController>();
            interactor = player.AddComponent<PlayerInteractor>();
            Wire.Ref(interactor, "carrier", carrier);
            Wire.Ref(interactor, "movement", movement);

            SaveAsConnectedPrefab(player, AssetUtil.GeneratedPrefabs + "/Player.prefab");

            // Questi collegamenti puntano a oggetti di scena: si fanno dopo aver creato il prefab.
            Wire.Ref(movement, "input", input);
            Wire.Ref(interactor, "input", input);
            Wire.Ref(movement, "cameraTransform", cam.transform);
            player.transform.position = PlayerStart;
            player.transform.rotation = Quaternion.identity;
            return player;
        }
    }
}
