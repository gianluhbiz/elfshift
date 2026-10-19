using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Il "guscio" del laboratorio: pavimento, pareti con aperture, zoccolo in legno, banchina con staccionata,
    /// neve all'esterno. Le dimensioni sono costanti pubbliche perché la scena le usa per posizionare tutto il resto.
    /// </summary>
    public static class ShellModels
    {
        public const float HalfWidth = 8f;     // interno: x da -8 a +8
        public const float HalfDepth = 6f;     // interno: z da -6 a +6
        public const float WallHeight = 4.4f;
        public const float WallThickness = 0.4f;
        public const float DoorHalfWidth = 2.5f;   // apertura sulla parete destra: z da -2.5 a +2.5
        public const float DockLength = 5f;        // la banchina va da x=8 a x=13
        public const float DockHalfDepth = 3.5f;
        public static readonly float[] WindowCenters = { -4.5f, 0.5f, 4.5f };
        public const float WindowWidth = 2.4f;
        public const float WindowBottom = 1.3f;
        public const float WindowHeight = 2.0f;

        public static GameObject Build(Transform parent, Material floorMaterial, Material dockMaterial)
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
            ground.HardBox(MeshBuilder.Xf(new Vector3(4f, -0.3f, 4f)), new Vector3(90f, 0.5f, 80f), ground.Sub("SnowGround"), 0f);
            ModelKit.Part(root, "SnowGround", ground);

            // ---- Pareti (intonaco) -------------------------------------------------------------
            MeshBuilder walls = new MeshBuilder();
            int plaster = walls.Sub("Plaster");
            float backZ = HalfDepth + WallThickness / 2f;
            float wallLen = HalfWidth * 2f + WallThickness * 2f;
            // parete di fondo: fascia bassa, fascia alta e pilastri tra le finestre
            walls.HardBox(MeshBuilder.Xf(new Vector3(0f, WindowBottom / 2f, backZ)), new Vector3(wallLen, WindowBottom, WallThickness), plaster, 0f);
            float topStart = WindowBottom + WindowHeight;
            walls.HardBox(MeshBuilder.Xf(new Vector3(0f, (topStart + WallHeight) / 2f, backZ)), new Vector3(wallLen, WallHeight - topStart, WallThickness), plaster, 0f);
            float pillarY = WindowBottom + WindowHeight / 2f;
            float prevEdge = -HalfWidth - WallThickness;
            for (int i = 0; i <= WindowCenters.Length; i++)
            {
                float nextEdge = i < WindowCenters.Length ? WindowCenters[i] - WindowWidth / 2f : HalfWidth + WallThickness;
                float width = nextEdge - prevEdge;
                if (width > 0.01f)
                    walls.HardBox(MeshBuilder.Xf(new Vector3((prevEdge + nextEdge) / 2f, pillarY, backZ)), new Vector3(width, WindowHeight, WallThickness), plaster, 0f);
                if (i < WindowCenters.Length) prevEdge = WindowCenters[i] + WindowWidth / 2f;
            }
            // parete sinistra intera
            float leftX = -HalfWidth - WallThickness / 2f;
            walls.HardBox(MeshBuilder.Xf(new Vector3(leftX, WallHeight / 2f, 0f)), new Vector3(WallThickness, WallHeight, HalfDepth * 2f), plaster, 0f);
            // parete destra con porta larga verso la banchina
            float rightX = HalfWidth + WallThickness / 2f;
            float segLen = HalfDepth + WallThickness - DoorHalfWidth;
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, WallHeight / 2f, DoorHalfWidth + segLen / 2f)), new Vector3(WallThickness, WallHeight, segLen), plaster, 0f);
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, WallHeight / 2f, -(DoorHalfWidth + segLen / 2f))), new Vector3(WallThickness, WallHeight, segLen), plaster, 0f);
            walls.HardBox(MeshBuilder.Xf(new Vector3(rightX, (3.3f + WallHeight) / 2f, 0f)), new Vector3(WallThickness, WallHeight - 3.3f, DoorHalfWidth * 2f), plaster, 0f);
            // basso muretto sul davanti (non copre la vista)
            float frontZ = -HalfDepth - 0.15f;
            walls.HardBox(MeshBuilder.Xf(new Vector3(0f, 0.375f, frontZ)), new Vector3(HalfWidth * 2f + WallThickness * 2f, 0.75f, 0.3f), plaster, 0f);
            ModelKit.Part(root, "Walls", walls);

            // ---- Zoccolo e rifiniture in legno, travi e cappelli di neve ----------------------------
            MeshBuilder wood = new MeshBuilder();
            float inner = 0.06f;
            // zoccolo + cimasa sul fondo
            wood.Box("WoodMid", new Vector3(0f, 0.55f, HalfDepth - inner / 2f + 0.01f), new Vector3(HalfWidth * 2f, 1.1f, inner), 0.015f);
            wood.Box("WoodDark", new Vector3(0f, 1.12f, HalfDepth - 0.05f), new Vector3(HalfWidth * 2f, 0.07f, 0.1f), 0.02f);
            // zoccolo sul lato sinistro
            wood.Box("WoodMid", new Vector3(-HalfWidth + inner / 2f - 0.01f, 0.55f, 0f), new Vector3(inner, 1.1f, HalfDepth * 2f), 0.015f);
            wood.Box("WoodDark", new Vector3(-HalfWidth + 0.05f, 1.12f, 0f), new Vector3(0.1f, 0.07f, HalfDepth * 2f), 0.02f);
            // zoccolo sul lato destro (interrotto dalla porta)
            float rightSeg = HalfDepth - DoorHalfWidth;
            for (int s = -1; s <= 1; s += 2)
            {
                float cz = s * (DoorHalfWidth + rightSeg / 2f);
                wood.Box("WoodMid", new Vector3(HalfWidth - inner / 2f + 0.01f, 0.55f, cz), new Vector3(inner, 1.1f, rightSeg), 0.015f);
                wood.Box("WoodDark", new Vector3(HalfWidth - 0.05f, 1.12f, cz), new Vector3(0.1f, 0.07f, rightSeg), 0.02f);
            }
            // travi verticali agli angoli e ai lati della porta
            float[][] posts =
            {
                new[] { -HalfWidth + 0.15f, HalfDepth - 0.15f }, new[] { HalfWidth - 0.15f, HalfDepth - 0.15f },
                new[] { HalfWidth - 0.15f, DoorHalfWidth + 0.1f }, new[] { HalfWidth - 0.15f, -DoorHalfWidth - 0.1f },
                new[] { -HalfWidth + 0.15f, -HalfDepth + 0.15f }, new[] { -HalfWidth + 0.15f, 0f },
            };
            for (int i = 0; i < posts.Length; i++)
                wood.Box("WoodDark", new Vector3(posts[i][0], WallHeight / 2f, posts[i][1]), new Vector3(0.3f, WallHeight, 0.3f), 0.03f);
            // trave sopra la porta
            wood.Box("WoodDark", new Vector3(HalfWidth - 0.1f, 3.25f, 0f), new Vector3(0.28f, 0.22f, DoorHalfWidth * 2f + 0.5f), 0.03f);
            // cornicione in alto con neve
            wood.Box("WoodMid", new Vector3(0f, WallHeight + 0.06f, backZ), new Vector3(wallLen + 0.3f, 0.14f, WallThickness + 0.2f), 0.03f);
            wood.Box("WoodMid", new Vector3(leftX, WallHeight + 0.06f, 0f), new Vector3(WallThickness + 0.2f, 0.14f, HalfDepth * 2f + 0.3f), 0.03f);
            wood.Box("WoodMid", new Vector3(rightX, WallHeight + 0.06f, 0f), new Vector3(WallThickness + 0.2f, 0.14f, HalfDepth * 2f + 0.3f), 0.03f);
            for (int i = 0; i < 8; i++)
            {
                float x = -HalfWidth + 1f + i * 2.2f;
                wood.Ball("Snow", new Vector3(x, WallHeight + 0.16f, backZ), 0.7f, new Vector3(1.5f, 0.2f, 0.45f), Vector3.zero, 12);
            }
            for (int i = 0; i < 5; i++)
            {
                float z = -HalfDepth + 1f + i * 2.5f;
                wood.Ball("Snow", new Vector3(leftX, WallHeight + 0.16f, z), 0.7f, new Vector3(0.45f, 0.2f, 1.7f), Vector3.zero, 12);
                if (Mathf.Abs(z) > DoorHalfWidth + 0.4f)
                    wood.Ball("Snow", new Vector3(rightX, WallHeight + 0.16f, z), 0.7f, new Vector3(0.45f, 0.2f, 1.7f), Vector3.zero, 12);
            }
            // muretto frontale: cimasa di legno e neve
            wood.Box("WoodMid", new Vector3(0f, 0.8f, frontZ), new Vector3(HalfWidth * 2f + WallThickness * 2f + 0.1f, 0.1f, 0.42f), 0.03f);
            for (int i = 0; i < 9; i++)
                wood.Ball("Snow", new Vector3(-HalfWidth + 0.2f + i * 2.0f, 0.88f, frontZ), 0.75f, new Vector3(1.3f, 0.2f, 0.4f), Vector3.zero, 10);
            ModelKit.Part(root, "Trims", wood);

            // ---- Mucchi di neve fuori dalle pareti e lungo la banchina -----------------------------
            MeshBuilder drifts = new MeshBuilder();
            for (int i = 0; i < 9; i++)
                drifts.Ball("Snow", new Vector3(-9f + i * 2.8f + ((i % 2) * 0.6f), 0f, HalfDepth + 1.4f + (i % 3) * 0.5f), 1f, new Vector3(1.8f + (i % 3) * 0.4f, 0.55f, 1.2f), Vector3.zero, 12);
            for (int i = 0; i < 6; i++)
                drifts.Ball("Snow", new Vector3(-HalfWidth - 1.2f - (i % 2) * 0.6f, 0f, -5f + i * 2.2f), 1f, new Vector3(1.0f, 0.5f, 1.5f), Vector3.zero, 12);
            for (int i = 0; i < 4; i++)
            {
                float side = i < 2 ? 1f : -1f;
                drifts.Ball("Snow", new Vector3(HalfWidth + 1.5f + (i % 2) * 2.4f, 0f, side * (DockHalfDepth + 1.1f + (i % 2) * 0.4f)), 1f, new Vector3(1.6f, 0.5f, 1.1f), Vector3.zero, 12);
            }
            drifts.Ball("Snow", new Vector3(HalfWidth + DockLength + 1.4f, 0f, 0f), 1f, new Vector3(1.4f, 0.55f, 3.0f), Vector3.zero, 14);
            ModelKit.Part(root, "SnowDrifts", drifts);

            // ---- Collisioni (invisibili) -------------------------------------------------------------
            GameObject colliders = new GameObject("Colliders");
            colliders.transform.SetParent(root, false);
            ModelKit.AddBoxCollider(colliders, new Vector3(0f, -0.5f, 0f), new Vector3(60f, 1f, 50f)); // pavimento/terreno
            AddWall(colliders.transform, new Vector3(0f, WallHeight / 2f, backZ), new Vector3(wallLen, WallHeight, WallThickness));
            AddWall(colliders.transform, new Vector3(leftX, WallHeight / 2f, 0f), new Vector3(WallThickness, WallHeight, HalfDepth * 2f + 1f));
            AddWall(colliders.transform, new Vector3(rightX, WallHeight / 2f, DoorHalfWidth + segLen / 2f), new Vector3(WallThickness, WallHeight, segLen));
            AddWall(colliders.transform, new Vector3(rightX, WallHeight / 2f, -(DoorHalfWidth + segLen / 2f)), new Vector3(WallThickness, WallHeight, segLen));
            AddWall(colliders.transform, new Vector3(0f, 1f, frontZ - 0.1f), new Vector3(wallLen, 2f, 0.5f));
            // recinto della banchina
            float dockCenterX = HalfWidth + DockLength / 2f + 0.2f;
            AddWall(colliders.transform, new Vector3(dockCenterX, 0.6f, DockHalfDepth), new Vector3(DockLength + 0.4f, 1.2f, 0.25f));
            AddWall(colliders.transform, new Vector3(dockCenterX, 0.6f, -DockHalfDepth), new Vector3(DockLength + 0.4f, 1.2f, 0.25f));
            AddWall(colliders.transform, new Vector3(HalfWidth + DockLength + 0.3f, 0.6f, 0f), new Vector3(0.25f, 1.2f, DockHalfDepth * 2f));
            return root.gameObject;
        }

        private static void AddWall(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject("WallCollider");
            go.transform.SetParent(parent, false);
            ModelKit.AddBoxCollider(go, center, size);
        }
    }
}
