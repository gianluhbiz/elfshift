using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Scaffali, banchi di lavoro e slitta. Tutti i modelli "guardano" verso +Z (il lato da cui si usano).</summary>
    public static class FurnitureModels
    {
        public const float ShelfWidth = 2.4f;
        public const float ShelfDepth = 0.75f;
        public const float ShelfHeight = 2.3f;
        public const float BenchWidth = 2.2f;
        public const float BenchDepth = 1.0f;
        public const float BenchTop = 0.9f;

        private static readonly string[] ToyColors = { "ClothBlue", "ClothPink", "ClothPurple", "ClothYellow", "ClothTeal", "ClothOrange", "RedSanta", "GreenElf" };

        // ============================================================================ SCAFFALE
        /// <summary>Scaffale a 3 ripiani. kind = "Wood" (legna), "Cloth" (stoffe), "Metal" (metalli).</summary>
        public static GameObject BuildShelf(Transform parent, string kind)
        {
            ModelKit.Begin("Shelf_" + kind);
            Transform root = ModelKit.Empty(parent, "Shelf_" + kind, Vector3.zero);
            float w = ShelfWidth, d = ShelfDepth, h = ShelfHeight;

            MeshBuilder frame = new MeshBuilder();
            // montanti laterali e pannello posteriore
            frame.Box("WoodMid", new Vector3(-(w / 2f - 0.05f), h / 2f, 0f), new Vector3(0.1f, h, d), 0.02f);
            frame.Box("WoodMid", new Vector3((w / 2f - 0.05f), h / 2f, 0f), new Vector3(0.1f, h, d), 0.02f);
            frame.Box("WoodDark", new Vector3(0f, h / 2f, -d / 2f + 0.03f), new Vector3(w - 0.1f, h - 0.06f, 0.05f), 0.01f);
            // assi dei ripiani con bordino frontale
            float[] boardY = { 0.2f, 0.85f, 1.5f, 2.26f };
            for (int i = 0; i < boardY.Length; i++)
            {
                frame.Box("WoodLight", new Vector3(0f, boardY[i], 0.005f), new Vector3(w - 0.1f, 0.07f, d - 0.03f), 0.018f);
                if (i < 3) frame.Box("WoodDeep", new Vector3(0f, boardY[i] + 0.075f, d / 2f - 0.025f), new Vector3(w - 0.14f, 0.09f, 0.03f), 0.01f);
            }
            // cappello di neve sul bordo superiore
            frame.Ball("Snow", new Vector3(-0.55f, h + 0.02f, 0.02f), 0.5f, new Vector3(1.1f, 0.2f, 0.7f), Vector3.zero, 14);
            frame.Ball("Snow", new Vector3(0.45f, h + 0.015f, 0.0f), 0.55f, new Vector3(1.0f, 0.17f, 0.68f), Vector3.zero, 14);
            frame.Ball("Snow", new Vector3(0.95f, h + 0.01f, 0.06f), 0.3f, new Vector3(0.9f, 0.2f, 0.9f), Vector3.zero, 12);
            ModelKit.Part(root, "Frame", frame);

            // insegna sopra lo scaffale con un simbolo
            MeshBuilder sign = new MeshBuilder();
            string signMat = kind == "Wood" ? "WoodEnd" : (kind == "Cloth" ? "ClothPink" : (kind == "Toys" ? "ClothYellow" : "MetalLight"));
            sign.Box("WoodDeep", new Vector3(0f, h + 0.3f, d / 2f - 0.1f), new Vector3(1.0f, 0.36f, 0.06f), 0.02f);
            sign.Cyl(signMat, new Vector3(0f, h + 0.3f, d / 2f - 0.07f), 0.12f, 0.03f, 0.008f, new Vector3(90f, 0f, 0f), 18);
            sign.Box("WoodDeep", new Vector3(-0.38f, h + 0.1f, d / 2f - 0.1f), new Vector3(0.05f, 0.22f, 0.05f), 0.01f);
            sign.Box("WoodDeep", new Vector3(0.38f, h + 0.1f, d / 2f - 0.1f), new Vector3(0.05f, 0.22f, 0.05f), 0.01f);
            ModelKit.Part(root, "Sign", sign);

            // merce
            GameObject goods = ModelKit.Part(root, "Goods", BuildShelfGoods(kind));
            goods.transform.localPosition = Vector3.zero;

            // punti di riferimento per il gameplay
            ModelKit.Empty(root, "SpawnOrigin", new Vector3(0f, 0.55f, 0.3f));
            ModelKit.Empty(root, "InteractionPoint", new Vector3(0f, 0f, 1.1f));

            GameObject shelfBody = new GameObject("Collider");
            shelfBody.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(shelfBody, new Vector3(0f, h / 2f, 0f), new Vector3(w, h, d));
            return root.gameObject;
        }

        private static MeshBuilder BuildShelfGoods(string kind)
        {
            MeshBuilder mb = new MeshBuilder();
            const float b0 = 0.235f, b1 = 0.885f, b2 = 1.535f;
            if (kind == "Wood")
            {
                // tronchi visti di testa nel vano basso
                for (int row = 0; row < 2; row++)
                {
                    int count = row == 0 ? 10 : 9;
                    for (int i = 0; i < count; i++)
                    {
                        float x = -0.9f + i * 0.2f + (row == 1 ? 0.1f : 0f);
                        ItemModels.AddLog(mb, new Vector3(x, b0 + 0.1f + row * 0.17f, 0f), 0.098f, 0.62f, 90f);
                    }
                }
                for (int i = 0; i < 3; i++) ItemModels.AddLogBundle(mb, new Vector3(-0.72f + i * 0.72f, b1, 0f), 0.95f);
                for (int i = 0; i < 2; i++) ItemModels.AddLogBundle(mb, new Vector3(-0.45f + i * 0.9f, b2, 0.02f), 0.95f);
            }
            else if (kind == "Cloth")
            {
                // rotoli di stoffa colorata visti di testa
                for (int row = 0; row < 2; row++)
                {
                    int count = row == 0 ? 8 : 7;
                    for (int i = 0; i < count; i++)
                    {
                        float x = -0.95f + i * 0.27f + (row == 1 ? 0.135f : 0f);
                        string c = ToyColors[(i * 3 + row * 2) % ToyColors.Length];
                        float y = b0 + 0.13f + row * 0.235f;
                        mb.CylC(c, new Vector3(x, y, 0f), 0.125f, 0.6f, 0.012f, new Vector3(90f, 0f, 0f), 16);
                        mb.CylC("Cream", new Vector3(x, y, 0.305f), 0.055f, 0.02f, 0.004f, new Vector3(90f, 0f, 0f), 10);
                    }
                }
                // pile di stoffa piegata
                for (int s = 0; s < 4; s++)
                {
                    for (int layer = 0; layer < 5; layer++)
                    {
                        string c = ToyColors[(s + layer) % ToyColors.Length];
                        mb.Box(c, new Vector3(-0.8f + s * 0.52f, b1 + 0.045f + layer * 0.075f, 0.02f), new Vector3(0.46f, 0.07f, 0.4f), 0.022f, new Vector3(0f, (layer % 2 == 0 ? 3f : -4f), 0f));
                    }
                }
                // rocchetti
                for (int i = 0; i < 6; i++)
                {
                    float x = -0.85f + i * 0.34f;
                    mb.Cyl("Cream", new Vector3(x, b2, 0f), 0.1f, 0.025f, 0.008f, Vector3.zero, 14);
                    mb.Cyl(ToyColors[(i + 2) % ToyColors.Length], new Vector3(x, b2 + 0.02f, 0f), 0.075f, 0.2f, 0.012f, Vector3.zero, 14);
                    mb.Cyl("Cream", new Vector3(x, b2 + 0.215f, 0f), 0.1f, 0.025f, 0.008f, Vector3.zero, 14);
                }
            }
            else if (kind == "Toys")
            {
                // vetrina dei giocattoli finiti (scenografia): orsetti, robot, tamburi, trottole, cubi, palloni
                string[] furs = { "CookieBrown", "WoodLight", "ClothPink", "Cream", "ClothPurple" };
                for (int i = 0; i < 5; i++)
                    DecorModels.AddTeddy(mb, new Vector3(-0.95f + i * 0.47f, b0, 0.0f), 1.0f, furs[i], (i - 2) * 9f);
                string[] bodies = { "ClothBlue", "ClothTeal", "ClothOrange", "ClothPurple" };
                for (int i = 0; i < 4; i++)
                    DecorModels.AddRobot(mb, new Vector3(-0.9f + i * 0.55f, b1, 0.02f), 1.0f, bodies[i], (i - 1.5f) * 10f);
                DecorModels.AddDrum(mb, new Vector3(1.0f, b1, 0.1f), 1.0f);
                DecorModels.AddBlocks(mb, new Vector3(-0.85f, b2, 0.05f), 1.3f);
                DecorModels.AddSpinTop(mb, new Vector3(-0.35f, b2, 0.05f), 1.4f, "ClothYellow");
                DecorModels.AddSpinTop(mb, new Vector3(0.0f, b2, 0.05f), 1.4f, "RedSanta");
                DecorModels.AddBeachBall(mb, new Vector3(0.5f, b2, 0.05f), 0.16f);
                DecorModels.AddDrum(mb, new Vector3(0.95f, b2, 0.0f), 0.8f);
            }
            else
            {
                // lingotti di metallo impilati a piramide
                for (int col = 0; col < 2; col++)
                {
                    float cx = -0.62f + col * 1.0f;
                    for (int level = 0; level < 4; level++)
                    {
                        int n = 4 - level;
                        for (int i = 0; i < n; i++)
                        {
                            float x = cx - (n - 1) * 0.1f + i * 0.2f;
                            mb.Box(level % 2 == 0 ? "MetalLight" : "MetalDark", new Vector3(x, b0 + 0.055f + level * 0.105f, 0f), new Vector3(0.19f, 0.1f, 0.4f), 0.015f);
                        }
                    }
                }
                // ingranaggi appoggiati al fondo
                float[] gx = { -0.85f, -0.3f, 0.2f, 0.7f };
                float[] gr = { 0.2f, 0.16f, 0.22f, 0.15f };
                for (int g = 0; g < gx.Length; g++) AddGear(mb, new Vector3(gx[g], b1 + gr[g] + 0.02f, -0.15f), gr[g], g % 2 == 0 ? "MetalDark" : "Gold");
                // tubi
                for (int i = 0; i < 3; i++) mb.CylC("MetalLight", new Vector3(0.1f + i * 0.0f, b2 + 0.05f + i * 0.0f, 0.05f - i * 0.0f), 0.05f, 0.9f, 0.01f, new Vector3(0f, 0f, 90f), 12);
                mb.CylC("MetalDark", new Vector3(-0.8f, b2 + 0.12f, 0f), 0.12f, 0.24f, 0.015f, Vector3.zero, 14);
                mb.CylC("Iron", new Vector3(-0.35f, b2 + 0.1f, 0f), 0.1f, 0.2f, 0.015f, Vector3.zero, 14);
            }
            return mb;
        }

        /// <summary>Ingranaggio verticale (piano XY, rivolto verso +Z) con denti.</summary>
        public static void AddGear(MeshBuilder mb, Vector3 center, float radius, string material)
        {
            mb.CylC(material, center, radius, 0.06f, 0.01f, new Vector3(90f, 0f, 0f), 18);
            int teeth = 10;
            for (int i = 0; i < teeth; i++)
            {
                float a = i * 360f / teeth;
                Vector3 off = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad), 0f) * (radius + radius * 0.07f);
                mb.Box(material, center + off, new Vector3(radius * 0.32f, radius * 0.28f, 0.06f), 0.01f, new Vector3(0f, 0f, a), 2);
            }
            mb.CylC("Iron", center + new Vector3(0f, 0f, 0.025f), radius * 0.32f, 0.03f, 0.006f, new Vector3(90f, 0f, 0f), 10);
        }

        // ============================================================================ BANCO DI LAVORO
        /// <summary>Banco di lavoro. kind = "Construction", "Paint", "Wrap". Con pads=true ha gli appoggi di ingresso/uscita.</summary>
        public static GameObject BuildWorkbench(Transform parent, string kind, bool pads)
        {
            ModelKit.Begin("Bench_" + kind);
            Transform root = ModelKit.Empty(parent, "Bench_" + kind, Vector3.zero);
            float w = BenchWidth, d = BenchDepth, top = BenchTop;

            MeshBuilder mb = new MeshBuilder();
            // piano, bordo, gambe, ripiano basso
            mb.Box("WoodLight", new Vector3(0f, top - 0.05f, 0f), new Vector3(w, 0.1f, d), 0.025f);
            mb.Box("WoodMid", new Vector3(0f, top - 0.17f, d / 2f - 0.07f), new Vector3(w - 0.12f, 0.14f, 0.05f), 0.015f);
            mb.Box("WoodMid", new Vector3(0f, top - 0.17f, -d / 2f + 0.07f), new Vector3(w - 0.12f, 0.14f, 0.05f), 0.015f);
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box("WoodDark", new Vector3(sx * (w / 2f - 0.12f), (top - 0.1f) / 2f, sz * (d / 2f - 0.12f)), new Vector3(0.14f, top - 0.1f, 0.14f), 0.025f);
            mb.Box("WoodMid", new Vector3(0f, 0.22f, 0f), new Vector3(w - 0.3f, 0.06f, d - 0.3f), 0.015f);
            // cassetta di scarti sotto il banco
            mb.Box("WoodDeep", new Vector3(-0.5f, 0.32f, 0.0f), new Vector3(0.5f, 0.2f, 0.4f), 0.02f);

            // pannello forato dietro il banco con attrezzi appesi (sul lato -Z)
            float backZ = -d / 2f + 0.02f;
            mb.Box("WoodDeep", new Vector3(0f, top + 0.62f, backZ), new Vector3(w - 0.2f, 0.95f, 0.05f), 0.015f);
            mb.Box("WoodDark", new Vector3(0f, top + 0.62f, backZ + 0.03f), new Vector3(w - 0.4f, 0.8f, 0.015f), 0.005f);

            if (kind == "Construction")
            {
                // martello appeso, sega, pinza
                mb.CylC("WoodMid", new Vector3(-0.65f, top + 0.62f, backZ + 0.07f), 0.02f, 0.4f, 0.006f, Vector3.zero, 8);
                mb.Box("MetalDark", new Vector3(-0.65f, top + 0.83f, backZ + 0.07f), new Vector3(0.22f, 0.09f, 0.07f), 0.015f);
                mb.Box("MetalLight", new Vector3(-0.1f, top + 0.7f, backZ + 0.07f), new Vector3(0.55f, 0.16f, 0.015f), 0.004f, new Vector3(0f, 0f, 8f));
                mb.Box("WoodMid", new Vector3(-0.42f, top + 0.66f, backZ + 0.07f), new Vector3(0.1f, 0.1f, 0.04f), 0.012f);
                mb.CylC("RedSanta", new Vector3(0.55f, top + 0.66f, backZ + 0.07f), 0.02f, 0.3f, 0.006f, Vector3.zero, 8);
                mb.Box("MetalLight", new Vector3(0.55f, top + 0.84f, backZ + 0.07f), new Vector3(0.1f, 0.12f, 0.03f), 0.01f);
                mb.Box("GreenElf", new Vector3(0.85f, top + 0.66f, backZ + 0.07f), new Vector3(0.26f, 0.26f, 0.03f), 0.015f);
                // morsa sul bancone (a sinistra)
                mb.Box("Iron", new Vector3(-0.9f, top + 0.1f, -0.2f), new Vector3(0.22f, 0.16f, 0.26f), 0.02f);
                mb.Box("MetalDark", new Vector3(-0.9f, top + 0.19f, -0.07f), new Vector3(0.18f, 0.06f, 0.06f), 0.01f);
                mb.CylC("MetalLight", new Vector3(-0.9f, top + 0.12f, 0.12f), 0.015f, 0.3f, 0.004f, new Vector3(90f, 0f, 0f), 8);
                // martello sul banco e trucioli
                mb.CylC("WoodMid", new Vector3(0.85f, top + 0.02f, -0.15f), 0.018f, 0.34f, 0.005f, new Vector3(0f, 78f, 90f), 8);
                mb.Box("MetalDark", new Vector3(0.88f, top + 0.04f, -0.31f), new Vector3(0.16f, 0.07f, 0.06f), 0.012f, new Vector3(0f, 78f, 0f));
                mb.Ball("WoodEnd", new Vector3(-0.3f, top + 0.01f, -0.3f), 0.05f, new Vector3(1.4f, 0.25f, 1f), new Vector3(0f, 20f, 0f), 8);
                mb.Ball("WoodEnd", new Vector3(0.2f, top + 0.01f, -0.38f), 0.04f, new Vector3(1.4f, 0.25f, 1f), new Vector3(0f, -30f, 0f), 8);
            }
            else if (kind == "Paint")
            {
                string[] paints = { "RedSanta", "ClothBlue", "ClothYellow", "GreenElf", "ClothPurple" };
                for (int i = 0; i < paints.Length; i++)
                {
                    float x = -0.9f + i * 0.45f;
                    mb.Cyl("MetalLight", new Vector3(x, top, -0.28f), 0.09f, 0.16f, 0.012f, Vector3.zero, 14);
                    mb.Cyl(paints[i], new Vector3(x, top + 0.155f, -0.28f), 0.082f, 0.02f, 0.006f, Vector3.zero, 14);
                    mb.Ball(paints[i], new Vector3(x + 0.07f, top + 0.12f, -0.2f), 0.025f, new Vector3(0.7f, 1.3f, 0.7f), Vector3.zero, 8);
                    // barattoli appesi al pannello
                    mb.Cyl(paints[i], new Vector3(x, top + 0.7f, backZ + 0.08f), 0.06f, 0.1f, 0.01f, new Vector3(90f, 0f, 0f), 12);
                }
                // pennelli in un barattolo
                mb.Cyl("MetalDark", new Vector3(0.75f, top, 0.25f), 0.07f, 0.14f, 0.01f, Vector3.zero, 12);
                for (int i = 0; i < 3; i++)
                {
                    mb.CylC("WoodMid", new Vector3(0.75f + (i - 1) * 0.025f, top + 0.2f, 0.25f), 0.012f, 0.26f, 0.004f, new Vector3(0f, 0f, (i - 1) * 10f), 6);
                    mb.Ball(paints[i], new Vector3(0.75f + (i - 1) * 0.045f, top + 0.33f, 0.25f), 0.022f, new Vector3(1f, 1.4f, 1f), Vector3.zero, 6);
                }
                // tavolozza
                mb.Cyl("WoodEnd", new Vector3(-0.2f, top, 0.25f), 0.2f, 0.02f, 0.008f, new Vector3(0f, 0f, 0f), 20);
                for (int i = 0; i < 4; i++)
                    mb.Ball(paints[i], new Vector3(-0.2f + Mathf.Cos(i * 1.4f) * 0.11f, top + 0.025f, 0.25f + Mathf.Sin(i * 1.4f) * 0.11f), 0.035f, new Vector3(1f, 0.4f, 1f), Vector3.zero, 8);
            }
            else // Wrap
            {
                // rotolo di carta regalo sul supporto
                mb.Box("WoodDark", new Vector3(-0.5f, top + 0.18f, -0.3f), new Vector3(0.06f, 0.36f, 0.06f), 0.01f);
                mb.Box("WoodDark", new Vector3(0.5f, top + 0.18f, -0.3f), new Vector3(0.06f, 0.36f, 0.06f), 0.01f);
                mb.CylC("RedSanta", new Vector3(0f, top + 0.3f, -0.3f), 0.14f, 0.9f, 0.015f, new Vector3(0f, 0f, 90f), 18);
                for (int i = 0; i < 4; i++) mb.CylC("Cream", new Vector3(-0.35f + i * 0.23f, top + 0.3f, -0.3f), 0.145f, 0.07f, 0.01f, new Vector3(0f, 0f, 90f), 18);
                // bobine di nastro
                string[] ribbons = { "GreenElf", "Gold", "ClothBlue" };
                for (int i = 0; i < ribbons.Length; i++)
                {
                    float x = -0.8f + i * 0.28f;
                    mb.Cyl(ribbons[i], new Vector3(x, top, 0.25f), 0.1f, 0.07f, 0.01f, Vector3.zero, 16);
                    mb.Cyl("WoodEnd", new Vector3(x, top + 0.02f, 0.25f), 0.04f, 0.06f, 0.005f, Vector3.zero, 10);
                }
                // pacco campione con fiocco
                AddGift(mb, new Vector3(0.7f, top, 0.2f), new Vector3(0.32f, 0.26f, 0.32f), "GreenElf", "Gold", 15f);
                // forbici
                mb.Box("RedSanta", new Vector3(0.05f, top + 0.015f, 0.3f), new Vector3(0.1f, 0.03f, 0.04f), 0.01f, new Vector3(0f, 25f, 0f));
                mb.Box("MetalLight", new Vector3(0.18f, top + 0.015f, 0.34f), new Vector3(0.16f, 0.015f, 0.04f), 0.004f, new Vector3(0f, 25f, 0f));
            }
            ModelKit.Part(root, "Body", mb);

            // appoggi (ingresso x2 + uscita) con anelli decorativi
            if (pads)
            {
                MeshBuilder padMesh = new MeshBuilder();
                float[] padX = { -0.65f, -0.05f, 0.72f };
                for (int i = 0; i < padX.Length; i++)
                {
                    bool output = i == 2;
                    padMesh.Cyl(output ? "Cream" : "WoodDark", new Vector3(padX[i], top, 0.08f), output ? 0.27f : 0.24f, 0.014f, 0.005f, Vector3.zero, 24);
                    padMesh.Ring(output ? "Gold" : "WoodDeep", new Vector3(padX[i], top + 0.012f, 0.08f), output ? 0.265f : 0.235f, 0.012f, Vector3.zero, 28, 6);
                }
                // freccia dipinta tra ingressi e uscita
                padMesh.Box("WoodDeep", new Vector3(0.32f, top + 0.008f, 0.08f), new Vector3(0.22f, 0.006f, 0.05f), 0.002f);
                padMesh.Cone("WoodDeep", new Vector3(0.43f, top + 0.004f, 0.08f), 0.07f, 0.1f, new Vector3(0f, 0f, 0f), 3);
                ModelKit.Part(root, "Pads", padMesh);
                Transform in1 = ModelKit.Empty(root, "Input1", new Vector3(-0.65f, top + 0.014f, 0.08f), new Vector3(0f, 90f, 0f));
                Transform in2 = ModelKit.Empty(root, "Input2", new Vector3(-0.05f, top + 0.014f, 0.08f), new Vector3(0f, 90f, 0f));
                Transform outp = ModelKit.Empty(root, "Output", new Vector3(0.72f, top + 0.014f, 0.08f), Vector3.zero);
                ModelKit.Empty(in1, "Fx", Vector3.zero);
                ModelKit.Empty(root, "WorkFx", new Vector3(-0.35f, top + 0.15f, 0.08f));
                ModelKit.Empty(root, "CompleteFx", new Vector3(0.72f, top + 0.3f, 0.08f));
                if (in2 == null || outp == null) Debug.LogWarning("Appoggi non creati");
            }
            ModelKit.Empty(root, "InteractionPoint", new Vector3(0f, 0f, 0.95f));

            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, top / 2f, 0f), new Vector3(w, top, d));
            return root.gameObject;
        }

        // ============================================================================ REGALI
        /// <summary>Pacco regalo con nastro a croce e fiocco. "pos" è il centro della base.</summary>
        public static void AddGift(MeshBuilder mb, Vector3 pos, Vector3 size, string boxMat, string ribbonMat, float yaw)
        {
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            Vector3 c = pos + new Vector3(0f, size.y / 2f, 0f);
            Vector3 e = new Vector3(0f, yaw, 0f);
            mb.Box(boxMat, c, size, Mathf.Min(0.025f, size.y * 0.15f), e);
            mb.Box(ribbonMat, c, new Vector3(size.x * 1.02f, size.y * 1.02f, size.z * 0.16f), 0.008f, e);
            mb.Box(ribbonMat, c, new Vector3(size.x * 0.16f, size.y * 1.02f, size.z * 1.02f), 0.008f, e);
            Vector3 top = pos + new Vector3(0f, size.y + 0.012f, 0f);
            float s = Mathf.Min(size.x, size.z);
            mb.Ball(ribbonMat, top + q * new Vector3(s * 0.16f, s * 0.05f, 0f), s * 0.13f, new Vector3(1.3f, 0.75f, 0.8f), new Vector3(0f, yaw, 25f), 10);
            mb.Ball(ribbonMat, top + q * new Vector3(-s * 0.16f, s * 0.05f, 0f), s * 0.13f, new Vector3(1.3f, 0.75f, 0.8f), new Vector3(0f, yaw, -25f), 10);
            mb.Ball(ribbonMat, top + new Vector3(0f, s * 0.04f, 0f), s * 0.07f, 8);
        }

        // ============================================================================ SLITTA
        public static GameObject BuildSleigh(Transform parent)
        {
            ModelKit.Begin("Sleigh");
            Transform root = ModelKit.Empty(parent, "Sleigh", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();

            // pattini dorati con riccio anteriore, uniti da traversine
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 0.62f;
                List<Vector3> path = new List<Vector3>();
                path.Add(new Vector3(x, 0.26f, -1.62f));
                path.Add(new Vector3(x, 0.14f, -1.5f));
                path.Add(new Vector3(x, 0.1f, -1.2f));
                path.Add(new Vector3(x, 0.1f, 0.2f));
                path.Add(new Vector3(x, 0.1f, 1.2f));
                Vector3[] curl = MeshBuilder.Bezier(new Vector3(x, 0.1f, 1.2f), new Vector3(x, 0.1f, 1.85f), new Vector3(x, 0.62f, 2.0f), new Vector3(x, 0.78f, 1.6f), 10);
                for (int i = 1; i < curl.Length; i++) path.Add(curl[i]);
                mb.Tube(path.ToArray(), 0.04f, 0.04f, 10, "Gold");
                // sostegni in legno verso lo scafo
                float[] pz = { -1.0f, 0.1f, 1.0f };
                for (int i = 0; i < pz.Length; i++)
                    mb.Box("WoodDark", new Vector3(x, 0.3f, pz[i]), new Vector3(0.07f, 0.34f, 0.1f), 0.02f);
            }
            for (int i = 0; i < 2; i++)
                mb.Box("WoodDark", new Vector3(0f, 0.16f, i == 0 ? -1.0f : 1.0f), new Vector3(1.24f, 0.05f, 0.09f), 0.015f);

            // scafo: fondo e fianchi
            mb.Box("WoodDeep", new Vector3(0f, 0.5f, 0.0f), new Vector3(1.2f, 0.08f, 2.7f), 0.02f);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2[] sideProfile =
                {
                    new Vector2(-1.3f, 0.46f), new Vector2(1.15f, 0.46f), new Vector2(1.5f, 0.66f), new Vector2(1.56f, 0.98f), new Vector2(-1.25f, 0.98f)
                };
                mb.Prism(MeshBuilder.Xf(new Vector3(side * 0.62f, 0f, 0f), new Vector3(0f, -90f, 0f)), sideProfile, 0.07f, mb.Sub("RedSanta"));
                // bordo dorato lungo il fianco
                mb.Box("Gold", new Vector3(side * 0.62f, 0.99f, 0.15f), new Vector3(0.1f, 0.05f, 2.9f), 0.015f);
                // riccio anteriore del fianco
                Vector3[] scroll = MeshBuilder.Bezier(new Vector3(side * 0.62f, 0.95f, 1.52f), new Vector3(side * 0.62f, 1.25f, 1.75f), new Vector3(side * 0.62f, 1.55f, 1.38f), new Vector3(side * 0.62f, 1.3f, 1.2f), 10);
                mb.Tube(scroll, 0.045f, 0.035f, 10, "Gold");
                mb.Ball("Gold", scroll[scroll.Length - 1], 0.05f, 8);
            }
            // schienale imbottito e sedile
            mb.Box("RedDeep", new Vector3(0f, 1.1f, -1.28f), new Vector3(1.26f, 0.74f, 0.14f), 0.05f, new Vector3(-14f, 0f, 0f));
            mb.Box("Gold", new Vector3(0f, 1.5f, -1.38f), new Vector3(1.3f, 0.07f, 0.1f), 0.03f, new Vector3(-14f, 0f, 0f));
            mb.Box("Cream", new Vector3(0f, 0.6f, -0.62f), new Vector3(1.08f, 0.18f, 0.9f), 0.07f);
            // panello frontale
            mb.Box("RedDeep", new Vector3(0f, 0.78f, 1.5f), new Vector3(1.18f, 0.5f, 0.07f), 0.03f, new Vector3(18f, 0f, 0f));

            // sacco dei regali con pacchi che spuntano
            mb.Ball("GreenDeep", new Vector3(0f, 0.95f, 0.5f), 0.5f, new Vector3(1.0f, 1.0f, 1.12f), Vector3.zero, 18);
            mb.Ring("Gold", new Vector3(0f, 1.38f, 0.5f), 0.2f, 0.035f, Vector3.zero, 16, 6);
            AddGift(mb, new Vector3(0.3f, 1.25f, 0.2f), new Vector3(0.3f, 0.26f, 0.3f), "ClothBlue", "Gold", 20f);
            AddGift(mb, new Vector3(-0.28f, 1.2f, 0.7f), new Vector3(0.34f, 0.24f, 0.3f), "ClothPink", "Cream", -15f);
            AddGift(mb, new Vector3(0.0f, 1.32f, 0.5f), new Vector3(0.26f, 0.22f, 0.26f), "ClothYellow", "RedSanta", 5f);
            AddGift(mb, new Vector3(0.28f, 0.55f, -0.62f), new Vector3(0.28f, 0.22f, 0.28f), "GreenElf", "Cream", 40f);
            ModelKit.Part(root, "Body", mb);

            // lanterne anteriori luminose
            MeshBuilder lamps = new MeshBuilder();
            lamps.Ball("LampGlow", new Vector3(0.62f, 1.3f, 1.2f), 0.06f, 10);
            lamps.Ball("LampGlow", new Vector3(-0.62f, 1.3f, 1.2f), 0.06f, 10);
            ModelKit.Part(root, "Lamps", lamps);

            ModelKit.Empty(root, "DropPoint", new Vector3(0f, 1.2f, 0.45f));
            ModelKit.Empty(root, "InteractionPoint", new Vector3(-1.5f, 0f, 0.2f));
            ModelKit.Empty(root, "DeliveryFx", new Vector3(0f, 1.5f, 0.45f));

            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.8f, 0.1f), new Vector3(1.5f, 1.6f, 3.4f));
            return root.gameObject;
        }
    }
}
