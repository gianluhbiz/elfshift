using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Decorazioni: albero di Natale, finestre, bacheca degli ordini, ghirlande, casse, pupazzo di neve, abeti...
    /// La forma "bella" di ogni oggetto è scolpita in Blender (Tools/Blender/models/props.py, file Assets/Models/Source/Prop_*.bytes);
    /// se un file manca si usa la forma semplice costruita qui (riserva), così il laboratorio si costruisce sempre.
    /// </summary>
    public static class PropModels
    {
        // Livelli di rami dell'albero scolpito in Blender: (raggio, quota della base, altezza).
        // Devono restare uguali a TREE_TIERS in Tools/Blender/models/props.py: servono a far correre le lucine sui rami.
        private static readonly Vector3[] TreeTiers =
        {
            new Vector3(1.30f, 0.50f, 1.15f), new Vector3(1.08f, 1.05f, 1.02f), new Vector3(0.88f, 1.55f, 0.92f),
            new Vector3(0.68f, 2.02f, 0.82f), new Vector3(0.46f, 2.46f, 0.72f),
        };

        /// <summary>Raggio esterno dei rami dell'albero scolpito all'altezza y (0 se non ci sono rami).</summary>
        private static float TreeRadiusAt(float y)
        {
            float best = 0f;
            for (int i = 0; i < TreeTiers.Length; i++)
            {
                float rel = (y - TreeTiers[i].y) / TreeTiers[i].z;
                if (rel < 0f || rel > 1f) continue;
                float s = 1f - Mathf.Pow(rel, 1f / 1.1f);
                best = Mathf.Max(best, TreeTiers[i].x * s);
            }
            return best;
        }
        // ============================================================================ ALBERO DI NATALE
        /// <summary>Albero di Natale alto circa 3,4 m. Restituisce la radice; i gruppi di lucine sono in "Bulbs_*".</summary>
        public static GameObject BuildChristmasTree(Transform parent, out List<Renderer> bulbRenderers, out List<Color> bulbColors)
        {
            ModelKit.Begin("ChristmasTree");
            Transform root = ModelKit.Empty(parent, "ChristmasTree", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            // albero scolpito (vaso, rami a lobi, palline, festoni e regali ai piedi)
            bool sculpted = mb.Import("Prop_ChristmasTree", Vector3.zero);
            if (!sculpted) BuildTreeFallback(mb);
            ModelKit.Part(root, "Tree", mb);

            MeshBuilder star = new MeshBuilder();
            if (!star.Import("Prop_TreeStar", Vector3.zero))
                star.Prism(MeshBuilder.Xf(new Vector3(0f, 3.28f, 0f)), MeshBuilder.StarOutline(5, 0.25f, 0.115f), 0.09f, star.Sub("StarGlow"));
            GameObject starObj = ModelKit.Part(root, "Star", star);
            starObj.AddComponent<Elfshift.Vfx.Bobbing>();

            // luci a spirale: un renderer per colore, così lampeggiano a gruppi
            bulbRenderers = new List<Renderer>();
            bulbColors = new List<Color>();
            string[] bulbMats = { "BulbWarm", "BulbRed", "BulbGreen", "BulbBlue" };
            MeshBuilder wire = new MeshBuilder();
            List<Vector3> spiral = new List<Vector3>();
            int steps = 90;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float a = t * Mathf.PI * 2f * 4.2f;
                float y = Mathf.Lerp(0.62f, 3.0f, t);
                // sull'albero scolpito il filo segue la superficie dei rami
                float r = sculpted ? Mathf.Max(0.1f, TreeRadiusAt(y) + 0.04f) : Mathf.Lerp(1.05f, 0.12f, t);
                spiral.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
            }
            wire.Tube(spiral.ToArray(), 0.014f, 0.014f, 5, "GreenDeep", null, 0, false);
            for (int g = 0; g < bulbMats.Length; g++)
            {
                MeshBuilder bulbs = new MeshBuilder();
                for (int i = g * 3 + 2; i <= steps; i += 12)
                {
                    Vector3 p = spiral[i];
                    Vector3 outward = new Vector3(p.x, 0f, p.z).normalized;
                    if (!AddBulb(bulbs, wire, bulbMats[g], p + outward * 0.015f))
                        bulbs.Ball(bulbMats[g], p + outward * 0.04f, 0.052f, 8);
                }
                GameObject go = ModelKit.Part(root, "Bulbs_" + bulbMats[g], bulbs);
                bulbRenderers.Add(go.GetComponent<Renderer>());
                bulbColors.Add(Palette.Specs[bulbMats[g]].Color);
            }
            ModelKit.Part(root, "Wire", wire);

            // regali ai piedi dell'albero (nel modello scolpito ci sono già)
            if (!sculpted)
            {
                MeshBuilder gifts = new MeshBuilder();
                FurnitureModels.AddGift(gifts, new Vector3(0.75f, 0f, 0.55f), new Vector3(0.42f, 0.34f, 0.38f), "ClothBlue", "Gold", 20f);
                FurnitureModels.AddGift(gifts, new Vector3(-0.8f, 0f, 0.6f), new Vector3(0.38f, 0.3f, 0.4f), "ClothYellow", "RedSanta", -25f);
                FurnitureModels.AddGift(gifts, new Vector3(0.1f, 0f, 1.0f), new Vector3(0.34f, 0.26f, 0.34f), "RedSanta", "Cream", 5f);
                FurnitureModels.AddGift(gifts, new Vector3(-0.35f, 0f, 1.05f), new Vector3(0.24f, 0.22f, 0.24f), "GreenElf", "Gold", 40f);
                ModelKit.Part(root, "Gifts", gifts);
            }

            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 1.5f, 0.2f), new Vector3(2.0f, 3.0f, 2.2f));
            return root.gameObject;
        }

        /// <summary>
        /// Lampadina scolpita appesa al punto "attach": il vetro (che lampeggia) va in "glass", il portalampada verde in "sockets".
        /// Restituisce falso se i modelli di Blender non ci sono.
        /// </summary>
        private static bool AddBulb(MeshBuilder glass, MeshBuilder sockets, string bulbMaterial, Vector3 attach)
        {
            if (!ImportedMesh.Exists("Prop_Bulb_" + bulbMaterial) || !ImportedMesh.Exists("Prop_BulbCap")) return false;
            float yaw = Mathf.Abs(attach.x * 37f + attach.z * 53f) % 360f;
            return glass.Import("Prop_Bulb_" + bulbMaterial, attach, new Vector3(0f, yaw, 0f)) && sockets.Import("Prop_BulbCap", attach, new Vector3(0f, yaw, 0f));
        }

        /// <summary>Forma semplice dell'albero (usata solo se manca il modello di Blender).</summary>
        private static void BuildTreeFallback(MeshBuilder mb)
        {
            // vaso rosso con bordo dorato e sottofondo
            mb.Cyl("RedSanta", Vector3.zero, 0.42f, 0.34f, 0.06f, Vector3.zero, 22);
            mb.Ring("Gold", new Vector3(0f, 0.33f, 0f), 0.4f, 0.025f, Vector3.zero, 22, 6);
            mb.Cyl("WoodDark", new Vector3(0f, 0.3f, 0f), 0.14f, 0.3f, 0.02f, Vector3.zero, 12);
            // quattro livelli di rami
            float[] baseR = { 1.25f, 1.02f, 0.8f, 0.58f };
            float[] tierH = { 1.1f, 0.98f, 0.88f, 0.8f };
            float[] tierY = { 0.45f, 1.1f, 1.7f, 2.25f };
            for (int i = 0; i < 4; i++)
            {
                Profile p = new Profile()
                    .Add(0f, 0f).Add(baseR[i] * 0.88f, 0f).Add(baseR[i], 0.07f)
                    .Add(baseR[i] * 0.62f, tierH[i] * 0.5f).Add(baseR[i] * 0.4f, tierH[i] * 0.62f).Add(0f, tierH[i]);
                mb.Lathe(MeshBuilder.Xf(new Vector3(0f, tierY[i], 0f)), p, 26, mb.Sub(i % 2 == 0 ? "GreenPine" : "GreenDeep"));
                // neve sulle punte dei rami
                Profile snow = new Profile()
                    .Add(0f, 0f).Add(baseR[i] * 0.5f, 0f).Add(baseR[i] * 0.55f, 0.03f).Add(baseR[i] * 0.3f, tierH[i] * 0.34f).Add(0f, tierH[i] * 0.4f);
                mb.Lathe(MeshBuilder.Xf(new Vector3(0f, tierY[i] + tierH[i] * 0.62f, 0f)), snow, 22, mb.Sub("Snow"));
            }
            // palline colorate sui rami
            string[] ballMats = { "RedSanta", "Gold", "ClothBlue", "Snow", "ClothPink" };
            for (int i = 0; i < 4; i++)
            {
                int count = 9 - i;
                for (int k = 0; k < count; k++)
                {
                    float a = (k / (float)count) * Mathf.PI * 2f + i * 0.7f;
                    float r = baseR[i] * 0.72f;
                    Vector3 pos = new Vector3(Mathf.Cos(a) * r, tierY[i] + tierH[i] * 0.2f, Mathf.Sin(a) * r);
                    mb.Ball(ballMats[(k + i) % ballMats.Length], pos, 0.075f, 10);
                }
            }
        }

        // ============================================================================ FINESTRA
        /// <summary>Finestra aperta (senza vetro, così si vede la neve cadere) con telaio, davanzale innevato e tende.</summary>
        public static GameObject BuildWindow(Transform parent, float width, float height)
        {
            ModelKit.Begin("Window");
            Transform root = ModelKit.Empty(parent, "Window", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            // il modello scolpito è fatto per 2,6 x 2,4 m: per altre misure lo si allunga
            if (mb.Import("Prop_Window", MeshBuilder.Xf(Vector3.zero, Vector3.zero, new Vector3(width / 2.6f, height / 2.4f, 1f))))
            {
                ModelKit.Part(root, "Frame", mb);
                return root.gameObject;
            }
            float t = 0.12f;
            float dz = 0.0f;
            // telaio esterno
            mb.Box("WoodLight", new Vector3(0f, height - t / 2f, dz), new Vector3(width + t, t, 0.34f), 0.025f);
            mb.Box("WoodLight", new Vector3(0f, t / 2f, dz), new Vector3(width + t, t, 0.34f), 0.025f);
            mb.Box("WoodLight", new Vector3(-(width / 2f), height / 2f, dz), new Vector3(t, height, 0.34f), 0.025f);
            mb.Box("WoodLight", new Vector3((width / 2f), height / 2f, dz), new Vector3(t, height, 0.34f), 0.025f);
            // montanti centrali a croce
            mb.Box("WoodMid", new Vector3(0f, height / 2f, dz), new Vector3(0.07f, height - t, 0.12f), 0.015f);
            mb.Box("WoodMid", new Vector3(0f, height * 0.55f, dz), new Vector3(width - t, 0.07f, 0.12f), 0.015f);
            // davanzale con neve
            mb.Box("WoodMid", new Vector3(0f, -0.04f, 0.12f), new Vector3(width + 0.5f, 0.1f, 0.5f), 0.03f);
            mb.Ball("Snow", new Vector3(-width * 0.25f, 0.04f, 0.14f), 0.5f, new Vector3(1.2f, 0.17f, 0.4f), Vector3.zero, 14);
            mb.Ball("Snow", new Vector3(width * 0.22f, 0.035f, 0.16f), 0.45f, new Vector3(1.1f, 0.16f, 0.38f), Vector3.zero, 14);
            // tende rosse con fermatende dorati
            for (int s = -1; s <= 1; s += 2)
            {
                float x = s * (width / 2f + 0.02f);
                mb.Taper("RedSanta", new Vector3(x - s * 0.1f, 0.15f, 0.28f), 0.2f, 0.17f, height - 0.1f, 0.06f, Vector3.zero, 14);
                mb.Ring("Gold", new Vector3(x - s * 0.1f, height * 0.42f, 0.28f), 0.18f, 0.03f, Vector3.zero, 14, 6);
            }
            mb.CylC("WoodDeep", new Vector3(0f, height + 0.12f, 0.28f), 0.025f, width + 0.9f, 0.006f, new Vector3(0f, 0f, 90f), 8);
            mb.Ball("Gold", new Vector3(-(width + 0.9f) / 2f, height + 0.12f, 0.28f), 0.05f, 8);
            mb.Ball("Gold", new Vector3((width + 0.9f) / 2f, height + 0.12f, 0.28f), 0.05f, 8);
            // ghirlanda di pino sopra la finestra
            Vector3[] garland = MeshBuilder.Bezier(new Vector3(-width / 2f - 0.1f, height + 0.05f, 0.2f), new Vector3(-width / 4f, height - 0.35f, 0.25f), new Vector3(width / 4f, height - 0.35f, 0.25f), new Vector3(width / 2f + 0.1f, height + 0.05f, 0.2f), 14);
            mb.Tube(garland, 0.07f, 0.07f, 8, "GreenPine");
            for (int i = 1; i < garland.Length - 1; i += 3)
                mb.Ball(i % 2 == 0 ? "RedSanta" : "Gold", garland[i] + new Vector3(0f, -0.04f, 0.06f), 0.045f, 8);
            ModelKit.Part(root, "Frame", mb);
            return root.gameObject;
        }

        // ============================================================================ BACHECA DEGLI ORDINI
        public static GameObject BuildCorkBoard(Transform parent)
        {
            ModelKit.Begin("CorkBoard");
            Transform root = ModelKit.Empty(parent, "CorkBoard", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (mb.Import("Prop_CorkBoard", Vector3.zero))
            {
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            mb.Box("WoodMid", Vector3.zero, new Vector3(2.0f, 1.3f, 0.1f), 0.03f);
            mb.Box("Cork", new Vector3(0f, 0f, 0.03f), new Vector3(1.8f, 1.1f, 0.06f), 0.015f);
            // letterine appuntate
            float[] lx = { -0.65f, -0.2f, 0.25f, 0.68f, -0.4f, 0.45f };
            float[] ly = { 0.2f, 0.25f, 0.18f, 0.22f, -0.25f, -0.2f };
            float[] la = { 6f, -5f, 4f, -8f, -4f, 7f };
            for (int i = 0; i < lx.Length; i++)
            {
                mb.Box("Cream", new Vector3(lx[i], ly[i], 0.075f), new Vector3(0.32f, 0.42f, 0.012f), 0.004f, new Vector3(0f, 0f, la[i]));
                mb.Box(i % 2 == 0 ? "RedSanta" : "GreenElf", new Vector3(lx[i], ly[i] + 0.12f, 0.083f), new Vector3(0.22f, 0.035f, 0.004f), 0.001f, new Vector3(0f, 0f, la[i]));
                mb.Box("WoodDark", new Vector3(lx[i], ly[i] + 0.02f, 0.083f), new Vector3(0.2f, 0.01f, 0.004f), 0.001f, new Vector3(0f, 0f, la[i]));
                mb.Box("WoodDark", new Vector3(lx[i], ly[i] - 0.03f, 0.083f), new Vector3(0.2f, 0.01f, 0.004f), 0.001f, new Vector3(0f, 0f, la[i]));
                mb.Ball(i % 3 == 0 ? "ClothBlue" : "RedSanta", new Vector3(lx[i], ly[i] + 0.18f, 0.09f), 0.022f, 8);
            }
            // stella e fiocco di neve decorativi sul bordo alto
            mb.Prism(MeshBuilder.Xf(new Vector3(0f, 0.78f, 0f)), MeshBuilder.StarOutline(5, 0.14f, 0.065f), 0.05f, mb.Sub("Gold"));
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        // ============================================================================ CORONA
        public static GameObject BuildWreath(Transform parent)
        {
            ModelKit.Begin("Wreath");
            Transform root = ModelKit.Empty(parent, "Wreath", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (mb.Import("Prop_Wreath", Vector3.zero))
            {
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            mb.Ring("GreenPine", Vector3.zero, 0.62f, 0.14f, new Vector3(90f, 0f, 0f), 30, 10);
            mb.Ring("GreenDeep", new Vector3(0f, 0f, 0.03f), 0.58f, 0.09f, new Vector3(90f, 0f, 0f), 30, 8);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f + 0.2f;
                mb.Ball(i % 3 == 0 ? "Gold" : "RedSanta", new Vector3(Mathf.Cos(a) * 0.62f, Mathf.Sin(a) * 0.62f, 0.12f), 0.04f, 8);
            }
            // fiocco rosso
            mb.Ball("RedSanta", new Vector3(-0.15f, -0.62f, 0.14f), 0.11f, new Vector3(1.4f, 0.8f, 0.5f), new Vector3(0f, 0f, 25f), 12);
            mb.Ball("RedSanta", new Vector3(0.15f, -0.62f, 0.14f), 0.11f, new Vector3(1.4f, 0.8f, 0.5f), new Vector3(0f, 0f, -25f), 12);
            mb.Ball("RedDeep", new Vector3(0f, -0.62f, 0.16f), 0.06f, 8);
            mb.Box("RedSanta", new Vector3(-0.08f, -0.8f, 0.13f), new Vector3(0.07f, 0.22f, 0.02f), 0.005f, new Vector3(0f, 0f, 12f));
            mb.Box("RedSanta", new Vector3(0.08f, -0.8f, 0.13f), new Vector3(0.07f, 0.22f, 0.02f), 0.005f, new Vector3(0f, 0f, -12f));
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        // ============================================================================ LAMPADA APPESA
        public static GameObject BuildHangingLamp(Transform parent, float cordLength)
        {
            ModelKit.Begin("HangingLamp");
            Transform root = ModelKit.Empty(parent, "HangingLamp", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (ImportedMesh.Exists("Prop_LampRose") && ImportedMesh.Exists("Prop_LampShade"))
            {
                mb.Import("Prop_LampRose", Vector3.zero);
                mb.CylC("Iron", new Vector3(0f, -cordLength / 2f, 0f), 0.01f, cordLength, 0.003f, Vector3.zero, 8);
                mb.Import("Prop_LampShade", new Vector3(0f, -cordLength, 0f));
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            mb.Cyl("WoodDark", new Vector3(0f, -0.03f, 0f), 0.1f, 0.06f, 0.015f, Vector3.zero, 12); // rosone al soffitto
            mb.CylC("Iron", new Vector3(0f, -cordLength / 2f, 0f), 0.012f, cordLength, 0.003f, Vector3.zero, 6);
            Profile shade = new Profile().Add(0.05f, 0f).Add(0.07f, -0.05f).Add(0.34f, -0.3f).Add(0.35f, -0.33f).Add(0.31f, -0.33f).Add(0.04f, -0.07f);
            mb.Lathe(MeshBuilder.Xf(new Vector3(0f, -cordLength, 0f)), shade, 22, mb.Sub("RedSanta"));
            mb.Ring("Gold", new Vector3(0f, -cordLength - 0.33f, 0f), 0.33f, 0.014f, Vector3.zero, 22, 6);
            mb.Ball("LampGlow", new Vector3(0f, -cordLength - 0.28f, 0f), 0.11f, 12);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        // ============================================================================ PICCOLI OGGETTI
        public static GameObject BuildCrate(Transform parent, Vector3 size, bool withSnow)
        {
            ModelKit.Begin("Crate");
            Transform root = ModelKit.Empty(parent, "Crate", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            // la cassa scolpita misura 0,9 x 0,7 x 0,9 m: la si adatta alla misura chiesta
            bool sculpted = mb.Import("Prop_Crate", MeshBuilder.Xf(Vector3.zero, Vector3.zero, new Vector3(size.x / 0.9f, size.y / 0.7f, size.z / 0.9f)));
            if (!sculpted) mb.Box("WoodLight", new Vector3(0f, size.y / 2f, 0f), size, 0.02f);
            if (!sculpted)
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box("WoodDark", new Vector3(sx * (size.x / 2f - 0.04f), size.y / 2f, sz * (size.z / 2f - 0.04f)), new Vector3(0.09f, size.y + 0.02f, 0.09f), 0.015f);
            if (!sculpted)
            {
                mb.Box("WoodMid", new Vector3(0f, size.y * 0.5f, size.z / 2f + 0.005f), new Vector3(size.x - 0.1f, 0.07f, 0.02f), 0.005f);
                mb.Box("WoodMid", new Vector3(size.x / 2f + 0.005f, size.y * 0.5f, 0f), new Vector3(0.02f, 0.07f, size.z - 0.1f), 0.005f);
            }
            if (withSnow) mb.Ball("Snow", new Vector3(0f, size.y + (sculpted ? 0.04f : 0.01f), 0f), Mathf.Max(size.x, size.z) * 0.5f, new Vector3(0.95f, 0.16f, 0.95f), Vector3.zero, 12);
            ModelKit.Part(root, "Mesh", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, size.y / 2f, 0f), size);
            return root.gameObject;
        }

        public static GameObject BuildBarrel(Transform parent, bool candyCanes)
        {
            ModelKit.Begin("Barrel");
            Transform root = ModelKit.Empty(parent, "Barrel", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Prop_Barrel", Vector3.zero))
            {
            Profile p = new Profile().Add(0f, 0f).Add(0.28f, 0f).Add(0.34f, 0.18f).Add(0.37f, 0.4f).Add(0.34f, 0.64f).Add(0.28f, 0.8f).Add(0f, 0.8f);
            mb.Lathe(MeshBuilder.Xf(Vector3.zero), p, 20, mb.Sub("WoodMid"));
            mb.Ring("Iron", new Vector3(0f, 0.14f, 0f), 0.32f, 0.02f, Vector3.zero, 20, 6);
            mb.Ring("Iron", new Vector3(0f, 0.66f, 0f), 0.325f, 0.02f, Vector3.zero, 20, 6);
            mb.Cyl("Cream", new Vector3(0f, 0.79f, 0f), 0.25f, 0.015f, 0.005f, Vector3.zero, 18);
            }
            if (candyCanes)
            {
                for (int i = 0; i < 4; i++)
                    AddCandyCane(mb, new Vector3(-0.12f + i * 0.08f, 0.78f, (i % 2 == 0 ? 0.04f : -0.05f)), 0.7f + (i % 3) * 0.08f, new Vector3((i - 1.5f) * 6f, i * 70f, (i - 1.5f) * 5f));
            }
            ModelKit.Part(root, "Mesh", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.4f, 0f), new Vector3(0.72f, 0.8f, 0.72f));
            return root.gameObject;
        }

        /// <summary>Bastoncino di zucchero a righe rosse e bianche.</summary>
        public static void AddCandyCane(MeshBuilder mb, Vector3 baseCenter, float height, Vector3 euler)
        {
            List<Vector3> path = new List<Vector3>();
            path.Add(new Vector3(0f, 0f, 0f));
            path.Add(new Vector3(0f, height * 0.5f, 0f));
            path.Add(new Vector3(0f, height - 0.1f, 0f));
            float hr = 0.1f;
            for (int i = 1; i <= 8; i++)
            {
                float a = i / 8f * Mathf.PI;
                path.Add(new Vector3(0f, height - 0.1f + Mathf.Sin(a) * hr, hr - Mathf.Cos(a) * hr));
            }
            // bastoncino scolpito (alto 0,7 m, righe a spirale): scalato all'altezza chiesta
            if (mb.Import("Prop_CandyCane", MeshBuilder.Xf(baseCenter, euler, Vector3.one * (height / 0.7f)))) return;
            Matrix4x4 xf = MeshBuilder.Xf(baseCenter, euler);
            Vector3[] world = new Vector3[path.Count];
            for (int i = 0; i < world.Length; i++) world[i] = xf.MultiplyPoint3x4(path[i]);
            mb.Tube(world, 0.028f, 0.028f, 8, "RedSanta", "Snow", 1, true);
        }

        public static GameObject BuildSnowman(Transform parent)
        {
            ModelKit.Begin("Snowman");
            Transform root = ModelKit.Empty(parent, "Snowman", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Prop_Snowman", Vector3.zero))
                BuildSnowmanFallback(mb);
            ModelKit.Part(root, "Mesh", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.9f, 0f), new Vector3(1.0f, 1.8f, 1.0f));
            return root.gameObject;
        }

        private static void BuildSnowmanFallback(MeshBuilder mb)
        {
            mb.Ball("Snow", new Vector3(0f, 0.42f, 0f), 0.5f, new Vector3(1f, 0.85f, 1f), Vector3.zero, 20);
            mb.Ball("Snow", new Vector3(0f, 1.08f, 0f), 0.38f, 18);
            mb.Ball("Snow", new Vector3(0f, 1.58f, 0f), 0.28f, 16);
            // viso
            mb.Ball("Black", new Vector3(0.09f, 1.64f, 0.25f), 0.035f, 8);
            mb.Ball("Black", new Vector3(-0.09f, 1.64f, 0.25f), 0.035f, 8);
            mb.Cone("Carrot", new Vector3(0f, 1.58f, 0.26f), 0.045f, 0.28f, new Vector3(90f, 0f, 0f), 10);
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2) * 0.28f;
                mb.Ball("Black", new Vector3(Mathf.Sin(a) * 0.17f, 1.5f - Mathf.Abs(a) * 0.08f, 0.2f), 0.022f, 6);
            }
            // bottoni, sciarpa, cappello
            for (int i = 0; i < 3; i++) mb.Ball("Black", new Vector3(0f, 1.2f - i * 0.17f, 0.36f - i * 0.02f), 0.035f, 6);
            mb.Ring("RedSanta", new Vector3(0f, 1.36f, 0f), 0.26f, 0.06f, new Vector3(8f, 0f, 0f), 18, 8);
            mb.Box("RedSanta", new Vector3(0.14f, 1.2f, 0.22f), new Vector3(0.1f, 0.3f, 0.05f), 0.02f, new Vector3(0f, 0f, -8f));
            mb.Cyl("Black", new Vector3(0f, 1.8f, 0f), 0.3f, 0.025f, 0.006f, Vector3.zero, 18);
            mb.Cyl("Black", new Vector3(0f, 1.8f, 0f), 0.18f, 0.28f, 0.02f, Vector3.zero, 18);
            mb.Ring("RedSanta", new Vector3(0f, 1.85f, 0f), 0.18f, 0.025f, Vector3.zero, 18, 6);
            // braccia di ramo
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3[] arm = { new Vector3(s * 0.34f, 1.12f, 0f), new Vector3(s * 0.62f, 1.28f, 0.02f), new Vector3(s * 0.85f, 1.5f, 0.05f) };
                mb.Tube(arm, 0.025f, 0.015f, 6, "Bark");
            }
        }

        /// <summary>Abete innevato di sfondo (senza collisioni). scale = altezza approssimativa in metri.</summary>
        public static GameObject BuildPine(Transform parent, float height)
        {
            ModelKit.Begin("Pine");
            Transform root = ModelKit.Empty(parent, "Pine", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            float s = height / 5f;
            if (mb.Import("Prop_Pine", MeshBuilder.Xf(Vector3.zero, Vector3.zero, Vector3.one * s)))
            {
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            mb.Cyl("Bark", Vector3.zero, 0.2f * s, 1.0f * s, 0.03f, Vector3.zero, 8);
            float y = 0.7f * s;
            float r = 1.25f * s;
            for (int i = 0; i < 5; i++)
            {
                float h = 1.5f * s;
                Profile cone = new Profile().Add(0f, 0f).Add(r, 0f, true).Add(r * 0.55f, h * 0.55f).Add(r * 0.4f, h * 0.6f).Add(0f, h);
                mb.Lathe(MeshBuilder.Xf(new Vector3(0f, y, 0f)), cone, 12, mb.Sub("GreenPine"));
                Profile snow = new Profile().Add(0f, 0f).Add(r * 0.62f, 0f, true).Add(r * 0.3f, h * 0.42f).Add(0f, h * 0.5f);
                mb.Lathe(MeshBuilder.Xf(new Vector3(0f, y + h * 0.5f, 0f)), snow, 12, mb.Sub("Snow"));
                y += h * 0.62f;
                r *= 0.8f;
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        public static GameObject BuildLanternPost(Transform parent)
        {
            ModelKit.Begin("LanternPost");
            Transform root = ModelKit.Empty(parent, "LanternPost", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Prop_LanternPost", Vector3.zero))
            {
            mb.Cyl("Iron", Vector3.zero, 0.09f, 0.12f, 0.03f, Vector3.zero, 10);
            mb.Cyl("Iron", Vector3.zero, 0.045f, 1.7f, 0.01f, Vector3.zero, 8);
            mb.Box("Iron", new Vector3(0f, 1.85f, 0f), new Vector3(0.3f, 0.06f, 0.3f), 0.015f);
            mb.Ball("LampGlow", new Vector3(0f, 2.02f, 0f), 0.12f, new Vector3(1f, 1.3f, 1f), Vector3.zero, 12);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box("Iron", new Vector3(sx * 0.13f, 2.02f, sz * 0.13f), new Vector3(0.025f, 0.34f, 0.025f), 0.005f);
            mb.Cone("Iron", new Vector3(0f, 2.19f, 0f), 0.24f, 0.18f, Vector3.zero, 4);
            mb.Ball("Snow", new Vector3(0f, 2.28f, 0f), 0.15f, new Vector3(1f, 0.5f, 1f), Vector3.zero, 10);
            }
            ModelKit.Part(root, "Mesh", mb);
            ModelKit.Empty(root, "LightPoint", new Vector3(0f, 2.02f, 0f));
            return root.gameObject;
        }

        /// <summary>Tratto di staccionata in legno con cappelli di neve (lungo l'asse X, lunghezza "length").</summary>
        public static GameObject BuildFence(Transform parent, float length)
        {
            ModelKit.Begin("Fence");
            Transform root = ModelKit.Empty(parent, "Fence", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 1.1f) + 1);
            float step = length / (posts - 1);
            if (ImportedMesh.Exists("Prop_FencePost") && ImportedMesh.Exists("Prop_FenceSpan"))
            {
                // pali scolpiti e tratti di traverse (lunghi 1 m) allungati fino al palo successivo
                for (int i = 0; i < posts; i++)
                {
                    float x = -length / 2f + i * step;
                    mb.Import("Prop_FencePost", new Vector3(x, 0f, 0f), new Vector3(0f, (i * 47) % 360, 0f));
                    if (i < posts - 1)
                        mb.Import("Prop_FenceSpan", MeshBuilder.Xf(new Vector3(x + step / 2f, 0f, 0f), Vector3.zero, new Vector3(step, 1f, 1f)));
                }
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            for (int i = 0; i < posts; i++)
            {
                float x = -length / 2f + i * step;
                mb.Box("WoodDark", new Vector3(x, 0.45f, 0f), new Vector3(0.14f, 0.9f, 0.14f), 0.025f);
                mb.Ball("Snow", new Vector3(x, 0.92f, 0f), 0.12f, new Vector3(1f, 0.55f, 1f), Vector3.zero, 8);
            }
            mb.Box("WoodLight", new Vector3(0f, 0.7f, 0f), new Vector3(length, 0.09f, 0.07f), 0.015f);
            mb.Box("WoodLight", new Vector3(0f, 0.38f, 0f), new Vector3(length, 0.09f, 0.07f), 0.015f);
            mb.Ball("Snow", new Vector3(0f, 0.76f, 0f), length * 0.5f, new Vector3(1f, 0.04f, 0.11f), Vector3.zero, 12);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Tappeto rosso con bordo crema.</summary>
        public static GameObject BuildRug(Transform parent, float width, float depth)
        {
            ModelKit.Begin("Rug");
            Transform root = ModelKit.Empty(parent, "Rug", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            // il tappeto scolpito misura 10 x 6,5 m: lo si adatta (lo spessore resta uguale)
            if (mb.Import("Prop_Rug", MeshBuilder.Xf(Vector3.zero, Vector3.zero, new Vector3(width / 10f, 1f, depth / 6.5f))))
            {
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }
            mb.Box("RugCream", new Vector3(0f, 0.012f, 0f), new Vector3(width, 0.024f, depth), 0.01f);
            mb.Box("Rug", new Vector3(0f, 0.016f, 0f), new Vector3(width - 0.4f, 0.028f, depth - 0.4f), 0.01f);
            mb.Box("RugCream", new Vector3(0f, 0.02f, 0f), new Vector3(width - 1.0f, 0.03f, depth - 1.0f), 0.01f);
            mb.Box("Rug", new Vector3(0f, 0.024f, 0f), new Vector3(width - 1.2f, 0.032f, depth - 1.2f), 0.01f);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>
        /// Filo di lucine di Natale appeso tra due punti (curva a catenaria). Le lampadine sono divise in
        /// quattro gruppi di colore, ognuno con il suo renderer, così possono lampeggiare a turno.
        /// </summary>
        public static GameObject BuildLightString(Transform parent, string name, Vector3 from, Vector3 to, float sag, int bulbCount,
            List<Renderer> bulbRenderers, List<Color> bulbColors)
        {
            ModelKit.Begin("Lights_" + name);
            Transform root = ModelKit.Empty(parent, name, Vector3.zero);
            int steps = 24;
            Vector3[] path = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y -= sag * 4f * t * (1f - t);
                path[i] = p;
            }
            MeshBuilder wire = new MeshBuilder();
            wire.Tube(path, 0.012f, 0.012f, 5, "GreenDeep", null, 0, false);

            string[] mats = { "BulbWarm", "BulbRed", "BulbGreen", "BulbBlue" };
            MeshBuilder[] groups = { new MeshBuilder(), new MeshBuilder(), new MeshBuilder(), new MeshBuilder() };
            for (int i = 0; i < bulbCount; i++)
            {
                float t = (i + 0.5f) / bulbCount;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y -= sag * 4f * t * (1f - t);
                if (!AddBulb(groups[i % 4], wire, mats[i % 4], p))
                    groups[i % 4].Ball(mats[i % 4], p + new Vector3(0f, -0.06f, 0f), 0.05f, new Vector3(0.8f, 1.15f, 0.8f), Vector3.zero, 8);
            }
            ModelKit.Part(root, "Wire", wire);
            for (int g = 0; g < 4; g++)
            {
                GameObject go = ModelKit.Part(root, "Bulbs_" + mats[g], groups[g]);
                bulbRenderers.Add(go.GetComponent<Renderer>());
                bulbColors.Add(Palette.Specs[mats[g]].Color);
            }
            return root.gameObject;
        }

        /// <summary>Freccia dorata luminosa che indica il prossimo bersaglio (punta verso il basso).</summary>
        public static GameObject BuildObjectiveArrow(Transform parent)
        {
            ModelKit.Begin("ObjectiveArrow");
            Transform root = ModelKit.Empty(parent, "ObjectiveMarker", Vector3.zero);
            Transform arrow = ModelKit.Empty(root, "Arrow", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Prop_ObjectiveArrow", Vector3.zero))
            {
                mb.Cone("StarGlow", new Vector3(0f, 0.75f, 0f), 0.3f, 0.75f, new Vector3(180f, 0f, 0f), 4);
                mb.Cyl("StarGlow", new Vector3(0f, 0.72f, 0f), 0.09f, 0.45f, 0.02f, Vector3.zero, 8);
                mb.Ring("Gold", new Vector3(0f, 0.2f, 0f), 0.34f, 0.025f, Vector3.zero, 20, 6);
            }
            GameObject part = ModelKit.Part(arrow, "Mesh", mb);
            MeshRenderer r = part.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Elfshift.Vfx.Bobbing bob = arrow.gameObject.AddComponent<Elfshift.Vfx.Bobbing>();
            Wire.Float(bob, "height", 0.18f);
            Wire.Float(bob, "speed", 2.4f);
            Wire.Float(bob, "spinDegreesPerSecond", 70f);
            return root.gameObject;
        }

        /// <summary>Mucchio di neve (ellissoide schiacciato).</summary>
        public static GameObject BuildSnowDrift(Transform parent, Vector3 radii)
        {
            ModelKit.Begin("SnowDrift");
            Transform root = ModelKit.Empty(parent, "SnowDrift", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Prop_SnowDrift", MeshBuilder.Xf(Vector3.zero, Vector3.zero, radii)))
                mb.Ball("Snow", new Vector3(0f, 0f, 0f), 1f, radii, Vector3.zero, 16);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }
    }
}
