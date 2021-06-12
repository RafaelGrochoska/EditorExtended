using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class HierarchyWindowGroupHeader
{
    private static readonly GUIStyle Style;

    static HierarchyWindowGroupHeader()
    {
        Style = Header.Style;

        EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGui;
        EditorApplication.RepaintHierarchyWindow();
    }

    private static int GetIndentation(Transform item)
    {
        var indentation = 0;
        var parent = item.parent;

        while (parent != null)
        {
            indentation += 14;
            parent = parent.parent;
        }

        return indentation;
    }

    private static void DrawLightBg(Rect selectionRect)
    {
        var bgRect = selectionRect;
        bgRect.x = 0;
        bgRect.width = Screen.width;
        EditorGUI.DrawRect(bgRect, Header.LightBgColor);
    }

    private static void DrawHideCube(Rect selectionRect)
    {
        var bgRect = selectionRect;
        bgRect.x = 0;
        bgRect.width = 32;
        EditorGUI.DrawRect(bgRect, Header.DarkBgColor);
    }

    private static void DrawDarkBg(Rect selectionRect, int indentation)
    {
        var bgRect = selectionRect;
        bgRect.x = indentation > 0 ? indentation + 32 : 0;
        bgRect.width = Screen.width - indentation;
        selectionRect.x = 18f;

        EditorGUI.DrawRect(bgRect, Header.DarkBgColor);
        if (indentation <= 0) return;

        var lineBgRect = bgRect;
        lineBgRect.y += 6;
        lineBgRect.height -= 12;
        lineBgRect.x = 0;
        lineBgRect.width = 44 + indentation;
        EditorGUI.DrawRect(lineBgRect, Header.DarkBgColor);
    }

    private static void DrawName(Rect selectionRect, int indentation, string name)
    {
        var nameRect = selectionRect;
        nameRect.x += indentation > 0 ? 5 : -10;
        nameRect.width -= indentation;
        EditorGUI.LabelField(nameRect, Header.Rename(name).ToUpperInvariant(), Style);
    }

    private static void DrawDestroyButton(Rect selectionRect, GameObject item)
    {
        //Destroy Button
        var destroyRect = selectionRect;
        destroyRect.width = 16f;
        destroyRect.x = Screen.width - 33f;
        var destroyContent = new GUIContent {text = "-"};
        var destroyStyle = new GUIStyle(Style) {fontSize = 20, alignment = TextAnchor.MiddleCenter};


        var e = Event.current;
        if (!GUI.Button(destroyRect, destroyContent, destroyStyle)) return;

        if (e.control)
            UnprotectGameObject(item);
        else Object.DestroyImmediate(item);
    }


    private static void UnprotectGameObject(GameObject item)
    {
        if (!item.CompareTag("Untagged"))
            item.tag = "Untagged";

        item.hideFlags = HideFlags.None;
        item.transform.hideFlags = HideFlags.None;

        item.name = Header.Rename(item.name);
    }

    private static void ProtectGameObject(GameObject item)
    {
        if (!item.CompareTag("EditorOnly"))
            item.tag = "EditorOnly";

        item.hideFlags = HideFlags.NotEditable;
        item.transform.hideFlags = HideFlags.HideInInspector;
    }

    private static void HierarchyWindowItemOnGui(int instanceId, Rect selectionRect)
    {
        var item = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

        if (item == null || !Header.HaveHeaderName(item.name)) return;

        if (!Header.IsValid(item))
        {
            Debug.LogWarning("Header can't have children or logic", item);
            item.hideFlags = HideFlags.None;
            return;
        }

        ProtectGameObject(item);

        var indentation = GetIndentation(item.transform);

        DrawLightBg(selectionRect);
        DrawHideCube(selectionRect);
        DrawDarkBg(selectionRect, indentation);
        DrawName(selectionRect, indentation, item.name);

        DrawDestroyButton(selectionRect, item);
    }
}


internal static class Header
{
    private const string HEADER_INDICATOR = "//";
    private static GUIStyle _style = null;

    internal static GUIStyle Style =>
        _style ?? (_style = new GUIStyle
        {
            fontSize = 13,
            normal = {textColor = TextColor},
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.BoldAndItalic
        });

    private static Color TextColor =>
        EditorGUIUtility.isProSkin ? new Color(0.7607f, 0.7607f, 0.7607f) : new Color(0.3f, 0.3f, 0.3f);

    internal static Color DarkBgColor =>
        EditorGUIUtility.isProSkin ? new Color(0.17647f, 0.17647f, 0.17647f) : new Color(0.67f, 0.67f, 0.67f);

    internal static Color LightBgColor =>
        EditorGUIUtility.isProSkin ? new Color(0.2196f, 0.2196f, 0.2196f) : new Color(0.76f, 0.76f, 0.76f);

    internal static string Rename(string name) =>
        name.Replace(HEADER_INDICATOR, "");

    internal static bool HaveHeaderName(string name) =>
        name.StartsWith(HEADER_INDICATOR, StringComparison.Ordinal);

    internal static bool IsValid(GameObject item) =>
        !(item.transform.childCount > 0 || item.GetComponents<Component>().Length > 1);
}