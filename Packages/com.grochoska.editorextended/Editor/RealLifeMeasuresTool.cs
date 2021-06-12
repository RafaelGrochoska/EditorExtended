// ReSharper disable DelegateSubtraction
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Collections.Generic;

#pragma warning disable 0649

[EditorTool("RealLife Measures")]
internal class RealLifeMeasuresTool : EditorTool
{
    [SerializeField] private Texture2D _toolIcon;

    private GUIContent _iconContent;

    private GameObject[] _selectedGameObject;

    private void OnEnable()
    {
        _iconContent = new GUIContent()
        {
            image = _toolIcon,
            text = "Object Measures",
            tooltip = "Select an object to discover it's size in meters."
        };
        SetSelected();
        Selection.selectionChanged += SetSelected;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= SetSelected;
    }

    private void SetSelected()
    {
        _selectedGameObject = Selection.gameObjects;
    }

    public override GUIContent toolbarIcon => _iconContent;

    public override void OnToolGUI(EditorWindow window)
    {
        foreach (var gb in _selectedGameObject)
        {
            var measurable = gb.GetComponentsInChildren<MeshFilter>().Where(_ => _.sharedMesh != null)
                .Select(_ => (_.transform, _.sharedMesh.bounds)).ToArray();

            if (measurable.Length == 0) continue;

            var (transform, bounds) = measurable[0];

            if (measurable.Length == 1)
            {
                DrawBoundsMeasures(bounds, transform.position, transform.rotation, transform.lossyScale);
                continue;
            }

            foreach (var (currentTransform, currentBounds) in measurable.Skip(1))
            {
                foreach (var point in GetPoints(currentBounds))
                    bounds.Encapsulate(TransformPointToOtherTransform(transform, point, currentTransform));
            }

            DrawBoundsMeasures(bounds, transform.position, transform.rotation, transform.lossyScale);
        }
    }

    private static void DrawBoundsMeasures(Bounds bounds, Vector3 worldCenter, Quaternion worldRotation,
        Vector3 worldScale)
    {
        var red = new Color(1f, 0.21f, 0.23f);
        var green = new Color(0.22f, 1f, 0.22f);
        var blue = new Color(0.31f, 0.29f, 1f);
        
        var defaultMatrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(worldCenter, worldRotation, worldScale);

        Handles.color = Color.white;
        Handles.DrawWireCube(bounds.center, bounds.extents * 2);

        var maxX = bounds.min + Vector3.right * bounds.size.x;
        var middleX = bounds.min + Vector3.right * bounds.size.x / 2;
        var maxY = bounds.min + Vector3.up * bounds.size.y;
        var middleY = bounds.min + Vector3.up * bounds.size.y / 2;
        var maxZ = bounds.min + Vector3.forward * bounds.size.z;
        var middleZ = bounds.min + Vector3.forward * bounds.size.z / 2;

        Handles.color = red;
        Handles.DrawLine(bounds.min, maxX);
        Handles.color = green;
        Handles.DrawLine(bounds.min, maxY);
        Handles.color = blue;
        Handles.DrawLine(bounds.min, maxZ);

        Handles.BeginGUI();
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 18
            };

            GUI.backgroundColor = new Color(0f, 0f, 0f, 0.75f);

            DrawLabel(middleX, red, bounds.size.y, worldScale.y, ref style);
            DrawLabel(middleY, green, bounds.size.y, worldScale.y, ref style);
            DrawLabel(middleZ, blue, bounds.size.z, worldScale.z, ref style);
        }
        Handles.EndGUI();
        Handles.matrix = defaultMatrix;
    }

    private static void DrawLabel(Vector3 middlePoint, Color color, float boundSize, float worldScale,
        ref GUIStyle style)
    {
        var rect = new Rect(0, 0, 100, 26) {position = HandleUtility.WorldToGUIPoint(middlePoint)};
        style.normal.textColor = color;
        GUI.Label(rect, GetMetricSize(boundSize, worldScale), style);
    }

    private static string GetMetricSize(float boundSize, float worldScale)
    {
        var size = boundSize * worldScale;
        var unity = size < 1 ? "cm" : size >= 1000 ? "km" : "m";

        size = size < 1 ? size * 100 : size >= 1000 ? size / 1000.0f : size;

        return $"{size:F2} {unity}";
    }

    private static Vector3 TransformPointToOtherTransform(Transform origin, Vector3 point, Transform target)
    {
        return origin.InverseTransformPoint(target.TransformPoint(point));
    }

    private static IEnumerable<Vector3> GetPoints(Bounds bounds)
    {
        var min = bounds.min;
        var max = bounds.max;
        var size = bounds.size;
        return new[]
        {
            min,
            min + Vector3.right * size.x,
            min + Vector3.up * size.y,
            min + Vector3.forward * size.z,
            max,
            max + Vector3.left * size.x,
            max + Vector3.down * size.y,
            max + Vector3.back * size.z
        };
    }
}