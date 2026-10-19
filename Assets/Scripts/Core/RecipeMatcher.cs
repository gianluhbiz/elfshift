using System.Collections.Generic;

namespace Elfshift.Core
{
    /// <summary>
    /// Logica pura (senza GameObject) per confrontare ciò che sta su un banco con le ricette.
    /// Nessuna allocazione: si può chiamare liberamente. Testata in Assets/Tests/EditMode.
    /// </summary>
    public static class RecipeMatcher
    {
        /// <summary>Restituisce la ricetta completata esattamente dagli oggetti dati, oppure null.</summary>
        public static RecipeDefinition FindComplete(
            IReadOnlyList<RecipeDefinition> recipes, StationType station, IReadOnlyList<ItemDefinition> items)
        {
            if (recipes == null || items == null || items.Count == 0) return null;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (!IsUsable(recipe, station)) continue;
                if (recipe.Inputs.Length != items.Count) continue;
                if (IsSubset(items, recipe.Inputs)) return recipe;
            }
            return null;
        }

        /// <summary>
        /// Vero se gli oggetti dati potrebbero ancora diventare (con altri ingredienti) una ricetta.
        /// Serve a rifiutare gli oggetti inutili prima che finiscano sul banco.
        /// </summary>
        public static bool CanBePartOfRecipe(
            IReadOnlyList<RecipeDefinition> recipes, StationType station, IReadOnlyList<ItemDefinition> items)
        {
            if (recipes == null || items == null) return false;
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (!IsUsable(recipe, station)) continue;
                if (items.Count > recipe.Inputs.Length) continue;
                if (IsSubset(items, recipe.Inputs)) return true;
            }
            return false;
        }

        private static bool IsUsable(RecipeDefinition recipe, StationType station)
        {
            return recipe != null && recipe.Station == station && recipe.Inputs != null && recipe.Output != null;
        }

        /// <summary>Vero se "small" è contenuto in "big" contando le ripetizioni (multiset).</summary>
        private static bool IsSubset(IReadOnlyList<ItemDefinition> small, IReadOnlyList<ItemDefinition> big)
        {
            for (int i = 0; i < small.Count; i++)
            {
                ItemDefinition item = small[i];
                if (item == null) return false;
                if (Count(small, item) > Count(big, item)) return false;
            }
            return true;
        }

        private static int Count(IReadOnlyList<ItemDefinition> list, ItemDefinition item)
        {
            int n = 0;
            for (int i = 0; i < list.Count; i++)
                if (ReferenceEquals(list[i], item)) n++;
            return n;
        }
    }
}
