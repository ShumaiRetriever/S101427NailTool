// NailPositionCorrector.cs 

using UnityEngine;
using System.Collections.Generic;

// ネイルオブジェクトの位置・回転・スケールを一括補正するコンポーネント
public class NailPositionCorrector : MonoBehaviour
{
    // 1つのネイルに対する補正データ
    [System.Serializable]
    public class CorrectionData
    {
        // 補正対象のTransform
        public Transform nailTransform;
        // 補正後のローカル座標
        public Vector3 correctLocalPosition;
        // 補正後のローカル回転
        public Quaternion correctLocalRotation;
        // 補正後のローカルスケール
        public Vector3 correctLocalScale;
    }

    // 全ネイルの補正データリスト
    public List<CorrectionData> corrections = new List<CorrectionData>();
    // 一度だけ補正するためのフラグ
    private bool hasCorrected = false;

    // LateUpdateで一度だけ全ネイルのTransformを補正し、完了後に自身を破棄
    void LateUpdate()
    {
        if (hasCorrected || corrections.Count == 0)
        {
            return;
        }

        foreach (var data in corrections)
        {
            if (data.nailTransform != null)
            {
                data.nailTransform.localPosition = data.correctLocalPosition;
                data.nailTransform.localRotation = data.correctLocalRotation;
                data.nailTransform.localScale = data.correctLocalScale;
            }
        }
        hasCorrected = true;
        Destroy(this);
    }
}