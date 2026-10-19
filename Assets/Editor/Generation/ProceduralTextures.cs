using System.IO;
using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Piccole texture disegnate da codice: assi di legno per il pavimento, pallino morbido
    /// per le particelle, rettangolo arrotondato per l'interfaccia, icone di biscotto e stella.
    /// </summary>
    public static class ProceduralTextures
    {
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Pavimento ad assi di legno orizzontali, ripetibile (tile).</summary>
        public static Texture2D WoodPlanks(int size = 512)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] px = new Color[size * size];
            int planks = 8;
            int plankH = size / planks;
            Color[] tones =
            {
                Palette.Hex("#C98B52"), Palette.Hex("#BC7C45"), Palette.Hex("#D39A5F"), Palette.Hex("#B57340"), Palette.Hex("#C58449")
            };
            for (int p = 0; p < planks; p++)
            {
                Color tone = tones[(int)(Hash(p, 3, 11) * tones.Length) % tones.Length];
                int jointU = (int)(Hash(p, 7, 5) * size);
                float grainSeed = Hash(p, 1, 9) * 50f;
                for (int y = 0; y < plankH; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        int py = p * plankH + y;
                        float u = x / (float)size;
                        float v = y / (float)plankH;
                        // venatura: onde lente con un po' di rumore
                        float grain = Mathf.Sin((v * 9f + Mathf.PerlinNoise(u * 3f + grainSeed, v * 2f) * 4f) * 3.1f) * 0.5f + 0.5f;
                        float fine = Mathf.PerlinNoise(u * 40f + grainSeed, v * 6f);
                        float shade = 0.9f + grain * 0.07f + (fine - 0.5f) * 0.06f;
                        Color c = tone * shade; c.a = 1f;
                        // bordo scuro tra le assi + bordo chiaro (smusso)
                        if (y < 3) c = Color.Lerp(c, Palette.Hex("#5B3920"), 0.75f);
                        else if (y < 6) c = Color.Lerp(c, Color.white, 0.07f);
                        // giunzione corta tra due assi della stessa fila
                        int dx = Mathf.Abs(x - jointU);
                        dx = Mathf.Min(dx, size - dx);
                        if (dx < 2) c = Color.Lerp(c, Palette.Hex("#5B3920"), 0.7f);
                        px[py * size + x] = c;
                    }
                }
            }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        /// <summary>Carta da parati a righe verticali color crema con sottile filo dorato e piccoli rombi.</summary>
        public static Texture2D WallStripes(int size = 512)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            Color[] px = new Color[size * size];
            Color a = Palette.Hex("#F3E6C9");
            Color b = Palette.Hex("#EBD7B0");
            Color gold = Palette.Hex("#D9A441");
            Color red = Palette.Hex("#C8505A");
            int stripes = 8;
            int w = size / stripes;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int sx = x % w;
                    Color c = ((x / w) % 2 == 0) ? a : b;
                    // leggera texture della carta
                    float n = (Mathf.PerlinNoise(x * 0.08f, y * 0.08f) - 0.5f) * 0.05f;
                    c = new Color(c.r + n, c.g + n, c.b + n, 1f);
                    // filo dorato ai bordi delle righe
                    if (sx < 3) c = Color.Lerp(c, gold, 0.7f);
                    // rombo rosso al centro della riga chiara, ripetuto ogni 64 pixel
                    if ((x / w) % 2 == 0)
                    {
                        float dx = Mathf.Abs(sx - w * 0.5f);
                        float dy = Mathf.Abs((y % 64) - 32f);
                        if (dx + dy < 7f) c = Color.Lerp(c, red, 0.85f);
                    }
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        /// <summary>Pallino bianco con bordo sfumato (per neve e scintille).</summary>
        public static Texture2D SoftCircle(int size = 64)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Rettangolo bianco con angoli arrotondati (usato in 9-slice per pannelli e barre).</summary>
        public static Texture2D RoundedRect(int size = 64, float radius = 20f)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float qy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - (size * 0.5f - radius));
                    float dist = Mathf.Sqrt(qx * qx + qy * qy) - radius;
                    float a = Mathf.Clamp01(0.5f - dist);
                    px[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Biscotto con gocce di cioccolato.</summary>
        public static Texture2D Cookie(int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            float c = size * 0.5f;
            float r = size * 0.44f;
            Color baseColor = Palette.Hex("#D9983F");
            Color edge = Palette.Hex("#B8742A");
            Color chip = Palette.Hex("#5A3320");
            Vector2[] chips =
            {
                new Vector2(0.38f, 0.62f), new Vector2(0.62f, 0.66f), new Vector2(0.5f, 0.42f),
                new Vector2(0.3f, 0.38f), new Vector2(0.7f, 0.42f), new Vector2(0.52f, 0.78f), new Vector2(0.46f, 0.24f)
            };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    Color col = Color.Lerp(baseColor, edge, Mathf.Clamp01((d / r - 0.7f) * 3f));
                    for (int i = 0; i < chips.Length; i++)
                    {
                        float cd = Mathf.Sqrt((x / (float)size - chips[i].x) * (x / (float)size - chips[i].x) + (y / (float)size - chips[i].y) * (y / (float)size - chips[i].y));
                        if (cd < 0.055f) col = chip;
                    }
                    col.a = a;
                    px[y * size + x] = col;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Stella dorata a cinque punte.</summary>
        public static Texture2D Star(int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            Vector2[] outline = MeshBuilder.StarOutline(5, size * 0.46f, size * 0.2f);
            Color gold = Palette.Hex("#FFC93C");
            Color light = Palette.Hex("#FFE58A");
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f + size * 0.03f);
                    float inside = InsidePolygon(outline, p) ? 1f : 0f;
                    float dEdge = Mathf.Clamp01(DistanceToPolygon(outline, p) / (size * 0.12f));
                    Color col = Color.Lerp(gold, light, 1f - dEdge);
                    col.a = inside;
                    px[y * size + x] = col;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static bool InsidePolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static float DistanceToPolygon(Vector2[] poly, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % poly.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                Vector2 q = a + ab * t;
                best = Mathf.Min(best, (p - q).magnitude);
            }
            return best;
        }

        // ------------------------------------------------------------------ Salvataggio
        /// <summary>Salva la texture come PNG e la importa (come sprite 9-slice se richiesto).</summary>
        public static Texture2D SavePng(Texture2D texture, string assetPath, bool asSprite, Vector4 border, bool linearColor = false)
        {
            string dir = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            AssetUtil.EnsureFolder(dir);
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = asSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = !asSprite;
                importer.wrapMode = asSprite ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                if (asSprite)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spriteBorder = border;
                    importer.spritePixelsPerUnit = 100f;
                }
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
    }
}
