using UnityEditor;
using UnityEngine;

public partial class ShaderEditor : EditorWindow
{
    private void OnGUI()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Shader Template Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Generate 3D template shaders or compute shaders from configurable editor options.");
        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Sync HLSL To ShaderPlugins", GUILayout.Width(220f), GUILayout.Height(24f)))
            {
                SyncSharedHlslFiles();
            }

            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.Space(8f);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            showOutput = EditorGUILayout.Foldout(showOutput, "Output", true);
            if (showOutput)
            {
                shaderName = EditorGUILayout.TextField("Shader Name", shaderName);
                shaderOutputFolder = EditorGUILayout.TextField("Shader Folder", shaderOutputFolder);
                if (templateType == ShaderTemplateType.URP3DTemplate)
                {
                    materialOutputFolder = EditorGUILayout.TextField("Material Folder", materialOutputFolder);
                }

                if (templateType == ShaderTemplateType.URP3DTemplate)
                {
                    DrawShaderCategoryControls();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Choose Shader Folder", GUILayout.Width(140f)))
                    {
                        SelectOutputFolder(true);
                        GUI.FocusControl(null);
                    }

                    if (templateType == ShaderTemplateType.URP3DTemplate &&
                        GUILayout.Button("Choose Material Folder", GUILayout.Width(150f)))
                    {
                        SelectOutputFolder(false);
                        GUI.FocusControl(null);
                    }

                    GUILayout.FlexibleSpace();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool hasDefaultShaderFolder = AssetDatabase.IsValidFolder(DefaultShaderFolder);
                    bool hasDefaultMaterialFolder = AssetDatabase.IsValidFolder(DefaultMaterialFolder);
                    bool hasAllDefaultFolders = hasDefaultShaderFolder && hasDefaultMaterialFolder;

                    if (!hasAllDefaultFolders)
                    {
                        if (GUILayout.Button("Create Default Folders", GUILayout.Width(160f)))
                        {
                            CreateDefaultFolder();
                            shaderOutputFolder = DefaultShaderFolder;
                            if (templateType == ShaderTemplateType.URP3DTemplate)
                            {
                                materialOutputFolder = DefaultMaterialFolder;
                            }

                            GUI.FocusControl(null);
                        }
                    }
                    else if (GUILayout.Button("Use Default Folders", GUILayout.Width(160f)))
                    {
                        shaderOutputFolder = DefaultShaderFolder;
                        if (templateType == ShaderTemplateType.URP3DTemplate)
                        {
                            materialOutputFolder = DefaultMaterialFolder;
                        }

                        GUI.FocusControl(null);
                    }

                    GUILayout.FlexibleSpace();
                }

                if (templateType == ShaderTemplateType.URP3DTemplate &&
                    (!AssetDatabase.IsValidFolder(DefaultShaderFolder) || !AssetDatabase.IsValidFolder(DefaultMaterialFolder)))
                {
                    EditorGUILayout.HelpBox(
                        "Default folders Assets/Resources/Shaders and Assets/Resources/Materials do not both exist. Create them or choose other folders under Assets.",
                        MessageType.Info);
                }
                else if (!AssetDatabase.IsValidFolder(DefaultShaderFolder))
                {
                    EditorGUILayout.HelpBox(
                        "Default folder Assets/Resources/Shaders does not exist. Create it or choose another folder under Assets.",
                        MessageType.Info);
                }
            }
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            showOptions = EditorGUILayout.Foldout(showOptions, "Options", true);
            if (showOptions)
            {
                EditorGUI.BeginChangeCheck();
                int selectedTemplateIndex = EditorGUILayout.Popup("Shader Type", (int)templateType, ShaderTypeLabels);
                templateType = (ShaderTemplateType)selectedTemplateIndex;
                if (EditorGUI.EndChangeCheck())
                {
                    ApplyTemplateDefaults();
                }

                if (templateType == ShaderTemplateType.ComputeShader)
                {
                    EditorGUILayout.HelpBox("Compute Shader will generate a basic kernel with RWTexture2D output.", MessageType.None);
                }
                else
                {
                    Draw3DTemplateOptions();
                }
            }
        }

        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            showPreview = EditorGUILayout.Foldout(showPreview, "Preview", true);
            if (showPreview)
            {
                string preview = BuildShaderSource();
                GUIStyle previewStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = false };

                previewScrollPosition = EditorGUILayout.BeginScrollView(
                    previewScrollPosition,
                    GUILayout.MinHeight(260f),
                    GUILayout.MaxHeight(260f));
                EditorGUILayout.TextArea(preview, previewStyle, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.Space(12f);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            bool canGenerateShader = IsInputValid(false, out string shaderValidationError);
            GUI.enabled = canGenerateShader;
            if (GUILayout.Button("Generate Shader", GUILayout.Width(180f), GUILayout.Height(32f)))
            {
                CreateShaderAsset(false);
            }

            string validationError = shaderValidationError;
            if (templateType == ShaderTemplateType.URP3DTemplate)
            {
                GUILayout.Space(8f);

                bool canGenerateShaderAndMaterial = IsInputValid(true, out string materialValidationError);
                GUI.enabled = canGenerateShaderAndMaterial;
                if (GUILayout.Button("Generate Shader + Material", GUILayout.Width(220f), GUILayout.Height(32f)))
                {
                    CreateShaderAsset(true);
                }

                if (!canGenerateShaderAndMaterial)
                {
                    validationError = materialValidationError;
                }
            }

            GUI.enabled = true;
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(validationError))
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void Draw3DTemplateOptions()
    {
        surfaceType = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", surfaceType);
        surfaceWorkflow = (SurfaceWorkflow)EditorGUILayout.EnumPopup("Workflow", surfaceWorkflow);

        showTextureOptions = EditorGUILayout.Foldout(showTextureOptions, "Texture Inputs", true);
        if (showTextureOptions)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                useMainTexture = EditorGUILayout.Toggle("Main Texture", useMainTexture);
                useTintColor = EditorGUILayout.Toggle("Tint Color", useTintColor);
                useNormalMap = EditorGUILayout.Toggle("Normal Map", useNormalMap);
                useMaskMap = EditorGUILayout.Toggle("Mask Map", useMaskMap);

                if (surfaceWorkflow == SurfaceWorkflow.Metallic)
                {
                    useMetallicMap = EditorGUILayout.Toggle("Metallic Map", useMetallicMap);
                }
                else
                {
                    useSpecularMap = EditorGUILayout.Toggle("Specular Map", useSpecularMap);
                }

                useRoughnessMap = EditorGUILayout.Toggle("Roughness Map", useRoughnessMap);
                useOcclusionMap = EditorGUILayout.Toggle("Occlusion Map", useOcclusionMap);
                useEmissionMap = EditorGUILayout.Toggle("Emission Map", useEmissionMap);
                useHeightMap = EditorGUILayout.Toggle("Height Map", useHeightMap);
            }
        }

        showRenderStateOptions = EditorGUILayout.Foldout(showRenderStateOptions, "Render State", true);
        if (showRenderStateOptions)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                enableAlphaClip = EditorGUILayout.Toggle("Alpha Clipping", enableAlphaClip);
                if (enableAlphaClip)
                {
                    alphaCutoff = EditorGUILayout.Slider("Alpha Cutoff", alphaCutoff, 0f, 1f);
                }

                enableDepthWrite = EditorGUILayout.Toggle("Depth Write", enableDepthWrite);
                depthTestMode = (DepthTestMode)EditorGUILayout.EnumPopup("Depth Test", depthTestMode);
                cullMode = (CullMode)EditorGUILayout.EnumPopup("Cull Mode", cullMode);

                if (surfaceType == SurfaceType.Transparent)
                {
                    blendMode = (BlendMode)EditorGUILayout.EnumPopup("Alpha Blend", blendMode);
                }
            }
        }
    }
}
