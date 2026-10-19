using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Elfshift.EditorTools
{
    /// <summary>Un punto di un profilo di rotazione (raggio, altezza). "Sharp" = spigolo vivo.</summary>
    public struct ProfilePoint
    {
        public float R;
        public float Y;
        public bool Sharp;

        public ProfilePoint(float r, float y, bool sharp)
        {
            R = r;
            Y = y;
            Sharp = sharp;
        }
    }

    /// <summary>
    /// Profilo 2D (raggio, altezza) che viene fatto ruotare attorno all'asse Y per ottenere
    /// forme "al tornio": cilindri smussati, coni, sfere, cappelli, vasi...
    /// </summary>
    public sealed class Profile
    {
        public readonly List<ProfilePoint> Points = new List<ProfilePoint>();

        public Profile Add(float r, float y, bool sharp = false)
        {
            Points.Add(new ProfilePoint(r, y, sharp));
            return this;
        }

        /// <summary>Arco di cerchio nel piano (raggio, altezza). Angolo 0 = verso l'esterno, 90 = verso l'alto.</summary>
        public Profile Arc(float centerR, float centerY, float radius, float fromDeg, float toDeg, int steps)
        {
            steps = Mathf.Max(1, steps);
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                Add(centerR + radius * Mathf.Cos(a), centerY + radius * Mathf.Sin(a));
            }
            return this;
        }

        /// <summary>Cilindro con base sull'origine, spigoli arrotondati (bevel) e tappi piatti.</summary>
        public static Profile Cylinder(float r, float h, float bevel, int bevelSteps = 3)
        {
            return Tapered(r, r, h, bevel, bevelSteps);
        }

        /// <summary>Cilindro con raggio diverso in basso e in alto (tronco di cono arrotondato).</summary>
        public static Profile Tapered(float r0, float r1, float h, float bevel, int bevelSteps = 3)
        {
            Profile p = new Profile();
            float b = Mathf.Min(bevel, Mathf.Min(r0, r1) * 0.95f, h * 0.49f);
            if (b < 0.0008f)
            {
                p.Add(0f, 0f);
                p.Add(r0, 0f, true);
                p.Add(r1, h, true);
                p.Add(0f, h);
                return p;
            }
            p.Add(0f, 0f);
            p.Arc(r0 - b, b, b, -90f, 0f, bevelSteps);
            p.Arc(r1 - b, h - b, b, 0f, 90f, bevelSteps);
            p.Add(0f, h);
            return p;
        }

        /// <summary>Sfera centrata sull'origine (da usare con scala per ottenere ellissoidi).</summary>
        public static Profile Sphere(float r, int rings = 10)
        {
            Profile p = new Profile();
            p.Arc(0f, 0f, r, -90f, 90f, rings);
            return p;
        }

        /// <summary>Cono con la base sull'origine.</summary>
        public static Profile Cone(float r, float h)
        {
            Profile p = new Profile();
            p.Add(0f, 0f);
            p.Add(r, 0f, true);
            p.Add(0f, h);
            return p;
        }

        /// <summary>Pillola (cilindro con estremi semisferici), base sull'origine.</summary>
        public static Profile Pill(float r, float length, int steps = 5)
        {
            Profile p = new Profile();
            p.Arc(0f, r, r, -90f, 0f, steps);
            p.Arc(0f, length - r, r, 0f, 90f, steps);
            return p;
        }
    }

    /// <summary>
    /// Costruttore di mesh 3D "a mano": scatole smussate, forme al tornio, tori, tubi lungo un percorso.
    /// Ogni forma usa un sotto-mesh legato al nome di un materiale (vedi <see cref="Sub"/>).
    /// I modelli del gioco sono prodotti qui, senza bisogno di Blender; potranno essere
    /// sostituiti in qualsiasi momento da modelli veri.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> verts = new List<Vector3>();
        private readonly List<Vector3> norms = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        // Colore per vertice: nei modelli fatti in Blender contiene l'ombreggiatura "ambient occlusion"
        // (angoli e fessure più scuri). Le forme create qui in C# usano il bianco (nessun effetto).
        private readonly List<Color> colors = new List<Color>();
        private readonly List<List<int>> tris = new List<List<int>>();
        private readonly List<string> materialKeys = new List<string>();

        public IReadOnlyList<string> MaterialKeys => materialKeys;
        public IReadOnlyList<Vector3> Vertices => verts;
        public IReadOnlyList<List<int>> Triangles => tris;
        public int VertexCount => verts.Count;

        /// <summary>Restituisce l'indice del sotto-mesh per il materiale con questo nome (lo crea se serve).</summary>
        public int Sub(string materialKey)
        {
            int index = materialKeys.IndexOf(materialKey);
            if (index >= 0) return index;
            materialKeys.Add(materialKey);
            tris.Add(new List<int>());
            return materialKeys.Count - 1;
        }

        // ------------------------------------------------------------------ Trasformazioni
        public static Matrix4x4 Xf(Vector3 pos)
        {
            return Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        }

        public static Matrix4x4 Xf(Vector3 pos, Vector3 eulerDeg)
        {
            return Matrix4x4.TRS(pos, Quaternion.Euler(eulerDeg), Vector3.one);
        }

        public static Matrix4x4 Xf(Vector3 pos, Vector3 eulerDeg, Vector3 scale)
        {
            return Matrix4x4.TRS(pos, Quaternion.Euler(eulerDeg), scale);
        }

        // ------------------------------------------------------------------ Funzioni comode
        /// <summary>Scatola centrata in "pos". Con radius &gt; 0 gli spigoli sono arrotondati.</summary>
        public void Box(string mat, Vector3 pos, Vector3 size, float radius = 0f, Vector3 euler = default(Vector3), int cornerSteps = 3)
        {
            if (radius > 0.001f) RoundedBox(Xf(pos, euler), size, radius, cornerSteps, Sub(mat));
            else HardBox(Xf(pos, euler), size, Sub(mat), 0f);
        }

        /// <summary>Cilindro con la BASE in "pos" e asse lungo Y (ruotabile con euler).</summary>
        public void Cyl(string mat, Vector3 pos, float radius, float height, float bevel = 0.02f, Vector3 euler = default(Vector3), int seg = 20)
        {
            Lathe(Xf(pos, euler), Profile.Cylinder(radius, height, bevel), seg, Sub(mat));
        }

        /// <summary>Cilindro CENTRATO in "pos" (utile per cilindri orizzontali: euler = (0,0,90) per l'asse X).</summary>
        public void CylC(string mat, Vector3 center, float radius, float length, float bevel = 0.02f, Vector3 euler = default(Vector3), int seg = 20)
        {
            Matrix4x4 xf = Xf(center, euler) * Matrix4x4.TRS(new Vector3(0f, -length * 0.5f, 0f), Quaternion.identity, Vector3.one);
            Lathe(xf, Profile.Cylinder(radius, length, bevel), seg, Sub(mat));
        }

        /// <summary>Tronco di cono arrotondato con base in "pos".</summary>
        public void Taper(string mat, Vector3 pos, float r0, float r1, float height, float bevel = 0.02f, Vector3 euler = default(Vector3), int seg = 20)
        {
            Lathe(Xf(pos, euler), Profile.Tapered(r0, r1, height, bevel), seg, Sub(mat));
        }

        /// <summary>Sfera o ellissoide (scale = semiassi).</summary>
        public void Ball(string mat, Vector3 center, float radius, Vector3 scale, Vector3 euler = default(Vector3), int seg = 16)
        {
            Lathe(Xf(center, euler, scale), Profile.Sphere(radius, Mathf.Max(6, seg / 2)), seg, Sub(mat));
        }

        public void Ball(string mat, Vector3 center, float radius, int seg = 16)
        {
            Ball(mat, center, radius, Vector3.one, Vector3.zero, seg);
        }

        /// <summary>Cono con la base in "pos".</summary>
        public void Cone(string mat, Vector3 pos, float radius, float height, Vector3 euler = default(Vector3), int seg = 20)
        {
            Lathe(Xf(pos, euler), Profile.Cone(radius, height), seg, Sub(mat));
        }

        /// <summary>Ciambella (toro) nel piano XZ, ruotabile con euler.</summary>
        public void Ring(string mat, Vector3 center, float major, float minor, Vector3 euler = default(Vector3), int seg = 24, int tubeSeg = 8)
        {
            Torus(Xf(center, euler), major, minor, seg, tubeSeg, Sub(mat));
        }

        // ------------------------------------------------------------------ Primitive: scatola
        /// <summary>Scatola a spigoli vivi. uvScale &gt; 0: la texture si ripete ogni 1/uvScale metri.</summary>
        public void HardBox(Matrix4x4 xf, Vector3 size, int sub, float uvScale)
        {
            Vector3 h = size * 0.5f;
            Matrix4x4 nxf = xf.inverse.transpose;
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] us = { Vector3.forward, Vector3.back, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] vs = { Vector3.up, Vector3.up, Vector3.forward, Vector3.back, Vector3.up, Vector3.up };

            for (int f = 0; f < 6; f++)
            {
                Vector3 n = normals[f];
                Vector3 c = Vector3.Scale(n, h);
                Vector3 ue = Vector3.Scale(us[f], h);
                Vector3 ve = Vector3.Scale(vs[f], h);
                Vector3[] corners = { c - ue - ve, c + ue - ve, c + ue + ve, c - ue + ve };
                int[] idx = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 uv;
                    if (uvScale > 0f) uv = new Vector2(Vector3.Dot(corners[i], us[f]) * uvScale, Vector3.Dot(corners[i], vs[f]) * uvScale);
                    else uv = new Vector2(i == 1 || i == 2 ? 1f : 0f, i >= 2 ? 1f : 0f);
                    idx[i] = AddVertex(xf, nxf, corners[i], n, uv);
                }
                AddTri(sub, idx[0], idx[1], idx[2]);
                AddTri(sub, idx[0], idx[2], idx[3]);
            }
        }

        // ------------------------------------------------------------------ Primitive: scatola arrotondata
        /// <summary>Scatola con spigoli arrotondati (come un giocattolo di legno morbido).</summary>
        public void RoundedBox(Matrix4x4 xf, Vector3 size, float radius, int cornerSteps, int sub)
        {
            Vector3 h = size * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.999f);
            Matrix4x4 nxf = xf.inverse.transpose;
            cornerSteps = Mathf.Max(1, cornerSteps);

            float[][] samples = { Samples(h.x, r, cornerSteps), Samples(h.y, r, cornerSteps), Samples(h.z, r, cornerSteps) };

            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int b = (axis + 1) % 3;
                    int c = (axis + 2) % 3;
                    int[,] grid = new int[samples[b].Length, samples[c].Length];
                    for (int ib = 0; ib < samples[b].Length; ib++)
                    {
                        for (int ic = 0; ic < samples[c].Length; ic++)
                        {
                            Vector3 q = Vector3.zero;
                            q[axis] = sign * h[axis];
                            q[b] = samples[b][ib];
                            q[c] = samples[c][ic];

                            Vector3 inner = new Vector3(
                                Mathf.Clamp(q.x, -(h.x - r), h.x - r),
                                Mathf.Clamp(q.y, -(h.y - r), h.y - r),
                                Mathf.Clamp(q.z, -(h.z - r), h.z - r));
                            Vector3 n = (q - inner).normalized;
                            Vector3 p = inner + n * r;
                            Vector2 uv = new Vector2((q[b] + h[b]) / Mathf.Max(0.0001f, size[b]), (q[c] + h[c]) / Mathf.Max(0.0001f, size[c]));
                            grid[ib, ic] = AddVertex(xf, nxf, p, n, uv);
                        }
                    }
                    for (int ib = 0; ib < samples[b].Length - 1; ib++)
                    {
                        for (int ic = 0; ic < samples[c].Length - 1; ic++)
                        {
                            AddTri(sub, grid[ib, ic], grid[ib + 1, ic], grid[ib + 1, ic + 1]);
                            AddTri(sub, grid[ib, ic], grid[ib + 1, ic + 1], grid[ib, ic + 1]);
                        }
                    }
                }
            }
        }

        private static float[] Samples(float half, float r, int steps)
        {
            float[] s = new float[(steps + 1) * 2];
            for (int i = 0; i <= steps; i++)
            {
                s[i] = -half + r * (i / (float)steps);
                s[steps + 1 + i] = (half - r) + r * (i / (float)steps);
            }
            return s;
        }

        // ------------------------------------------------------------------ Primitive: forma al tornio
        public void Lathe(Matrix4x4 xf, Profile profile, int segments, int sub, float sweepDegrees = 360f)
        {
            List<ProfilePoint> pts = profile.Points;
            int n = pts.Count;
            if (n < 2) return;
            segments = Mathf.Max(3, segments);
            Matrix4x4 nxf = xf.inverse.transpose;

            // Normali 2D di ogni segmento del profilo (verso l'esterno rispetto alla direzione di percorrenza).
            Vector2[] segN = new Vector2[n - 1];
            bool[] segValid = new bool[n - 1];
            for (int i = 0; i < n - 1; i++)
            {
                float dr = pts[i + 1].R - pts[i].R;
                float dy = pts[i + 1].Y - pts[i].Y;
                float len = Mathf.Sqrt(dr * dr + dy * dy);
                if (len > 1e-6f)
                {
                    segN[i] = new Vector2(dy / len, -dr / len);
                    segValid[i] = true;
                }
            }
            // Segmenti degeneri: prendono la normale del vicino valido più prossimo.
            for (int i = 0; i < n - 1; i++)
            {
                if (segValid[i]) continue;
                for (int d = 1; d < n; d++)
                {
                    int a = i - d, b = i + d;
                    if (a >= 0 && segValid[a]) { segN[i] = segN[a]; break; }
                    if (b < n - 1 && segValid[b]) { segN[i] = segN[b]; break; }
                }
            }

            // Anelli di vertici (uno per punto del profilo, due se lo spigolo è vivo).
            List<Vector4> rings = new List<Vector4>(n * 2); // x=r, y=y, z=nr, w=ny
            List<int> ringProfileIndex = new List<int>(n * 2);
            List<float> ringV = new List<float>(n * 2);
            float totalLen = 0f;
            float[] cum = new float[n];
            for (int i = 1; i < n; i++)
            {
                float dr = pts[i].R - pts[i - 1].R;
                float dy = pts[i].Y - pts[i - 1].Y;
                totalLen += Mathf.Sqrt(dr * dr + dy * dy);
                cum[i] = totalLen;
            }
            for (int i = 0; i < n; i++)
            {
                Vector2 nPrev = i > 0 ? segN[i - 1] : segN[0];
                Vector2 nNext = i < n - 1 ? segN[i] : segN[n - 2];
                float v = totalLen > 0f ? cum[i] / totalLen : 0f;
                if (pts[i].Sharp && i > 0 && i < n - 1)
                {
                    rings.Add(new Vector4(pts[i].R, pts[i].Y, nPrev.x, nPrev.y));
                    ringProfileIndex.Add(i);
                    ringV.Add(v);
                    rings.Add(new Vector4(pts[i].R, pts[i].Y, nNext.x, nNext.y));
                    ringProfileIndex.Add(i);
                    ringV.Add(v);
                }
                else
                {
                    Vector2 avg = nPrev + nNext;
                    if (avg.sqrMagnitude < 1e-8f) avg = nNext;
                    avg.Normalize();
                    rings.Add(new Vector4(pts[i].R, pts[i].Y, avg.x, avg.y));
                    ringProfileIndex.Add(i);
                    ringV.Add(v);
                }
            }

            int ringCount = rings.Count;
            int[,] grid = new int[ringCount, segments + 1];
            for (int j = 0; j < ringCount; j++)
            {
                Vector4 ring = rings[j];
                for (int k = 0; k <= segments; k++)
                {
                    float ang = sweepDegrees * (k / (float)segments) * Mathf.Deg2Rad;
                    float c = Mathf.Cos(ang);
                    float s = Mathf.Sin(ang);
                    Vector3 p = new Vector3(ring.x * c, ring.y, ring.x * s);
                    Vector3 nn = new Vector3(ring.z * c, ring.w, ring.z * s);
                    grid[j, k] = AddVertex(xf, nxf, p, nn, new Vector2(k / (float)segments, ringV[j]));
                }
            }

            for (int j = 0; j < ringCount - 1; j++)
            {
                if (ringProfileIndex[j] == ringProfileIndex[j + 1]) continue; // i due anelli di uno spigolo non si collegano
                for (int k = 0; k < segments; k++)
                {
                    int a = grid[j, k], b = grid[j, k + 1], c = grid[j + 1, k + 1], d = grid[j + 1, k];
                    AddTri(sub, a, d, c);
                    AddTri(sub, a, c, b);
                }
            }
        }

        // ------------------------------------------------------------------ Primitive: toro
        public void Torus(Matrix4x4 xf, float major, float minor, int segments, int tubeSegments, int sub)
        {
            Matrix4x4 nxf = xf.inverse.transpose;
            int[,] grid = new int[segments + 1, tubeSegments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                for (int j = 0; j <= tubeSegments; j++)
                {
                    float b = (j / (float)tubeSegments) * Mathf.PI * 2f;
                    float cb = Mathf.Cos(b), sb = Mathf.Sin(b);
                    Vector3 n = new Vector3(cb * ca, sb, cb * sa);
                    Vector3 p = new Vector3(major * ca, 0f, major * sa) + n * minor;
                    grid[i, j] = AddVertex(xf, nxf, p, n, new Vector2(i / (float)segments, j / (float)tubeSegments));
                }
            }
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < tubeSegments; j++)
                {
                    AddTri(sub, grid[i, j], grid[i + 1, j], grid[i + 1, j + 1]);
                    AddTri(sub, grid[i, j], grid[i + 1, j + 1], grid[i, j + 1]);
                }
            }
        }

        // ------------------------------------------------------------------ Primitive: tubo lungo un percorso
        /// <summary>
        /// Tubo che segue un percorso (maniglie, bastoncini di zucchero, pattini, cappello a punta...).
        /// Il raggio passa linearmente da r0 a r1. Con stripeLength &gt; 0 i segmenti alternano due materiali.
        /// </summary>
        public void Tube(Vector3[] path, float r0, float r1, int sides, string matA, string matB = null, int stripeLength = 0, bool caps = true,
            Vector3 startTangent = default(Vector3), Vector3 endTangent = default(Vector3))
        {
            int m = path.Length;
            if (m < 2) return;
            sides = Mathf.Max(3, sides);
            int subA = Sub(matA);
            int subB = matB != null ? Sub(matB) : subA;

            Vector3[] tangents = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                Vector3 prev = path[Mathf.Max(0, i - 1)];
                Vector3 next = path[Mathf.Min(m - 1, i + 1)];
                Vector3 t = (next - prev);
                tangents[i] = t.sqrMagnitude > 1e-10f ? t.normalized : Vector3.up;
            }

            // Tangenti imposte dall'esterno: servono per far combaciare due tubi consecutivi senza fessure.
            if (startTangent.sqrMagnitude > 1e-8f) tangents[0] = startTangent.normalized;
            if (endTangent.sqrMagnitude > 1e-8f) tangents[m - 1] = endTangent.normalized;

            // Sistema di riferimento trasportato in parallelo lungo il percorso (nessuna torsione).
            Vector3 normal = Vector3.Cross(tangents[0], Mathf.Abs(Vector3.Dot(tangents[0], Vector3.up)) > 0.95f ? Vector3.right : Vector3.up).normalized;
            Vector3[] ns = new Vector3[m];
            Vector3[] bs = new Vector3[m];
            for (int i = 0; i < m; i++)
            {
                if (i > 0)
                {
                    normal = normal - tangents[i] * Vector3.Dot(normal, tangents[i]);
                    if (normal.sqrMagnitude < 1e-10f) normal = Vector3.Cross(tangents[i], Vector3.right);
                    normal.Normalize();
                }
                ns[i] = normal;
                bs[i] = Vector3.Cross(tangents[i], normal).normalized;
            }

            Matrix4x4 id = Matrix4x4.identity;
            int[,] grid = new int[m, sides + 1];
            for (int i = 0; i < m; i++)
            {
                float radius = Mathf.Lerp(r0, r1, m > 1 ? i / (float)(m - 1) : 0f);
                for (int s = 0; s <= sides; s++)
                {
                    float a = (s / (float)sides) * Mathf.PI * 2f;
                    Vector3 dir = ns[i] * Mathf.Cos(a) + bs[i] * Mathf.Sin(a);
                    grid[i, s] = AddVertex(id, id, path[i] + dir * radius, dir, new Vector2(s / (float)sides, i / (float)(m - 1)));
                }
            }
            for (int i = 0; i < m - 1; i++)
            {
                int sub = (stripeLength > 0 && ((i / stripeLength) % 2 == 1)) ? subB : subA;
                for (int s = 0; s < sides; s++)
                {
                    AddTri(sub, grid[i, s], grid[i + 1, s], grid[i + 1, s + 1]);
                    AddTri(sub, grid[i, s], grid[i + 1, s + 1], grid[i, s + 1]);
                }
            }
            if (!caps) return;
            AddTubeCap(path[0], -tangents[0], ns[0], bs[0], r0, sides, subA);
            AddTubeCap(path[m - 1], tangents[m - 1], ns[m - 1], bs[m - 1], r1, sides, subA);
        }

        private void AddTubeCap(Vector3 center, Vector3 normal, Vector3 u, Vector3 v, float radius, int sides, int sub)
        {
            Matrix4x4 id = Matrix4x4.identity;
            int c = AddVertex(id, id, center, normal, new Vector2(0.5f, 0.5f));
            int[] ring = new int[sides + 1];
            for (int s = 0; s <= sides; s++)
            {
                float a = (s / (float)sides) * Mathf.PI * 2f;
                Vector3 p = center + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius;
                ring[s] = AddVertex(id, id, p, normal, new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f));
            }
            for (int s = 0; s < sides; s++) AddTri(sub, c, ring[s], ring[s + 1]);
        }

        // ------------------------------------------------------------------ Primitive: prisma (stelle, sagome piatte)
        /// <summary>
        /// Sagoma 2D (nel piano XY) estrusa lungo Z. La sagoma deve essere "a stella" rispetto al suo
        /// centro (convessa o a stella): va bene per stelle, cuori, bandierine, fianchi della slitta.
        /// </summary>
        public void Prism(Matrix4x4 xf, IList<Vector2> outline, float thickness, int sub)
        {
            int n = outline.Count;
            if (n < 3) return;
            Matrix4x4 nxf = xf.inverse.transpose;
            float hz = thickness * 0.5f;

            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < n; i++) centroid += outline[i];
            centroid /= n;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 normal = new Vector3(0f, 0f, side);
                int c = AddVertex(xf, nxf, new Vector3(centroid.x, centroid.y, side * hz), normal, new Vector2(0.5f, 0.5f));
                int[] ring = new int[n];
                for (int i = 0; i < n; i++)
                    ring[i] = AddVertex(xf, nxf, new Vector3(outline[i].x, outline[i].y, side * hz), normal, outline[i]);
                for (int i = 0; i < n; i++) AddTri(sub, c, ring[i], ring[(i + 1) % n]);
            }

            for (int i = 0; i < n; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % n];
                Vector2 edge = b - a;
                if (edge.sqrMagnitude < 1e-10f) continue;
                Vector2 e = edge.normalized;
                Vector2 nrm2 = new Vector2(e.y, -e.x);
                // La normale deve guardare fuori dal centro.
                if (Vector2.Dot(nrm2, (a + b) * 0.5f - centroid) < 0f) nrm2 = -nrm2;
                Vector3 normal = new Vector3(nrm2.x, nrm2.y, 0f);
                int v0 = AddVertex(xf, nxf, new Vector3(a.x, a.y, -hz), normal, Vector2.zero);
                int v1 = AddVertex(xf, nxf, new Vector3(b.x, b.y, -hz), normal, Vector2.right);
                int v2 = AddVertex(xf, nxf, new Vector3(b.x, b.y, hz), normal, Vector2.one);
                int v3 = AddVertex(xf, nxf, new Vector3(a.x, a.y, hz), normal, Vector2.up);
                AddTri(sub, v0, v1, v2);
                AddTri(sub, v0, v2, v3);
            }
        }

        /// <summary>Contorno di una stella a "points" punte.</summary>
        public static Vector2[] StarOutline(int points, float outerRadius, float innerRadius)
        {
            Vector2[] outline = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? outerRadius : innerRadius;
                outline[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return outline;
        }

        // ------------------------------------------------------------------ Percorsi
        /// <summary>Curva di Bézier cubica campionata in "steps+1" punti.</summary>
        public static Vector3[] Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps)
        {
            Vector3[] pts = new Vector3[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float u = 1f - t;
                pts[i] = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
            }
            return pts;
        }

        // ------------------------------------------------------------------ Interni
        private int AddVertex(Matrix4x4 xf, Matrix4x4 nxf, Vector3 p, Vector3 n, Vector2 uv)
        {
            verts.Add(xf.MultiplyPoint3x4(p));
            Vector3 wn = nxf.MultiplyVector(n);
            norms.Add(wn.sqrMagnitude > 1e-12f ? wn.normalized : Vector3.up);
            uvs.Add(uv);
            colors.Add(Color.white);
            return verts.Count - 1;
        }

        // ------------------------------------------------------------------ Modelli fatti in Blender
        /// <summary>
        /// Aggiunge al builder un modello scolpito in Blender (file Assets/Models/Source/NOME.bytes),
        /// spostato/ruotato/scalato con "xf". I materiali del modello hanno i nomi della tavolozza.
        /// Restituisce falso (senza aggiungere nulla) se il file non esiste: chi chiama può usare una forma di riserva.
        /// </summary>
        public bool Import(string modelName, Matrix4x4 xf)
        {
            ImportedMesh data = ImportedMesh.Load(modelName);
            if (data == null) return false;
            Matrix4x4 nxf = xf.inverse.transpose;
            int baseIndex = verts.Count;
            for (int i = 0; i < data.Positions.Length; i++)
            {
                verts.Add(xf.MultiplyPoint3x4(data.Positions[i]));
                Vector3 wn = nxf.MultiplyVector(data.Normals[i]);
                norms.Add(wn.sqrMagnitude > 1e-12f ? wn.normalized : Vector3.up);
                uvs.Add(data.Uvs[i]);
                colors.Add(data.Colors[i]);
            }
            // Una scala negativa (specchio) inverte il verso dei triangoli: lo correggiamo.
            bool mirrored = xf.determinant < 0f;
            for (int s = 0; s < data.SubmeshKeys.Length; s++)
            {
                List<int> list = tris[Sub(data.SubmeshKeys[s])];
                int[] t = data.Submeshes[s];
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    list.Add(baseIndex + t[i]);
                    if (mirrored) { list.Add(baseIndex + t[i + 2]); list.Add(baseIndex + t[i + 1]); }
                    else { list.Add(baseIndex + t[i + 1]); list.Add(baseIndex + t[i + 2]); }
                }
            }
            return true;
        }

        /// <summary>Come <see cref="Import(string, Matrix4x4)"/> con posizione, rotazione (gradi) e scala uniforme.</summary>
        public bool Import(string modelName, Vector3 pos, Vector3 euler = default(Vector3), float scale = 1f)
        {
            return Import(modelName, Xf(pos, euler, Vector3.one * scale));
        }

        /// <summary>
        /// Aggiunge un triangolo orientandolo automaticamente verso l'esterno (in base alle normali dei vertici),
        /// così non servono calcoli di orientamento nelle singole forme. Ignora i triangoli degeneri.
        /// </summary>
        private void AddTri(int sub, int a, int b, int c)
        {
            Vector3 pa = verts[a], pb = verts[b], pc = verts[c];
            Vector3 cross = Vector3.Cross(pb - pa, pc - pa);
            if (cross.sqrMagnitude < 1e-14f) return;
            Vector3 nsum = norms[a] + norms[b] + norms[c];
            List<int> list = tris[sub];
            if (Vector3.Dot(cross, nsum) < 0f)
            {
                list.Add(a); list.Add(c); list.Add(b);
            }
            else
            {
                list.Add(a); list.Add(b); list.Add(c);
            }
        }

        /// <summary>Crea la mesh Unity con un sotto-mesh per materiale.</summary>
        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.subMeshCount = tris.Count;
            for (int i = 0; i < tris.Count; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
