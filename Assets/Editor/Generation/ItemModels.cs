using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Modelli 3D degli oggetti trasportabili. L'origine di ogni modello è al centro della sua base.
    /// I modelli veri sono scolpiti in Blender (Tools/Blender/models/items.py → Assets/Models/Source/Item_*.bytes);
    /// se un file manca si usa la vecchia forma geometrica di riserva, così il gioco funziona comunque.
    /// </summary>
    public static class ItemModels
    {
        // Misure del tronchetto modellato in Blender (Item_Log): servono a scalarlo alle misure richieste.
        private const float BlenderLogRadius = 0.1f;
        private const float BlenderLogLength = 0.56f;

        /// <summary>Un tronchetto orizzontale (asse X), con le sezioni chiare alle estremità.</summary>
        public static void AddLog(MeshBuilder mb, Vector3 center, float radius, float length, float yawDegrees = 0f)
        {
            Matrix4x4 logXf = Matrix4x4.TRS(center, Quaternion.Euler(0f, yawDegrees, 0f),
                new Vector3(length / BlenderLogLength, radius / BlenderLogRadius, radius / BlenderLogRadius));
            if (mb.Import("Item_Log", logXf)) return;

            float h = length;
            float b = Mathf.Min(0.02f, radius * 0.3f);
            // Corteccia: tubo con bordi arrotondati, senza tappi.
            Profile bark = new Profile();
            bark.Add(radius - b, 0f);
            bark.Arc(radius - b, b, b, -90f, 0f, 2);
            bark.Arc(radius - b, h - b, b, 0f, 90f, 2);
            Matrix4x4 xf = MeshBuilder.Xf(center, new Vector3(0f, yawDegrees, 0f)) * MeshBuilder.Xf(Vector3.zero, new Vector3(0f, 0f, 90f))
                           * MeshBuilder.Xf(new Vector3(0f, -h * 0.5f, 0f));
            mb.Lathe(xf, bark, 10, mb.Sub("Bark"));
            // Sezioni di taglio (legno chiaro) un filo sporgenti.
            Profile cap = Profile.Cylinder(radius - b * 0.6f, 0.012f, 0.004f, 1);
            mb.Lathe(xf * MeshBuilder.Xf(new Vector3(0f, -0.004f, 0f)), cap, 10, mb.Sub("WoodEnd"));
            mb.Lathe(xf * MeshBuilder.Xf(new Vector3(0f, h - 0.008f, 0f)), cap, 10, mb.Sub("WoodEnd"));
            // Anello centrale più scuro: simula gli anelli del tronco.
            Profile ring = Profile.Cylinder(radius * 0.45f, 0.016f, 0.004f, 1);
            mb.Lathe(xf * MeshBuilder.Xf(new Vector3(0f, -0.006f, 0f)), ring, 8, mb.Sub("WoodMid"));
            mb.Lathe(xf * MeshBuilder.Xf(new Vector3(0f, h - 0.01f, 0f)), ring, 8, mb.Sub("WoodMid"));
        }

        /// <summary>Fascio di tre tronchetti legati con un nastro rosso: l'oggetto "Legno".</summary>
        public static void AddLogBundle(MeshBuilder mb, Vector3 origin, float scale = 1f)
        {
            if (mb.Import("Item_Wood", origin, Vector3.zero, scale)) return;

            float r = 0.1f * scale;
            float len = 0.56f * scale;
            AddLog(mb, origin + new Vector3(0f, r, -r * 1.05f), r, len);
            AddLog(mb, origin + new Vector3(0f, r, r * 1.05f), r, len);
            AddLog(mb, origin + new Vector3(0f, r + r * 1.8f, 0f), r, len);
            // nastro rosso con fiocco
            float ringR = r * 1.95f;
            for (int i = -1; i <= 1; i += 2)
                mb.Ring("RedSanta", origin + new Vector3(i * 0.15f * scale, r * 1.35f, 0f), ringR, 0.016f * scale, new Vector3(0f, 0f, 90f), 20, 6);
            mb.Ball("RedSanta", origin + new Vector3(0.15f * scale, r * 3.1f, -0.045f * scale), 0.04f * scale, new Vector3(1f, 1f, 1.8f), Vector3.zero, 8);
            mb.Ball("RedSanta", origin + new Vector3(0.15f * scale, r * 3.1f, 0.045f * scale), 0.04f * scale, new Vector3(1f, 1f, 1.8f), Vector3.zero, 8);
            mb.Ball("RedDeep", origin + new Vector3(0.15f * scale, r * 3.1f, 0f), 0.028f * scale, 8);
        }

        public static GameObject BuildWood(Transform parent)
        {
            ModelKit.Begin("Item_Wood");
            Transform root = ModelKit.Empty(parent, "Item_Wood", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            AddLogBundle(mb, Vector3.zero);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Trenino giocattolo (locomotiva rossa + vagone verde con cubetti), lungo l'asse Z.</summary>
        public static GameObject BuildTrain(Transform parent)
        {
            ModelKit.Begin("Item_Train");
            Transform root = ModelKit.Empty(parent, "Item_Train", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (mb.Import("Item_Train", Vector3.zero))
            {
                ModelKit.Part(root, "Mesh", mb);
                return root.gameObject;
            }

            // --- forma di riserva (se manca il modello di Blender) ---
            AddTrainFallback(mb);
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }


        /// <summary>Trenino fatto di forme semplici (se manca il modello di Blender).</summary>
        private static void AddTrainFallback(MeshBuilder mb)
        {
            // telaio
            mb.Box("WoodDeep", new Vector3(0f, 0.105f, 0f), new Vector3(0.2f, 0.05f, 0.92f), 0.015f);
            // --- locomotiva (z positivo)
            mb.CylC("RedSanta", new Vector3(0f, 0.215f, 0.25f), 0.095f, 0.34f, 0.025f, new Vector3(90f, 0f, 0f), 18);
            mb.CylC("MetalDark", new Vector3(0f, 0.215f, 0.44f), 0.1f, 0.06f, 0.015f, new Vector3(90f, 0f, 0f), 18);
            mb.Ball("LampGlow", new Vector3(0f, 0.215f, 0.475f), 0.032f, 10);
            mb.Ring("Gold", new Vector3(0f, 0.215f, 0.2f), 0.097f, 0.01f, new Vector3(90f, 0f, 0f), 18, 6);
            mb.Ring("Gold", new Vector3(0f, 0.215f, 0.33f), 0.097f, 0.01f, new Vector3(90f, 0f, 0f), 18, 6);
            // camino svasato e cupola
            Profile chim = new Profile().Add(0f, 0f).Add(0.032f, 0f).Add(0.032f, 0.05f).Add(0.052f, 0.11f).Add(0.052f, 0.125f).Add(0f, 0.125f);
            mb.Lathe(MeshBuilder.Xf(new Vector3(0f, 0.3f, 0.38f)), chim, 14, mb.Sub("Iron"));
            mb.Ball("Gold", new Vector3(0f, 0.315f, 0.25f), 0.036f, new Vector3(1f, 0.8f, 1f), Vector3.zero, 10);
            // cabina con tetto
            mb.Box("RedDeep", new Vector3(0f, 0.23f, 0.0f), new Vector3(0.2f, 0.2f, 0.16f), 0.02f);
            mb.Box("Cream", new Vector3(0f, 0.343f, 0.0f), new Vector3(0.235f, 0.03f, 0.19f), 0.012f);
            mb.Box("EyeDark", new Vector3(0.101f, 0.25f, 0.0f), new Vector3(0.012f, 0.07f, 0.075f), 0.004f);
            mb.Box("EyeDark", new Vector3(-0.101f, 0.25f, 0.0f), new Vector3(0.012f, 0.07f, 0.075f), 0.004f);
            // --- vagone (z negativo)
            mb.Box("GreenElf", new Vector3(0f, 0.17f, -0.27f), new Vector3(0.2f, 0.12f, 0.36f), 0.025f);
            mb.Box("GreenDeep", new Vector3(0f, 0.235f, -0.27f), new Vector3(0.215f, 0.02f, 0.38f), 0.008f);
            mb.Box("ClothYellow", new Vector3(0f, 0.285f, -0.17f), new Vector3(0.09f, 0.09f, 0.09f), 0.015f, new Vector3(0f, 12f, 0f));
            mb.Box("ClothBlue", new Vector3(0.01f, 0.285f, -0.3f), new Vector3(0.09f, 0.09f, 0.09f), 0.015f, new Vector3(0f, -9f, 0f));
            mb.Box("ClothPink", new Vector3(-0.005f, 0.375f, -0.235f), new Vector3(0.08f, 0.08f, 0.08f), 0.015f, new Vector3(0f, 25f, 0f));
            // gancio
            mb.CylC("Gold", new Vector3(0f, 0.125f, -0.075f), 0.014f, 0.16f, 0.004f, new Vector3(90f, 0f, 0f), 8);
            // ruote
            float[] wheelZ = { 0.38f, 0.2f, 0.02f, -0.18f, -0.36f };
            for (int i = 0; i < wheelZ.Length; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 0.115f;
                    mb.CylC("Iron", new Vector3(x, 0.07f, wheelZ[i]), i < 3 ? 0.07f : 0.06f, 0.035f, 0.01f, new Vector3(0f, 0f, 90f), 16);
                    mb.CylC("Gold", new Vector3(x + side * 0.012f, 0.07f, wheelZ[i]), 0.028f, 0.04f, 0.008f, new Vector3(0f, 0f, 90f), 10);
                }
            }
        }

        // ============================================================================ nuove materie prime
        /// <summary>Rotolo di stoffa (materia prima per l'orsetto).</summary>
        public static GameObject BuildCloth(Transform parent)
        {
            ModelKit.Begin("Item_Cloth");
            Transform root = ModelKit.Empty(parent, "Item_Cloth", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Item_Cloth", Vector3.zero))
            {
                mb.CylC("RedSanta", new Vector3(0f, 0.11f, 0f), 0.11f, 0.5f, 0.02f, new Vector3(0f, 0f, 90f), 20);
                mb.Ring("Gold", new Vector3(0f, 0.11f, 0f), 0.115f, 0.008f, new Vector3(0f, 0f, 90f), 20, 6);
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Pacchetto di lamiere con ingranaggio (materia prima per il robot).</summary>
        public static GameObject BuildMetal(Transform parent)
        {
            ModelKit.Begin("Item_Metal");
            Transform root = ModelKit.Empty(parent, "Item_Metal", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Item_Metal", Vector3.zero))
            {
                mb.Box("MetalLight", new Vector3(0f, 0.02f, 0f), new Vector3(0.44f, 0.03f, 0.28f), 0.008f);
                mb.Box("MetalDark", new Vector3(0f, 0.055f, 0f), new Vector3(0.44f, 0.03f, 0.28f), 0.008f);
                FurnitureModels.AddGear(mb, new Vector3(0.05f, 0.085f, 0f), 0.08f, "Gold");
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        // ============================================================================ giocattoli nelle varie fasi
        /// <summary>I tre giocattoli del laboratorio.</summary>
        public enum ToyKind { Train, Teddy, Robot }

        /// <summary>Fasi della lavorazione: grezzo (appena costruito), dipinto/decorato.</summary>
        public enum ToyStage { Raw, Painted }

        /// <summary>
        /// Giocattolo in una fase della lavorazione. La versione grezza usa lo stesso modello di quella finita
        /// ma con i colori "naturali" (legno chiaro, stoffa grezza, latta): così si vede subito cosa manca.
        /// </summary>
        public static GameObject BuildToy(Transform parent, ToyKind kind, ToyStage stage)
        {
            string name = "Item_" + kind + (stage == ToyStage.Raw ? "Raw" : "Painted");
            ModelKit.Begin(name);
            Transform root = ModelKit.Empty(parent, name, Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (stage == ToyStage.Raw) mb.Remap = RawRemap(kind);
            switch (kind)
            {
                case ToyKind.Train:
                    if (!mb.Import("Item_Train", Vector3.zero)) AddTrainFallback(mb);
                    break;
                case ToyKind.Teddy:
                    DecorModels.AddTeddy(mb, Vector3.zero, 0.62f, "CookieBrown", 0f);
                    break;
                default:
                    DecorModels.AddRobot(mb, Vector3.zero, 0.6f, "ClothBlue", 0f);
                    break;
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Pacco regalo pronto per la slitta: carta e nastro con i colori del giocattolo che contiene.</summary>
        public static GameObject BuildGift(Transform parent, ToyKind kind)
        {
            string name = "Item_" + kind + "Gift";
            ModelKit.Begin(name);
            Transform root = ModelKit.Empty(parent, name, Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            switch (kind)
            {
                case ToyKind.Train:
                    FurnitureModels.AddGift(mb, Vector3.zero, new Vector3(0.46f, 0.3f, 0.36f), "RedSanta", "Gold", 0f);
                    break;
                case ToyKind.Teddy:
                    FurnitureModels.AddGift(mb, Vector3.zero, new Vector3(0.38f, 0.38f, 0.38f), "ClothPink", "Cream", 0f);
                    break;
                default:
                    FurnitureModels.AddGift(mb, Vector3.zero, new Vector3(0.36f, 0.42f, 0.36f), "ClothBlue", "Gold", 0f);
                    break;
            }
            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Colori della versione grezza di ogni giocattolo.</summary>
        private static System.Func<string, string> RawRemap(ToyKind kind)
        {
            switch (kind)
            {
                case ToyKind.Train:
                    // legno naturale: le parti scure restano scure, tutto il resto diventa legno chiaro
                    return key => key == "WoodDeep" || key == "WoodDark" || key == "Iron" ? "WoodDark" : "WoodLight";
                case ToyKind.Teddy:
                    // stoffa grezza color crema, senza occhi né fiocco
                    return key => "Cream";
                default:
                    // latta non verniciata
                    return key => key == "MetalDark" || key == "Iron" || key == "EyeDark" || key == "Black" ? "MetalDark" : "MetalLight";
            }
        }


    }
}