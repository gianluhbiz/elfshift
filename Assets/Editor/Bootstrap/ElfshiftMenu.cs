using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elfshift.EditorBootstrap
{
    /// <summary>
    /// Voci del menu "Elfshift". Sta in un assembly SEPARATO e senza dipendenze apposta: così il menu compare
    /// sempre, anche quando il resto del progetto ha errori di compilazione, e può spiegare cosa fare.
    /// Gli strumenti veri (assembly "Elfshift.Editor") vengono chiamati tramite reflection.
    /// </summary>
    public static class ElfshiftMenu
    {
        private const string ScenePath = "Assets/Scenes/Workshop.unity";
        private const string BuilderType = "Elfshift.EditorTools.WorkshopBuilder, Elfshift.Editor";
        private const string CheckerType = "Elfshift.EditorTools.ProjectChecker, Elfshift.Editor";

        private const string MissingToolsMessage =
            "Gli strumenti di Elfshift non sono stati compilati: c'è un errore da qualche parte.\n\n" +
            "1) Apri la Console: menu Window > General > Console.\n" +
            "2) Premi il pulsante \"Clear\" e poi aspetta che Unity finisca di compilare (barra in basso a destra).\n" +
            "3) Se restano messaggi ROSSI, copiane il testo (clic sul messaggio, poi Ctrl+C) e mandalo a Claude.\n\n" +
            "Cause più comuni:\n" +
            "• il progetto non è stato creato con il template \"Universal 3D\" (manca URP);\n" +
            "• mancano i pacchetti Input System o Universal RP (Window > Package Manager > Unity Registry);\n" +
            "• la cartella Assets è stata copiata nel posto sbagliato: devono esistere Assets/Scripts e Assets/Editor.";

        private const string StylizedPrefKey = "Elfshift.StylizedShader";
        private const string StylizedMenu = "Elfshift/Grafica/Shader stilizzato (attivo)";

        [MenuItem(StylizedMenu)]
        public static void ToggleStylizedShader()
        {
            bool now = !EditorPrefs.GetBool(StylizedPrefKey, true);
            EditorPrefs.SetBool(StylizedPrefKey, now);
            EditorUtility.DisplayDialog("Elfshift - grafica",
                now ? "Shader stilizzato ATTIVATO.\nRilancia \"Fase 1 > 2 - Costruisci il laboratorio\" per applicarlo."
                    : "Shader stilizzato DISATTIVATO (si usa il shader standard di URP).\nRilancia \"Fase 1 > 2 - Costruisci il laboratorio\" per applicarlo.",
                "OK");
        }

        [MenuItem(StylizedMenu, true)]
        public static bool ToggleStylizedShaderValidate()
        {
            Menu.SetChecked(StylizedMenu, EditorPrefs.GetBool(StylizedPrefKey, true));
            return true;
        }

        [MenuItem("Elfshift/Fase 1/0 - Aiuto: il menu non funziona?")]
        public static void Help()
        {
            bool tools = Type.GetType(BuilderType) != null;
            EditorUtility.DisplayDialog("Elfshift - aiuto",
                tools ? "Gli strumenti sono stati compilati correttamente. Usa le voci 1 e 2 del menu." : MissingToolsMessage, "OK");
        }

        [MenuItem("Elfshift/Fase 1/1 - Verifica il progetto")]
        public static void CheckProject()
        {
            Type checker = Type.GetType(CheckerType);
            if (checker == null)
            {
                EditorUtility.DisplayDialog("Elfshift - verifica del progetto", MissingToolsMessage, "OK");
                return;
            }
            string summary = (string)checker.GetMethod("Summary", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            EditorUtility.DisplayDialog("Elfshift - verifica del progetto", summary, "OK");
        }

        [MenuItem("Elfshift/Fase 1/2 - Costruisci il laboratorio")]
        public static void BuildWorkshop()
        {
            Type checker = Type.GetType(CheckerType);
            Type builder = Type.GetType(BuilderType);
            if (checker == null || builder == null)
            {
                EditorUtility.DisplayDialog("Elfshift", MissingToolsMessage, "OK");
                return;
            }

            bool ok = (bool)checker.GetMethod("IsOk", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            if (!ok)
            {
                string summary = (string)checker.GetMethod("Summary", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                EditorUtility.DisplayDialog("Elfshift - serve una correzione", summary, "OK");
                return;
            }

            string error = (string)builder.GetMethod("BuildAll", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            if (error != null)
            {
                EditorUtility.DisplayDialog("Elfshift", error, "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Elfshift - laboratorio pronto!",
                "La scena \"Workshop\" è stata creata e aperta.\n\nPremi il pulsante Play (▶) in alto al centro per giocare.\n\n" +
                "Controlli: WASD = muoviti, Maiusc = scatta, Spazio = salta, clic sinistro = prendi/posa, E (tenuto premuto) = costruisci.",
                "Ho capito");
        }

        [MenuItem("Elfshift/Fase 1/3 - Apri la scena del laboratorio")]
        public static void OpenWorkshop()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Elfshift", "La scena non esiste ancora: usa prima \"2 - Costruisci il laboratorio\".", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
