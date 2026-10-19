using Elfshift.Core;
using Elfshift.Orders;
using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Una "linea" di produzione: grezzo → dipinto → pacco, con le tre ricette e la letterina.</summary>
    public sealed class ToyLine
    {
        public ItemDefinition Raw;
        public ItemDefinition Painted;
        public ItemDefinition Gift;
        public RecipeDefinition BuildRecipe;
        public RecipeDefinition PaintRecipe;
        public RecipeDefinition WrapRecipe;
        public OrderDefinition Order;
    }

    /// <summary>Prefab 3D degli oggetti, preparati dal costruttore prima dei dati.</summary>
    public sealed class ItemPrefabs
    {
        public GameObject Wood, Cloth, Metal;
        public GameObject TrainRaw, TrainPainted, TrainGift;
        public GameObject TeddyRaw, TeddyPainted, TeddyGift;
        public GameObject RobotRaw, RobotPainted, RobotGift;
    }

    /// <summary>Tutti gli asset di dati del laboratorio.</summary>
    public sealed class GameData
    {
        public ItemDefinition Wood;
        public ItemDefinition Cloth;
        public ItemDefinition Metal;
        public ToyLine Train;
        public ToyLine Teddy;
        public ToyLine Robot;

        public ToyLine[] Lines => new[] { Train, Teddy, Robot };

        public RecipeDefinition[] RecipesFor(StationType station)
        {
            ToyLine[] lines = Lines;
            RecipeDefinition[] result = new RecipeDefinition[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                result[i] = station == StationType.Construction ? lines[i].BuildRecipe : (station == StationType.Paint ? lines[i].PaintRecipe : lines[i].WrapRecipe);
            return result;
        }

        public OrderDefinition[] Orders => new[] { Train.Order, Teddy.Order, Robot.Order };
    }

    /// <summary>
    /// Crea (o aggiorna) gli asset ScriptableObject in Assets/Settings/Data.
    /// Se un asset esiste già NON viene sovrascritto: tranne il modello 3D, che viene aggiornato solo se
    /// è ancora quello generato. Così puoi modificare i valori o sostituire i modelli senza perdere il lavoro.
    /// </summary>
    public static class DataBuilder
    {
        public static GameData Build(ItemPrefabs p)
        {
            AssetUtil.EnsureFolder(AssetUtil.DataFolder);
            GameData data = new GameData();

            data.Wood = SetupItem("Item_Wood", "wood", "Legno", ItemState.RawMaterial, p.Wood, Palette.Hex("#B67540"));
            data.Cloth = SetupItem("Item_Cloth", "cloth", "Stoffa", ItemState.RawMaterial, p.Cloth, Palette.Hex("#D12F3F"));
            data.Metal = SetupItem("Item_Metal", "metal", "Metallo", ItemState.RawMaterial, p.Metal, Palette.Hex("#8C99A8"));

            // Trenino: 2 legni. Orsetto: 2 stoffe. Robot: metallo + metallo.
            data.Train = SetupLine("Train", "Trenino", data.Wood, data.Wood, p.TrainRaw, p.TrainPainted, p.TrainGift, Palette.Hex("#D12F3F"), 2.5f, 160f, 120, 6);
            data.Teddy = SetupLine("Teddy", "Orsetto", data.Cloth, data.Cloth, p.TeddyRaw, p.TeddyPainted, p.TeddyGift, Palette.Hex("#B8772F"), 2.8f, 170f, 140, 7);
            data.Robot = SetupLine("Robot", "Robot", data.Metal, data.Metal, p.RobotRaw, p.RobotPainted, p.RobotGift, Palette.Hex("#4F82D8"), 3.0f, 180f, 160, 8);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static ToyLine SetupLine(string key, string name, ItemDefinition a, ItemDefinition b,
            GameObject rawPrefab, GameObject paintedPrefab, GameObject giftPrefab, Color accent,
            float buildSeconds, float orderSeconds, int points, int cookies)
        {
            string lower = key.ToLowerInvariant();
            ToyLine line = new ToyLine();
            line.Raw = SetupItem("Item_" + key + "Raw", "toy_" + lower + "_raw", name + " grezzo", ItemState.Toy, rawPrefab, accent);
            line.Painted = SetupItem("Item_" + key + "Painted", "toy_" + lower, name, ItemState.PaintedToy, paintedPrefab, accent);
            line.Gift = SetupItem("Item_" + key + "Gift", "gift_" + lower, "Pacco " + name.ToLowerInvariant(), ItemState.WrappedGift, giftPrefab, accent);

            line.BuildRecipe = SetupRecipe("Recipe_" + key + "Build", StationType.Construction, new[] { a, b }, line.Raw, buildSeconds);
            line.PaintRecipe = SetupRecipe("Recipe_" + key + "Paint", StationType.Paint, new[] { line.Raw }, line.Painted, 2.0f);
            line.WrapRecipe = SetupRecipe("Recipe_" + key + "Wrap", StationType.Wrap, new[] { line.Painted }, line.Gift, 1.6f);

            string orderPath = AssetUtil.DataFolder + "/Order_" + key + ".asset";
            bool isNew = AssetDatabase.LoadAssetAtPath<OrderDefinition>(orderPath) == null;
            line.Order = AssetUtil.LoadOrCreate<OrderDefinition>(orderPath);
            if (isNew || line.Order.RequestedItem == null || line.Order.RequestedItem.State != ItemState.WrappedGift)
            {
                // Si consegna il PACCO; sulla letterina si vede il giocattolo dipinto.
                line.Order.EditorSetup(line.Gift, orderSeconds, points, cookies, points / 2, 25, line.Painted);
                EditorUtility.SetDirty(line.Order);
            }
            return line;
        }

        private static RecipeDefinition SetupRecipe(string assetName, StationType station, ItemDefinition[] inputs, ItemDefinition output, float seconds)
        {
            string path = AssetUtil.DataFolder + "/" + assetName + ".asset";
            bool isNew = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path) == null;
            RecipeDefinition recipe = AssetUtil.LoadOrCreate<RecipeDefinition>(path);
            if (isNew || recipe.Output == null)
            {
                recipe.EditorSetup(station, inputs, output, seconds);
                EditorUtility.SetDirty(recipe);
            }
            return recipe;
        }

        private static ItemDefinition SetupItem(string assetName, string id, string displayName, ItemState state, GameObject prefab, Color accent)
        {
            string path = AssetUtil.DataFolder + "/" + assetName + ".asset";
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            bool isNew = item == null;
            if (isNew) item = AssetUtil.LoadOrCreate<ItemDefinition>(path);

            GameObject visual = prefab;
            if (!isNew && item.VisualPrefab != null)
            {
                // Se hai assegnato un tuo modello (non generato) lo lasciamo dov'è.
                string existing = AssetDatabase.GetAssetPath(item.VisualPrefab);
                if (!string.IsNullOrEmpty(existing) && !existing.Contains("/Generated/")) visual = item.VisualPrefab;
            }
            item.EditorSetup(isNew ? id : item.Id, isNew ? displayName : item.DisplayName, state, visual, accent);
            EditorUtility.SetDirty(item);
            return item;
        }
    }
}
