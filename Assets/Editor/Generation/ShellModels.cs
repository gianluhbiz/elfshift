using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// La "Bottega degli Elfi": un edificio raccolto e accogliente (ispirato agli uffici postali "cozy"),
    /// con il LABORATORIO al centro, il MAGAZZINO a ovest (si entra da un arco) e la porta d'ingresso a sud
    /// che dà sul CORTILE innevato, dove aspetta la slitta.
    ///   Laboratorio: x da -8 a 8, z da -6 a 6, soffitto a 4,2 m.
    ///   Magazzino:   x da -14 a -8, z da -2 a 6, soffitto a 3,6 m.
    /// Le pareti sono descritte come segmenti con aperture (porte, arco, finestre): il codice costruisce da solo
    /// muri, carta da parati, rivestimento esterno in legno, boiserie, pilastri, travi, tetto e collisioni.
    /// </summary>
    public static class ShellModels
    {
        // ---- misure principali (metri) --------------------------------------------------------
        public const float MainMinX = -8f, MainMaxX = 8f, MainMinZ = -6f, MainMaxZ = 6f;
        public const float AnnexMinX = -14f, AnnexMaxX = -8f, AnnexMinZ = -2f, AnnexMaxZ = 6f;
        public const float WallHeight = 4.2f;
        public const float AnnexHeight = 3.6f;
        public const float WallThickness = 0.3f;

        /// <summary>Porta d'ingresso sulla parete sud (verso il cortile).</summary>
        public const float DoorCenterX = 3.5f, DoorWidth = 3.0f, DoorHeight = 3.0f;
        /// <summary>Arco tra laboratorio e magazzino (parete x = -8).</summary>
        public const float ArchCenterZ = 2.2f, ArchWidth = 3.2f, ArchHeight = 3.0f;

        public const float WindowBottom = 1.25f;
        public const float WindowHeight = 2.0f;
        private const float WallUv = 0.4f; // la carta da parati si ripete ogni 2,5 m

        /// <summary>Un'apertura in una parete: "Center" è la distanza dall'inizio del segmento.</summary>
        public struct Opening
        {
            public float Center, Width, Bottom, Top;
            public bool IsWindow;
            public Opening(float center, float width, float bottom, float top, bool window)
            {
                Center = center; Width = width; Bottom = bottom; Top = top; IsWindow = window;
            }
        }

        /// <summary>Una parete: da "From" a "To" (sul filo interno), con l'interno verso "Inward".</summary>
        public sealed class WallSpec
        {
            public string Name;
            public Vector3 From, To;
            public Vector3 Inward;
            public float Height;
            /// <summary>Falso = parete divisoria tra due stanze (carta da parati e boiserie su entrambi i lati).</summary>
            public bool Interior = true;
            public readonly List<Opening> Openings = new List<Opening>();
            public float Length => Vector3.Distance(From, To);
            public Vector3 Dir => (To - From).normalized;
            /// <summary>Rotazione (gradi attorno a Y) di un oggetto appoggiato alla parete che guarda dentro la stanza.</summary>
            public float FacingYaw => Mathf.Atan2(Inward.x, Inward.z) * Mathf.Rad2Deg;
            public Vector3 PointAt(float distance) { return From + Dir * distance; }
        }

        /// <summary>Le pareti della bottega (servono anche al costruttore per appendere finestre e decorazioni).</summary>
        public static List<WallSpec> Walls()
        {
            List<WallSpec> walls = new List<WallSpec>();
            float wb = WindowBottom, wt = WindowBottom + WindowHeight;

            // --- laboratorio ---
            WallSpec north = W("North", new Vector3(MainMinX, 0f, MainMaxZ), new Vector3(MainMaxX, 0f, MainMaxZ), Vector3.back, WallHeight);
            north.Openings.Add(new Opening(6.5f, 2.0f, wb, wt, true));   // x = -1.5, tra i due banchi
            north.Openings.Add(new Opening(13.0f, 1.6f, wb, wt, true));  // x = 5.0
            walls.Add(north);

            WallSpec south = W("South", new Vector3(MainMinX, 0f, MainMinZ), new Vector3(MainMaxX, 0f, MainMinZ), Vector3.forward, WallHeight);
            south.Openings.Add(new Opening(3.6f, 1.8f, wb, wt, true));   // x = -4.4 (angolo del camino)
            south.Openings.Add(new Opening(DoorCenterX - MainMinX, DoorWidth, 0f, DoorHeight, false));
            south.Openings.Add(new Opening(14.6f, 1.4f, wb, wt, true));  // x = 6.6
            walls.Add(south);

            WallSpec east = W("East", new Vector3(MainMaxX, 0f, MainMinZ), new Vector3(MainMaxX, 0f, MainMaxZ), Vector3.left, WallHeight);
            east.Openings.Add(new Opening(2.4f, 1.8f, wb, wt, true));    // z = -3.6
            east.Openings.Add(new Opening(9.6f, 1.6f, wb, wt, true));    // z = 3.6
            walls.Add(east);

            // parete ovest del laboratorio: un tratto pieno (camino) e la divisoria con l'arco verso il magazzino
            walls.Add(W("WestLow", new Vector3(MainMinX, 0f, MainMinZ), new Vector3(MainMinX, 0f, AnnexMinZ), Vector3.right, WallHeight));
            WallSpec divider = W("Divider", new Vector3(MainMinX, 0f, AnnexMinZ), new Vector3(MainMinX, 0f, MainMaxZ), Vector3.right, WallHeight);
            divider.Interior = false;
            divider.Openings.Add(new Opening(ArchCenterZ - AnnexMinZ, ArchWidth, 0f, ArchHeight, false));
            walls.Add(divider);

            // --- magazzino ---
            walls.Add(W("AnnexNorth", new Vector3(AnnexMinX, 0f, AnnexMaxZ), new Vector3(AnnexMaxX, 0f, AnnexMaxZ), Vector3.back, AnnexHeight));
            WallSpec aWest = W("AnnexWest", new Vector3(AnnexMinX, 0f, AnnexMinZ), new Vector3(AnnexMinX, 0f, AnnexMaxZ), Vector3.right, AnnexHeight);
            aWest.Openings.Add(new Opening(6.6f, 1.2f, 1.6f, 2.8f, true)); // finestrella in alto
            walls.Add(aWest);
            walls.Add(W("AnnexSouth", new Vector3(AnnexMinX, 0f, AnnexMinZ), new Vector3(AnnexMaxX, 0f, AnnexMinZ), Vector3.forward, AnnexHeight));
            return walls;
        }

        private static WallSpec W(string name, Vector3 from, Vector3 to, Vector3 inward, float height)
        {
            WallSpec w = new WallSpec();
            w.Name = name;
            w.From = from;
            w.To = to;
            w.Inward = inward;
            w.Height = height;
            return w;
        }

        // ======================================================================================
        public static GameObject Build(Transform parent, Material floorMaterial, Material deckMaterial, Material wallMaterial)
        {
            ModelKit.Begin("Shell");
            Transform root = ModelKit.Empty(parent, "Workshop_Shell", Vector3.zero);
            List<WallSpec> walls = Walls();

            // ---- pavimenti ------------------------------------------------------------------
            MeshBuilder floor = new MeshBuilder();
            floor.HardBox(MeshBuilder.Xf(new Vector3(0f, -0.2f, 0f)), new Vector3(MainMaxX - MainMinX + 0.6f, 0.4f, MainMaxZ - MainMinZ + 0.6f), floor.Sub("Floor"), 0.5f);
            floor.HardBox(MeshBuilder.Xf(new Vector3((AnnexMinX + AnnexMaxX) / 2f - 0.15f, -0.2f, (AnnexMinZ + AnnexMaxZ) / 2f)),
                new Vector3(AnnexMaxX - AnnexMinX + 0.3f, 0.4f, AnnexMaxZ - AnnexMinZ + 0.6f), floor.Sub("Floor"), 0.5f);
            ModelKit.PartWithMaterial(root, "Floor", floor, floorMaterial);

            // veranda davanti alla porta e sentiero di assi fino alla slitta
            MeshBuilder deck = new MeshBuilder();
            deck.HardBox(MeshBuilder.Xf(new Vector3(DoorCenterX, -0.08f, MainMinZ - 1.6f)), new Vector3(5.2f, 0.2f, 3.0f), deck.Sub("Dock"), 0.5f);
            deck.HardBox(MeshBuilder.Xf(new Vector3(DoorCenterX, -0.11f, MainMinZ - 4.9f)), new Vector3(2.0f, 0.2f, 3.8f), deck.Sub("Dock"), 0.5f);
            ModelKit.PartWithMaterial(root, "Deck", deck, deckMaterial);

            // terreno innevato
            MeshBuilder ground = new MeshBuilder();
            ground.HardBox(MeshBuilder.Xf(new Vector3(0f, -0.3f, -10f)), new Vector3(170f, 0.5f, 150f), ground.Sub("SnowGround"), 0f);
            ModelKit.Part(root, "SnowGround", ground);

            // ---- pareti: carta da parati dentro, assi di legno fuori ---------------------------
            MeshBuilder paper = new MeshBuilder();
            MeshBuilder outside = new MeshBuilder();
            int paperSub = paper.Sub("Wallpaper");
            for (int i = 0; i < walls.Count; i++) AddWall(paper, paperSub, outside, walls[i]);
            ModelKit.PartWithMaterial(root, "Walls", paper, wallMaterial);
            ModelKit.Part(root, "Cladding", outside);

            // ---- boiserie, pilastri, architravi ----------------------------------------------------
            MeshBuilder wood = new MeshBuilder();
            bool fancyWood = ImportedMesh.Exists("Shell_Wainscot");
            for (int i = 0; i < walls.Count; i++)
            {
                AddWainscot(wood, walls[i], walls[i].Inward, fancyWood);
                if (!walls[i].Interior) AddWainscot(wood, walls[i], -walls[i].Inward, fancyWood);
            }
            AddPosts(wood);
            ModelKit.Part(root, "Trims", wood);

            // ---- soffitti con travi (non proiettano ombre) -----------------------------------------
            MeshBuilder ceiling = new MeshBuilder();
            ceiling.HardBox(MeshBuilder.Xf(new Vector3(0f, WallHeight + 0.1f, 0f)), new Vector3(MainMaxX - MainMinX + 0.6f, 0.2f, MainMaxZ - MainMinZ + 0.6f), ceiling.Sub("PlasterDark"), 0f);
            ceiling.HardBox(MeshBuilder.Xf(new Vector3((AnnexMinX + AnnexMaxX) / 2f, AnnexHeight + 0.1f, (AnnexMinZ + AnnexMaxZ) / 2f)), new Vector3(AnnexMaxX - AnnexMinX + 0.6f, 0.2f, AnnexMaxZ - AnnexMinZ + 0.6f), ceiling.Sub("PlasterDark"), 0f);
            NoShadows(ModelKit.Part(root, "Ceiling", ceiling));
            MeshBuilder beams = new MeshBuilder();
            AddBeams(beams);
            NoShadows(ModelKit.Part(root, "CeilingBeams", beams));

            // ---- tetto spiovente innevato (si vede dal cortile) -------------------------------------
            MeshBuilder roof = new MeshBuilder();
            AddRoof(roof);
            NoShadows(ModelKit.Part(root, "Roof", roof));

            // ---- cumuli di neve lungo i muri esterni -----------------------------------------------
            MeshBuilder drifts = new MeshBuilder();
            AddDrifts(drifts);
            ModelKit.Part(root, "SnowDrifts", drifts);

            // ---- collisioni (invisibili) -------------------------------------------------------------
            GameObject colliders = new GameObject("Colliders");
            colliders.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(colliders, new Vector3(0f, -0.5f, -10f), new Vector3(120f, 1f, 110f)); // pavimento/terreno
            for (int i = 0; i < walls.Count; i++) AddWallColliders(colliders.transform, walls[i]);
            return root.gameObject;
        }

        private static void NoShadows(GameObject go)
        {
            MeshRenderer r = go != null ? go.GetComponent<MeshRenderer>() : null;
            if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Rotazione (gradi) che porta l'asse X locale lungo la parete.</summary>
        private static float AlongYaw(WallSpec w)
        {
            Vector3 d = w.Dir;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg - 90f;
        }

        // ---------------------------------------------------------------------------------------
        /// <summary>Divide la parete in pezzi attorno alle aperture e li aggiunge (muro + rivestimento esterno).</summary>
        private static void AddWall(MeshBuilder paper, int paperSub, MeshBuilder outside, WallSpec w)
        {
            float t = WallThickness;
            // il muro si estende di uno spessore oltre gli estremi per chiudere gli angoli
            float end = w.Length + t;
            List<Opening> ops = new List<Opening>(w.Openings);
            ops.Sort((a, b) => a.Center.CompareTo(b.Center));
            float yaw = AlongYaw(w);

            float cursor = -t;
            for (int i = 0; i <= ops.Count; i++)
            {
                float next = i < ops.Count ? ops[i].Center - ops[i].Width / 2f : end;
                if (next - cursor > 0.01f) WallPiece(paper, paperSub, outside, w, (cursor + next) / 2f, next - cursor, 0f, w.Height, yaw);
                if (i < ops.Count)
                {
                    Opening o = ops[i];
                    if (o.Bottom > 0.01f) WallPiece(paper, paperSub, outside, w, o.Center, o.Width, 0f, o.Bottom, yaw);
                    if (o.Top < w.Height - 0.01f) WallPiece(paper, paperSub, outside, w, o.Center, o.Width, o.Top, w.Height, yaw);
                    cursor = o.Center + o.Width / 2f;
                }
            }
        }

        private static void WallPiece(MeshBuilder paper, int paperSub, MeshBuilder outside, WallSpec w, float along, float width, float y0, float y1, float yaw)
        {
            float t = WallThickness;
            Vector3 outward = -w.Inward;
            Vector3 c = w.PointAt(along) + outward * (t / 2f) + Vector3.up * ((y0 + y1) / 2f);
            paper.HardBox(MeshBuilder.Xf(c, new Vector3(0f, yaw, 0f)), new Vector3(width, y1 - y0, t), paperSub, WallUv);
            if (!w.Interior) return;

            // rivestimento esterno ad assi orizzontali (leggermente sporgente)
            Vector3 oc = w.PointAt(along) + outward * (t + 0.03f) + Vector3.up * ((y0 + y1) / 2f);
            outside.Box("WoodMid", oc, new Vector3(width + 0.02f, y1 - y0, 0.06f), 0f, new Vector3(0f, yaw, 0f));
            for (float y = y0 + 0.3f; y < y1 - 0.05f; y += 0.3f)
            {
                Vector3 bc = w.PointAt(along) + outward * (t + 0.065f) + Vector3.up * y;
                outside.Box("WoodDark", bc, new Vector3(width + 0.02f, 0.035f, 0.02f), 0f, new Vector3(0f, yaw, 0f));
            }
        }

        private static void AddWallColliders(Transform parent, WallSpec w)
        {
            // Solo porte e arco sono aperti: le finestre restano "piene" per il giocatore.
            float t = WallThickness;
            float yaw = AlongYaw(w);
            float cursor = -t, end = w.Length + t;
            List<Opening> doors = Doors(w);
            for (int i = 0; i <= doors.Count; i++)
            {
                float next = i < doors.Count ? doors[i].Center - doors[i].Width / 2f : end;
                if (next - cursor > 0.01f)
                {
                    GameObject go = new GameObject("WallCollider_" + w.Name);
                    go.transform.SetParent(parent, false);
                    go.transform.position = w.PointAt((cursor + next) / 2f) - w.Inward * (t / 2f);
                    go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    ModelKit.AddBoxCollider(go, new Vector3(0f, w.Height / 2f, 0f), new Vector3(next - cursor, w.Height, t));
                }
                if (i < doors.Count) cursor = doors[i].Center + doors[i].Width / 2f;
            }
        }

        private static List<Opening> Doors(WallSpec w)
        {
            List<Opening> doors = new List<Opening>();
            foreach (Opening o in w.Openings) if (!o.IsWindow) doors.Add(o);
            doors.Sort((a, b) => a.Center.CompareTo(b.Center));
            return doors;
        }

        // ---------------------------------------------------------------------------------------
        /// <summary>Boiserie lungo una faccia della parete, interrotta da porte e arco.</summary>
        private static void AddWainscot(MeshBuilder b, WallSpec w, Vector3 inward, bool fancy)
        {
            float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            // per la faccia "dall'altra parte" di una divisoria la superficie è spostata di uno spessore
            Vector3 shift = Vector3.Dot(inward, w.Inward) > 0f ? Vector3.zero : -w.Inward * WallThickness;
            float cursor = 0f, len = w.Length;
            List<Opening> doors = Doors(w);
            for (int i = 0; i <= doors.Count; i++)
            {
                float next = i < doors.Count ? doors[i].Center - doors[i].Width / 2f : len;
                if (next - cursor > 0.3f)
                {
                    Vector3 from = w.PointAt(cursor) + shift;
                    Vector3 to = w.PointAt(next) + shift;
                    if (fancy)
                    {
                        WainscotRun(b, from, to, yaw);
                    }
                    else
                    {
                        Vector3 mid = (from + to) / 2f + inward * 0.03f;
                        b.Box("WoodMid", mid + Vector3.up * 0.6f, new Vector3(Vector3.Distance(from, to), 1.2f, 0.06f), 0.015f, new Vector3(0f, yaw, 0f));
                        b.Box("WoodDark", mid + Vector3.up * 1.22f + inward * 0.02f, new Vector3(Vector3.Distance(from, to), 0.08f, 0.1f), 0.02f, new Vector3(0f, yaw, 0f));
                    }
                }
                if (i < doors.Count) cursor = doors[i].Center + doors[i].Width / 2f;
            }
        }

        /// <summary>Ripete il modulo di boiserie (largo 1 m) da "from" a "to", stirandolo un poco per riempire il tratto.</summary>
        private static void WainscotRun(MeshBuilder b, Vector3 from, Vector3 to, float yaw)
        {
            float length = Vector3.Distance(from, to);
            int count = Mathf.Max(1, Mathf.CeilToInt(length - 0.01f));
            float stretch = length / count;
            for (int i = 0; i < count; i++)
            {
                Vector3 pos = Vector3.Lerp(from, to, (i + 0.5f) / count);
                b.Import("Shell_Wainscot", MeshBuilder.Xf(pos, new Vector3(0f, yaw, 0f), new Vector3(stretch, 1f, 1f)));
            }
        }

        /// <summary>Pilastri di legno con saette: agli angoli, lungo le pareti e ai lati di porta e arco.</summary>
        private static void AddPosts(MeshBuilder b)
        {
            bool fancy = ImportedMesh.Exists("Shell_Post");
            float i0 = 0.15f;
            // (x, z, direzione in cui guarda il pilastro: le saette vanno lungo la parete)
            float[][] posts =
            {
                new[] { MainMinX + i0, MainMaxZ - i0, 180f }, new[] { MainMaxX - i0, MainMaxZ - i0, 180f },
                new[] { MainMaxX - i0, MainMinZ + i0, 0f }, new[] { MainMinX + i0, MainMinZ + i0, 0f },
                new[] { -3.0f, MainMaxZ - i0, 180f }, new[] { 3.6f, MainMaxZ - i0, 180f },
                new[] { DoorCenterX - DoorWidth / 2f - 0.25f, MainMinZ + i0, 0f }, new[] { DoorCenterX + DoorWidth / 2f + 0.25f, MainMinZ + i0, 0f },
                new[] { -2.0f, MainMinZ + i0, 0f },
                new[] { MainMaxX - i0, -1.2f, -90f },
                new[] { MainMinX + i0, ArchCenterZ - ArchWidth / 2f - 0.25f, 90f }, new[] { MainMinX + i0, ArchCenterZ + ArchWidth / 2f + 0.25f, 90f },
            };
            for (int i = 0; i < posts.Length; i++)
            {
                Vector3 p = new Vector3(posts[i][0], 0f, posts[i][1]);
                // il modello del pilastro è alto 5 m: lo accorcio all'altezza della stanza
                if (fancy) b.Import("Shell_Post", MeshBuilder.Xf(p, new Vector3(0f, posts[i][2], 0f), new Vector3(1f, WallHeight / 5f, 1f)));
                else b.Box("WoodDark", p + Vector3.up * (WallHeight / 2f), new Vector3(0.3f, WallHeight, 0.3f), 0.03f);
            }
            // architravi sopra la porta e l'arco
            b.Box("WoodDark", new Vector3(DoorCenterX, DoorHeight + 0.12f, MainMinZ - WallThickness / 2f), new Vector3(DoorWidth + 0.6f, 0.24f, WallThickness + 0.12f), 0.03f);
            b.Box("WoodDark", new Vector3(MainMinX - WallThickness / 2f, ArchHeight + 0.12f, ArchCenterZ), new Vector3(WallThickness + 0.12f, 0.24f, ArchWidth + 0.6f), 0.03f);
            // cornicione esterno in cima ai muri lunghi
            b.Box("WoodMid", new Vector3(0f, WallHeight + 0.05f, MainMaxZ + 0.25f), new Vector3(MainMaxX - MainMinX + 0.9f, 0.14f, 0.5f), 0.03f);
            b.Box("WoodMid", new Vector3(0f, WallHeight + 0.05f, MainMinZ - 0.25f), new Vector3(MainMaxX - MainMinX + 0.9f, 0.14f, 0.5f), 0.03f);
        }

        private static void AddBeams(MeshBuilder b)
        {
            bool fancy = ImportedMesh.Exists("Shell_Beam");
            // laboratorio: 3 travi maestre lungo X e travetti lungo Z
            float[] beamZ = { -3f, 0f, 3f };
            for (int i = 0; i < beamZ.Length; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    Vector3 p = new Vector3(MainMinX + 2f + k * 4f, WallHeight - 0.15f, beamZ[i]);
                    if (fancy) b.Import("Shell_Beam", MeshBuilder.Xf(p, new Vector3(((i + k) % 3 - 1) * 0.5f, (k % 2) * 180f, 0f), Vector3.one));
                    else b.Box("WoodDark", p, new Vector3(4f, 0.3f, 0.4f), 0.04f);
                }
            }
            for (int i = 0; i < 6; i++)
            {
                float x = MainMinX + 1.4f + i * 2.64f;
                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = new Vector3(x, WallHeight - 0.1f, MainMinZ + 2f + k * 4f);
                    if (fancy) b.Import("Shell_Beam", MeshBuilder.Xf(p, new Vector3(0f, 90f + (k % 2) * 180f, 0f), new Vector3(1f, 0.66f, 0.7f)));
                    else b.Box("WoodDark", p, new Vector3(0.28f, 0.2f, 4f), 0.04f);
                }
            }
            // magazzino: travi più basse
            for (int k = 0; k < 3; k++)
            {
                float z = AnnexMinZ + 1.4f + k * 2.6f;
                Vector3 p = new Vector3((AnnexMinX + AnnexMaxX) / 2f, AnnexHeight - 0.14f, z);
                if (fancy) b.Import("Shell_Beam", MeshBuilder.Xf(p, Vector3.zero, new Vector3(1.55f, 0.9f, 0.85f)));
                else b.Box("WoodDark", p, new Vector3(6.2f, 0.27f, 0.34f), 0.04f);
            }
        }

        /// <summary>Tetto a due falde sopra il laboratorio e a una falda sopra il magazzino, coperti di neve.</summary>
        private static void AddRoof(MeshBuilder b)
        {
            float over = 0.7f;
            float halfDepth = (MainMaxZ - MainMinZ) / 2f + over;
            float rise = 2.6f;
            float slope = Mathf.Atan2(rise, halfDepth) * Mathf.Rad2Deg;
            float slopeLen = Mathf.Sqrt(halfDepth * halfDepth + rise * rise);
            float width = MainMaxX - MainMinX + over * 2f;
            float baseY = WallHeight + 0.25f;
            for (int s = -1; s <= 1; s += 2)
            {
                // falda: il centro sta a metà strada tra la gronda (z = s*halfDepth) e il colmo (z = 0)
                Vector3 c = new Vector3(0f, baseY + rise / 2f, s * halfDepth / 2f);
                Vector3 e = new Vector3(s * slope, 0f, 0f);
                b.Box("WoodDeep", c, new Vector3(width, 0.16f, slopeLen), 0.03f, e);
                b.Box("Snow", c + Vector3.up * 0.15f, new Vector3(width + 0.1f, 0.2f, slopeLen + 0.05f), 0.09f, e);
            }
            // timpani di legno alle due estremità (triangoli)
            List<Vector2> gable = new List<Vector2> { new Vector2(-(MainMaxZ - MainMinZ) / 2f, 0f), new Vector2((MainMaxZ - MainMinZ) / 2f, 0f), new Vector2(0f, rise * (halfDepth - over) / halfDepth + 0.25f) };
            b.Prism(MeshBuilder.Xf(new Vector3(MainMinX - 0.1f, WallHeight, 0f), new Vector3(0f, -90f, 0f)), gable, 0.2f, b.Sub("WoodMid"));
            b.Prism(MeshBuilder.Xf(new Vector3(MainMaxX + 0.1f, WallHeight, 0f), new Vector3(0f, -90f, 0f)), gable, 0.2f, b.Sub("WoodMid"));
            // comignolo di mattoni sopra il camino
            b.Box("Brick", new Vector3(MainMinX + 0.9f, WallHeight + 2.4f, -3.8f), new Vector3(0.9f, 2.6f, 0.9f), 0.04f);
            b.Box("Snow", new Vector3(MainMinX + 0.9f, WallHeight + 3.75f, -3.8f), new Vector3(1.05f, 0.14f, 1.05f), 0.06f);

            // magazzino: falda unica che scende verso ovest
            float aw = AnnexMaxX - AnnexMinX + over;
            float ad = AnnexMaxZ - AnnexMinZ + over * 2f;
            float aRise = 0.9f;
            float aSlope = Mathf.Atan2(aRise, aw) * Mathf.Rad2Deg;
            float aLen = Mathf.Sqrt(aw * aw + aRise * aRise);
            Vector3 ac = new Vector3((AnnexMinX + AnnexMaxX) / 2f - over / 2f, AnnexHeight + 0.25f + aRise / 2f, (AnnexMinZ + AnnexMaxZ) / 2f);
            b.Box("WoodDeep", ac, new Vector3(aLen, 0.16f, ad), 0.03f, new Vector3(0f, 0f, aSlope));
            b.Box("Snow", ac + Vector3.up * 0.15f, new Vector3(aLen + 0.05f, 0.2f, ad + 0.1f), 0.09f, new Vector3(0f, 0f, aSlope));
        }

        private static void AddDrifts(MeshBuilder b)
        {
            System.Random rng = new System.Random(5);
            for (float x = MainMinX + 0.5f; x < MainMaxX; x += 1.9f)
            {
                b.Ball("Snow", new Vector3(x, 0f, MainMaxZ + 0.9f + (float)rng.NextDouble() * 0.4f), 1f, new Vector3(1.3f, 0.45f, 0.9f), Vector3.zero, 12);
                if (Mathf.Abs(x - DoorCenterX) > 3.4f)
                    b.Ball("Snow", new Vector3(x, 0f, MainMinZ - 0.9f - (float)rng.NextDouble() * 0.4f), 1f, new Vector3(1.3f, 0.42f, 0.9f), Vector3.zero, 12);
            }
            for (float z = MainMinZ + 0.6f; z < MainMaxZ; z += 1.9f)
                b.Ball("Snow", new Vector3(MainMaxX + 0.9f + (float)rng.NextDouble() * 0.4f, 0f, z), 1f, new Vector3(0.9f, 0.45f, 1.3f), Vector3.zero, 12);
            for (float z = AnnexMinZ + 0.6f; z < AnnexMaxZ; z += 1.9f)
                b.Ball("Snow", new Vector3(AnnexMinX - 0.9f, 0f, z), 1f, new Vector3(0.9f, 0.45f, 1.3f), Vector3.zero, 12);
            for (float x = AnnexMinX + 0.5f; x < AnnexMaxX; x += 1.9f)
            {
                b.Ball("Snow", new Vector3(x, 0f, AnnexMaxZ + 0.9f), 1f, new Vector3(1.3f, 0.42f, 0.9f), Vector3.zero, 12);
                b.Ball("Snow", new Vector3(x, 0f, AnnexMinZ - 0.9f), 1f, new Vector3(1.3f, 0.42f, 0.9f), Vector3.zero, 12);
            }
            for (float z = MainMinZ + 0.6f; z < AnnexMinZ - 0.5f; z += 1.9f)
                b.Ball("Snow", new Vector3(MainMinX - 0.9f, 0f, z), 1f, new Vector3(0.9f, 0.45f, 1.3f), Vector3.zero, 12);
        }
    }
}
