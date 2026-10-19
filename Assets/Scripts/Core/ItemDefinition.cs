using UnityEngine;

namespace Elfshift.Core
{
    /// <summary>
    /// Descrive UN tipo di oggetto (es. "Legno", "Trenino").
    /// È un asset (ScriptableObject): i dati stanno qui, la logica di gioco non cambia.
    ///
    /// Per sostituire il modello 3D provvisorio con uno definitivo basta
    /// assegnare un altro prefab al campo "Visual Prefab". Nient'altro da toccare.
    /// </summary>
    [CreateAssetMenu(menuName = "Elfshift/Oggetto", fileName = "Item_Nuovo")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identità")]
        [Tooltip("Identificatore stabile usato nei salvataggi. Se vuoto si usa il nome dell'asset.")]
        [SerializeField] private string id = "";
        [SerializeField] private string displayName = "Oggetto";
        [SerializeField] private ItemState state = ItemState.RawMaterial;

        [Header("Aspetto")]
        [Tooltip("Prefab 3D mostrato nel mondo, in mano, nelle icone. Origine = base dell'oggetto.")]
        [SerializeField] private GameObject visualPrefab;
        [Tooltip("Colore usato per i segnaposto e per le piccole decorazioni dell'interfaccia.")]
        [SerializeField] private Color accentColor = Color.white;

        /// <summary>Identificatore univoco (per i salvataggi).</summary>
        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => displayName;
        public ItemState State => state;
        public GameObject VisualPrefab => visualPrefab;
        public Color AccentColor => accentColor;

#if UNITY_EDITOR
        /// <summary>Usato dagli strumenti editor per creare/aggiornare l'asset.</summary>
        public void EditorSetup(string newId, string newName, ItemState newState, GameObject prefab, Color accent)
        {
            id = newId;
            displayName = newName;
            state = newState;
            visualPrefab = prefab;
            accentColor = accent;
        }
#endif
    }
}
