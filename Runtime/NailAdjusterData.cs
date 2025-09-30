// NailAdjusterData.cs

using UnityEngine;
using System.Collections.Generic;

// 指ごとのネイルオブジェクトの調整データを管理
public class NailAdjusterData : MonoBehaviour
{
    // 1本の指に対応するネイルオブジェクトの情報
    [System.Serializable]
    public class NailMapping
    {
        // 対応する指の種類
        public HumanBodyBones finger;

        // 対応するネイルオブジェクト
        public GameObject nailObject;

        // 初期配置時のワールド座標
        public Vector3 initialWorldPosition;

        // 初期配置時のワールド回転
        public Quaternion initialWorldRotation;

        // 基本スケール（ローカル）
        public Vector3 baseScale;

        // UIで操作する位置オフセット（ローカル軸）
        public Vector3 positionOffset;

        // UIで操作する回転オフセット（オイラー角）
        public Vector3 rotationOffset;

        // UIで操作するスケールオフセット
        public Vector3 scaleOffset = Vector3.one;
    }

    // 全ての指のネイルマッピングリスト
    public List<NailMapping> nailMappings = new List<NailMapping>();
}