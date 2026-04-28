using UnityEditor;
using System.IO;
using UnityEngine;

/// <summary>
/// Unity 项目目录一键生成工具
/// 规范：Shaders、Materials、Audio、Art、Prefabs、Textures 放在 Resources 下
/// </summary>
public static class ProjectDirectoryGenerator
{
    // 根目录
    private static readonly string Root = "Assets";

    // 你要的目录结构（全部按你的要求排列）
    private static readonly string[] DirectoryList =
    {
        // 一级目录
        "Scenes",
        "Scripts",
        "Config",
        "Data",
        "Plugins",
        "Editor",
        "StreamingAssets",

        // Resources 下的资源目录（你指定的 6 个）
        "Resources/Shaders",
        "Resources/Materials",
        "Resources/Audio",
        "Resources/Art",
        "Resources/Prefabs",
        "Resources/Textures",
        "Resources/UI",
        "Resources/Fonts"
    };

    [MenuItem("Tools/生成项目标准目录", false, 10)]
    public static void Generate()
    {
        bool hasExisting = false;

        foreach (var dir in DirectoryList)
        {
            string path = Path.Combine(Root, dir);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"<color=cyan>创建：{path}</color>");
            }
            else
            {
                hasExisting = true;
                Debug.Log($"<color=grey>已存在：{path}</color>");
            }
        }

        AssetDatabase.Refresh();

        if (hasExisting)
            EditorUtility.DisplayDialog("完成", "部分目录已存在，未重复创建", "OK");
        else
            EditorUtility.DisplayDialog("成功", "所有目录已生成完成！", "OK");
    }
}