using UnityEngine;
using UnityEditor;

// エディタ拡張用のユーティリティ関数群
public static class EditorUtilities
{
    // Vector3の各軸をスライダーで調整できるフィールドを表示
    // XYZラベルは固定
    public static Vector3 Vector3WithSliders(GUIContent label, Vector3 value, float sliderMin, float sliderMax)
    {
        EditorGUILayout.LabelField(label);
        EditorGUI.indentLevel++;
        var result = value;
        result.x = AxisWithSlider("X", result.x, sliderMin, sliderMax);
        result.y = AxisWithSlider("Y", result.y, sliderMin, sliderMax);
        result.z = AxisWithSlider("Z", result.z, sliderMin, sliderMax);
        EditorGUI.indentLevel--;
        return result;
    }

    // XYZ各軸のラベルを個別に指定できるバージョン
    public static Vector3 LabeledAxesVector3WithSliders(GUIContent label, Vector3 value, GUIContent xLabel, GUIContent yLabel, GUIContent zLabel, float min, float max)
    {
        EditorGUILayout.LabelField(label);
        EditorGUI.indentLevel++;
        var result = value;
        result.x = AxisWithSlider(xLabel, result.x, min, max);
        result.y = AxisWithSlider(yLabel, result.y, min, max);
        result.z = AxisWithSlider(zLabel, result.z, min, max);
        EditorGUI.indentLevel--;
        return result;
    }
    
    // スライダー付きfloatフィールド（ラベルはGUIContent指定）
    private static float AxisWithSlider(GUIContent label, float value, float min, float max)
    {
        EditorGUILayout.BeginHorizontal();
        float result = EditorGUILayout.FloatField(label, value, GUILayout.Width(EditorGUIUtility.labelWidth + 60));
        result = GUILayout.HorizontalSlider(result, min, max);
        EditorGUILayout.EndHorizontal();
        return result;
    }
    
    // スライダー付きfloatフィールド（ラベルはstring指定）
    private static float AxisWithSlider(string label, float value, float min, float max)
    {
        return AxisWithSlider(new GUIContent(label), value, min, max);
    }
}