using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Legge i modelli scolpiti in Blender e salvati nel formato semplice "EMSH" (file .bytes in Assets/Models/Source).
    /// I file sono prodotti dagli script in Tools/Blender: contengono vertici già nel sistema di Unity
    /// (Y in alto, Z in avanti, metri), normali, coordinate UV, ombreggiatura nei colori dei vertici
    /// e un gruppo di triangoli per ogni materiale (con i nomi della tavolozza, es. "WoodMid").
    ///
    /// Formato (little endian):
    ///   "EMSH" | int versione (1 = dati in chiaro, 2 = il resto del file è compresso con gzip) | int nVertici
    ///   nVertici x (float3 posizione) | nVertici x (float3 normale) | nVertici x (float2 uv) | nVertici x (byte4 colore RGBA)
    ///   int nGruppi | per gruppo: int lunghezzaNome, byte[] nome UTF8, int nIndici, int[] indici
    /// </summary>
    public sealed class ImportedMesh
    {
        public const string SourceFolder = "Assets/Models/Source";

        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector2[] Uvs;
        public Color[] Colors;
        public string[] SubmeshKeys;
        public int[][] Submeshes;

        private static readonly Dictionary<string, ImportedMesh> cache = new Dictionary<string, ImportedMesh>();

        /// <summary>Svuota la memoria dei modelli letti (a ogni nuova costruzione del laboratorio).</summary>
        public static void ClearCache()
        {
            cache.Clear();
        }

        public static string PathOf(string modelName)
        {
            return SourceFolder + "/" + modelName + ".bytes";
        }

        public static bool Exists(string modelName)
        {
            return File.Exists(PathOf(modelName));
        }

        /// <summary>Carica il modello (o null se il file manca o è rovinato: in quel caso scrive un avviso).</summary>
        public static ImportedMesh Load(string modelName)
        {
            ImportedMesh data;
            if (cache.TryGetValue(modelName, out data)) return data;

            string path = PathOf(modelName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Elfshift] Modello Blender mancante: " + path + " (uso la forma di riserva).");
                cache[modelName] = null;
                return null;
            }

            try
            {
                data = Read(File.ReadAllBytes(path));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Elfshift] Modello Blender non leggibile: " + path + " (" + e.Message + ")");
                data = null;
            }
            cache[modelName] = data;
            return data;
        }

        private static ImportedMesh Read(byte[] bytes)
        {
            if (bytes.Length < 8 || Encoding.ASCII.GetString(bytes, 0, 4) != "EMSH") throw new IOException("intestazione non valida");
            int version = System.BitConverter.ToInt32(bytes, 4);
            if (version == 1) return ReadBody(new MemoryStream(bytes, 8, bytes.Length - 8));
            if (version == 2)
            {
                // versione compressa: decomprimo tutto in memoria e leggo come la versione 1
                using (GZipStream zip = new GZipStream(new MemoryStream(bytes, 8, bytes.Length - 8), CompressionMode.Decompress))
                using (MemoryStream plain = new MemoryStream())
                {
                    zip.CopyTo(plain);
                    plain.Position = 0;
                    return ReadBody(plain);
                }
            }
            throw new IOException("versione non supportata " + version);
        }

        private static ImportedMesh ReadBody(Stream stream)
        {
            using (BinaryReader r = new BinaryReader(stream))
            {
                int n = r.ReadInt32();
                if (n < 0 || n > 2000000) throw new IOException("numero di vertici non valido");
                ImportedMesh m = new ImportedMesh();
                m.Positions = new Vector3[n];
                m.Normals = new Vector3[n];
                m.Uvs = new Vector2[n];
                m.Colors = new Color[n];
                for (int i = 0; i < n; i++) m.Positions[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < n; i++) m.Normals[i] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < n; i++) m.Uvs[i] = new Vector2(r.ReadSingle(), r.ReadSingle());
                for (int i = 0; i < n; i++)
                {
                    byte cr = r.ReadByte(), cg = r.ReadByte(), cb = r.ReadByte(), ca = r.ReadByte();
                    m.Colors[i] = new Color(cr / 255f, cg / 255f, cb / 255f, ca / 255f);
                }

                int groups = r.ReadInt32();
                m.SubmeshKeys = new string[groups];
                m.Submeshes = new int[groups][];
                for (int g = 0; g < groups; g++)
                {
                    int len = r.ReadInt32();
                    m.SubmeshKeys[g] = Encoding.UTF8.GetString(r.ReadBytes(len));
                    int count = r.ReadInt32();
                    int[] idx = new int[count];
                    for (int i = 0; i < count; i++)
                    {
                        idx[i] = r.ReadInt32();
                        if (idx[i] < 0 || idx[i] >= n) throw new IOException("indice fuori dai limiti");
                    }
                    m.Submeshes[g] = idx;
                }
                return m;
            }
        }
    }
}
