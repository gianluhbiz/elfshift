using System.Collections.Generic;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Trasforma i <see cref="MeshBuilder"/> in oggetti di scena (MeshFilter + MeshRenderer),
    /// salvando le mesh come asset in Assets/Models/Generated.
    /// </summary>
    public static class ModelKit
    {
        /// <summary>Nome del modello in costruzione: serve a dare nomi chiari ai file delle mesh.</summary>
        public static string CurrentModel = "Model";

        public static void Begin(string modelName)
        {
            CurrentModel = modelName;
        }

        public static Transform Empty(Transform parent, string name, Vector3 localPos, Vector3 localEuler = default(Vector3))
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);
            return go.transform;
        }

        /// <summary>Crea un oggetto con la mesh del builder e i materiali della tavolozza.</summary>
        public static GameObject Part(Transform parent, string name, MeshBuilder builder, Vector3 localPos = default(Vector3), Vector3 localEuler = default(Vector3))
        {
            return CreatePart(parent, name, builder, localPos, localEuler, null);
        }

        /// <summary>Come Part, ma con un materiale diverso da quelli della tavolozza (es. texture).</summary>
        public static GameObject PartWithMaterial(Transform parent, string name, MeshBuilder builder, Material material, Vector3 localPos = default(Vector3))
        {
            return CreatePart(parent, name, builder, localPos, Vector3.zero, material);
        }

        private static GameObject CreatePart(Transform parent, string name, MeshBuilder builder, Vector3 localPos, Vector3 localEuler, Material overrideMaterial)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(localEuler);

            Mesh mesh = builder.ToMesh(CurrentModel + "_" + name);
            AssetUtil.SaveMesh(mesh, AssetUtil.GeneratedModels, CurrentModel + "_" + name);

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            Material[] materials = new Material[builder.MaterialKeys.Count];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = overrideMaterial != null ? overrideMaterial : MaterialLibrary.Get(builder.MaterialKeys[i]);
            renderer.sharedMaterials = materials;
            return go;
        }

        public static BoxCollider AddBoxCollider(GameObject go, Vector3 center, Vector3 size)
        {
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return box;
        }

        /// <summary>Calcola i limiti (nel sistema locale di "root") di tutti i renderer sotto di esso.</summary>
        public static Bounds LocalBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            bool first = true;
            Bounds result = new Bounds(Vector3.zero, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds b = renderers[i].bounds;
                Vector3 min = root.InverseTransformPoint(b.min);
                Vector3 max = root.InverseTransformPoint(b.max);
                Bounds local = new Bounds((min + max) * 0.5f, Vector3.zero);
                local.Encapsulate(min);
                local.Encapsulate(max);
                if (first) { result = local; first = false; }
                else result.Encapsulate(local);
            }
            return result;
        }
    }
}
