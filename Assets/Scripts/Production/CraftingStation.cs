using System.Collections.Generic;
using Elfshift.Core;
using Elfshift.Interaction;
using UnityEngine;

namespace Elfshift.Production
{
    /// <summary>
    /// Banco di lavoro generico: accetta ingredienti, riconosce la ricetta e produce un oggetto
    /// dopo che il giocatore ha tenuto premuto E per il tempo richiesto.
    /// Lo stesso componente servirà per il banco di costruzione, di verniciatura e dei pacchi
    /// (cambia solo il tipo di postazione e l'elenco di ricette).
    /// </summary>
    public sealed class CraftingStation : Interactable, IItemReceiver, IItemSource, IHoldInteractable
    {
        [Header("Configurazione")]
        [SerializeField] private StationType stationType = StationType.Construction;
        [SerializeField] private RecipeDefinition[] recipes = new RecipeDefinition[0];

        [Header("Punti d'appoggio")]
        [Tooltip("Dove vengono posati gli ingredienti, nell'ordine.")]
        [SerializeField] private Transform[] inputSlots = new Transform[0];
        [Tooltip("Dove compare il prodotto finito.")]
        [SerializeField] private Transform outputSlot;

        [Header("Effetti (facoltativi)")]
        [SerializeField] private ParticleSystem workEffect;
        [SerializeField] private ParticleSystem completeEffect;

        private readonly List<WorldItem> inputItems = new List<WorldItem>(4);
        private readonly List<ItemDefinition> inputDefinitions = new List<ItemDefinition>(4);
        private WorldItem outputItem;
        private RecipeDefinition readyRecipe;
        private string holdPrompt = string.Empty;
        private string pickupPrompt = string.Empty;
        private bool working;

        /// <summary>Quanti ingredienti ci sono sul banco.</summary>
        public int InputCount => inputItems.Count;

        /// <summary>Vero se sul banco c'è un prodotto finito da prendere.</summary>
        public bool HasOutput => outputItem != null;

        /// <summary>Moltiplicatore di velocità (1 = normale). I miglioramenti del negozio lo modificheranno.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        // ---------------------------------------------------------------- IItemReceiver
        public string PlacePrompt => UiText.PlaceOnBench;

        public bool Accepts(ItemDefinition candidate, out string reason)
        {
            reason = null;
            if (candidate == null) { reason = UiText.NotNeededHere; return false; }
            if (outputItem != null) { reason = UiText.TakeFinishedFirst; return false; }
            if (inputItems.Count >= inputSlots.Length) { reason = UiText.BenchFull; return false; }

            // Prova ad aggiungere il candidato e controlla che esista ancora una ricetta possibile.
            inputDefinitions.Add(candidate);
            bool useful = RecipeMatcher.CanBePartOfRecipe(recipes, stationType, inputDefinitions);
            inputDefinitions.RemoveAt(inputDefinitions.Count - 1);
            if (!useful) { reason = UiText.NotNeededHere; return false; }
            return true;
        }

        public void Receive(WorldItem item)
        {
            if (item == null) return;
            Transform slot = inputSlots[inputItems.Count];
            item.AttachTo(slot, Vector3.zero, Vector3.zero, ItemPlacement.Slot, 0.2f);
            inputItems.Add(item);
            inputDefinitions.Add(item.Definition);
            RefreshRecipe();
        }

        // ---------------------------------------------------------------- IItemSource
        public bool CanGive => outputItem != null || inputItems.Count > 0;
        public string PickupPrompt => pickupPrompt;

        public WorldItem Take()
        {
            WorldItem taken;
            if (outputItem != null)
            {
                taken = outputItem;
                outputItem = null;
            }
            else if (inputItems.Count > 0)
            {
                // Si può riprendere l'ultimo ingrediente posato (utile se ci si sbaglia).
                int last = inputItems.Count - 1;
                taken = inputItems[last];
                inputItems.RemoveAt(last);
                inputDefinitions.RemoveAt(last);
            }
            else
            {
                return null;
            }
            RefreshRecipe();
            return taken;
        }

        // ---------------------------------------------------------------- IHoldInteractable
        public bool CanHold => outputItem == null && readyRecipe != null;
        public float HoldDuration => readyRecipe != null ? readyRecipe.CraftSeconds / Mathf.Max(0.1f, SpeedMultiplier) : 1f;
        public string HoldPrompt => holdPrompt;

        public void HoldProgress(float normalized)
        {
            if (!working)
            {
                working = true;
                if (workEffect != null) workEffect.Play();
            }
            // Gli ingredienti saltellano mentre si lavora, con ritmo e ampiezza crescenti.
            float bounce = Mathf.Abs(Mathf.Sin(Time.time * 14f)) * 0.05f * Mathf.Lerp(0.4f, 1f, normalized);
            for (int i = 0; i < inputItems.Count; i++)
                if (inputItems[i] != null) inputItems[i].transform.localPosition = Vector3.up * bounce;
        }

        public void HoldCancelled()
        {
            StopWorking();
        }

        public void HoldCompleted()
        {
            if (readyRecipe == null || outputSlot == null) { StopWorking(); return; }

            ItemDefinition result = readyRecipe.Output;
            for (int i = 0; i < inputItems.Count; i++)
                if (inputItems[i] != null) Destroy(inputItems[i].gameObject);
            inputItems.Clear();
            inputDefinitions.Clear();

            outputItem = WorldItem.Create(result, outputSlot.position, outputSlot.rotation);
            if (outputItem != null)
            {
                outputItem.AttachTo(outputSlot, Vector3.zero, Vector3.zero, ItemPlacement.Slot, 0f);
                outputItem.Pop();
            }

            StopWorking();
            if (completeEffect != null) completeEffect.Play();
            RefreshRecipe();
        }

        // ---------------------------------------------------------------- Interni
        private void StopWorking()
        {
            if (working && workEffect != null) workEffect.Stop();
            working = false;
            for (int i = 0; i < inputItems.Count; i++)
                if (inputItems[i] != null) inputItems[i].transform.localPosition = Vector3.zero;
        }

        /// <summary>Ricalcola ricetta pronta e testi mostrati. Chiamata a ogni cambiamento del banco.</summary>
        private void RefreshRecipe()
        {
            readyRecipe = outputItem == null ? RecipeMatcher.FindComplete(recipes, stationType, inputDefinitions) : null;
            holdPrompt = readyRecipe != null ? UiText.HoldToBuild(readyRecipe.Output.DisplayName) : string.Empty;

            if (outputItem != null && outputItem.Definition != null)
                pickupPrompt = UiText.Take(outputItem.Definition.DisplayName);
            else if (inputItems.Count > 0 && inputItems[inputItems.Count - 1] != null)
                pickupPrompt = UiText.Take(inputItems[inputItems.Count - 1].Definition.DisplayName);
            else
                pickupPrompt = string.Empty;
        }

        private void Awake()
        {
            if (inputSlots == null || inputSlots.Length == 0)
                Debug.LogError("[Elfshift] Il banco '" + name + "' non ha punti d'appoggio per gli ingredienti.", this);
            if (outputSlot == null)
                Debug.LogError("[Elfshift] Il banco '" + name + "' non ha il punto di uscita.", this);
        }
    }
}
