// NailPositionCorrector.cs 

using UnityEngine;
using System.Collections.Generic;
using VRC.SDKBase;

// ネイルオブジェクトの位置・回転・スケールを一括補正するコンポーネント
public class NailPositionCorrector : MonoBehaviour, IEditorOnly
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


    void LateUpdate()
    {
        Debug.Log("NailPositionCorrector LateUpdate called.");

        if (hasCorrected || corrections.Count == 0)
        {
            Debug.Log("No corrections needed or already corrected.");
            return;
        }

        foreach (var data in corrections)
        {
            if (data.nailTransform != null)
            {
                // ★このログを追加
                Debug.Log($"Correcting {data.nailTransform.name}: Pos={data.correctLocalPosition}", data.nailTransform.gameObject);

                data.nailTransform.localPosition = data.correctLocalPosition;
                data.nailTransform.localRotation = data.correctLocalRotation;
                data.nailTransform.localScale = data.correctLocalScale;
            }
            else
            {
                // ★nullチェックも入れておくと安心
                Debug.LogWarning("data.nailTransform is null in corrections list.");
            }
        }
        hasCorrected = true;
        Destroy(this);
    }
}