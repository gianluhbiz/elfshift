using Elfshift.Player;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>Modello provvisorio dell'elfo (cappello a punta, tunica verde, calze a righe, scarpe con campanelle).</summary>
    public static class CharacterModels
    {
        /// <summary>
        /// Costruisce il corpo dell'elfo sotto "parent" e collega l'animatore procedurale.
        /// Restituisce il Transform "HandSocket" (dove compare l'oggetto trasportato).
        /// </summary>
        public static Transform BuildElf(Transform parent, out GameObject visualRoot)
        {
            ModelKit.Begin("Elf");
            Transform root = ModelKit.Empty(parent, "ElfVisual", Vector3.zero);
            visualRoot = root.gameObject;

            Transform legL = BuildLeg(root, "LegL", -0.12f);
            Transform legR = BuildLeg(root, "LegR", 0.12f);

            // ---- Corpo (pivot all'altezza dei fianchi) ---------------------------------------
            Transform body = ModelKit.Empty(root, "Body", new Vector3(0f, 0.55f, 0f));
            MeshBuilder tunic = new MeshBuilder();
            Profile tp = new Profile()
                .Add(0f, -0.03f).Add(0.2f, -0.03f).Add(0.27f, 0f).Add(0.255f, 0.08f)
                .Add(0.21f, 0.22f).Add(0.215f, 0.36f).Add(0.19f, 0.48f).Add(0.12f, 0.55f).Add(0f, 0.58f);
            tunic.Lathe(MeshBuilder.Xf(Vector3.zero), tp, 28, tunic.Sub("GreenElf"));
            tunic.Ring("White", new Vector3(0f, 0.0f, 0f), 0.268f, 0.034f, Vector3.zero, 28, 8);   // bordo pelliccia
            tunic.Ring("WoodDeep", new Vector3(0f, 0.2f, 0f), 0.212f, 0.028f, Vector3.zero, 28, 8); // cintura
            tunic.Box("Gold", new Vector3(0f, 0.2f, 0.232f), new Vector3(0.09f, 0.08f, 0.035f), 0.012f);
            tunic.Ball("Gold", new Vector3(0f, 0.33f, 0.215f), 0.022f, 10);
            tunic.Ball("Gold", new Vector3(0f, 0.42f, 0.205f), 0.022f, 10);
            tunic.Ring("White", new Vector3(0f, 0.535f, 0f), 0.125f, 0.03f, Vector3.zero, 24, 8);    // colletto
            ModelKit.Part(body, "Tunic", tunic);

            // ---- Braccia ---------------------------------------------------------------------
            Transform armL = BuildArm(body, "ArmL", -0.245f);
            Transform armR = BuildArm(body, "ArmR", 0.245f);

            // ---- Testa -----------------------------------------------------------------------
            Transform head = ModelKit.Empty(body, "Head", new Vector3(0f, 0.6f, 0f));
            MeshBuilder face = new MeshBuilder();
            face.Ball("Skin", new Vector3(0f, 0.22f, 0f), 0.27f, new Vector3(1f, 0.92f, 0.97f), Vector3.zero, 26);
            // orecchie a punta
            face.Lathe(MeshBuilder.Xf(new Vector3(0.235f, 0.25f, 0f), new Vector3(0f, 0f, -72f), new Vector3(1f, 1f, 0.45f)), Profile.Cone(0.075f, 0.24f), 14, face.Sub("Skin"));
            face.Lathe(MeshBuilder.Xf(new Vector3(-0.235f, 0.25f, 0f), new Vector3(0f, 0f, 72f), new Vector3(1f, 1f, 0.45f)), Profile.Cone(0.075f, 0.24f), 14, face.Sub("Skin"));
            // occhi e luci negli occhi
            face.Ball("EyeDark", new Vector3(0.1f, 0.25f, 0.238f), 0.045f, new Vector3(0.8f, 1.15f, 0.6f), Vector3.zero, 12);
            face.Ball("EyeDark", new Vector3(-0.1f, 0.25f, 0.238f), 0.045f, new Vector3(0.8f, 1.15f, 0.6f), Vector3.zero, 12);
            face.Ball("White", new Vector3(0.112f, 0.275f, 0.262f), 0.013f, 8);
            face.Ball("White", new Vector3(-0.088f, 0.275f, 0.262f), 0.013f, 8);
            // guance, naso, bocca
            face.Ball("Cheek", new Vector3(0.165f, 0.165f, 0.205f), 0.052f, new Vector3(1f, 0.7f, 0.4f), Vector3.zero, 10);
            face.Ball("Cheek", new Vector3(-0.165f, 0.165f, 0.205f), 0.052f, new Vector3(1f, 0.7f, 0.4f), Vector3.zero, 10);
            face.Ball("Cheek", new Vector3(0f, 0.19f, 0.268f), 0.036f, 10);
            face.Ball("EyeDark", new Vector3(0f, 0.115f, 0.252f), 0.032f, new Vector3(2.1f, 0.34f, 0.3f), Vector3.zero, 10);
            // frangetta castana sotto il cappello
            face.Ball("WoodDeep", new Vector3(0f, 0.37f, 0.12f), 0.15f, new Vector3(1.3f, 0.45f, 0.9f), Vector3.zero, 12);
            ModelKit.Part(head, "Face", face);

            // ---- Cappello a punta con pompon ------------------------------------------------
            Transform hat = ModelKit.Empty(head, "Hat", new Vector3(0f, 0.355f, 0.01f), new Vector3(-6f, 0f, 0f));
            Vector3[] curve = MeshBuilder.Bezier(
                Vector3.zero, new Vector3(0f, 0.5f, 0.05f), new Vector3(0f, 0.8f, -0.16f), new Vector3(0f, 0.62f, -0.52f), 18);
            int mid = 8;
            Vector3[] lower = new Vector3[mid + 1];
            System.Array.Copy(curve, 0, lower, 0, mid + 1);
            Vector3[] upper = new Vector3[curve.Length - mid];
            System.Array.Copy(curve, mid, upper, 0, upper.Length);

            Vector3 joinTangent = (curve[mid + 1] - curve[mid - 1]).normalized;
            MeshBuilder hatLower = new MeshBuilder();
            hatLower.Tube(lower, 0.285f, 0.15f, 20, "RedSanta", null, 0, false, Vector3.zero, joinTangent);
            hatLower.Ring("White", Vector3.zero, 0.275f, 0.06f, Vector3.zero, 26, 8);   // bordo di pelliccia
            ModelKit.Part(hat, "HatBase", hatLower);

            Transform hatTip = ModelKit.Empty(hat, "HatTip", lower[mid]);
            Vector3[] upperLocal = new Vector3[upper.Length];
            for (int i = 0; i < upper.Length; i++) upperLocal[i] = upper[i] - lower[mid];
            MeshBuilder hatUpper = new MeshBuilder();
            hatUpper.Tube(upperLocal, 0.15f, 0.04f, 20, "RedSanta", null, 0, false, joinTangent, Vector3.zero);
            Vector3 tipEnd = upperLocal[upperLocal.Length - 1];
            hatUpper.Ball("White", tipEnd + new Vector3(0f, -0.02f, -0.01f), 0.085f, 14);
            ModelKit.Part(hatTip, "HatUpper", hatUpper);

            // ---- Punto di presa degli oggetti -----------------------------------------------
            Transform hand = ModelKit.Empty(body, "HandSocket", new Vector3(0f, 0.28f, 0.46f));

            // ---- Animatore procedurale ------------------------------------------------------
            ElfProceduralAnimator animator = root.gameObject.AddComponent<ElfProceduralAnimator>();
            Wire.Ref(animator, "body", body);
            Wire.Ref(animator, "head", head);
            Wire.Ref(animator, "armLeft", armL);
            Wire.Ref(animator, "armRight", armR);
            Wire.Ref(animator, "legLeft", legL);
            Wire.Ref(animator, "legRight", legR);
            Wire.Ref(animator, "hatTip", hatTip);
            return hand;
        }

        /// <summary>
        /// Braccia e mani in vista davanti alla telecamera (prima persona) e punto in cui compare l'oggetto trasportato.
        /// Restituisce il Transform "HandSocketFP".
        /// </summary>
        public static Transform BuildFirstPersonHands(Transform cameraTransform)
        {
            ModelKit.Begin("FirstPersonHands");
            Transform root = ModelKit.Empty(cameraTransform, "FirstPersonHands", Vector3.zero);
            for (int side = -1; side <= 1; side += 2)
            {
                MeshBuilder mb = new MeshBuilder();
                // Braccio modellato in Blender (manica a campana, polsino di pelliccia, muffola a maglia con pollice).
                // Il file contiene il braccio destro: per il sinistro lo specchiamo sull'asse X.
                Matrix4x4 mirror = MeshBuilder.Xf(Vector3.zero, Vector3.zero, new Vector3(side < 0 ? -1f : 1f, 1f, 1f));
                bool imported = mb.Import("Char_FPArm", mirror);
                if (!imported)
                {
                    // forma di riserva: manica verde, polsino bianco e mano (guanto color pelle)
                    mb.Taper("GreenElf", new Vector3(0f, 0f, 0f), 0.075f, 0.095f, 0.5f, 0.04f, Vector3.zero, 14);
                    mb.Ring("White", new Vector3(0f, 0.5f, 0f), 0.082f, 0.024f, Vector3.zero, 14, 6);
                    mb.Ball("Skin", new Vector3(0f, 0.57f, 0f), 0.085f, new Vector3(1f, 1.1f, 0.9f), Vector3.zero, 12);
                }

                // Posa: la muffola sta in basso ai lati dello schermo, l'avambraccio sale dall'angolo in basso
                // verso l'interno e in avanti, e il dorso della mano guarda la telecamera (come nei giochi in prima persona).
                // Il palmo è ruotato un po' verso il centro (pollice in alto e verso l'interno).
                Vector3 mittenPos = new Vector3(side * 0.34f, -0.27f, 0.6f);
                Vector3 forearm = new Vector3(-side * 0.3f, 0.55f, 0.78f).normalized;
                Quaternion pose = BasisRotation(forearm, mittenPos.normalized + new Vector3(-side * 0.5f, 0f, 0f));
                Vector3 armPos = mittenPos - pose * new Vector3(0f, MittenCenter, 0f);
                GameObject arm = ModelKit.Part(root, side < 0 ? "HandLeft" : "HandRight", mb, armPos, Vector3.zero);
                arm.transform.localRotation = pose;
                MeshRenderer r = arm.GetComponent<MeshRenderer>();
                if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return ModelKit.Empty(root, "HandSocketFP", new Vector3(0.0f, -0.3f, 0.62f), new Vector3(0f, 0f, 0f));
        }

        /// <summary>Altezza (lungo il braccio) del centro della muffola nel modello Char_FPArm.</summary>
        private const float MittenCenter = 0.56f;

        /// <summary>
        /// Rotazione che porta l'asse Y locale su "yAxis" e l'asse Z locale il più vicino possibile a "zHint"
        /// (costruita a mano dalla base ortonormale, senza Quaternion.LookRotation).
        /// </summary>
        private static Quaternion BasisRotation(Vector3 yAxis, Vector3 zHint)
        {
            Vector3 y = yAxis.normalized;
            Vector3 z = (zHint - y * Vector3.Dot(zHint, y)).normalized;
            Vector3 x = Vector3.Cross(y, z);
            // matrice di rotazione con colonne x, y, z -> quaternione
            float m00 = x.x, m01 = y.x, m02 = z.x;
            float m10 = x.y, m11 = y.y, m12 = z.y;
            float m20 = x.z, m21 = y.z, m22 = z.z;
            float trace = m00 + m11 + m22;
            float qw, qx, qy, qz;
            if (trace > 0f)
            {
                float s = Mathf.Sqrt(trace + 1f) * 2f;
                qw = 0.25f * s; qx = (m21 - m12) / s; qy = (m02 - m20) / s; qz = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                float s = Mathf.Sqrt(1f + m00 - m11 - m22) * 2f;
                qw = (m21 - m12) / s; qx = 0.25f * s; qy = (m01 + m10) / s; qz = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                float s = Mathf.Sqrt(1f + m11 - m00 - m22) * 2f;
                qw = (m02 - m20) / s; qx = (m01 + m10) / s; qy = 0.25f * s; qz = (m12 + m21) / s;
            }
            else
            {
                float s = Mathf.Sqrt(1f + m22 - m00 - m11) * 2f;
                qw = (m10 - m01) / s; qx = (m02 + m20) / s; qy = (m12 + m21) / s; qz = 0.25f * s;
            }
            return new Quaternion(qx, qy, qz, qw);
        }

        private static Transform BuildLeg(Transform root, string name, float x)
        {
            Transform leg = ModelKit.Empty(root, name, new Vector3(x, 0.55f, 0f));
            MeshBuilder mb = new MeshBuilder();
            // calza a righe rosse e bianche
            for (int i = 0; i < 4; i++)
                mb.Cyl(i % 2 == 0 ? "RedSanta" : "Cream", new Vector3(0f, -0.43f + 0.1f * i, 0f), 0.072f, 0.105f, 0.012f, Vector3.zero, 14);
            // scarpa verde con punta arricciata e campanella
            mb.Taper("GreenDeep", new Vector3(0f, -0.55f, 0f), 0.105f, 0.085f, 0.14f, 0.03f, Vector3.zero, 16);
            mb.Ball("GreenDeep", new Vector3(0f, -0.5f, 0.1f), 0.085f, new Vector3(0.95f, 0.8f, 1.35f), Vector3.zero, 14);
            mb.Ball("GreenDeep", new Vector3(0f, -0.455f, 0.21f), 0.05f, 10);
            mb.Ball("Gold", new Vector3(0f, -0.42f, 0.245f), 0.032f, 10);
            mb.Ring("White", new Vector3(0f, -0.425f, 0f), 0.088f, 0.022f, Vector3.zero, 16, 6);
            ModelKit.Part(leg, "LegMesh", mb);
            return leg;
        }

        private static Transform BuildArm(Transform body, string name, float x)
        {
            Transform arm = ModelKit.Empty(body, name, new Vector3(x, 0.47f, 0f));
            MeshBuilder mb = new MeshBuilder();
            mb.Ball("GreenElf", Vector3.zero, 0.09f, 12);
            mb.Taper("GreenElf", new Vector3(0f, -0.32f, 0f), 0.062f, 0.082f, 0.32f, 0.035f, Vector3.zero, 14);
            mb.Ring("White", new Vector3(0f, -0.3f, 0f), 0.066f, 0.022f, Vector3.zero, 14, 6);
            mb.Ball("Skin", new Vector3(0f, -0.37f, 0f), 0.068f, 12);
            ModelKit.Part(arm, "ArmMesh", mb);
            return arm;
        }
    }
}
