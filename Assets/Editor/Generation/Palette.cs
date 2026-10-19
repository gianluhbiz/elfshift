using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Descrizione di un materiale della tavolozza del gioco.</summary>
    public struct MaterialSpec
    {
        public Color Color;
        public float Smoothness;
        public float Metallic;
        public float Emission; // 0 = nessuna luce propria; >0 = intensità del colore emesso

        public MaterialSpec(Color color, float smoothness, float metallic, float emission)
        {
            Color = color;
            Smoothness = smoothness;
            Metallic = metallic;
            Emission = emission;
        }
    }

    /// <summary>
    /// La tavolozza di colori del gioco: rossi caldi, verdi profondi, legni dorati, neve azzurrina.
    /// Tutti i modelli usano SOLO questi materiali, così lo stile resta coerente.
    /// Per cambiare il look dell'intero gioco basta modificare i valori qui e rigenerare.
    /// </summary>
    public static class Palette
    {
        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString(hex, out c);
            return c;
        }

        private static MaterialSpec M(string hex, float smooth = 0.28f, float metal = 0f, float emission = 0f)
        {
            return new MaterialSpec(Hex(hex), smooth, metal, emission);
        }

        public static readonly Dictionary<string, MaterialSpec> Specs = new Dictionary<string, MaterialSpec>
        {
            // Rossi e verdi di Natale
            { "RedSanta",   M("#D12F3F") },
            { "RedDeep",    M("#8E1F33") },
            { "RedLight",   M("#E8606A") },
            { "GreenElf",   M("#2E9A62") },
            { "GreenDeep",  M("#1D5F45") },
            { "GreenPine",  M("#2A6B4E") },
            { "GreenLight", M("#6CC17E") },
            // Neutri caldi
            { "Snow",       M("#F7FAFF", 0.45f) },
            { "SnowShade",  M("#CFE1F5", 0.35f) },
            { "Cream",      M("#F8EBD0") },
            { "Plaster",    M("#EBD9B8") },
            { "PlasterDark",M("#D7BE96") },
            // Legni
            { "WoodLight",  M("#DDA25F") },
            { "WoodMid",    M("#B67540") },
            { "WoodDark",   M("#7A4A2B") },
            { "WoodDeep",   M("#573320") },
            { "WoodEnd",    M("#F0CF98") },
            { "Bark",       M("#8A5B35") },
            // Metalli e pietra
            { "Gold",       M("#F3B83C", 0.7f, 0.65f) },
            { "MetalLight", M("#B4C0CC", 0.6f, 0.75f) },
            { "MetalDark",  M("#5E6B7A", 0.5f, 0.75f) },
            { "Iron",       M("#363C46", 0.45f, 0.6f) },
            { "Stone",      M("#9096A3") },
            { "Brick",      M("#B9573C") },
            { "Cork",       M("#C99458") },
            // Stoffe e colori dei giocattoli
            { "ClothBlue",  M("#4F82D8") },
            { "ClothPink",  M("#EB78A6") },
            { "ClothPurple",M("#8D66C9") },
            { "ClothYellow",M("#F6CF4A") },
            { "ClothOrange",M("#F29A3C") },
            { "ClothTeal",  M("#3FB7B0") },
            // Personaggi
            { "Skin",       M("#F6CBA8", 0.3f) },
            { "Cheek",      M("#EE8E8E", 0.2f) },
            { "EyeDark",    M("#2A1D1B", 0.7f) },
            { "Black",      M("#2B2730") },
            { "White",      M("#FFFFFF") },
            { "Carrot",     M("#F28A2B") },
            { "CookieBrown",M("#B8772F") },
            // Luci (emissivi)
            { "WindowNight",M("#1B2C55", 0.4f, 0f, 0.7f) },
            { "BulbWarm",   M("#FFD27A", 0.6f, 0f, 1.6f) },
            { "BulbRed",    M("#FF5A5A", 0.6f, 0f, 1.6f) },
            { "BulbGreen",  M("#6DFF9A", 0.6f, 0f, 1.6f) },
            { "BulbBlue",   M("#6FA8FF", 0.6f, 0f, 1.6f) },
            { "StarGlow",   M("#FFD45C", 0.6f, 0.3f, 1.8f) },
            { "LampGlow",   M("#FFE2A6", 0.5f, 0f, 2.0f) },
            // Terreno e decori
            { "Rug",        M("#B8263C", 0.12f) },
            { "RugCream",   M("#F3E3C0", 0.12f) },
            { "SnowGround", M("#EAF2FC", 0.35f) },
            { "Night",      M("#14203F", 0.2f) },
        };
    }
}
