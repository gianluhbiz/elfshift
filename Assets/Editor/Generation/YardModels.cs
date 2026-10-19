using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Modelli del cortile e delle aperture: cassetta della posta, renne, cornici delle porte, cassetta di carote, balla di fieno.
    /// Le forme vere sono scolpite in Blender (Tools/Blender/models/yard.py → Assets/Models/Source/Yard_*.bytes);
    /// se un file manca si usa una forma semplice di riserva, così il gioco funziona comunque.
    /// Tutti i modelli guardano verso +Z e hanno l'origine a terra, al centro.
    /// </summary>
    public static class YardModels
    {
        // Cerniera della bandierina e perno del collo della renna (convertiti dalle coordinate di Blender).
        private static readonly Vector3 FlagHinge = new Vector3(0.228f, 1.1f, -0.13f);
        private static readonly Vector3 NeckPivot = new Vector3(0f, 1.12f, 0.53f);

        // Misure dei moduli della cornice (devono corrispondere a yard.py).
        private const float CasingWidth = 0.16f;
        private const float PlinthHeight = 0.26f;
        private const float CornerSize = 0.2f;

        // ================================================================== cassetta della posta
        /// <summary>
        /// Cassetta della posta rossa su palo (alta circa 1,35 m). Figli:
        /// "Body" (fissa), "Flag" (bandierina: rotazione locale Z = 0 alzata, -90 abbassata),
        /// "Letters" (letterine che spuntano dalla fessura, da accendere/spegnere), "InteractionPoint".
        /// </summary>
        public static GameObject BuildMailbox(Transform parent)
        {
            ModelKit.Begin("Yard_Mailbox");
            Transform root = ModelKit.Empty(parent, "Mailbox", Vector3.zero);

            MeshBuilder body = new MeshBuilder();
            if (!body.Import("Yard_MailboxBody", Vector3.zero))
            {
                body.Box("WoodMid", new Vector3(0f, 0.45f, 0f), new Vector3(0.11f, 0.9f, 0.11f), 0.015f);
                body.Box("RedSanta", new Vector3(0f, 1.05f, 0f), new Vector3(0.42f, 0.3f, 0.56f), 0.08f);
                body.Box("Black", new Vector3(0f, 1.2f, 0.29f), new Vector3(0.21f, 0.022f, 0.03f), 0f);
            }
            ModelKit.Part(root, "Body", body);

            Transform flag = ModelKit.Empty(root, "Flag", FlagHinge);
            MeshBuilder flagMesh = new MeshBuilder();
            if (!flagMesh.Import("Yard_MailboxFlag", Vector3.zero))
            {
                flagMesh.Box("Gold", new Vector3(0f, 0.12f, 0f), new Vector3(0.018f, 0.24f, 0.035f), 0.005f);
                flagMesh.Box("RedSanta", new Vector3(0f, 0.2f, 0.045f), new Vector3(0.014f, 0.08f, 0.12f), 0.006f);
            }
            ModelKit.Part(flag, "Mesh", flagMesh);

            Transform letters = ModelKit.Empty(root, "Letters", Vector3.zero);
            MeshBuilder lettersMesh = new MeshBuilder();
            if (!lettersMesh.Import("Yard_MailboxLetters", Vector3.zero))
                lettersMesh.Box("Cream", new Vector3(0f, 1.2f, 0.33f), new Vector3(0.16f, 0.006f, 0.14f), 0.002f);
            ModelKit.Part(letters, "Mesh", lettersMesh);

            ModelKit.Empty(root, "InteractionPoint", new Vector3(0f, 0f, 0.9f));
            ModelKit.AddBoxCollider(root.gameObject, new Vector3(0f, 0.68f, 0f), new Vector3(0.46f, 1.36f, 0.6f));
            return root.gameObject;
        }

        // ================================================================== renna
        /// <summary>
        /// Renna paffuta in stile cartone (lunga ~1,5 m, corna fino a ~1,75 m). variant 1 = naso rosso luminoso.
        /// Figli: "Body" (fisso) e "Head" (testa e corna, perno al collo: ruotarlo su X per farle annuire).
        /// </summary>
        public static GameObject BuildReindeer(Transform parent, int variant)
        {
            int v = variant == 1 ? 1 : 0;
            ModelKit.Begin("Yard_Reindeer" + v);
            Transform root = ModelKit.Empty(parent, "Reindeer", Vector3.zero);

            MeshBuilder body = new MeshBuilder();
            if (!body.Import("Yard_ReindeerBody", Vector3.zero))
            {
                body.Ball("ReindeerBrown", new Vector3(0f, 0.77f, -0.04f), 1f, new Vector3(0.37f, 0.35f, 0.6f), Vector3.zero, 16);
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        body.Cyl("ReindeerBrown", new Vector3(sx * 0.165f, 0f, sz * 0.3f), 0.09f, 0.66f, 0.02f);
                body.Ring("RedSanta", new Vector3(0f, 1f, 0.45f), 0.158f, 0.03f, new Vector3(-35f, 0f, 0f));
            }
            ModelKit.Part(root, "Body", body);

            Transform head = ModelKit.Empty(root, "Head", NeckPivot);
            MeshBuilder headMesh = new MeshBuilder();
            if (!headMesh.Import("Yard_ReindeerHead_" + v, Vector3.zero))
            {
                headMesh.Ball("ReindeerBrown", new Vector3(0f, 0.12f, 0.13f), 1f, new Vector3(0.19f, 0.19f, 0.24f), Vector3.zero, 14);
                headMesh.Ball(v == 1 ? "BulbRed" : "Black", new Vector3(0f, 0.09f, 0.43f), 0.05f, 10);
                for (int sx = -1; sx <= 1; sx += 2)
                    headMesh.Cyl("Antler", new Vector3(sx * 0.1f, 0.28f, 0.06f), 0.025f, 0.45f, 0.01f, new Vector3(-15f, 0f, sx * -20f));
            }
            ModelKit.Part(head, "Mesh", headMesh);

            ModelKit.AddBoxCollider(root.gameObject, new Vector3(0f, 0.75f, 0.05f), new Vector3(0.8f, 1.5f, 1.4f));
            return root.gameObject;
        }

        // ================================================================== cornice delle porte
        /// <summary>
        /// Cornice di legno per un'apertura nel muro (su entrambe le facce, più il rivestimento interno e la soglia).
        /// Origine in basso al centro dell'apertura, a metà dello spessore del muro: l'apertura va da x=-width/2 a +width/2
        /// e da y=0 a y=height; il muro va da z=-wallThickness/2 a +wallThickness/2.
        /// </summary>
        public static GameObject BuildDoorFrame(Transform parent, float width, float height, float wallThickness)
        {
            ModelKit.Begin("Yard_DoorFrame_" + Mathf.RoundToInt(width * 100f) + "x" + Mathf.RoundToInt(height * 100f));
            Transform root = ModelKit.Empty(parent, "DoorFrame", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            float half = width / 2f;
            float t = wallThickness / 2f;
            float jambX = half + CasingWidth / 2f;

            // due facce: davanti (+Z, rotazione 0) e dietro (-Z, rotazione 180)
            for (int side = 0; side < 2; side++)
            {
                float yaw = side == 0 ? 0f : 180f;
                float z = side == 0 ? t : -t;
                Quaternion face = Quaternion.Euler(0f, yaw, 0f);
                for (int sx = -1; sx <= 1; sx += 2)
                {
                    Vector3 basePos = new Vector3(sx * jambX, 0f, z);
                    // zoccolo, montante (scalato in altezza) e blocco d'angolo con rosetta
                    AddPiece(mb, "Yard_DoorPlinth", basePos, face, Vector3.one, "WoodDeep", new Vector3(0.2f, PlinthHeight, 0.065f));
                    float jambLength = Mathf.Max(0.05f, height - PlinthHeight);
                    AddPiece(mb, "Yard_DoorCasing", new Vector3(sx * jambX, PlinthHeight, z), face, new Vector3(1f, jambLength, 1f), "WoodDark", new Vector3(CasingWidth, 1f, 0.05f));
                    AddPiece(mb, "Yard_DoorCorner", new Vector3(sx * jambX, height + CornerSize / 2f, z), face, Vector3.one, "WoodDeep", new Vector3(CornerSize, CornerSize, 0.07f), true);
                }
                // architrave: la stessa tavola dei montanti, girata in orizzontale e lunga quanto l'apertura
                float startX = side == 0 ? -half : half;
                Quaternion lintelRot = Quaternion.Euler(0f, yaw, -90f);
                AddPiece(mb, "Yard_DoorCasing", new Vector3(startX, height + CasingWidth / 2f + 0.02f, z), lintelRot, new Vector3(1f, width, 1f), null, Vector3.zero);
                // chiave di volta al centro
                AddPiece(mb, "Yard_DoorKeystone", new Vector3(0f, height + 0.06f, z), face, Vector3.one, "WoodDark", new Vector3(0.22f, 0.26f, 0.09f), false, new Vector3(0f, 0.07f, 0f));
            }
            if (!ImportedMesh.Exists("Yard_DoorCasing"))
            {
                // riserva per gli architravi (il modulo ruotato non ha una forma di riserva semplice)
                mb.Box("WoodDark", new Vector3(0f, height + CasingWidth / 2f + 0.02f, t + 0.025f), new Vector3(width, CasingWidth, 0.05f), 0.01f);
                mb.Box("WoodDark", new Vector3(0f, height + CasingWidth / 2f + 0.02f, -t - 0.025f), new Vector3(width, CasingWidth, 0.05f), 0.01f);
            }

            // rivestimento interno dell'apertura (stipiti e cielino) e soglia
            mb.Box("WoodMid", new Vector3(-half + 0.015f, height / 2f, 0f), new Vector3(0.03f, height, wallThickness + 0.02f), 0.008f);
            mb.Box("WoodMid", new Vector3(half - 0.015f, height / 2f, 0f), new Vector3(0.03f, height, wallThickness + 0.02f), 0.008f);
            mb.Box("WoodMid", new Vector3(0f, height - 0.015f, 0f), new Vector3(width, 0.03f, wallThickness + 0.02f), 0.008f);
            mb.Box("WoodDark", new Vector3(0f, 0.012f, 0f), new Vector3(width + 0.1f, 0.024f, wallThickness + 0.12f), 0.008f);

            ModelKit.Part(root, "Mesh", mb);
            return root.gameObject;
        }

        /// <summary>Aggiunge un modulo di Blender; se manca, una scatola semplice con le misure date (base in origine).</summary>
        private static void AddPiece(MeshBuilder mb, string model, Vector3 pos, Quaternion rot, Vector3 scale, string fallbackMaterial, Vector3 fallbackSize,
                                     bool centered = false, Vector3 fallbackOffset = default(Vector3))
        {
            if (mb.Import(model, Matrix4x4.TRS(pos, rot, scale)) || fallbackMaterial == null) return;
            // la scatola sporge verso +Z dal piano del muro, come i moduli veri
            Vector3 local = fallbackOffset + new Vector3(0f, centered ? 0f : fallbackSize.y / 2f, fallbackSize.z / 2f);
            Vector3 center = pos + rot * Vector3.Scale(local, scale);
            mb.Box(fallbackMaterial, center, Vector3.Scale(fallbackSize, scale), 0.01f, rot.eulerAngles);
        }

        // ================================================================== decori
        /// <summary>Cassetta di legno piena di carote (circa 0,55 x 0,38 m).</summary>
        public static GameObject BuildCarrotCrate(Transform parent)
        {
            ModelKit.Begin("Yard_CarrotCrate");
            Transform root = ModelKit.Empty(parent, "CarrotCrate", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Yard_CarrotCrate", Vector3.zero))
            {
                mb.Box("WoodLight", new Vector3(0f, 0.14f, 0f), new Vector3(0.55f, 0.28f, 0.38f), 0.01f);
                for (int i = 0; i < 5; i++)
                    mb.Cone("Carrot", new Vector3(-0.2f + i * 0.1f, 0.28f, 0f), 0.03f, 0.18f, new Vector3(180f, 0f, 0f), 10);
            }
            ModelKit.Part(root, "Mesh", mb);
            ModelKit.AddBoxCollider(root.gameObject, new Vector3(0f, 0.15f, 0f), new Vector3(0.56f, 0.3f, 0.4f));
            return root.gameObject;
        }

        /// <summary>Balla di fieno tonda legata con lo spago (asse lungo X, circa 0,7 x 0,84 m).</summary>
        public static GameObject BuildHayBale(Transform parent)
        {
            ModelKit.Begin("Yard_HayBale");
            Transform root = ModelKit.Empty(parent, "HayBale", Vector3.zero);
            MeshBuilder mb = new MeshBuilder();
            if (!mb.Import("Yard_HayBale", Vector3.zero))
                mb.CylC("Hay", new Vector3(0f, 0.42f, 0f), 0.42f, 0.7f, 0.08f, new Vector3(0f, 0f, 90f));
            ModelKit.Part(root, "Mesh", mb);
            ModelKit.AddBoxCollider(root.gameObject, new Vector3(0f, 0.42f, 0f), new Vector3(0.72f, 0.84f, 0.84f));
            return root.gameObject;
        }
    }
}
