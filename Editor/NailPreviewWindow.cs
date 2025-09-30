
// NailPreviewWindow.cs
// Unity Editor上でネイルチップのプレビューを表示するウィンドウ
// 操作方法: 左ドラッグで回転 / 中ドラッグで移動 / ホイールでズーム / Fキーでリセット
using UnityEngine;
using UnityEditor;


// ネイルプレビューウィンドウ本体
public class NailPreviewWindow : EditorWindow
{

    // プレビュー対象のTransform
    public static Transform targetFollowTransform;
    // プレビュー用カメラ
    private Camera previewCamera;
    // プレビューカメラのGameObject
    private GameObject previewCamGO;
    // カメラの注視点
    private Vector3 pivotPoint;
    // カメラの回転
    private Quaternion viewRotation = Quaternion.Euler(30, 0, 0);
    // カメラ距離
    private float distance = 0.1f;
    // 最後に追従したTransform
    private Transform lastFollowedTarget;

    // カメラの回転角度
    private Vector2 viewAngles = new Vector2(30f, 0f);

    private const float ZOOM_SPEED = 0.02f;   // ズーム速度
    private const float ORBIT_SPEED = 0.4f;   // 回転速度
    private const float PAN_SPEED = 0.001f;   // 移動速度

    // MenuItem属性削除（Toolsメニューに表示しない）
    public static void ShowWindow() => GetWindow<NailPreviewWindow>("Nail Preview");


    // ウィンドウ有効化時にカメラを生成
    private void OnEnable()
    {
        previewCamGO = new GameObject("NailPreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        previewCamera = previewCamGO.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.cameraType = CameraType.SceneView;
        previewCamera.fieldOfView = 30;
        previewCamera.nearClipPlane = 0.001f;
    }


    // ウィンドウ無効化時にカメラを破棄
    private void OnDisable()
    {
        if (previewCamGO != null) DestroyImmediate(previewCamGO);
    }


    // カメラの状態を更新
    private void UpdateCameraState()
    {
        if (previewCamera == null) return;

        // 対象が切り替わったら注視点・回転をリセット
        if (targetFollowTransform != null && targetFollowTransform != lastFollowedTarget)
        {
            FocusAndResetView();
            lastFollowedTarget = targetFollowTransform;
        }

        viewRotation = Quaternion.Euler(viewAngles.x, viewAngles.y, 0);

        previewCamera.transform.rotation = viewRotation;
        previewCamera.transform.position = pivotPoint - (viewRotation * Vector3.forward * distance);

        Repaint();
    }
    

    // ユーザー入力の処理（カメラ操作）
    private void HandleInput(Event e)
    {
        // Fキーで注視点リセット
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.F && targetFollowTransform != null)
            {
                FocusAndResetView();
                e.Use();
                return;
            }
        }

        // ウィンドウ外は無視
        if (!new Rect(0, 0, position.width, position.height).Contains(e.mousePosition))
        {
            return;
        }

        switch (e.type)
        {
            case EventType.ScrollWheel:
                // ホイールでズーム
                distance *= 1f + e.delta.y * ZOOM_SPEED;
                distance = Mathf.Max(0.01f, distance);
                e.Use();
                break;

            case EventType.MouseDrag:
                if (e.button == 2)
                {
                    // 中ドラッグで移動
                    Vector3 move = previewCamera.transform.right * -e.delta.x * PAN_SPEED * distance +
                                   previewCamera.transform.up * e.delta.y * PAN_SPEED * distance;
                    pivotPoint += move;
                    e.Use();
                }
                else if (e.button == 1)
                {
                    // 右ドラッグで回転
                    viewAngles.y += e.delta.x * ORBIT_SPEED * 2f;
                    viewAngles.x -= e.delta.y * ORBIT_SPEED * 2f;
                    viewAngles.x = Mathf.Clamp(viewAngles.x, -89f, 89f);
                    e.Use();
                }
                break;
        }
    }


    // 注視点・回転をターゲットにリセット
    private void FocusAndResetView()
    {
        if (targetFollowTransform == null) return;
        pivotPoint = targetFollowTransform.position;
        distance = 0.05f;

        Quaternion resetRotation = targetFollowTransform.rotation * Quaternion.Euler(0, 180, 0);
        Vector3 euler = resetRotation.eulerAngles;
        viewAngles.x = euler.x;
        viewAngles.y = euler.y;
    }


    // ウィンドウ描画処理
    private void OnGUI()
    {
        if (previewCamera == null) return;

        HandleInput(Event.current);
        UpdateCameraState();

        // プレビュー描画
        Rect previewRect = new Rect(0, 0, position.width, position.height);
        Handles.DrawCamera(previewRect, previewCamera);

        // 操作説明
        GUI.Label(new Rect(5, 5, 500, 20), "右ドラッグ:回転 / 中ドラッグ:移動 / ホイール:ズーム / F:リセット");
    }
}