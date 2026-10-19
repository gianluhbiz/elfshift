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

        // Posizioni principali (metri). Il salone va da x=-14 a 14 e da z=-10 a 10; la banchina continua oltre x=14.
        private static readonly Vector3 PlayerStart = new Vector3(-6f, 0f, -5f);
        private static readonly Vector3 ConstructionBenchPos = new Vector3(-6.5f, 0f, 6.8f);
        private static readonly Vector3 PaintBenchPos = new Vector3(0f, 0f, 6.8f);
        private static readonly Vector3 WrapBenchPos = new Vector3(6.5f, 0f, 6.8f);
        private static readonly Vector3 SleighPos = new Vector3(18.5f, 0f, 0f);

        private static GameData data;
        private static Material particleMaterial;
        private static Material blobMaterial;
        private static Mesh blobMesh;
        private static Transform blobGroup;
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

                Step("Shader e qualità grafica...", 0.05f);
                AssetDatabase.ImportAsset("Assets/Shaders/ElfshiftStylizedLit.shader", ImportAssetOptions.ForceSynchronousImport);
                EffectsBuilder.ApplyQualitySettings();

                Step("Texture e materiali...", 0.08f);
                Texture2D planks = SaveTexture(ProceduralTextures.WoodPlanks(512), AssetUtil.GeneratedTextures + "/floor_planks.png");
                Material floorMat = MaterialLibrary.GetTextured("FloorPlanks", planks, Vector2.one, 0.3f);
                Material dockMat = MaterialLibrary.GetTextured("DockPlanks", planks, Vector2.one, 0.2f);
                dockMat.SetColor("_BaseColor", new Color(0.8f, 0.84f, 0.95f));
                dockMat.SetColor("_Color", new Color(0.8f, 0.84f, 0.95f));
                Texture2D stripes = SaveTexture(ProceduralTextures.WallStripes(512), AssetUtil.GeneratedTextures + "/wallpaper.png");
                Material wallMat = MaterialLibrary.GetTextured("Wallpaper", stripes, Vector2.one, 0.1f);
                Texture2D soft = SaveTexture(ProceduralTextures.SoftCircle(64), AssetUtil.GeneratedTextures + "/soft_circle.png");
                particleMaterial = EffectsBuilder.ParticleMaterial(soft);
                blobMaterial = MaterialLibrary.GetBlobShadow(soft);

                Step("Oggetti di gioco (legno, trenino)...", 0.16f);
                GameObject woodPrefab = SaveAsPrefab(ItemModels.BuildWood(null), AssetUtil.GeneratedPrefabs + "/Items/Item_Wood.prefab");
                GameObject trainPrefab = SaveAsPrefab(ItemModels.BuildTrain(null), AssetUtil.GeneratedPrefabs + "/Items/Item_Train.prefab");
                data = DataBuilder.Build(woodPrefab, trainPrefab);

                Step("Sistemi di gioco...", 0.24f);
                GameObject systems = new GameObject("_Game");
                GameSession session = systems.AddComponent<GameSession>();
                InputReader input = systems.AddComponent<InputReader>();
                OrderManager orders = systems.AddComponent<OrderManager>();
                Wire.Ref(orders, "session", session);
                Wire.Refs(orders, "pool", new UnityEngine.Object[] { data.TrainOrder });
                Wire.Int(orders, "maxActive", 1);
                GameObject iconsObject = new GameObject("ItemIcons");
                ItemIconService icons = iconsObject.AddComponent<ItemIconService>();

                Step("Pavimento, pareti e soffitto...", 0.32f);
                GameObject environment = new GameObject("Environment");
                ShellModels.Build(environment.transform, floorMat, dockMat, wallMat);
                BuildBlobMesh();
                blobGroup = new GameObject("ContactShadows").transform;
                BuildWindowsAndWallDecor(environment.transform);

                Step("Arredamento e postazioni...", 0.45f);
                GameObject furniture = new GameObject("Furniture");
                BuildShelves(furniture.transform);
                CraftingStation bench = BuildBenches(furniture.transform);
                Sleigh sleigh = BuildSleigh(furniture.transform);
                Transform shelfAnchor = furniture.transform.Find("Shelves/Shelf_Wood");

                Step("Decorazioni e angoli accoglienti...", 0.58f);
                BuildDecor(furniture.transform);
                BuildLounge(furniture.transform);
                BuildOutdoors(furniture.transform);

                Step("Luci ed effetti...", 0.7f);
                Light sun = BuildLighting(environment.transform);
                GameObject weather = BuildWeather();

                Step("Elfo e telecamera in prima persona...", 0.8f);
                GameObject player = BuildPlayer(input, out PlayerInteractor interactor, out Carrier carrier);

                Wire.Ref(sleigh, "orders", orders);
                if (bench == null || sun == null || weather == null) Debug.LogWarning("[Elfshift] Elementi mancanti nella scena.");

                Step("Interfaccia...", 0.9f);
                HudSources hudSources = new HudSources();
                hudSources.Session = session;
                hudSources.Orders = orders;
                hudSources.Interactor = interactor;
                hudSources.Carrier = carrier;
                hudSources.Icons = icons;
                hudSources.Bench = bench;
                hudSources.ShelfAnchor = shelfAnchor;
                hudSources.SleighAnchor = sleigh.transform;
                hudSources.MarkerObject = PropModels.BuildObjectiveArrow(null).transform;
                HudBuilder.Build(hudSources);

                Step("Salvataggio scena...", 0.96f);
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(scene, ScenePath);
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
                AssetDatabase.Refresh();
                if (player == null) Debug.LogWarning("[Elfshift] Giocatore mancante.");
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
            MaterialLibrary.ClearShaderCache();
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

        // ------------------------------------------------------------------ ombre morbide a terra (finto "ambient occlusion")
        private static void BuildBlobMesh()
        {
            ModelKit.Begin("Blob");
            MeshBuilder mb = new MeshBuilder();
            mb.HardBox(MeshBuilder.Xf(Vector3.zero), new Vector3(1f, 0.004f, 1f), mb.Sub("BlobShadow"), 0f);
            blobMesh = mb.ToMesh("BlobUnit");
            AssetUtil.SaveMesh(blobMesh, AssetUtil.GeneratedModels, "BlobUnit");
        }

        /// <summary>Alone scuro e morbido sul pavimento sotto un oggetto: lo "ancora" a terra.</summary>
        private static void AddBlob(Vector3 center, Vector2 size, float yaw = 0f)
        {
            GameObject go = new GameObject("Blob");
            go.transform.SetParent(blobGroup, false);
            go.transform.position = new Vector3(center.x, 0.045f, center.z);
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = new Vector3(size.x, 1f, size.y);
            go.AddComponent<MeshFilter>().sharedMesh = blobMesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = blobMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
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
                window.name = "Window_N" + (i + 1);
                Place(window.transform, group, new Vector3(ShellModels.WindowCenters[i], ShellModels.WindowBottom, wallInnerZ + 0.12f), 180f);
                GameObject frontWindow = PropModels.BuildWindow(null, ShellModels.WindowWidth, ShellModels.WindowHeight);
                frontWindow.name = "Window_S" + (i + 1);
                Place(frontWindow.transform, group, new Vector3(ShellModels.WindowCenters[i], ShellModels.WindowBottom, -wallInnerZ - 0.12f), 0f);
            }

            // bacheca degli ordini, corona e orologio sulla parete nord (tra le finestre)
            GameObject board = PropModels.BuildCorkBoard(null);
            board.name = "OrderBoard";
            Place(board.transform, group, new Vector3(-7.75f, 2.8f, wallInnerZ - 0.08f), 180f);
            GameObject wreath = PropModels.BuildWreath(null);
            Place(wreath.transform, group, new Vector3(3.35f, 2.9f, wallInnerZ - 0.1f), 180f);
            GameObject clock = DecorModels.BuildWallClock(null);
            Place(clock.transform, group, new Vector3(-2.25f, 3.3f, wallInnerZ - 0.1f), 180f);
            // sulla parete sud: una seconda corona e una bacheca decorativa
            GameObject wreathS = PropModels.BuildWreath(null);
            Place(wreathS.transform, group, new Vector3(-2.25f, 2.9f, -wallInnerZ + 0.1f), 0f);
            GameObject boardS = PropModels.BuildCorkBoard(null);
            boardS.name = "Board_S";
            Place(boardS.transform, group, new Vector3(3.35f, 2.8f, -wallInnerZ + 0.08f), 0f);

            // Luci di Natale appese lungo le pareti
            Transform lights = new GameObject("LightStrings").transform;
            lights.SetParent(group, false);
            float y = 4.35f;
            for (int i = 0; i < 7; i++)
            {
                float x0 = -14f + i * 4f;
                PropModels.BuildLightString(lights, "North_" + i, new Vector3(x0 + 0.1f, y, wallInnerZ - 0.12f), new Vector3(x0 + 3.9f, y, wallInnerZ - 0.12f), 0.45f, 10, BulbRenderers, BulbColors);
                PropModels.BuildLightString(lights, "South_" + i, new Vector3(x0 + 0.1f, y, -wallInnerZ + 0.12f), new Vector3(x0 + 3.9f, y, -wallInnerZ + 0.12f), 0.45f, 10, BulbRenderers, BulbColors);
            }
            for (int i = 0; i < 5; i++)
            {
                float z0 = -10f + i * 4f;
                PropModels.BuildLightString(lights, "West_" + i, new Vector3(-ShellModels.HalfWidth + 0.12f, y, z0 + 0.1f), new Vector3(-ShellModels.HalfWidth + 0.12f, y, z0 + 3.9f), 0.45f, 10, BulbRenderers, BulbColors);
            }

            // festoni di bandierine che attraversano il salone
            DecorModels.BuildBunting(group, "Bunting_A", new Vector3(-13.5f, 4.45f, -5f), new Vector3(13.5f, 4.45f, -5f), 0.7f, 34);
            DecorModels.BuildBunting(group, "Bunting_B", new Vector3(-13.5f, 4.45f, 0.2f), new Vector3(13.5f, 4.45f, 0.2f), 0.7f, 34);
        }

        // ============================================================================ scaffali
        private static void BuildShelves(Transform parent)
        {
            Transform group = new GameObject("Shelves").transform;
            group.SetParent(parent, false);
            float westX = -ShellModels.HalfWidth + FurnitureModels.ShelfDepth / 2f + 0.06f;
            float eastX = ShellModels.HalfWidth - FurnitureModels.ShelfDepth / 2f - 0.06f;

            // Scaffale del legno: l'unico attivo nella Fase 1
            GameObject wood = FurnitureModels.BuildShelf(null, "Wood");
            ItemShelf shelf = wood.AddComponent<ItemShelf>();
            Wire.Ref(shelf, "item", data.Wood);
            Wire.Ref(shelf, "spawnOrigin", wood.transform.Find("SpawnOrigin"));
            Wire.Ref(shelf, "bounceTarget", wood.transform.Find("Goods"));
            Wire.Ref(shelf, "interactionPoint", wood.transform.Find("InteractionPoint"));
            Wire.Float(shelf, "radius", 2.4f);
            Wire.Str(shelf, "displayName", "Scaffale del legno");
            SaveAsConnectedPrefab(wood, AssetUtil.GeneratedPrefabs + "/Stations/Shelf_Wood.prefab");
            Place(wood.transform, group, new Vector3(westX, 0f, 6.0f), 90f);
            AddBlob(new Vector3(westX + 0.2f, 0f, 6.0f), new Vector2(1.9f, 3.4f));

            // Scaffali di stoffa e metallo: per ora solo scenografia (si attiveranno nella Fase 2)
            GameObject cloth = FurnitureModels.BuildShelf(null, "Cloth");
            Place(cloth.transform, group, new Vector3(westX, 0f, 2.5f), 90f);
            AddBlob(new Vector3(westX + 0.2f, 0f, 2.5f), new Vector2(1.9f, 3.4f));
            GameObject metal = FurnitureModels.BuildShelf(null, "Metal");
            Place(metal.transform, group, new Vector3(westX, 0f, -1.0f), 90f);
            AddBlob(new Vector3(westX + 0.2f, 0f, -1.0f), new Vector2(1.9f, 3.4f));

            // Vetrine dei giocattoli finiti (scenografia)
            GameObject toysW = FurnitureModels.BuildShelf(null, "Toys");
            Place(toysW.transform, group, new Vector3(westX, 0f, -4.6f), 90f);
            AddBlob(new Vector3(westX + 0.2f, 0f, -4.6f), new Vector2(1.9f, 3.4f));
            GameObject toysE1 = FurnitureModels.BuildShelf(null, "Toys");
            Place(toysE1.transform, group, new Vector3(eastX, 0f, 6.4f), -90f);
            AddBlob(new Vector3(eastX - 0.2f, 0f, 6.4f), new Vector2(1.9f, 3.4f));
            GameObject toysE2 = FurnitureModels.BuildShelf(null, "Toys");
            Place(toysE2.transform, group, new Vector3(eastX, 0f, -6.4f), -90f);
            AddBlob(new Vector3(eastX - 0.2f, 0f, -6.4f), new Vector2(1.9f, 3.4f));
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
            Wire.Float(station, "radius", 2.4f);
            Wire.Str(station, "displayName", "Banco di costruzione");
            ParticleSystem dust = EffectsBuilder.CreateWorkDust(construction.transform.Find("WorkFx"), "WorkDust", Vector3.zero, particleMaterial);
            ParticleSystem sparkle = EffectsBuilder.CreateSparkle(construction.transform.Find("CompleteFx"), "CompleteSparkle", Vector3.zero, particleMaterial);
            Wire.Ref(station, "workEffect", dust);
            Wire.Ref(station, "completeEffect", sparkle);
            SaveAsConnectedPrefab(construction, AssetUtil.GeneratedPrefabs + "/Stations/Bench_Construction.prefab");
            Place(construction.transform, group, ConstructionBenchPos, 180f);
            AddBlob(ConstructionBenchPos + new Vector3(0f, 0f, 0.2f), new Vector2(3.0f, 1.9f));

            // Banco di verniciatura e banco dei pacchi: scenografia (si attiveranno nella Fase 2)
            GameObject paint = FurnitureModels.BuildWorkbench(null, "Paint", false);
            Place(paint.transform, group, PaintBenchPos, 180f);
            AddBlob(PaintBenchPos + new Vector3(0f, 0f, 0.2f), new Vector2(3.0f, 1.9f));
            GameObject wrap = FurnitureModels.BuildWorkbench(null, "Wrap", false);
            Place(wrap.transform, group, WrapBenchPos, 180f);
            AddBlob(WrapBenchPos + new Vector3(0f, 0f, 0.2f), new Vector2(3.0f, 1.9f));
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
            Wire.Float(sleigh, "radius", 2.8f);
            Wire.Str(sleigh, "displayName", "Slitta di Babbo Natale");
            ParticleSystem confetti = EffectsBuilder.CreateConfetti(sleighObject.transform.Find("DeliveryFx"), "Confetti", Vector3.zero, particleMaterial);
            Wire.Ref(sleigh, "deliveryEffect", confetti);
            SaveAsConnectedPrefab(sleighObject, AssetUtil.GeneratedPrefabs + "/Stations/Sleigh.prefab");
            Place(sleighObject.transform, group, SleighPos, 0f);
            AddBlob(SleighPos + new Vector3(0.2f, 0f, 0.2f), new Vector2(2.6f, 4.8f));

            // Lanterne e staccionata della banchina
            float dockX = ShellModels.HalfWidth + ShellModels.DockLength / 2f + 0.2f;
            for (int sx = 0; sx < 2; sx++)
            {
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    GameObject post = PropModels.BuildLanternPost(null);
                    float px = sx == 0 ? ShellModels.HalfWidth + 0.8f : ShellModels.HalfWidth + ShellModels.DockLength - 0.1f;
                    Place(post.transform, group, new Vector3(px, 0f, sz * (ShellModels.DockHalfDepth - 0.5f)), 0f);
                    AddPointLight(post.transform, "Light", new Vector3(0f, 2.02f, 0f), new Color(1f, 0.8f, 0.5f), 1.8f, 7f, true);
                }
            }
            for (int sz = -1; sz <= 1; sz += 2)
            {
                GameObject fence = PropModels.BuildFence(null, ShellModels.DockLength);
                Place(fence.transform, group, new Vector3(dockX, 0f, sz * (ShellModels.DockHalfDepth - 0.1f)), 0f);
            }
            GameObject endFence = PropModels.BuildFence(null, ShellModels.DockHalfDepth * 2f);
            Place(endFence.transform, group, new Vector3(ShellModels.HalfWidth + ShellModels.DockLength + 0.3f, 0f, 0f), 90f);

            // pila di regali pronti accanto alla slitta
            MeshBuilder gifts = new MeshBuilder();
            ModelKit.Begin("DockGifts");
            FurnitureModels.AddGift(gifts, new Vector3(0f, 0f, 0f), new Vector3(0.7f, 0.55f, 0.6f), "ClothBlue", "Gold", 15f);
            FurnitureModels.AddGift(gifts, new Vector3(0.8f, 0f, 0.15f), new Vector3(0.5f, 0.4f, 0.5f), "RedSanta", "Cream", -20f);
            FurnitureModels.AddGift(gifts, new Vector3(0.15f, 0.55f, 0f), new Vector3(0.4f, 0.35f, 0.4f), "GreenElf", "Gold", 40f);
            GameObject giftPile = new GameObject("DockGifts");
            ModelKit.Part(giftPile.transform, "Mesh", gifts);
            Place(giftPile.transform, group, new Vector3(16.2f, 0f, -3.6f), 25f);
            return sleigh;
        }

        // ============================================================================ decorazioni del salone
        private static void BuildDecor(Transform parent)
        {
            Transform group = new GameObject("Decor").transform;
            group.SetParent(parent, false);

            // tappeti
            GameObject rug = PropModels.BuildRug(null, 10f, 6.5f);
            Place(rug.transform, group, new Vector3(0f, 0f, -1.5f), 0f);
            GameObject rug2 = PropModels.BuildRug(null, 5f, 3.4f);
            Place(rug2.transform, group, new Vector3(11f, 0f, 5.2f), 0f);
            GameObject rug3 = PropModels.BuildRug(null, 3.6f, 2.4f);
            Place(rug3.transform, group, new Vector3(-6.5f, 0f, 4.4f), 0f);

            // tavolo espositore al centro
            GameObject display = DecorModels.BuildDisplayTable(null);
            Place(display.transform, group, new Vector3(-1f, 0f, -2.5f), 0f);
            AddBlob(new Vector3(-1f, 0f, -2.5f), new Vector2(2.8f, 2.8f));

            // lampadario sopra il tavolo
            Vector3 chandelierLight;
            GameObject chandelier = DecorModels.BuildChandelier(null, 1.4f, out chandelierLight);
            Place(chandelier.transform, group, new Vector3(-1f, ShellModels.WallHeight, -2.5f), 0f);
            AddPointLight(chandelier.transform, "ChandelierLight", chandelierLight, new Color(1f, 0.82f, 0.55f), 3.0f, 13f, false);

            // albero di Natale grande nell'angolo sud-ovest
            List<Renderer> treeBulbs;
            List<Color> treeColors;
            GameObject tree = PropModels.BuildChristmasTree(null, out treeBulbs, out treeColors);
            Place(tree.transform, group, new Vector3(-11.2f, 0f, -7.4f), 20f);
            BulbRenderers.AddRange(treeBulbs);
            BulbColors.AddRange(treeColors);
            AddPointLight(tree.transform, "TreeGlow", new Vector3(0f, 2.0f, 1.6f), new Color(1f, 0.85f, 0.55f), 1.6f, 6.5f, false);
            AddBlob(new Vector3(-11.2f, 0f, -7.4f), new Vector2(4.2f, 4.2f));

            // casse e barili sparsi
            GameObject crate1 = PropModels.BuildCrate(null, new Vector3(0.95f, 0.75f, 0.95f), false);
            Place(crate1.transform, group, new Vector3(-12.6f, 0f, -9.0f), 8f);
            GameObject crate2 = PropModels.BuildCrate(null, new Vector3(0.8f, 0.65f, 0.8f), false);
            Place(crate2.transform, group, new Vector3(-12.5f, 0.75f, -8.95f), -14f);
            GameObject crate3 = PropModels.BuildCrate(null, new Vector3(0.85f, 0.7f, 0.85f), false);
            Place(crate3.transform, group, new Vector3(-9.2f, 0f, -9.1f), -5f);
            GameObject crate4 = PropModels.BuildCrate(null, new Vector3(0.9f, 0.7f, 0.9f), false);
            Place(crate4.transform, group, new Vector3(12.5f, 0f, -9.0f), 12f);
            GameObject crate5 = PropModels.BuildCrate(null, new Vector3(0.8f, 0.6f, 0.8f), false);
            Place(crate5.transform, group, new Vector3(11.5f, 0f, -9.1f), -8f);
            AddBlob(new Vector3(-12.6f, 0f, -9.0f), new Vector2(1.8f, 1.8f));
            AddBlob(new Vector3(-9.2f, 0f, -9.1f), new Vector2(1.5f, 1.5f));
            AddBlob(new Vector3(12.0f, 0f, -9.0f), new Vector2(3.0f, 1.8f));
            GameObject barrel1 = PropModels.BuildBarrel(null, true);
            Place(barrel1.transform, group, new Vector3(-7.8f, 0f, -9.0f), 0f);
            GameObject barrel2 = PropModels.BuildBarrel(null, false);
            Place(barrel2.transform, group, new Vector3(-7.0f, 0f, -8.3f), 30f);
            GameObject barrel3 = PropModels.BuildBarrel(null, true);
            Place(barrel3.transform, group, new Vector3(9.8f, 0f, -9.0f), 60f);
            AddBlob(new Vector3(-7.4f, 0f, -8.7f), new Vector2(2.2f, 1.9f));
            AddBlob(new Vector3(9.8f, 0f, -9.0f), new Vector2(1.2f, 1.2f));

            // pila di regali vicino alla porta
            MeshBuilder gifts = new MeshBuilder();
            ModelKit.Begin("GiftPile");
            FurnitureModels.AddGift(gifts, new Vector3(0f, 0f, 0f), new Vector3(0.55f, 0.45f, 0.5f), "RedSanta", "Gold", 12f);
            FurnitureModels.AddGift(gifts, new Vector3(0.62f, 0f, 0.1f), new Vector3(0.42f, 0.34f, 0.42f), "ClothBlue", "Cream", -18f);
            FurnitureModels.AddGift(gifts, new Vector3(0.1f, 0.45f, 0f), new Vector3(0.36f, 0.3f, 0.36f), "GreenElf", "Gold", 35f);
            GameObject giftPile = new GameObject("GiftPile");
            ModelKit.Part(giftPile.transform, "Mesh", gifts);
            Place(giftPile.transform, group, new Vector3(12.0f, 0f, -4.2f), 20f);

            // lampade appese sopra i banchi (ognuna con la sua luce calda)
            float[] lampX = { ConstructionBenchPos.x, PaintBenchPos.x, WrapBenchPos.x };
            for (int i = 0; i < lampX.Length; i++)
            {
                GameObject lamp = PropModels.BuildHangingLamp(null, 1.5f);
                Place(lamp.transform, group, new Vector3(lampX[i], ShellModels.WallHeight, 6.8f), 0f);
                AddPointLight(lamp.transform, "LampLight", new Vector3(0f, -1.5f - 0.28f, 0f), new Color(1f, 0.78f, 0.5f), 2.6f, 9f, false);
            }
            // luci d'ambiente morbide per gli scaffali
            AddPointLight(group, "ShelfLight_A", new Vector3(-11.5f, 3.2f, 3.5f), new Color(1f, 0.82f, 0.55f), 1.6f, 9f, false);
            AddPointLight(group, "ShelfLight_B", new Vector3(-11.5f, 3.2f, -3.0f), new Color(1f, 0.82f, 0.55f), 1.6f, 9f, false);

            // lampeggio delle lucine
            GameObject twinkleObject = new GameObject("TwinkleLights");
            twinkleObject.transform.SetParent(group, false);
            TwinkleLights twinkle = twinkleObject.AddComponent<TwinkleLights>();
            twinkle.EditorSetup(BulbRenderers.ToArray(), BulbColors.ToArray());
            EditorUtility.SetDirty(twinkle);
        }

        // ============================================================================ angolo caminetto (zona relax)
        private static void BuildLounge(Transform parent)
        {
            Transform group = new GameObject("Lounge").transform;
            group.SetParent(parent, false);

            Light fireLight;
            GameObject fireplace = DecorModels.BuildFireplace(null, ShellModels.WallHeight, out fireLight);
            Place(fireplace.transform, group, new Vector3(11f, 0f, ShellModels.HalfDepth - 0.55f), 180f);
            AddBlob(new Vector3(11f, 0f, 9.0f), new Vector2(5.0f, 3.2f));

            GameObject chairA = DecorModels.BuildArmchair(null, "RedSanta");
            Place(chairA.transform, group, new Vector3(9.0f, 0f, 5.9f), 205f);
            GameObject chairB = DecorModels.BuildArmchair(null, "GreenElf");
            Place(chairB.transform, group, new Vector3(13.0f, 0f, 5.9f), 155f);
            GameObject table = DecorModels.BuildSideTable(null);
            Place(table.transform, group, new Vector3(11f, 0f, 5.3f), 0f);
            AddBlob(new Vector3(9.0f, 0f, 5.9f), new Vector2(1.7f, 1.7f));
            AddBlob(new Vector3(13.0f, 0f, 5.9f), new Vector2(1.7f, 1.7f));
            AddBlob(new Vector3(11f, 0f, 5.3f), new Vector2(1.1f, 1.1f));
        }

        // ============================================================================ esterno: pupazzi di neve, abeti
        private static void BuildOutdoors(Transform parent)
        {
            Transform group = new GameObject("Outdoors").transform;
            group.SetParent(parent, false);

            GameObject snowman = PropModels.BuildSnowman(null);
            Place(snowman.transform, group, new Vector3(20.5f, 0f, 8.6f), 200f);
            GameObject snowman2 = PropModels.BuildSnowman(null);
            snowman2.transform.localScale = Vector3.one * 0.7f;
            Place(snowman2.transform, group, new Vector3(22.2f, 0f, 7.8f), 170f);

            // anello di abeti innevati attorno al laboratorio (posizioni pseudo-casuali ma sempre uguali)
            System.Random rng = new System.Random(7);
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 70; attempt++)
            {
                float x = (float)(rng.NextDouble() * 80.0 - 40.0);
                float z = (float)(rng.NextDouble() * 70.0 - 35.0);
                // niente alberi dentro o troppo vicino al laboratorio e alla banchina
                bool nearHall = x > -19f && x < 19f && z > -15f && z < 15f;
                bool nearDock = x > 12f && x < 31f && z > -12f && z < 12f;
                if (nearHall || nearDock) continue;
                float height = 5f + (float)rng.NextDouble() * 4f;
                GameObject pine = PropModels.BuildPine(null, height);
                Place(pine.transform, group, new Vector3(x, 0f, z), (float)(rng.NextDouble() * 360.0));
                placed++;
            }
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
            sun.intensity = 1.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.55f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.38f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.24f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.016f;
            RenderSettings.fogColor = new Color(0.1f, 0.15f, 0.3f);

            EffectsBuilder.CreatePostProcessing(parent, "Assets/Settings/WorkshopVolumeProfile.asset");
            return sun;
        }

        private static GameObject BuildWeather()
        {
            GameObject weather = new GameObject("Weather");
            // Neve fuori dalle finestre, sulla banchina e attorno al laboratorio.
            EffectsBuilder.CreateStars(weather.transform, "Stars", particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_North", new Vector3(4f, 12f, 26f), new Vector3(90f, 1f, 30f), 260f, particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_South", new Vector3(4f, 12f, -26f), new Vector3(90f, 1f, 30f), 260f, particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_Dock", new Vector3(25f, 12f, 0f), new Vector3(22f, 1f, 40f), 140f, particleMaterial);
            EffectsBuilder.CreateSnow(weather.transform, "Snow_West", new Vector3(-26f, 12f, 0f), new Vector3(20f, 1f, 50f), 140f, particleMaterial);
            return weather;
        }

        // ============================================================================ giocatore in prima persona
        private static GameObject BuildPlayer(InputReader input, out PlayerInteractor interactor, out Carrier carrier)
        {
            GameObject player = new GameObject("Player");
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.92f, 0f);
            controller.stepOffset = 0.3f;
            controller.skinWidth = 0.04f;
            controller.slopeLimit = 45f;

            // Corpo dell'elfo: in prima persona non si vede, ma proietta la sua ombra sul pavimento.
            GameObject visual;
            CharacterModels.BuildElf(player.transform, out visual);
            Renderer[] bodyRenderers = visual.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < bodyRenderers.Length; i++)
                bodyRenderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            // Telecamera all'altezza degli occhi
            GameObject camObject = new GameObject("Main Camera");
            camObject.tag = "MainCamera";
            camObject.transform.SetParent(player.transform, false);
            camObject.transform.localPosition = new Vector3(0f, 1.62f, 0.05f);
            Camera cam = camObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.14f);
            cam.fieldOfView = 68f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 260f;
            camObject.AddComponent<AudioListener>();

            UniversalAdditionalCameraData extra = cam.GetUniversalAdditionalCameraData();
            extra.renderPostProcessing = true;
            extra.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            extra.antialiasingQuality = AntialiasingQuality.Medium;

            FirstPersonLook look = camObject.AddComponent<FirstPersonLook>();
            Wire.Ref(look, "body", player.transform);

            Transform handSocket = CharacterModels.BuildFirstPersonHands(camObject.transform);
            Transform handsRoot = handSocket.parent;

            carrier = player.AddComponent<Carrier>();
            Wire.Ref(carrier, "handSocket", handSocket);
            PlayerController movement = player.AddComponent<PlayerController>();
            FirstPersonFeel feel = camObject.AddComponent<FirstPersonFeel>();
            Wire.Ref(feel, "player", movement);
            Wire.Ref(feel, "look", look);
            Wire.Ref(feel, "cam", cam);
            Wire.Ref(feel, "hands", handsRoot);
            interactor = player.AddComponent<PlayerInteractor>();
            Wire.Ref(interactor, "carrier", carrier);
            Wire.Ref(interactor, "movement", movement);
            Wire.Ref(interactor, "eye", camObject.transform);

            SaveAsConnectedPrefab(player, AssetUtil.GeneratedPrefabs + "/Player.prefab");

            // Questi collegamenti puntano a oggetti di scena: si fanno dopo aver creato il prefab.
            Wire.Ref(movement, "input", input);
            Wire.Ref(interactor, "input", input);
            Wire.Ref(look, "input", input);
            player.transform.position = PlayerStart;
            player.transform.rotation = Quaternion.identity;
            return player;
        }
    }
}
