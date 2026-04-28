using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class ShaderEditor : EditorWindow
{
    private const string DefaultShaderFolder = "Assets/Resources/Shaders";
    private const string DefaultMaterialFolder = "Assets/Resources/Materials";
    private const string DefaultSharedHlslFolder = "Assets/Editor/LuminChen-ShaderTemplate/HLSL";
    private const string SharedHlslPluginsRoot = "Assets/ShaderPlugins";
    private const string SharedHlslPluginFolder = "Assets/ShaderPlugins/LuminChen-ShaderTemplate/HLSL";
    private const string SharedHlslFileName = "LuminChenShaderCommon.hlsl";
    private static readonly string[] ShaderTypeLabels = { "3D Template", "Compute Shader" };
    private const string NewCategoryOption = "<New Category...>";

    private enum ShaderTemplateType
    {
        URP3DTemplate,
        ComputeShader
    }

    private enum SurfaceType
    {
        Opaque,
        Transparent
    }

    private enum BlendMode
    {
        Alpha,
        Additive,
        Premultiply,
        Multiply
    }

    private enum DepthTestMode
    {
        LessEqual,
        Less,
        Greater,
        GreaterEqual,
        Equal,
        NotEqual,
        Always
    }

    private enum CullMode
    {
        Back,
        Front,
        Off
    }

    private enum SurfaceWorkflow
    {
        Metallic,
        Specular
    }

    private string shaderName = "New3DTemplateShader";
    private string shaderOutputFolder = DefaultShaderFolder;
    private string materialOutputFolder = DefaultMaterialFolder;
    private ShaderTemplateType templateType = ShaderTemplateType.URP3DTemplate;
    private SurfaceType surfaceType = SurfaceType.Opaque;
    private BlendMode blendMode = BlendMode.Alpha;
    private DepthTestMode depthTestMode = DepthTestMode.LessEqual;
    private CullMode cullMode = CullMode.Back;
    private SurfaceWorkflow surfaceWorkflow = SurfaceWorkflow.Metallic;
    private bool useMainTexture = true;
    private bool useTintColor = true;
    private bool useNormalMap;
    private bool useMaskMap;
    private bool useMetallicMap;
    private bool useSpecularMap;
    private bool useRoughnessMap;
    private bool useOcclusionMap;
    private bool useEmissionMap;
    private bool useHeightMap;
    private bool enableDepthWrite = true;
    private bool enableAlphaClip;
    private float alphaCutoff = 0.5f;
    private bool hasPromptedForDefaultFolder;
    private string selectedShaderCategory = "Custom";
    private readonly List<string> customShaderCategories = new List<string>();
    private Vector2 scrollPosition;
    private Vector2 previewScrollPosition;
    private bool showOutput = true;
    private bool showOptions = true;
    private bool showPreview;
    private bool showTextureOptions;
    private bool showRenderStateOptions;
    private bool hasShownSharedHlslStatusThisOpen;

    private class ShaderCategoryInputWindow : EditorWindow
    {
        private string categoryName = string.Empty;
        private System.Action<string> onConfirm;

        public static void Open(System.Action<string> onConfirmCallback)
        {
            ShaderCategoryInputWindow window = CreateInstance<ShaderCategoryInputWindow>();
            window.titleContent = new GUIContent("New Category");
            window.minSize = new Vector2(320f, 90f);
            window.maxSize = new Vector2(320f, 90f);
            window.onConfirm = onConfirmCallback;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Shader Category", EditorStyles.boldLabel);
            GUI.SetNextControlName("CategoryField");
            categoryName = EditorGUILayout.TextField(categoryName);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(80f)))
                {
                    Close();
                }

                EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(categoryName));
                if (GUILayout.Button("Create", GUILayout.Width(80f)))
                {
                    onConfirm?.Invoke(categoryName);
                    Close();
                }
                EditorGUI.EndDisabledGroup();
            }

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.FocusTextInControl("CategoryField");
            }
        }
    }

    [MenuItem("Tools/Shader Template Editor")]
    public static void OpenWindow()
    {
        ShaderEditor window = GetWindow<ShaderEditor>("Shader Template");
        window.minSize = new Vector2(520f, 480f);
        window.EnsureOutputFolderState();
    }

    private void OnEnable()
    {
        EnsureOutputFolderState();
    }
}
