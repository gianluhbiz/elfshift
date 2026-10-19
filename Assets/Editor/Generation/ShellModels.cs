using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Il "guscio" del grande laboratorio: pavimento, pareti con finestre e porta, zoccolo in legno, soffitto a travi,
    /// banchina con staccionata, neve all'esterno. Le dimensioni sono costanti pubbliche perché la scena le usa
    /// per posizionare tutto il resto.
    /// </summary>
    public static class ShellModels
    {
        public const float HalfWidth = 14f;    // interno: x da -14 a +14
        public const float HalfDepth = 10f;    // interno: z da -10 a +10
        public const float WallHeight = 5.0f;
        public const float WallThickness = 0.4f;
        public const float DoorHalfWidth = 2.5f;   // apertura sulla parete destra: z da -2.5 a +2.5
        public const float DoorHeight = 3.6f;
        public const float DockLength = 8f;        // la banchina va da x=14 a x=22
        public const float DockHalfDepth = 5.5f;
        public static readonly float[] WindowCenters = { -10.5f, -5f, 0.5f, 6f };
        public const float WindowWidth = 2.6f;
        public const float WindowBottom = 1.4f;
        public const float WindowHeight = 2.4f;
        private const float WallUv = 0.4f; // la carta da parati si ripete ogni 2,5 m

        public static GameObject Build(Transform parent, Material floorMaterial, Material dockMaterial, Material wallMaterial)
        {
            ModelKit.Begin("Shell");
            Transform root = ModelKit.Empty(parent, "Workshop_Shell", Vector3.zero);

            // ---- Pavimento e banchina ------------------------------------------------------
            MeshBuilder floor = new MeshBuilder();
            floor.HardBox(MeshBuilder.Xf(new Vector3(0f, -0.2f, 0f)), new Vector3(HalfWidth * 2f + 0.8f, 0.4f, HalfDepth * 2f + 0.8f), floor.Sub("Floor"), 0.5f);
            ModelKit.PartWithMaterial(root, "Floor", floor, floorMaterial);

            MeshBuilder dock = new MeshBuilder();
            dock.HardBox(MeshBuilder.Xf(new Vector3(HalfWidth + DockLength / 2f + 0.2f, -0.2f, 0f)), new Vector3(DockLength + 0.4f, 0.4f, DockHalfDepth * 2f), dock.Sub("Dock"), 0.5f);
            ModelKit.PartWithMaterial(root, "Dock", dock, dockMaterial);

            // ---- Terreno innevato esterno -----------------------------------------------------
            MeshBuilder ground = new MeshBuilder();
            ground.HardBox(MeshBuilder.Xf(new Vector3(8f, -0.3f, 0f)), new Vector3(170f, 0.5f, 150f), ground.Sub("SnowGround"), 0f);
            ModelKit.Part(root, "SnowGround", ground);

            // ---- Pareti (carta da parati a righe) ---------------------------------------------
            MeshBuilder walls = new MeshBuilder();
            int paper = walls.Sub("Wallpaper");
            float backZ = HalfDepth + WallThickness / 2f;
            float wallLen = HalfWidth * 2f + WallThickness * 2f;
            AddWindowWall(walls, paper, backZ, wallLen);
            AddWindowWall(walls, paper, -backZ, wallLen);
            // parete sinistra intera
            float leftX = -HalfWidth - WallThickness / 2f;
            walls.HardBox(MeshBuilder.Xf(new Vector3(leftX, WallHeight / 2f, 0f)), new Vector3(WallThickness, WallHeight, HalfDepth * 2f), paper, WallUv);
            // parete destra con porta larga verso la banchina
            float rightX = HalfWidth + WallThickness / 2f;
            float segLen = HalfDepth + WallThickness - DoorHalfWidth;
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, WallHeight / 2f, DoorHalfWidth + segLen / 2f)), new Vector3(WallThickness, WallHeight, segLen), paper, WallUv);
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, WallHeight / 2f, -(DoorHalfWidth + segLen / 2f))), new Vector3(WallThickness, WallHeight, segLen), paper, WallUv);
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, (DoorHeight + WallHeight) / 2f, 0f)), new Vector3(WallThickness, WallHeight - DoorHeight, DoorHalfWidth * 2f), paper, WallUv);
            ModelKit.PartWithMaterial(root, "Walls", walls, wallMaterial);

            // ---- Zoccolo e rifiniture in legno, travi e cappelli di neve ----------------------------
            MeshBuilder wood = new MeshBuilder();
            float inner = 0.06f;
            // zoccolo + cimasa su pareti di fondo, frontale e sinistra
            wood.Box("WoodMid", new Vector3(0f, 0.6f, HalfDepth - inner / 2f + 0.01f), new Vector3(HalfWidth * 2f, 1.2f, inner), 0.015f);
            wood.Box("WoodDark", new Vector3(0f, 1.22f, HalfDepth - 0.05f), new Vector3(HalfWidth * 2f, 0.08f, 0.1f), 0.02f);
            wood.Box("WoodMid", new Vector3(0f, 0.6f, -HalfDepth + inner / 2f - 0.01f), new Vector3(HalfWidth * 2f, 1.2f, inner), 0.015f);
            wood.Box("WoodDark", new Vector3(0f, 1.22f, -HalfDepth + 0.05f), new Vector3(HalfWidth * 2f, 0.08f, 0.1f), 0.02f);
            wood.Box("WoodMid", new Vector3(-HalfWidth + inner / 2f - 0.01f, 0.6f, 0f), new Vector3(inner, 1.2f, HalfDepth * 2f), 0.015f);
            wood.Box("WoodDark", new Vector3(-HalfWidth + 0.05f, 1.22f, 0f), new Vector3(0.1f, 0.08f, HalfDepth * 2f), 0.02f);
            // zoccolo sul lato destro (interrotto dalla porta)
            float rightSeg = HalfDepth - DoorHalfWidth;
            for (int s = -1; s <= 1; s += 2)
            {
                float cz = s * (DoorHalfWidth + rightSeg / 2f);
                wood.Box("WoodMid", new Vector3(HalfWidth - inner / 2f + 0.01f, 0.6f, cz), new Vector3(inner, 1.2f, rightSeg), 0.015f);
                wood.Box("WoodDark", new Vector3(HalfWidth - 0.05f, 1.22f, cz), new Vector3(0.1f, 0.08f, rightSeg), 0.02f);
            }
            // travi verticali agli angoli, lungo le pareti e ai lati della porta
            float[][] posts =
            {
                new[] { -HalfWidth + 0.15f, HalfDepth - 0.15f }, new[] { HalfWidth - 0.15f, HalfDepth - 0.15f },
                new[] { HalfWidth - 0.15f, -HalfDepth + 0.15f }, new[] { -HalfWidth + 0.15f, -HalfDepth + 0.15f },
                new[] { HalfWidth - 0.15f, DoorHalfWidth + 0.1f }, new[] { HalfWidth - 0.15f, -DoorHalfWidth - 0.1f },
                new[] { -HalfWidth + 0.15f, 4.25f }, new[] { -HalfWidth + 0.15f, 0.75f }, new[] { -HalfWidth + 0.15f, -3.0f }, new[] { -HalfWidth + 0.15f, -8.2f },
                new[] { -2.7f, HalfDepth - 0.15f }, new[] { 3.3f, HalfDepth - 0.15f }, new[] { -7.8f, HalfDepth - 0.15f },
                new[] { -2.7f, -HalfDepth + 0.15f }, new[] { 3.3f, -HalfDepth + 0.15f }, new[] { -7.8f, -HalfDepth + 0.15f },
            };
            for (int i = 0; i < posts.Length; i++)
                wood.Box("WoodDark", new Vector3(posts[i][0], WallHeight / 2f, posts[i][1]), new Vector3(0.3f, WallHeight, 0.3f), 0.03f);
            // trave sopra la porta
            wood.Box("WoodDark", new Vector3(HalfWidth - 0.1f, DoorHeight - 0.05f, 0f), new Vector3(0.28f, 0.24f, DoorHalfWidth * 2f + 0.5f), 0.03f);
            // cornicione in alto con neve
            wood.Box("WoodMid", new Vector3(0f, WallHeight + 0.06f, backZ), new Vector3(wallLen + 0.3f, 0.14f, WallThickness + 0.2f), 0.03f);
            wood.Box("WoodMid", new Vector3(0f, WallHeight + 0.06f, -backZ), new Vector3(wallLen + 0.3f, 0.14f, WallThickness + 0.2f), 0.03f);
            wood.Box("WoodMid", new Vector3(leftX, WallHeight + 0.06f, 0f), new Vector3(WallThickness + 0.2f, 0.14f, HalfDepth * 2f + 0.3f), 0.03f);
            wood.Box("WoodMid", new Vector3(rightX, WallHeight + 0.06f, 0f), new Vector3(WallThickness + 0.2f, 0.14f, HalfDepth * 2f + 0.3f), 0.03f);
            for (int i = 0; i < 14; i++)
            {
                float x = -HalfWidth + 1f + i * 2.1f;
                wood.Ball("Snow", new Vector3(x, WallHeight + 0.16f, backZ), 0.7f, new Vector3(1.5f, 0.2f, 0.45f), Vector3.zero, 12);
                wood.Ball("Snow", new Vector3(x, WallHeight + 0.16f, -backZ), 0.7f, new Vector3(1.5f, 0.2f, 0.45f), Vector3.zero, 12);
            }
            for (int i = 0; i < 9; i++)
            {
                float z = -HalfDepth + 1f + i * 2.4f;
                wood.Ball("Snow", new Vector3(leftX, WallHeight + 0.16f, z), 0.7f, new Vector3(0.45f, 0.2f, 1.7f), Vector3.zero, 12);
                if (Mathf.Abs(z) > DoorHalfWidth + 0.4f)
                    wood.Ball("Snow", new Vector3(rightX, WallHeight + 0.16f, z), 0.7f, new Vector3(0.45f, 0.2f, 1.7f), Vector3.zero, 12);
            }
            ModelKit.Part(root, "Trims", wood);

            // ---- Soffitto: intonaco con travi di legno (non proietta ombre, cosi' la luce principale entra comunque) ----
            MeshBuilder ceiling = new MeshBuilder();
            ceiling.HardBox(MeshBuilder.Xf(new Vector3(0f, WallHeight + 0.15f, 0f)), new Vector3(wallLen + 0.2f, 0.3f, HalfDepth * 2f + WallThickness * 2f + 0.2f), ceiling.Sub("PlasterDark"), 0f);
            GameObject ceilingObject = ModelKit.Part(root, "Ceiling", ceiling);
            MeshRenderer ceilingRenderer = ceilingObject.GetComponent<MeshRenderer>();
            if (ceilingRenderer != null) ceilingRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MeshBuilder beams = new MeshBuilder();
            float[] beamZ = { -7.5f, -2.5f, 2.5f, 7.5f };
            for (int i = 0; i < beamZ.Length; i++)
                beams.Box("WoodDark", new Vector3(0f, WallHeight - 0.15f, beamZ[i]), new Vector3(HalfWidth * 2f, 0.3f, 0.4f), 0.04f);
            for (int i = 0; i < 8; i++)
                beams.Box("WoodDark", new Vector3(-12.25f + i * 3.5f, WallHeight - 0.11f, 0f), new Vector3(0.34f, 0.22f, HalfDepth * 2f), 0.04f);
            GameObject beamObject = ModelKit.Part(root, "CeilingBeams", beams);
            MeshRenderer beamRenderer = beamObject.GetComponent<MeshRenderer>();
            if (beamRenderer != null) beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // ---- Mucchi di neve fuori dalle pareti e lungo la banchina -----------------------------
            MeshBuilder drifts = new MeshBuilder();
            for (int i = 0; i < 16; i++)
                drifts.Ball("Snow", new Vector3(-16f + i * 2.9f + ((i % 2) * 0.6f), 0f, HalfDepth + 1.5f + (i % 3) * 0.6f), 1f, new Vector3(1.8f + (i % 3) * 0.4f, 0.55f, 1.2f), Vector3.zero, 12);
            for (int i = 0; i < 16; i++)
                drifts.Ball("Snow", new Vector3(-16f + i * 2.9f + ((i % 2) * 0.5f), 0f, -HalfDepth - 1.5f - (i % 3) * 0.6f), 1f, new Vector3(1.8f + (i % 3) * 0.4f, 0.55f, 1.2f), Vector3.zero, 12);
            for (int i = 0; i < 9; i++)
                drifts.Ball("Snow", new Vector3(-HalfWidth - 1.3f - (i % 2) * 0.6f, 0f, -9f + i * 2.2f), 1f, new Vector3(1.0f, 0.5f, 1.5f), Vector3.zero, 12);
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? 1f : -1f;
                drifts.Ball("Snow", new Vector3(HalfWidth + 1.8f + (i % 3) * 2.6f, 0f, side * (DockHalfDepth + 1.2f + (i % 2) * 0.5f)), 1f, new Vector3(1.6f, 0.5f, 1.1f), Vector3.zero, 12);
            }
            drifts.Ball("Snow", new Vector3(HalfWidth + DockLength + 1.6f, 0f, 0f), 1f, new Vector3(1.4f, 0.55f, 3.4f), Vector3.zero, 14);
            ModelKit.Part(root, "SnowDrifts", drifts);

            // ---- Collisioni (invisibili) -------------------------------------------------------------
            GameObject colliders = new GameObject("Colliders");
            colliders.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(colliders, new Vector3(0f, -0.5f, 0f), new Vector3(120f, 1f, 110f)); // pavimento/terreno
            AddWall(colliders.transform, new Vector3(0f, WallHeight / 2f, backZ), new Vector3(wallLen, WallHeight, WallThickness));
            AddWall(colliders.transform, new Vector3(0f, WallHeight / 2f, -backZ), new Vector3(wallLen, WallHeight, WallThickness));
            AddWall(colliders.transform, new Vector3(leftX, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, HalfDepth * 2f + 1f));
            AddWall(colliders.transform, new Vector3(rightX, WallHeight / 2f, DoorHalfWidth + segLen / 2f), new Vector3(WallThickness, WallHeight, segLen));
            AddWall(colliders.transform, new Vector3(rightX, WallHeight / 2f, -(DoorHalfWidth + segLen / 2f)), new Vector3(WallThickness, WallHeight, segLen));
            // recinto della banchina
            float dockCenterX = HalfWidth + DockLength / 2f + 0.2f;
            AddWall(colliders.transform, new Vector3(dockCenterX, 0.6f, DockHalfDepth), new Vector3(DockLength + 0.4f, 1.2f, 0.25f));
            AddWall(colliders.transform, new Vector3(dockCenterX, 0.6f, -DockHalfDepth), new Vector3(DockLength + 0.4f, 1.2f, 0.25f));
            AddWall(colliders.transform, new Vector3(HalfWidth + DockLength + 0.3f, 0.6f, 0f), new Vector3(0.25f, 1.2f, DockHalfDepth * 2f));
            return root.gameObject;
        }

        /// <summary>Parete lunga con aperture per le finestre (z = centro della parete).</summary>
        private static void AddWindowWall(MeshBuilder walls, int paper, float z, float wallLen)
        {
            walls.HardBox(MeshBuilder.Xf(new Vector3(0f, WindowBottom / 2f, z)), new Vector3(wallLen, WindowBottom, WallThickness), paper, WallUv);
            float topStart = WindowBottom + WindowHeight;
            walls.HardBox(MeshBuilder.Xf(new Vector3(0f, (topStart + WallHeight) / 2f, z)), new Vector3(wallLen, WallHeight - topStart, WallThickness), paper, WallUv);
            float pillarY = WindowBottom + WindowHeight / 2f;
            float prevEdge = -HalfWidth - WallThickness;
            for (int i = 0; i <= WindowCenters.Length; i++)
            {
                float nextEdge = i < WindowCenters.Length ? WindowCenters[i] - WindowWidth / 2f : HalfWidth + WallThickness;
                float width = nextEdge - prevEdge;
                if (width > 0.01f)
                    walls.HardBox(MeshBuilder.Xf(new Vector3((prevEdge + nextEdge) / 2f, pillarY, z)), new Vector3(width, WindowHeight, WallThickness), paper, WallUv);
                if (i < WindowCenters.Length) prevEdge = WindowCenters[i] + WindowWidth / 2f;
            }
        }

        private static void AddWall(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject("WallCollider");
            go.transform.SetParent(parent, false);
            ModelKit.AddBoxCollider(go, center, size);
        }
    }
}
