using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Piccole utilità per creare cartelle e salvare asset generati.</summary>
    public static class AssetUtil
    {
        public const string GeneratedModels = "Assets/Models/Generated";
        public const string GeneratedMaterials = "Assets/Materials/Generated";
        public const string GeneratedTextures = "Assets/Textures/Generated";
        public const string GeneratedPrefabs = "Assets/Prefabs/Generated";
        public const string DataFolder = "Assets/Settings/Data";
        public const string UiPrefabs = "Assets/Prefabs/UI";
        public const string SceneFolder = "Assets/Scenes";

        /// <summary>Crea (se serve) tutte le cartelle di un percorso tipo "Assets/A/B/C".</summary>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && parent != "Assets") EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Svuota una cartella generata (la ricrea vuota), per ripartire puliti.</summary>
        public static void ResetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            EnsureFolder(path);
        }

        /// <summary>Salva una mesh come asset (obbligatorio per usarla nei prefab).</summary>
        public static Mesh SaveMesh(Mesh mesh, string folder, string assetName)
        {
            EnsureFolder(folder);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + assetName + ".asset");
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
