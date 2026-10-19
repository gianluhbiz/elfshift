using UnityEngine;

namespace Elfshift.Core
{
    /// <summary>
    /// Una ricetta: quali oggetti entrano in una postazione e quale oggetto esce.
    /// L'ordine degli ingredienti NON conta (Metallo + Legno = Legno + Metallo).
    /// Per aggiungere un giocattolo basta creare un nuovo asset ricetta, senza toccare il codice.
    /// </summary>
    [CreateAssetMenu(menuName = "Elfshift/Ricetta", fileName = "Recipe_Nuova")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [SerializeField] private StationType station = StationType.Construction;
        [SerializeField] private ItemDefinition[] inputs = new ItemDefinition[0];
        [SerializeField] private ItemDefinition output;
        [Min(0.2f)]
        [Tooltip("Secondi di lavorazione tenendo premuto E (prima dei miglioramenti).")]
        [SerializeField] private float craftSeconds = 2.5f;

        public StationType Station => station;
        public ItemDefinition[] Inputs => inputs;
        public ItemDefinition Output => output;
        public float CraftSeconds => craftSeconds;

#if UNITY_EDITOR
        public void EditorSetup(StationType newStation, ItemDefinition[] newInputs, ItemDefinition newOutput, float seconds)
        {
            station = newStation;
            inputs = newInputs;
            output = newOutput;
            craftSeconds = seconds;
        }
#endif
    }
}
