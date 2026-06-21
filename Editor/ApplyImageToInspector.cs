using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ApplyImageToInspector : EditorWindow
{
    static Texture2D _texture;

    static readonly List<Texture2D> _slices = new();
    static readonly List<int> _hierarchyObjects = new();
    static float _textureAlpha = 0.1f;
    static bool _isApplyPicture;

    static HashSet<int> CachedExpandedHierarchyWindowIDs;

        public static HashSet<int> GetExpandedHierarchyWindowIDs()
    {
        var expandedIDs = new HashSet<int>();

        var hierarchyWindowType = typeof(EditorWindow).Assembly
            .GetType("UnityEditor.SceneHierarchyWindow");

        if (hierarchyWindowType == null) return expandedIDs;

        // 1. 현재 활성화된 Hierarchy 창 가져오기
        var lastInteractedProperty = hierarchyWindowType.GetProperty(
            "lastInteractedHierarchyWindow",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        var hierarchyWindow = lastInteractedProperty?.GetValue(null) as EditorWindow;
        if (hierarchyWindow == null) return expandedIDs;

#if UNITY_6000_0_OR_NEWER
        // ─── Unity 6 이상 처리 방식 ───
        // Unity 6에서는 m_SceneHierarchy 대신 프로퍼티나 다른 이름으로 래핑되어 있을 수 있습니다.
        // 가장 안전한 방법은 대소문자나 바뀐 필드명을 유연하게 찾는 것입니다.
        var sceneHierarchyProperty = hierarchyWindowType.GetProperty(
            "sceneHierarchy",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        object sceneHierarchy = null;
        if (sceneHierarchyProperty != null)
        {
            sceneHierarchy = sceneHierarchyProperty.GetValue(hierarchyWindow);
        }
        else
        {
            // 프로퍼티가 없다면 필드로 우회 (Unity 6 특정 서브패치 대응)
            var sceneHierarchyField = hierarchyWindowType.GetField(
                "m_SceneHierarchy",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            sceneHierarchy = sceneHierarchyField?.GetValue(hierarchyWindow);
        }

#else
    // ─── Unity 2022 이하 구버전 처리 방식 ───
    var sceneHierarchyField = hierarchyWindowType.GetField(
        "m_SceneHierarchy",
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    var sceneHierarchy = sceneHierarchyField?.GetValue(hierarchyWindow);
#endif

        if (sceneHierarchy == null) return expandedIDs;

        // 2. SceneHierarchy 클래스 타입 가져오기
        var sceneHierarchyType = typeof(EditorWindow).Assembly
            .GetType("UnityEditor.SceneHierarchy");

        if (sceneHierarchyType == null) return expandedIDs;

        // 3. 메서드 호출
        var getExpandedMethod = sceneHierarchyType.GetMethod(
            "GetExpandedGameObjects",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (getExpandedMethod != null)
        {
            var resultList = getExpandedMethod.Invoke(sceneHierarchy, null) as List<GameObject>;
            if (resultList != null)
            {
                foreach (var go in resultList)
                {
                    if (go != null) expandedIDs.Add(go.GetInstanceID());
                }
            }
        }

        return expandedIDs;
    }
    

    public static EditorWindow HeiraracyWindow
    {
        get
        {
            var type =
                typeof(EditorWindow).Assembly.GetType(
                    "UnityEditor.SceneHierarchyWindow");

            if (type == null)
                return null;
             
            var property =
                type.GetProperty(
                    "lastInteractedHierarchyWindow",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static);

            return property?.GetValue(null) as EditorWindow;
        }
    }

    [MenuItem("Tools/Apply Image To Hierarchy")]
    static void Open()
    {
        GetWindow<ApplyImageToInspector>("Hierarchy Background");
    }

    private void OnEnable()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

        EditorApplication.hierarchyChanged -= CreateTexture;
        EditorApplication.hierarchyChanged += CreateTexture;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
        EditorApplication.hierarchyChanged -= CreateTexture;
    }

    private void Update()
    {
        if (CachedExpandedHierarchyWindowIDs == null) return;
   
        if(GetExpandedHierarchyWindowIDs().Count != CachedExpandedHierarchyWindowIDs.Count)
        {
            if (_texture)
            {
                if (_isApplyPicture)
                {
                    CreateTexture();
                }
            }
        }

    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        _texture = (Texture2D)EditorGUILayout.ObjectField(
            "Texture",
            _texture,
            typeof(Texture2D),
            false);

        GUILayout.Space(10);


        if (GUILayout.Button("Apply"))
        {
            _isApplyPicture = true;
            CreateTexture();
        }

        if (GUILayout.Button("Clear"))
        {
            _isApplyPicture = false;
            ClearTextures();
        }

        GUILayout.Space(10);

        GUILayout.Label($"Objects : {_hierarchyObjects.Count}");
        GUILayout.Label($"Slices : {_slices.Count}");

        GUILayout.Space(10);
        GUILayout.Label("Alpha");
        _textureAlpha =  GUILayout.HorizontalSlider(_textureAlpha, 0, 1);
        GUILayout.Space(10);
        EditorApplication.RepaintHierarchyWindow();
    }

    static void OnHierarchyGUI(int instanceID, Rect rect)
    {
        if (!_isApplyPicture) return;

        if (_slices.Count == 0)
            return;

        int index = _hierarchyObjects.IndexOf(instanceID);

        if (index < 0)
            return;

        if (index >= _slices.Count)
            return;

        var hierarchy = HeiraracyWindow;

        Rect reRect = new Rect(
            0,
            rect.y,
            hierarchy.position.width,
            rect.height);

        GUI.color = new Color(1, 1, 1, _textureAlpha);

        GUI.DrawTexture(
            reRect,
            _slices[index],
            ScaleMode.StretchToFill,
            true);

        GUI.color = new Color(1, 1, 1, 1);
    }

    static void RefreshHierarchyObjects()
    {
        _hierarchyObjects.Clear();

        var expandedIDs = GetExpandedHierarchyWindowIDs();

        Scene scene = SceneManager.GetActiveScene();

        foreach (var root in scene.GetRootGameObjects())
        {
            AddRecursive(root.transform, expandedIDs);
        }
        CachedExpandedHierarchyWindowIDs = expandedIDs;

    }

    static void AddRecursive(
     Transform tr,
     HashSet<int> expandedIDs)
    {
        _hierarchyObjects.Add(tr.gameObject.GetInstanceID());

        // 접혀있으면 여기서 종료
        if (!expandedIDs.Contains(tr.gameObject.GetInstanceID()))
            return;

        foreach (Transform child in tr)
        {
            AddRecursive(child, expandedIDs);
        }
    }

    static Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt =
            RenderTexture.GetTemporary(width, height);

        Graphics.Blit(source, rt);

        RenderTexture previous =
            RenderTexture.active;

        RenderTexture.active = rt;

        Texture2D result =
            new Texture2D(
                width,
                height,
                TextureFormat.RGBA32,
                false);

        result.ReadPixels(
            new Rect(0, 0, width, height),
            0,
            0);

        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    static void CreateTexture()
    {
        if (_texture == null)
            return;

        RefreshHierarchyObjects();

        int count = _hierarchyObjects.Count;

        if (count <= 0)
            return;

        ClearTextures();

        Texture2D resized =
            ResizeTexture(
                _texture,
                _texture.width,
                count);

        for (int i = 0; i < count; i++)
        {
            Texture2D slice =
                new Texture2D(
                    resized.width,
                    1,
                    TextureFormat.RGBA32,
                    false);

            slice.filterMode = FilterMode.Point;
            slice.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels =
                resized.GetPixels(
                    0,
                    count - i - 1,
                    resized.width,
                    1);

            slice.SetPixels(pixels);
            slice.Apply();

            _slices.Add(slice);
        }

        DestroyImmediate(resized);
    }

    static void ClearTextures()
    {
        foreach (var tex in _slices)
        {
            if (tex != null)
                DestroyImmediate(tex);
        }

        _slices.Clear();
    }

    
}
