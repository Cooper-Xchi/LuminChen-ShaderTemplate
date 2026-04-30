using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class ShaderEditor : EditorWindow
{
    private bool IsInputValid(bool requiresMaterialFolder, out string message)
    {
        if (string.IsNullOrWhiteSpace(shaderName))
        {
            message = "Shader Name is required.";
            return false;
        }

        if (SupportsShaderCategory() && string.IsNullOrWhiteSpace(GetActiveShaderCategory()))
        {
            message = "Shader Category is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(shaderOutputFolder) || !shaderOutputFolder.StartsWith("Assets"))
        {
            message = "Shader Folder must be under Assets.";
            return false;
        }

        if (!AssetDatabase.IsValidFolder(shaderOutputFolder))
        {
            message = "Shader Folder must already exist under Assets.";
            return false;
        }

        if (requiresMaterialFolder && (string.IsNullOrWhiteSpace(materialOutputFolder) || !materialOutputFolder.StartsWith("Assets")))
        {
            message = "Material Folder must be under Assets.";
            return false;
        }

        if (requiresMaterialFolder && !AssetDatabase.IsValidFolder(materialOutputFolder))
        {
            message = "Material Folder must already exist under Assets.";
            return false;
        }

        message = null;
        return true;
    }

    private void CreateShaderAsset(bool createMaterial)
    {
        if (!IsInputValid(createMaterial, out string validationError))
        {
            EditorUtility.DisplayDialog("Invalid Input", validationError, "OK");
            return;
        }

        string shaderFolderPath = shaderOutputFolder.TrimEnd('/');
        Directory.CreateDirectory(shaderFolderPath);

        string shaderFilePath = Path.Combine(shaderFolderPath, shaderName + GetFileExtension());
        bool shaderExists = File.Exists(shaderFilePath);
        if (shaderExists)
        {
            bool shouldReplaceShader = EditorUtility.DisplayDialog(
                "Shader Already Exists",
                "The shader already exists:\n" + shaderFilePath + "\n\nReplace it?",
                "Replace",
                "Cancel");

            if (!shouldReplaceShader)
            {
                return;
            }
        }

        string materialAssetPath = null;
        string materialResultMessage = string.Empty;
        if (createMaterial && SupportsMaterialGeneration())
        {
            MaterialGenerationDecision materialDecision = ResolveMaterialGenerationDecision();
            if (materialDecision.IsCanceled)
            {
                return;
            }

            materialAssetPath = materialDecision.AssetPath;
            materialResultMessage = materialDecision.ResultMessage;
        }

        File.WriteAllText(shaderFilePath, BuildShaderSource(), Encoding.UTF8);

        AssetDatabase.Refresh();

        Object createdAsset = AssetDatabase.LoadAssetAtPath<Object>(shaderFilePath);
        if (createdAsset != null)
        {
            EditorGUIUtility.PingObject(createdAsset);
            Selection.activeObject = createdAsset;
        }

        string dialogMessage = (shaderExists ? "Replaced shader at:\n" : "Created shader at:\n") + shaderFilePath;

        if (createMaterial && SupportsMaterialGeneration())
        {
            if (!string.IsNullOrEmpty(materialAssetPath))
            {
                CreateMaterialAsset(shaderFilePath, materialAssetPath);
            }

            if (!string.IsNullOrEmpty(materialResultMessage))
            {
                dialogMessage += "\n\n" + materialResultMessage;
            }
        }

        EditorUtility.DisplayDialog("Shader Created", dialogMessage, "OK");
    }

    private static string SanitizeShaderPath(string value)
    {
        string trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return "New3DTemplateShader";
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            trimmed = trimmed.Replace(invalidChar.ToString(), string.Empty);
        }

        return trimmed.Replace('\\', '/');
    }

    private static string SanitizeShaderCategory(string value)
    {
        string trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        string[] segments = trimmed.Replace('\\', '/').Split('/');
        List<string> sanitizedSegments = new List<string>();

        foreach (string segment in segments)
        {
            string cleanSegment = segment.Trim();
            if (string.IsNullOrEmpty(cleanSegment))
            {
                continue;
            }

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                cleanSegment = cleanSegment.Replace(invalidChar.ToString(), string.Empty);
            }

            if (!string.IsNullOrEmpty(cleanSegment))
            {
                sanitizedSegments.Add(cleanSegment);
            }
        }

        return string.Join("/", sanitizedSegments);
    }

    private string GetFileExtension()
    {
        return templateType == ShaderTemplateType.ComputeShader ? ".compute" : ".shader";
    }

    private bool SupportsMaterialGeneration()
    {
        return templateType != ShaderTemplateType.ComputeShader;
    }

    private bool SupportsShaderCategory()
    {
        return templateType != ShaderTemplateType.ComputeShader;
    }

    private string GetSharedHlslFolder()
    {
        string pluginFilePath = SharedHlslPluginFolder + "/Core/" + SharedHlslFileName;
        if (File.Exists(pluginFilePath))
        {
            return SharedHlslPluginFolder;
        }

        return DefaultSharedHlslFolder;
    }

    private string GetSharedHlslIncludePath()
    {
        return GetSharedHlslFolder() + "/" + SharedHlslFileName;
    }

    private string BuildSharedHlslIncludeBlock()
    {
        string sharedFolder = GetSharedHlslFolder();
        string fullSharedFolderPath = Path.GetFullPath(sharedFolder);
        if (!Directory.Exists(fullSharedFolderPath))
        {
            return string.Empty;
        }

        string[] includeFiles = Directory.GetFiles(fullSharedFolderPath, "*.hlsl", SearchOption.AllDirectories);
        if (includeFiles.Length == 0)
        {
            return string.Empty;
        }

        List<string> assetPaths = new List<string>();
        foreach (string includeFile in includeFiles)
        {
            string relativePath = includeFile.Replace('\\', '/').Substring(fullSharedFolderPath.Replace('\\', '/').Length).TrimStart('/');
            assetPaths.Add(sharedFolder + "/" + relativePath);
        }

        assetPaths.Sort((left, right) =>
        {
            bool leftIsCommon = left.EndsWith("/Core/" + SharedHlslFileName);
            bool rightIsCommon = right.EndsWith("/Core/" + SharedHlslFileName);

            if (leftIsCommon && !rightIsCommon)
            {
                return -1;
            }

            if (!leftIsCommon && rightIsCommon)
            {
                return 1;
            }

            return string.CompareOrdinal(left, right);
        });

        StringBuilder includeBlock = new StringBuilder();
        foreach (string assetPath in assetPaths)
        {
            includeBlock.Append("            #include \"");
            includeBlock.Append(assetPath.Replace('\\', '/'));
            includeBlock.AppendLine("\"");
        }

        return includeBlock.ToString();
    }

    private void ApplyTemplateDefaults()
    {
        if (templateType == ShaderTemplateType.ComputeShader)
        {
            shaderName = "NewComputeShader";
            return;
        }

        if (templateType == ShaderTemplateType.URPPostProcess)
        {
            shaderName = "NewPostProcessShader";
            selectedShaderCategory = "Custom";
            return;
        }

        if (templateType == ShaderTemplateType.URPSkybox)
        {
            shaderName = "NewSkyboxShader";
            selectedShaderCategory = "Custom";
            return;
        }

        shaderName = "New3DTemplateShader";
        selectedShaderCategory = "Custom";
        surfaceType = SurfaceType.Opaque;
        blendMode = BlendMode.Alpha;
        depthTestMode = DepthTestMode.LessEqual;
        cullMode = CullMode.Back;
        surfaceWorkflow = SurfaceWorkflow.Metallic;
        useMainTexture = true;
        useTintColor = true;
        useNormalMap = false;
        useMaskMap = false;
        useMetallicMap = false;
        useSpecularMap = false;
        useRoughnessMap = false;
        useOcclusionMap = false;
        useEmissionMap = false;
        useHeightMap = false;
        enableDepthWrite = true;
        enableAlphaClip = false;
        alphaCutoff = 0.5f;
    }

    private void EnsureOutputFolderState()
    {
        bool hasDefaultShaderFolder = AssetDatabase.IsValidFolder(DefaultShaderFolder);
        bool hasDefaultMaterialFolder = AssetDatabase.IsValidFolder(DefaultMaterialFolder);

        if (hasDefaultShaderFolder && hasDefaultMaterialFolder)
        {
            if (string.IsNullOrWhiteSpace(shaderOutputFolder) || shaderOutputFolder == "Assets")
            {
                shaderOutputFolder = DefaultShaderFolder;
            }

            if (string.IsNullOrWhiteSpace(materialOutputFolder) || materialOutputFolder == "Assets")
            {
                materialOutputFolder = DefaultMaterialFolder;
            }

            return;
        }

        if (hasPromptedForDefaultFolder)
        {
            return;
        }

        hasPromptedForDefaultFolder = true;
        bool shouldCreate = EditorUtility.DisplayDialog(
            "Missing Default Folders",
            "The project does not contain both default folders:\nAssets/Resources/Shaders\nAssets/Resources/Materials\n\nCreate them now?",
            "Create",
            "Choose Manually");

        if (shouldCreate)
        {
            CreateDefaultFolder();
            shaderOutputFolder = DefaultShaderFolder;
            materialOutputFolder = DefaultMaterialFolder;
            return;
        }

        if (shaderOutputFolder == DefaultShaderFolder)
        {
            shaderOutputFolder = "Assets";
        }

        if (materialOutputFolder == DefaultMaterialFolder)
        {
            materialOutputFolder = "Assets";
        }
    }

    private void CreateDefaultFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(DefaultShaderFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Shaders");
        }

        if (!AssetDatabase.IsValidFolder(DefaultMaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Materials");
        }

        AssetDatabase.Refresh();
    }

    private void SelectOutputFolder(bool selectShaderFolder)
    {
        string absoluteAssetsPath = Path.GetFullPath(Application.dataPath);
        string currentFolder = selectShaderFolder ? shaderOutputFolder : materialOutputFolder;
        string initialFolder = AssetDatabase.IsValidFolder(currentFolder)
            ? Path.GetFullPath(currentFolder)
            : absoluteAssetsPath;

        string panelTitle = selectShaderFolder ? "Select Shader Output Folder" : "Select Material Output Folder";
        string selectedFolder = EditorUtility.OpenFolderPanel(panelTitle, initialFolder, string.Empty);
        if (string.IsNullOrEmpty(selectedFolder))
        {
            return;
        }

        string normalizedAssetsPath = absoluteAssetsPath.Replace('\\', '/');
        string normalizedSelectedFolder = Path.GetFullPath(selectedFolder).Replace('\\', '/');

        if (!normalizedSelectedFolder.StartsWith(normalizedAssetsPath))
        {
            EditorUtility.DisplayDialog("Invalid Folder", "Please choose a folder inside the project's Assets directory.", "OK");
            return;
        }

        string assetFolder = "Assets" + normalizedSelectedFolder.Substring(normalizedAssetsPath.Length);
        if (selectShaderFolder)
        {
            shaderOutputFolder = assetFolder;
        }
        else
        {
            materialOutputFolder = assetFolder;
        }
    }

    private void CreateMaterialAsset(string shaderFilePath, string materialAssetPath)
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderFilePath);
        if (shader == null)
        {
            return;
        }

        Material material = new Material(shader);
        AssetDatabase.CreateAsset(material, materialAssetPath);
        AssetDatabase.SaveAssets();
    }

    private MaterialGenerationDecision ResolveMaterialGenerationDecision()
    {
        string materialFolderPath = materialOutputFolder.TrimEnd('/');
        Directory.CreateDirectory(materialFolderPath);

        string materialAssetPath = Path.Combine(materialFolderPath, shaderName + ".mat").Replace('\\', '/');
        if (!File.Exists(materialAssetPath))
        {
            return new MaterialGenerationDecision
            {
                AssetPath = materialAssetPath,
                ResultMessage = "Created material at:\n" + materialAssetPath
            };
        }

        int choice = EditorUtility.DisplayDialogComplex(
            "Material Already Exists",
            "The material already exists:\n" + materialAssetPath,
            "Replace",
            "Skip",
            "Cancel");

        if (choice == 2)
        {
            return new MaterialGenerationDecision { IsCanceled = true };
        }

        if (choice == 1)
        {
            return new MaterialGenerationDecision
            {
                AssetPath = null,
                ResultMessage = "Kept existing material at:\n" + materialAssetPath
            };
        }

        AssetDatabase.DeleteAsset(materialAssetPath);
        return new MaterialGenerationDecision
        {
            AssetPath = materialAssetPath,
            ResultMessage = "Replaced material at:\n" + materialAssetPath
        };
    }

    private struct MaterialGenerationDecision
    {
        public string AssetPath;
        public string ResultMessage;
        public bool IsCanceled;
    }

    private void EnsureSharedHlslLocation()
    {
        EnsureFolderPath(DefaultSharedHlslFolder);
        EnsureFolderPath(SharedHlslPluginsRoot);

        string sourceFolderFullPath = Path.GetFullPath(DefaultSharedHlslFolder);
        if (!Directory.Exists(sourceFolderFullPath))
        {
            return;
        }

        string[] sourceFiles = Directory.GetFiles(sourceFolderFullPath, "*.hlsl", SearchOption.AllDirectories);
        if (sourceFiles.Length == 0)
        {
            return;
        }

        if (AssetDatabase.IsValidFolder(SharedHlslPluginFolder))
        {
            AssetDatabase.DeleteAsset(SharedHlslPluginFolder);
        }

        EnsureFolderPath(SharedHlslPluginFolder);
        string targetFolderFullPath = Path.GetFullPath(SharedHlslPluginFolder);

        foreach (string sourceFile in sourceFiles)
        {
            string relativePath = sourceFile.Replace('\\', '/').Substring(sourceFolderFullPath.Replace('\\', '/').Length).TrimStart('/');
            string targetFile = Path.Combine(targetFolderFullPath, relativePath);
            string targetDirectory = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(sourceFile, targetFile, true);
        }

        AssetDatabase.Refresh();

        if (sourceFiles.Length > 0)
        {
            ShowSharedHlslStatusOnce(
                "Initialization Complete",
                "Copied shared HLSL files to:\n" + SharedHlslPluginFolder);
        }
    }

    private void SyncSharedHlslFiles()
    {
        hasShownSharedHlslStatusThisOpen = false;
        EnsureSharedHlslLocation();
    }

    private void EnsureFolderPath(string assetFolderPath)
    {
        string normalizedPath = assetFolderPath.Replace('\\', '/');
        string[] segments = normalizedPath.Split('/');
        string currentPath = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = currentPath + "/" + segments[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, segments[i]);
            }

            currentPath = nextPath;
        }
    }

    private void ShowSharedHlslStatusOnce(string title, string message)
    {
        if (hasShownSharedHlslStatusThisOpen)
        {
            return;
        }

        hasShownSharedHlslStatusThisOpen = true;
        EditorUtility.DisplayDialog(title, message, "OK");
    }
}
