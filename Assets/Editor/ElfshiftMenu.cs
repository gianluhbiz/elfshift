using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Voci del menu "Elfshift" in alto nell'Editor di Unity.</summary>
    public static class ElfshiftMenu
    {
        [MenuItem("Elfshift/Fase 1/1 - Verifica il progetto")]
        public static void CheckProject()
        {
            ProjectChecker.Report report = ProjectChecker.Run();
            EditorUtility.DisplayDialog("Elfshift - verifica del progetto", ProjectChecker.Format(report), "OK");
        }

        [MenuItem("Elfshift/Fase 1/2 - Costruisci il laboratorio")]
        public static void BuildWorkshop()
        {
            ProjectChecker.Report report = ProjectChecker.Run();
            if (!report.Ok)
            {
                EditorUtility.DisplayDialog("Elfshift - serve una correzione", ProjectChecker.Format(report), "OK");
                return;
            }

            string error = WorkshopBuilder.BuildAll();
            if (error != null)
            {
                EditorUtility.DisplayDialog("Elfshift", error, "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Elfshift - laboratorio pronto!",
                "La scena \"Workshop\" è stata creata e aperta.\n\nPremi il pulsante Play (▶) in alto al centro per giocare.\n\n" +
                "Controlli: WASD/frecce = muoviti, Spazio = prendi/posa, E (tenuto premuto) = costruisci.",
                "Ho capito");
        }

        [MenuItem("Elfshift/Fase 1/3 - Apri la scena del laboratorio")]
        public static void OpenWorkshop()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WorkshopBuilder.ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Elfshift", "La scena non esiste ancora: usa prima \"2 - Costruisci il laboratorio\".", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(WorkshopBuilder.ScenePath);
        }
    }
}
