using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Elfshift.EditorTools
{
    /// <summary>Controlla che il progetto Unity sia configurato come serve al gioco (URP e nuovo Input System).</summary>
    public static class ProjectChecker
    {
        public sealed class Report
        {
            public readonly List<string> Problems = new List<string>();
            public readonly List<string> Notes = new List<string>();
            public bool Ok => Problems.Count == 0;
        }

        public static Report Run()
        {
            Report report = new Report();

            // 1) Pipeline grafica: serve URP (materiali stilizzati, ombre morbide, post-elaborazione)
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            {
                report.Problems.Add(
                    "Il progetto non usa URP (Universal Render Pipeline): i materiali risulterebbero rosa.\n" +
                    "Soluzione: crea un nuovo progetto dal template \"Universal 3D\" in Unity Hub e copia lì la cartella Assets.");
            }

            // 2) Input: serve il pacchetto "Input System" attivo
            int handler = ReadActiveInputHandler();
            if (handler == 0)
            {
                report.Problems.Add(
                    "Il nuovo Input System non è attivo.\n" +
                    "Soluzione: Edit > Project Settings > Player > Other Settings > \"Active Input Handling\" = " +
                    "\"Input System Package (New)\" (oppure \"Both\"), poi riavvia Unity quando richiesto.");
            }

            // 3) Versione
            string version = Application.unityVersion;
            if (!version.StartsWith("6000."))
                report.Notes.Add("Versione di Unity: " + version + ". Il progetto è pensato per Unity 6 (6000.x LTS).");
            else
                report.Notes.Add("Versione di Unity: " + version + " (ok).");

            return report;
        }

        /// <summary>Legge "Active Input Handling" dalle impostazioni del progetto: 0 = vecchio, 1 = nuovo, 2 = entrambi. -1 = sconosciuto.</summary>
        private static int ReadActiveInputHandler()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return -1;
            SerializedObject so = new SerializedObject(assets[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            return p != null ? p.intValue : -1;
        }

        public static string Format(Report report)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (report.Ok) sb.AppendLine("Tutto a posto: il progetto è configurato correttamente.");
            for (int i = 0; i < report.Problems.Count; i++) sb.AppendLine("• " + report.Problems[i]).AppendLine();
            for (int i = 0; i < report.Notes.Count; i++) sb.AppendLine("- " + report.Notes[i]);
            return sb.ToString();
        }
    }
}
