using UnityEditor;
using UnityEngine;

namespace Elfshift.EditorTools
{
    /// <summary>
    /// Piccolo aiuto per riempire i campi [SerializeField] privati dei componenti dagli strumenti
    /// editor (è lo stesso meccanismo dell'Inspector, quindi i valori vengono salvati nella scena).
    /// </summary>
    public static class Wire
    {
        private static SerializedProperty Find(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null)
                Debug.LogError("[Elfshift] Campo '" + field + "' non trovato su " + target.GetType().Name + ".", target);
            return p;
        }

        public static void Ref(Object target, string field, Object value)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Refs(Object target, string field, Object[] values)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Float(Object target, string field, float value)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Int(Object target, string field, int value)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Enum(Object target, string field, int enumIndex)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.enumValueIndex = enumIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Vec2(Object target, string field, Vector2 value)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Vec3(Object target, string field, Vector3 value)
        {
            SerializedObject so;
            SerializedProperty p = Find(target, field, out so);
            if (p == null) return;
            p.vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
