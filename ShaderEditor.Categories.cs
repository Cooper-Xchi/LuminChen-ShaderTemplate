using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class ShaderEditor : EditorWindow
{
    private void DrawShaderCategoryControls()
    {
        List<string> categories = GetShaderCategories();
        int selectedIndex = categories.IndexOf(selectedShaderCategory);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
            selectedShaderCategory = categories[selectedIndex];
        }

        int newIndex = EditorGUILayout.Popup("Shader Category", selectedIndex, categories.ToArray());
        string newSelection = categories[newIndex];
        if (newSelection == NewCategoryOption)
        {
            PromptAndAddShaderCategory();
        }
        else
        {
            selectedShaderCategory = newSelection;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            EditorGUI.BeginDisabledGroup(!customShaderCategories.Contains(selectedShaderCategory));
            if (GUILayout.Button("Delete Category", GUILayout.Width(120f)))
            {
                DeleteSelectedShaderCategory();
            }
            EditorGUI.EndDisabledGroup();
        }

        EditorGUILayout.LabelField("Shader Path", BuildShaderPath(), EditorStyles.miniLabel);
    }

    private List<string> GetShaderCategories()
    {
        HashSet<string> categories = new HashSet<string> { "Custom" };
        string[] shaderGuids = AssetDatabase.FindAssets("t:Shader");

        foreach (string guid in shaderGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null || string.IsNullOrWhiteSpace(shader.name))
            {
                continue;
            }

            int separatorIndex = shader.name.IndexOf('/');
            if (separatorIndex <= 0)
            {
                continue;
            }

            string category = shader.name.Substring(0, separatorIndex).Trim();
            if (!string.IsNullOrEmpty(category))
            {
                categories.Add(category);
            }
        }

        List<string> result = new List<string>(categories);
        result.AddRange(customShaderCategories);
        result = new List<string>(new HashSet<string>(result));
        result.Sort();
        result.Add(NewCategoryOption);
        return result;
    }

    private string GetActiveShaderCategory()
    {
        return SanitizeShaderCategory(selectedShaderCategory);
    }

    private string BuildShaderPath()
    {
        string category = GetActiveShaderCategory();
        string name = SanitizeShaderPath(shaderName);

        if (string.IsNullOrEmpty(category))
        {
            return name;
        }

        return category + "/" + name;
    }

    private void PromptAndAddShaderCategory()
    {
        ShaderCategoryInputWindow.Open(AddShaderCategory);
    }

    private void AddShaderCategory(string categoryName)
    {
        string sanitizedCategory = SanitizeShaderCategory(categoryName);
        if (string.IsNullOrWhiteSpace(sanitizedCategory))
        {
            EditorUtility.DisplayDialog("Invalid Category", "Shader category cannot be empty.", "OK");
            return;
        }

        if (!customShaderCategories.Contains(sanitizedCategory))
        {
            customShaderCategories.Add(sanitizedCategory);
        }

        selectedShaderCategory = sanitizedCategory;
        Repaint();
    }

    private void DeleteSelectedShaderCategory()
    {
        if (!customShaderCategories.Contains(selectedShaderCategory))
        {
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Delete Shader Category",
            "Delete category \"" + selectedShaderCategory + "\" from the list?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        customShaderCategories.Remove(selectedShaderCategory);
        selectedShaderCategory = "Custom";
        Repaint();
    }
}
