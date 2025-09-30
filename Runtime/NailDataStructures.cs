using UnityEngine;
using System.Collections.Generic;

// --- プリセット保存・読み込み用のデータ構造定義 ---
[System.Serializable]
public class NailPresetData
{
    public List<FingerPresetData> fingerPresets = new List<FingerPresetData>();
}


/// 指ごとのネイル調整データ
[System.Serializable]
public class FingerPresetData
{
    public string fingerBoneName; // "RightIndexDistal" のようなHumanBodyBonesの名前
    public Vector3 positionOffset;
    public Vector3 rotationOffset;
    public Vector3 scaleOffset;
    public List<BlendShapePresetData> blendShapes = new List<BlendShapePresetData>();
}

/// BlendShapeのプリセットデータ
[System.Serializable]
public class BlendShapePresetData
{
    public string name;
    public float weight;
}