using Elfshift.Core;
using Elfshift.Orders;
using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Gli asset di dati della Fase 1: oggetti, ricetta e ordine.</summary>
    public sealed class GameData
    {
        public ItemDefinition Wood;
        public ItemDefinition Train;
        public RecipeDefinition TrainRecipe;
        public OrderDefinition TrainOrder;
    }

    /// <summary>
    /// Crea (o aggiorna) gli asset ScriptableObject in Assets/Settings/Data.
    /// Se un asset esiste già NON viene sovrascritto: tranne il modello 3D, che viene aggiornato solo se
    /// è ancora quello generato. Così puoi modificare i valori o sostituire i modelli senza perdere il lavoro.
    /// </summary>
    public static class DataBuilder
    {
        public static GameData Build(GameObject woodPrefab, GameObject trainPrefab)
        {
            AssetUtil.EnsureFolder(AssetUtil.DataFolder);
            GameData data = new GameData();

            data.Wood = SetupItem(AssetUtil.DataFolder + "/Item_Wood.asset", "wood", "Legno", ItemState.RawMaterial, woodPrefab, Palette.Hex("#B67540"));
            data.Train = SetupItem(AssetUtil.DataFolder + "/Item_Train.asset", "toy_train", "Trenino", ItemState.Toy, trainPrefab, Palette.Hex("#D12F3F"));

            string recipePath = AssetUtil.DataFolder + "/Recipe_Train.asset";
            bool recipeIsNew = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(recipePath) == null;
            data.TrainRecipe = AssetUtil.LoadOrCreate<RecipeDefinition>(recipePath);
            if (recipeIsNew)
            {
                // Legno + legno = trenino, 2,5 secondi tenendo premuto E sul banco di costruzione.
                data.TrainRecipe.EditorSetup(StationType.Construction, new[] { data.Wood, data.Wood }, data.Train, 2.5f);
                EditorUtility.SetDirty(data.TrainRecipe);
            }

            string orderPath = AssetUtil.DataFolder + "/Order_Train.asset";
            bool orderIsNew = AssetDatabase.LoadAssetAtPath<OrderDefinition>(orderPath) == null;
            data.TrainOrder = AssetUtil.LoadOrCreate<OrderDefinition>(orderPath);
            if (orderIsNew)
            {
                data.TrainOrder.EditorSetup(data.Train, 120f, 100, 5, 50, 20);
                EditorUtility.SetDirty(data.TrainOrder);
            }

            AssetDatabase.SaveAssets();
            return data;
        }

        private static ItemDefinition SetupItem(string path, string id, string displayName, ItemState state, GameObject prefab, Color accent)
        {
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
