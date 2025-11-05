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
    [SerializeField]
    public List<CorrectionData> corrections = new List<CorrectionData>();

    public void Apply()
    {
        Debug.Log("Applying NailPositionCorrector...", this);

        if (corrections.Count == 0)
        {
            Debug.LogWarning("No corrections needed or already corrected.");
            return;
        }

        foreach (var data in corrections)
        {
            if (data.nailTransform != null)
            {
                Debug.Log($"[BEFORE    ] Target Nail: {data.nailTransform.name} (Pos={data.correctLocalPosition}, Rot={data.correctLocalRotation.eulerAngles}, Scale={data.correctLocalScale})", data.nailTransform);
                Debug.Log($"[CORRECTION] Applying Position={data.correctLocalPosition}, Rotation={data.correctLocalRotation.eulerAngles}, Scale={data.correctLocalScale}");

                data.nailTransform.localPosition = data.correctLocalPosition;
                data.nailTransform.localRotation = data.correctLocalRotation;
                data.nailTransform.localScale = data.correctLocalScale;

                Debug.Log($"[AFTER     ] Target Nail: {data.nailTransform.name} (Pos={data.nailTransform.localPosition}, Rot={data.nailTransform.localRotation.eulerAngles}, Scale={data.nailTransform.localScale})", data.nailTransform);
            }
            else
            {
                Debug.LogWarning("data.nailTransform is null in corrections list.");
            }
        }
    }
}