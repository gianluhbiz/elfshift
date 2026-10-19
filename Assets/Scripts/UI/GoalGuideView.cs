using System.Collections.Generic;
using Elfshift.Core;
using Elfshift.Interaction;
using Elfshift.Orders;
using Elfshift.Player;
using Elfshift.Production;
using UnityEngine;
using UnityEngine.UI;

namespace Elfshift.UI
{
    /// <summary>
    /// Riga "Obiettivo" in alto e freccia dorata nel mondo: dice SEMPRE il prossimo passo utile.
    /// Guarda la letterina più urgente, risale la catena delle ricette (pacco ← dipinto ← costruito ← materiali)
    /// e controlla cosa hai in mano e cosa c'è sui banchi. Pensa anche al camino quando fa freddo
    /// e alla cassetta quando ci sono letterine da ritirare.
    /// </summary>
    public sealed class GoalGuideView : MonoBehaviour
    {
        [SerializeField] private OrderManager orders;
        [SerializeField] private Carrier carrier;
        [SerializeField] private CraftingStation[] stations = new CraftingStation[0];
        [SerializeField] private ItemShelf[] shelves = new ItemShelf[0];
        [SerializeField] private Sleigh sleigh;
        [SerializeField] private Mailbox mailbox;
        [SerializeField] private Fireplace fireplace;

        [Header("Interfaccia")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text label;
        [SerializeField] private UiPop labelPop;

        [Header("Freccia dorata sopra il prossimo bersaglio")]
        [SerializeField] private Transform marker;
        [SerializeField] private float markerHeight = 2.9f;

        private string shown;
        private Transform currentTarget;
        private float timer;
        private readonly List<ItemDefinition> missing = new List<ItemDefinition>(4);

        private void Update()
        {
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                timer = 0.2f;
                if (orders == null || carrier == null || root == null || label == null) return;
                currentTarget = null;
                Show(ComputeGoal());
            }
            UpdateMarker();
        }

        // ------------------------------------------------------------------ logica
        private string ComputeGoal()
        {
            ItemDefinition held = carrier.HasItem ? carrier.Held.Definition : null;

            // 1) Il camino si sta spegnendo: priorità al fuoco.
            if (fireplace != null && fireplace.IsCold)
            {
                ItemShelf fuelShelf = ShelfFor(FuelItem());
                if (held != null && fireplace.Accepts(held, out _)) return Target(fireplace, UiText.GoalFeedFire);
                if (held == null && fuelShelf != null) return Target(fuelShelf, UiText.GoalFetchFirewood);
            }

            Order order = orders.MostUrgent();
            if (order == null)
            {
                if (mailbox != null && orders.PendingCount > 0) return Target(mailbox, UiText.GoalCollectMail);
                if (held != null) return UiText.GoalWaitWithItem(held.DisplayName);
                return UiText.GoalWaitOrder;
            }

            // Ci sono letterine nella cassetta e posto per aprirle: conviene ritirarle (il tempo non parte finché sono lì).
            ItemDefinition goal = order.Definition.RequestedItem;
            if (held != null && ReferenceEquals(held, goal)) return Target(sleigh, UiText.GoalDeliver(held.DisplayName));
            string step = NextStep(goal, held, 0);
            if (step != null) return step;
            if (mailbox != null && orders.CollectableCount > 0) return Target(mailbox, UiText.GoalCollectMail);
            return UiText.GoalWaitOrder;
        }

        /// <summary>Il prossimo passo per ottenere "item" (ricorsivo lungo la catena delle ricette).</summary>
        private string NextStep(ItemDefinition item, ItemDefinition held, int depth)
        {
            if (item == null || depth > 6) return null;

            CraftingStation station;
            RecipeDefinition recipe = RecipeFor(item, out station);
            if (recipe == null)
            {
                // Materia prima: si prende dallo scaffale.
                ItemShelf shelf = ShelfFor(item);
                if (shelf == null) return null;
                if (held != null) return UiText.GoalHandsFull(held.DisplayName);
                return Target(shelf, UiText.GoalTake(item.DisplayName, shelf.DisplayName));
            }

            if (ReferenceEquals(station.OutputDefinition, item))
            {
                if (held != null) return UiText.GoalHandsFull(held.DisplayName);
                return Target(station, UiText.GoalTakeFrom(item.DisplayName, station.DisplayName));
            }
            if (station.HasOutput)
            {
                // Sul banco c'è un altro prodotto finito: va tolto prima.
                if (held != null) return UiText.GoalHandsFull(held.DisplayName);
                return Target(station, UiText.GoalTakeFrom(station.OutputDefinition.DisplayName, station.DisplayName));
            }
            if (ReferenceEquals(station.ReadyRecipe, recipe)) return Target(station, UiText.GoalWork(station.DisplayName, item.DisplayName));

            // Cosa manca sul banco per questa ricetta? (se c'è qualcosa di estraneo va ripreso)
            missing.Clear();
            missing.AddRange(recipe.Inputs);
            IReadOnlyList<ItemDefinition> onBench = station.Inputs;
            for (int i = 0; i < onBench.Count; i++)
            {
                int index = missing.IndexOf(onBench[i]);
                if (index >= 0) missing.RemoveAt(index);
                else
                {
                    if (held != null) return UiText.GoalHandsFull(held.DisplayName);
                    return Target(station, UiText.GoalClearBench(station.DisplayName));
                }
            }
            if (missing.Count == 0) return Target(station, UiText.GoalWork(station.DisplayName, item.DisplayName));

            ItemDefinition need = missing[0];
            if (held != null && missing.Contains(held)) return Target(station, UiText.GoalBring(held.DisplayName, station.DisplayName));
            return NextStep(need, held, depth + 1);
        }

        private RecipeDefinition RecipeFor(ItemDefinition output, out CraftingStation where)
        {
            where = null;
            for (int s = 0; s < stations.Length; s++)
            {
                CraftingStation st = stations[s];
                if (st == null || st.Recipes == null) continue;
                for (int r = 0; r < st.Recipes.Length; r++)
                {
                    RecipeDefinition recipe = st.Recipes[r];
                    if (recipe != null && ReferenceEquals(recipe.Output, output) && recipe.Station == st.Type)
                    {
                        where = st;
                        return recipe;
                    }
                }
            }
            return null;
        }

        private ItemShelf ShelfFor(ItemDefinition item)
        {
            if (item == null) return null;
            for (int i = 0; i < shelves.Length; i++)
                if (shelves[i] != null && ReferenceEquals(shelves[i].Item, item)) return shelves[i];
            return null;
        }

        private ItemDefinition FuelItem()
        {
            // Il combustibile è ciò che il camino accetta: lo cerco tra gli scaffali.
            for (int i = 0; i < shelves.Length; i++)
            {
                if (shelves[i] == null || shelves[i].Item == null) continue;
                string reason;
                if (fireplace.Accepts(shelves[i].Item, out reason) || reason == UiText.FireFull) return shelves[i].Item;
            }
            return null;
        }

        private string Target(Interactable target, string text)
        {
            currentTarget = target != null ? target.transform : null;
            return text;
        }

        // ------------------------------------------------------------------ resa
        private void UpdateMarker()
        {
            if (marker == null) return;
            bool visible = currentTarget != null;
            if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
            if (visible) marker.position = currentTarget.position + Vector3.up * markerHeight;
        }

        private void Show(string text)
        {
            bool visible = !string.IsNullOrEmpty(text);
            if (root.activeSelf != visible) root.SetActive(visible);
            if (!visible || string.Equals(text, shown)) return;
            shown = text;
            label.text = text;
            if (labelPop != null) labelPop.Play();
        }
    }
}
