using UnityEditor;
using UnityEngine;

namespace DeliveryBot.EditorTools
{
    /// <summary>Shared helpers for the editor-time factories that assemble the world from primitives.</summary>
    public static class BuildKit
    {
        public const string Root = "Assets/_Project";

        public static void EnsureFolder(string sub)
        {
            if (!AssetDatabase.IsValidFolder($"{Root}/{sub}")) AssetDatabase.CreateFolder(Root, sub);
        }

        /// <summary>Loads or creates a material asset. Uses URP Lit when present, else Standard.</summary>
        public static Material Mat(string name, Color color, bool emissive = false, bool transparent = false)
        {
            var path = $"{Root}/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
            }
            if (transparent) MakeStandardTransparent(mat);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void MakeStandardTransparent(Material mat)
        {
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        /// <summary>Creates a primitive as a child with local transform + material. Collider removed unless requested.</summary>
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale,
            Material mat, bool keepCollider = false, Quaternion? localRot = null, bool isStatic = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.isStatic = isStatic;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot ?? Quaternion.identity;
            go.transform.localScale = localScale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Node(string name, Transform parent, Vector3 localPos, bool isStatic = false)
        {
            var go = new GameObject(name) { isStatic = isStatic };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        /// <summary>Sets a private [SerializeField] on a component through SerializedObject.</summary>
        public static void SetField(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[BuildKit] Field '{field}' not found on {target.GetType().Name}");
                return;
            }
            switch (value)
            {
                case Object obj: prop.objectReferenceValue = obj; break;
                case Object[] arr:
                    prop.arraySize = arr.Length;
                    for (var i = 0; i < arr.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                    break;
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                case Color c: prop.colorValue = c; break;
                case Vector3 v: prop.vector3Value = v; break;
                default: Debug.LogWarning($"[BuildKit] Unsupported value type for '{field}'"); break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop != null) prop.enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureTag(string tag)
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (asset == null || asset.Length == 0) return;
            var so = new SerializedObject(asset[0]);
            var tags = so.FindProperty("tags");
            for (var i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static GameObject SavePrefab(GameObject go, string name)
        {
            EnsureFolder("Prefabs");
            var path = $"{Root}/Prefabs/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }
    }
}
