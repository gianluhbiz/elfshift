using System.Collections.Generic;
using Elfshift.Vfx;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Giocattoli decorativi, camino, poltrone, lampadario, orologio, festoni: dettagli che rendono vivo il laboratorio.
    /// La forma vera di ogni oggetto è scolpita in Blender (Tools/Blender/models/decor.py, file Assets/Models/Source/Decor_*.bytes);
    /// se un file manca si usa la vecchia forma semplice costruita qui sotto (riserva).
    /// </summary>
    public static class DecorModels
    {
        /// <summary>Aggiunge il modello di Blender se esiste (senza avvisi se manca una variante di colore).</summary>
        private static bool TryImport(MeshBuilder mb, string model, Vector3 pos, Vector3 euler, float scale)
        {
            return ImportedMesh.Exists(model) && mb.Import(model, pos, euler, scale);
        }

        private static bool TryImport(MeshBuilder mb, string model, Matrix4x4 xf)
        {
            return ImportedMesh.Exists(model) && mb.Import(model, xf);
        }

        // ============================================================================ giocattoli (da aggiungere a un MeshBuilder)
        /// <summary>Orsetto di peluche. pos = centro della base; altezza circa 0,6 * scale.</summary>
        public static void AddTeddy(MeshBuilder mb, Vector3 pos, float scale, string fur, float yaw = 0f)
        {
            if (TryImport(mb, "Decor_Teddy_" + fur, pos, new Vector3(0f, yaw, 0f), scale)) return;
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            System.Func<float, float, float, Vector3> P = (x, y, z) => pos + q * (new Vector3(x, y, z) * scale);
            Vector3 e = new Vector3(0f, yaw, 0f);
            mb.Ball(fur, P(0f, 0.2f, 0f), 0.17f * scale, new Vector3(1f, 1.12f, 0.92f), e, 14);
            mb.Ball(fur, P(0f, 0.46f, 0.01f), 0.135f * scale, new Vector3(1f, 0.95f, 0.95f), e, 14);
            mb.Ball(fur, P(-0.1f, 0.57f, 0f), 0.05f * scale, 8);
            mb.Ball(fur, P(0.1f, 0.57f, 0f), 0.05f * scale, 8);
            mb.Ball("Cream", P(0f, 0.43f, 0.1f), 0.055f * scale, new Vector3(1f, 0.8f, 0.8f), e, 10);
            mb.Ball("Black", P(0f, 0.455f, 0.15f), 0.02f * scale, 6);
            mb.Ball("Black", P(-0.05f, 0.5f, 0.12f), 0.016f * scale, 6);
            mb.Ball("Black", P(0.05f, 0.5f, 0.12f), 0.016f * scale, 6);
            mb.Ball(fur, P(-0.18f, 0.24f, 0.05f), 0.065f * scale, new Vector3(0.8f, 1.1f, 0.8f), e, 8);
            mb.Ball(fur, P(0.18f, 0.24f, 0.05f), 0.065f * scale, new Vector3(0.8f, 1.1f, 0.8f), e, 8);
            mb.Ball(fur, P(-0.1f, 0.06f, 0.09f), 0.08f * scale, new Vector3(0.9f, 0.8f, 1.1f), e, 8);
            mb.Ball(fur, P(0.1f, 0.06f, 0.09f), 0.08f * scale, new Vector3(0.9f, 0.8f, 1.1f), e, 8);
            // fiocco rosso al collo
            mb.Ball("RedSanta", P(-0.05f, 0.34f, 0.12f), 0.04f * scale, new Vector3(1.4f, 0.8f, 0.7f), e, 8);
            mb.Ball("RedSanta", P(0.05f, 0.34f, 0.12f), 0.04f * scale, new Vector3(1.4f, 0.8f, 0.7f), e, 8);
        }

        /// <summary>Robot di latta. pos = centro della base; altezza circa 0,62 * scale.</summary>
        public static void AddRobot(MeshBuilder mb, Vector3 pos, float scale, string body, float yaw = 0f)
        {
            if (TryImport(mb, "Decor_Robot_" + body, pos, new Vector3(0f, yaw, 0f), scale)) return;
            Vector3 e = new Vector3(0f, yaw, 0f);
            Quaternion q = Quaternion.Euler(0f, yaw, 0f);
            System.Func<float, float, float, Vector3> P = (x, y, z) => pos + q * (new Vector3(x, y, z) * scale);
            mb.Box("MetalDark", P(-0.07f, 0.06f, 0f), new Vector3(0.09f, 0.12f, 0.1f) * scale, 0.015f * scale, e);
            mb.Box("MetalDark", P(0.07f, 0.06f, 0f), new Vector3(0.09f, 0.12f, 0.1f) * scale, 0.015f * scale, e);
            mb.Box(body, P(0f, 0.27f, 0f), new Vector3(0.24f, 0.28f, 0.17f) * scale, 0.03f * scale, e);
            mb.Box("RedSanta", P(0f, 0.3f, 0.09f), new Vector3(0.14f, 0.1f, 0.01f) * scale, 0.005f, e);
            mb.Ball("Gold", P(-0.035f, 0.3f, 0.1f), 0.014f * scale, 6);
            mb.Ball("Gold", P(0.035f, 0.3f, 0.1f), 0.014f * scale, 6);
            mb.Box("MetalLight", P(0f, 0.5f, 0f), new Vector3(0.2f, 0.15f, 0.15f) * scale, 0.03f * scale, e);
            mb.Ball("BulbBlue", P(-0.05f, 0.52f, 0.08f), 0.022f * scale, 8);
            mb.Ball("BulbBlue", P(0.05f, 0.52f, 0.08f), 0.022f * scale, 8);
            mb.Box("EyeDark", P(0f, 0.465f, 0.078f), new Vector3(0.09f, 0.015f, 0.01f) * scale, 0.003f, e);
            mb.CylC("MetalDark", P(0f, 0.62f, 0f), 0.008f * scale, 0.08f * scale, 0.002f, Vector3.zero, 6);
            mb.Ball("RedSanta", P(0f, 0.67f, 0f), 0.025f * scale, 8);
            mb.Box("MetalDark", P(-0.16f, 0.28f, 0f), new Vector3(0.05f, 0.2f, 0.06f) * scale, 0.01f, e + new Vector3(0f, 0f, -12f));
            mb.Box("MetalDark", P(0.16f, 0.28f, 0f), new Vector3(0.05f, 0.2f, 0.06f) * scale, 0.01f, e + new Vector3(0f, 0f, 12f));
        }

        public static void AddDrum(MeshBuilder mb, Vector3 pos, float scale)
        {
            if (TryImport(mb, "Decor_Drum", pos, Vector3.zero, scale)) return;
            mb.Cyl("RedSanta", pos, 0.15f * scale, 0.2f * scale, 0.01f, Vector3.zero, 18);
            mb.Cyl("Cream", pos + new Vector3(0f, 0.2f * scale, 0f), 0.15f * scale, 0.012f * scale, 0.004f, Vector3.zero, 18);
            mb.Ring("Gold", pos + new Vector3(0f, 0.01f * scale, 0f), 0.15f * scale, 0.012f * scale, Vector3.zero, 18, 5);
            mb.Ring("Gold", pos + new Vector3(0f, 0.2f * scale, 0f), 0.15f * scale, 0.012f * scale, Vector3.zero, 18, 5);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                mb.Box("Gold", pos + new Vector3(Mathf.Cos(a) * 0.152f, 0.1f, Mathf.Sin(a) * 0.152f) * scale, new Vector3(0.012f, 0.19f, 0.012f) * scale, 0.003f);
            }
            mb.CylC("WoodLight", pos + new Vector3(-0.04f, 0.225f, 0.03f) * scale, 0.01f * scale, 0.2f * scale, 0.003f, new Vector3(80f, 20f, 0f), 6);
            mb.CylC("WoodLight", pos + new Vector3(0.05f, 0.225f, 0.0f) * scale, 0.01f * scale, 0.2f * scale, 0.003f, new Vector3(80f, -25f, 0f), 6);
        }

        public static void AddSpinTop(MeshBuilder mb, Vector3 pos, float scale, string color)
        {
            if (TryImport(mb, "Decor_SpinTop_" + color, pos, Vector3.zero, scale)) return;
            mb.Cone(color, pos, 0.1f * scale, 0.11f * scale, new Vector3(180f, 0f, 0f), 16);
            mb.Cyl("ClothBlue", pos + new Vector3(0f, 0.11f * scale, 0f), 0.1f * scale, 0.045f * scale, 0.012f, Vector3.zero, 16);
            mb.Cyl("Gold", pos + new Vector3(0f, 0.155f * scale, 0f), 0.022f * scale, 0.09f * scale, 0.006f, Vector3.zero, 8);
        }

        public static void AddBlocks(MeshBuilder mb, Vector3 pos, float scale)
        {
            if (TryImport(mb, "Decor_Blocks", pos, Vector3.zero, scale)) return;
            string[] colors = { "ClothYellow", "ClothBlue", "RedSanta", "GreenElf" };
            float s = 0.11f * scale;
            for (int i = 0; i < 2; i++)
                mb.Box(colors[i], pos + new Vector3((i - 0.5f) * s * 1.1f, s * 0.5f, 0f), new Vector3(s, s, s), 0.012f * scale, new Vector3(0f, i * 14f - 7f, 0f));
            mb.Box(colors[2], pos + new Vector3(0f, s * 1.5f, 0f), new Vector3(s, s, s), 0.012f * scale, new Vector3(0f, 18f, 0f));
            // lettere (quadratini chiari) sul blocco superiore
            mb.Box("Cream", pos + new Vector3(0.01f, s * 1.5f, s * 0.5f), new Vector3(s * 0.5f, s * 0.5f, 0.004f), 0.002f, new Vector3(0f, 18f, 0f));
        }

        public static void AddBeachBall(MeshBuilder mb, Vector3 pos, float radius)
        {
            // il modello di Blender ha raggio 0,16
            if (TryImport(mb, "Decor_BeachBall", pos, Vector3.zero, radius / 0.16f)) return;
            mb.Ball("ClothPink", pos + new Vector3(0f, radius, 0f), radius, 16);
            mb.Ring("Snow", pos + new Vector3(0f, radius, 0f), radius * 0.99f, radius * 0.16f, Vector3.zero, 18, 6);
            mb.Ring("ClothYellow", pos + new Vector3(0f, radius, 0f), radius * 0.99f, radius * 0.14f, new Vector3(90f, 0f, 0f), 18, 6);
        }

        // ============================================================================ CAMINO
        /// <summary>Camino di mattoni con focolare, fuoco acceso, mensola con calze e ghirlanda. Fronte verso +Z, alto fino al soffitto.</summary>
        public static GameObject BuildFireplace(Transform parent, float ceilingHeight, out Light fireLight)
        {
            ModelKit.Begin("Fireplace");
            Transform root = ModelKit.Empty(parent, "Fireplace", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();

            if (TryImport(mb, "Decor_Fireplace", Vector3.zero, Vector3.zero, 1f))
            {
                // cappa in intonaco: il modello è alto 2,51 m e viene allungato fino al soffitto
                float chimney = Mathf.Max(0.1f, ceilingHeight - 2.49f);
                TryImport(mb, "Decor_FireplaceChimney", MeshBuilder.Xf(new Vector3(0f, 2.49f, 0f), Vector3.zero, new Vector3(1f, chimney / 2.51f, 1f)));
            }
            else
                AddFireplaceFallback(mb, ceilingHeight);
            ModelKit.Part(root, "Body", mb);

            // fiamme emissive (due oggetti separati: FlameFlicker li fa tremolare)
            MeshBuilder fire = new MeshBuilder();
            if (!TryImport(fire, "Decor_FireplaceFlames", Vector3.zero, Vector3.zero, 1f))
            {
                fire.Cone("FireOrange", new Vector3(-0.3f, 0.3f, 0.02f), 0.2f, 0.75f, Vector3.zero, 10);
                fire.Cone("FireOrange", new Vector3(0.28f, 0.3f, -0.02f), 0.18f, 0.62f, Vector3.zero, 10);
                fire.Cone("FireOrange", new Vector3(0f, 0.38f, 0f), 0.24f, 0.9f, Vector3.zero, 10);
            }
            ModelKit.Part(root, "Flames", fire);
            MeshBuilder inner = new MeshBuilder();
            if (!TryImport(inner, "Decor_FireplaceFlamesInner", Vector3.zero, Vector3.zero, 1f))
            {
                inner.Cone("FireYellow", new Vector3(0f, 0.38f, 0.04f), 0.13f, 0.6f, Vector3.zero, 10);
                inner.Cone("FireYellow", new Vector3(-0.28f, 0.3f, 0.05f), 0.09f, 0.4f, Vector3.zero, 8);
            }
            GameObject innerObject = ModelKit.Part(root, "FlamesInner", inner);

            Transform flamesTransform = root.Find("Flames");
            FlameFlicker flicker = root.gameObject.AddComponent<FlameFlicker>();
            flicker.EditorSetup(new[] { flamesTransform, innerObject.transform });

            GameObject lightObject = new GameObject("FireLight");
            lightObject.transform.SetParent(root, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.9f, 0.5f);
            fireLight = lightObject.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.22f);
            fireLight.intensity = 3.2f;
            fireLight.range = 12f;
            fireLight.shadows = LightShadows.None;
            lightObject.AddComponent<FlickerLight>();

            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 1.2f, 0.1f), new Vector3(4.0f, 2.4f, 1.3f));
            return root.gameObject;
        }

        /// <summary>Forma semplice di riserva del camino (se manca il modello di Blender).</summary>
        private static void AddFireplaceFallback(MeshBuilder mb, float ceilingHeight)
        {
            // focolare in pietra e struttura in mattoni
            mb.Box("Stone", new Vector3(0f, 0.08f, 1.05f), new Vector3(4.0f, 0.16f, 1.1f), 0.03f);
            for (int s = -1; s <= 1; s += 2)
                mb.Box("Brick", new Vector3(s * 1.45f, 1.15f, 0f), new Vector3(0.7f, 2.3f, 1.1f), 0.03f);
            mb.Box("Brick", new Vector3(0f, 2.0f, 0f), new Vector3(3.6f, 0.5f, 1.1f), 0.03f);
            mb.Box("Brick", new Vector3(0f, (2.25f + ceilingHeight) / 2f, -0.1f), new Vector3(2.9f, ceilingHeight - 2.25f, 0.8f), 0.03f);
            // fughe: strisce chiare orizzontali sui mattoni
            for (int i = 0; i < 9; i++)
                mb.Box("PlasterDark", new Vector3(0f, 0.3f + i * 0.25f, 0.555f), new Vector3(3.5f, 0.02f, 0.01f), 0.002f);
            // fondo del focolare, mensola, bordo in pietra
            mb.Box("Iron", new Vector3(0f, 0.95f, -0.42f), new Vector3(2.2f, 1.8f, 0.12f), 0.02f);
            mb.Box("WoodDark", new Vector3(0f, 2.38f, 0.12f), new Vector3(4.0f, 0.22f, 1.3f), 0.05f);
            mb.Box("Stone", new Vector3(0f, 1.82f, 0.58f), new Vector3(2.5f, 0.12f, 0.1f), 0.02f);
            mb.Ball("Snow", new Vector3(0f, ceilingHeight + 0.1f, 0f), 0.9f, new Vector3(1.5f, 0.15f, 0.7f), Vector3.zero, 10);
            // ceppi di legna nel focolare
            ItemModels.AddLog(mb, new Vector3(-0.35f, 0.2f, 0.05f), 0.11f, 0.9f, 12f);
            ItemModels.AddLog(mb, new Vector3(0.3f, 0.2f, -0.05f), 0.1f, 0.85f, -15f);
            ItemModels.AddLog(mb, new Vector3(0f, 0.38f, 0f), 0.1f, 0.8f, 3f);
            // ghirlanda e calze sulla mensola
            Vector3[] garland = MeshBuilder.Bezier(new Vector3(-1.9f, 2.52f, 0.72f), new Vector3(-1.0f, 2.2f, 0.78f), new Vector3(1.0f, 2.2f, 0.78f), new Vector3(1.9f, 2.52f, 0.72f), 16);
            mb.Tube(garland, 0.08f, 0.08f, 8, "GreenPine");
            for (int i = 1; i < garland.Length - 1; i += 2)
                mb.Ball(i % 4 == 1 ? "RedSanta" : "Gold", garland[i] + new Vector3(0f, -0.05f, 0.06f), 0.05f, 8);
            string[] sockColors = { "RedSanta", "GreenElf", "RedSanta" };
            for (int i = 0; i < 3; i++)
            {
                float x = -1.1f + i * 1.1f;
                mb.Ball("Gold", new Vector3(x, 2.27f, 0.62f), 0.03f, 6);
                mb.Taper(sockColors[i], new Vector3(x, 1.52f, 0.62f), 0.12f, 0.15f, 0.72f, 0.05f, Vector3.zero, 12);
                mb.Ball(sockColors[i], new Vector3(x + 0.07f, 1.52f, 0.62f), 0.14f, new Vector3(1.3f, 0.7f, 0.9f), Vector3.zero, 10);
                mb.Ring("White", new Vector3(x, 2.2f, 0.62f), 0.14f, 0.045f, Vector3.zero, 12, 6);
            }
            // candele sulla mensola
            for (int s = -1; s <= 1; s += 2)
            {
                mb.Cyl("Cream", new Vector3(s * 1.7f, 2.49f, 0.2f), 0.045f, 0.28f, 0.01f, Vector3.zero, 10);
                mb.Ball("LampGlow", new Vector3(s * 1.7f, 2.82f, 0.2f), 0.035f, new Vector3(1f, 1.5f, 1f), Vector3.zero, 8);
            }
        }

        // ============================================================================ POLTRONA
        public static GameObject BuildArmchair(Transform parent, string fabric)
        {
            ModelKit.Begin("Armchair");
            Transform root = ModelKit.Empty(parent, "Armchair", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            string dark = fabric == "RedSanta" ? "RedDeep" : "GreenDeep";
            if (!TryImport(mb, "Decor_Armchair_" + fabric, Vector3.zero, Vector3.zero, 1f))
            {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    mb.Cyl("WoodDark", new Vector3(sx * 0.38f, 0f, sz * 0.34f), 0.05f, 0.16f, 0.015f, Vector3.zero, 8);
            mb.Box("WoodDark", new Vector3(0f, 0.2f, 0f), new Vector3(0.95f, 0.1f, 0.88f), 0.03f);
            mb.Box(fabric, new Vector3(0f, 0.36f, 0.06f), new Vector3(0.72f, 0.22f, 0.66f), 0.08f);
            mb.Box(fabric, new Vector3(0f, 0.68f, -0.32f), new Vector3(0.86f, 0.74f, 0.24f), 0.1f, new Vector3(-8f, 0f, 0f));
            for (int s = -1; s <= 1; s += 2)
                mb.Box(dark, new Vector3(s * 0.45f, 0.46f, 0f), new Vector3(0.17f, 0.42f, 0.82f), 0.07f);
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 3; c++)
                    mb.Ball("Gold", new Vector3(-0.22f + c * 0.22f, 0.6f + r * 0.2f, -0.19f - r * 0.01f), 0.022f, 6);
            mb.Box("ClothYellow", new Vector3(-0.2f, 0.56f, 0.1f), new Vector3(0.3f, 0.28f, 0.1f), 0.04f, new Vector3(-20f, 15f, 8f));
            mb.Box("GreenElf", new Vector3(0.15f, 0.5f, 0.06f), new Vector3(0.5f, 0.07f, 0.6f), 0.03f, new Vector3(0f, 12f, 0f));
            }
            ModelKit.Part(root, "Body", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.5f, 0f), new Vector3(0.95f, 1.0f, 0.88f));
            return root.gameObject;
        }

        public static GameObject BuildSideTable(Transform parent)
        {
            ModelKit.Begin("SideTable");
            Transform root = ModelKit.Empty(parent, "SideTable", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!TryImport(mb, "Decor_SideTable", Vector3.zero, Vector3.zero, 1f))
            {
            mb.Cyl("WoodDark", Vector3.zero, 0.24f, 0.05f, 0.015f, Vector3.zero, 16);
            mb.Cyl("WoodDark", new Vector3(0f, 0.04f, 0f), 0.06f, 0.5f, 0.01f, Vector3.zero, 10);
            mb.Cyl("WoodLight", new Vector3(0f, 0.54f, 0f), 0.36f, 0.05f, 0.015f, Vector3.zero, 20);
            mb.Cyl("Cream", new Vector3(-0.08f, 0.59f, 0.05f), 0.13f, 0.015f, 0.004f, Vector3.zero, 14);
            for (int i = 0; i < 4; i++)
                mb.Ball("CookieBrown", new Vector3(-0.08f + Mathf.Cos(i * 1.6f) * 0.05f, 0.615f + (i == 3 ? 0.02f : 0f), 0.05f + Mathf.Sin(i * 1.6f) * 0.05f), 0.04f, new Vector3(1f, 0.35f, 1f), Vector3.zero, 8);
            mb.Cyl("Cream", new Vector3(0.18f, 0.59f, -0.1f), 0.045f, 0.09f, 0.01f, Vector3.zero, 10);
            mb.Ring("Cream", new Vector3(0.235f, 0.64f, -0.1f), 0.03f, 0.008f, new Vector3(0f, 0f, 90f), 8, 5);
            }
            ModelKit.Part(root, "Body", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.3f, 0f), new Vector3(0.72f, 0.6f, 0.72f));
            return root.gameObject;
        }

        // ============================================================================ TAVOLO ESPOSITORE
        public static GameObject BuildDisplayTable(Transform parent)
        {
            ModelKit.Begin("DisplayTable");
            Transform root = ModelKit.Empty(parent, "DisplayTable", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!TryImport(mb, "Decor_DisplayTable", Vector3.zero, Vector3.zero, 1f))
            {
            mb.Cyl("WoodDark", Vector3.zero, 0.7f, 0.08f, 0.02f, Vector3.zero, 24);
            mb.Cyl("WoodMid", new Vector3(0f, 0.06f, 0f), 0.14f, 0.72f, 0.02f, Vector3.zero, 14);
            mb.Cyl("WoodLight", new Vector3(0f, 0.76f, 0f), 1.05f, 0.07f, 0.02f, Vector3.zero, 28);
            mb.Taper("RedSanta", new Vector3(0f, 0.5f, 0f), 1.12f, 1.08f, 0.32f, 0.04f, Vector3.zero, 28);
            mb.Cyl("Cream", new Vector3(0f, 0.83f, 0f), 0.98f, 0.02f, 0.008f, Vector3.zero, 28);
            mb.Ring("Gold", new Vector3(0f, 0.84f, 0f), 0.96f, 0.014f, Vector3.zero, 28, 6);
            }
            // giocattoli in vetrina (sul centrino, a 0,85 m)
            AddTeddy(mb, new Vector3(-0.4f, 0.85f, 0.15f), 0.9f, "CookieBrown", 200f);
            AddRobot(mb, new Vector3(0.4f, 0.85f, 0.1f), 0.9f, "ClothTeal", 160f);
            AddDrum(mb, new Vector3(0.05f, 0.85f, -0.45f), 1.0f);
            AddBlocks(mb, new Vector3(-0.2f, 0.85f, -0.4f), 1.2f);
            AddSpinTop(mb, new Vector3(0.35f, 0.85f, -0.35f), 1.2f, "ClothPink");
            ModelKit.Part(root, "Body", mb);
            GameObject col = new GameObject("Collider");
            col.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(col, new Vector3(0f, 0.45f, 0f), new Vector3(2.1f, 0.9f, 2.1f));
            return root.gameObject;
        }

        // ============================================================================ LAMPADARIO
        public static GameObject BuildChandelier(Transform parent, float cordLength, out Vector3 lightLocalPos)
        {
            ModelKit.Begin("Chandelier");
            Transform root = ModelKit.Empty(parent, "Chandelier", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!TryImport(mb, "Decor_ChandelierMount", Vector3.zero, Vector3.zero, 1f))
                mb.Cyl("WoodDark", new Vector3(0f, -0.04f, 0f), 0.14f, 0.08f, 0.02f, Vector3.zero, 12);
            mb.CylC("Iron", new Vector3(0f, -cordLength / 2f, 0f), 0.016f, cordLength, 0.004f, Vector3.zero, 8);
            Vector3 center = new Vector3(0f, -cordLength, 0f);
            if (!TryImport(mb, "Decor_Chandelier", center, Vector3.zero, 1f))
            {
            mb.Ring("Gold", center, 0.8f, 0.04f, Vector3.zero, 28, 8);
            mb.Ring("Iron", center + new Vector3(0f, 0.18f, 0f), 0.45f, 0.025f, Vector3.zero, 22, 6);
            mb.Ball("Gold", center + new Vector3(0f, 0.08f, 0f), 0.1f, 10);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a) * 0.8f, 0.04f, Mathf.Sin(a) * 0.8f);
                mb.Cyl("Cream", pos, 0.04f, 0.16f, 0.008f, Vector3.zero, 8);
                mb.Ball("LampGlow", pos + new Vector3(0f, 0.2f, 0f), 0.04f, new Vector3(1f, 1.6f, 1f), Vector3.zero, 8);
                Vector3[] chain = { center + new Vector3(0f, 0.2f, 0f), pos + new Vector3(0f, 0.05f, 0f) };
                mb.Tube(chain, 0.008f, 0.008f, 4, "Iron", null, 0, false);
                mb.Ball("RedSanta", center + new Vector3(Mathf.Cos(a + 0.5f) * 0.8f, -0.06f, Mathf.Sin(a + 0.5f) * 0.8f), 0.05f, 8);
            }
            }
            ModelKit.Part(root, "Mesh", mb);
            lightLocalPos = center + new Vector3(0f, 0.1f, 0f);
            return root.gameObject;
        }

        // ============================================================================ OROLOGIO
        public static GameObject BuildWallClock(Transform parent)
        {
            ModelKit.Begin("WallClock");
            Transform root = ModelKit.Empty(parent, "WallClock", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!TryImport(mb, "Decor_WallClock", Vector3.zero, Vector3.zero, 1f))
            {
            mb.Cyl("Cream", Vector3.zero, 0.42f, 0.06f, 0.01f, new Vector3(90f, 0f, 0f), 28);
            mb.Ring("Gold", new Vector3(0f, 0f, 0.0f), 0.42f, 0.04f, new Vector3(90f, 0f, 0f), 28, 8);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                mb.Box("WoodDeep", new Vector3(Mathf.Sin(a) * 0.34f, Mathf.Cos(a) * 0.34f, 0.035f), new Vector3(i % 3 == 0 ? 0.04f : 0.02f, i % 3 == 0 ? 0.07f : 0.045f, 0.01f), 0.002f, new Vector3(0f, 0f, -i * 30f));
            }
            mb.Box("WoodDeep", new Vector3(0.05f, 0.08f, 0.045f), new Vector3(0.03f, 0.26f, 0.012f), 0.004f, new Vector3(0f, 0f, -30f));
            mb.Box("WoodDeep", new Vector3(-0.06f, -0.02f, 0.05f), new Vector3(0.025f, 0.34f, 0.012f), 0.004f, new Vector3(0f, 0f, 70f));
            mb.Ball("RedSanta", new Vector3(0f, 0f, 0.055f), 0.03f, 8);
            // pendolo e campanellini
            mb.Box("WoodMid", new Vector3(0f, -0.62f, 0.0f), new Vector3(0.04f, 0.45f, 0.02f), 0.005f);
            mb.Ball("Gold", new Vector3(0f, -0.86f, 0f), 0.1f, new Vector3(1f, 1f, 0.35f), Vector3.zero, 12);
            // fiocco di agrifoglio
            mb.Ball("GreenElf", new Vector3(-0.22f, 0.42f, 0.04f), 0.07f, new Vector3(1.3f, 0.6f, 0.4f), new Vector3(0f, 0f, 30f), 8);
            mb.Ball("GreenElf", new Vector3(0.22f, 0.42f, 0.04f), 0.07f, new Vector3(1.3f, 0.6f, 0.4f), new Vector3(0f, 0f, -30f), 8);
            mb.Ball("RedSanta", new Vector3(0f, 0.42f, 0.06f), 0.04f, 8);
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        // ============================================================================ FESTONI DI BANDIERINE
        /// <summary>Filo di bandierine triangolari colorate appeso tra due punti.</summary>
        public static GameObject BuildBunting(Transform parent, string name, Vector3 from, Vector3 to, float sag, int flagCount)
        {
            ModelKit.Begin("Bunting_" + name);
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
            MeshBuilder mb = new MeshBuilder();
            mb.Tube(path, 0.01f, 0.01f, 6, "WoodDeep", null, 0, false);
            string[] colors = { "RedSanta", "GreenElf", "Gold", "Snow", "ClothBlue" };
            // bandierine di stoffa fatte in Blender (il giallo è stoffa, non metallo)
            string[] flagModels = { "Decor_BuntingFlag_RedSanta", "Decor_BuntingFlag_GreenElf", "Decor_BuntingFlag_ClothYellow", "Decor_BuntingFlag_Snow", "Decor_BuntingFlag_ClothBlue" };
            Vector3 dir = to - from;
            Vector2[] triangle = { new Vector2(-0.17f, 0f), new Vector2(0.17f, 0f), new Vector2(0f, -0.34f) };
            for (int i = 0; i < flagCount; i++)
            {
                float t = (i + 0.5f) / flagCount;
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y -= sag * 4f * t * (1f - t);
                // le bandierine si orientano lungo il filo
                float tiltYaw = Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? 0f : 90f;
                // piccola oscillazione diversa per ogni bandierina: sembrano appese a mano
                float sway = Mathf.Sin(i * 12.9898f) * 7f;
                float twist = Mathf.Sin(i * 78.233f) * 6f;
                if (TryImport(mb, flagModels[i % flagModels.Length], p, new Vector3(sway, tiltYaw + twist, 0f), 1f)) continue;
                mb.Prism(MeshBuilder.Xf(p + new Vector3(0f, -0.01f, 0f), new Vector3(0f, tiltYaw, 0f)), triangle, 0.012f, mb.Sub(colors[i % colors.Length]));
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }
    }
}
