/*
 * Nail Tool Editor
 * Unity用 ネイル（爪）オブジェクトの自動配置・微調整ツール
 * 
 * 概要:
 * - アバターの指先にネイルプレハブを自動配置
 * - 配置後、各指ごとに位置・回転・スケール・BlendShapeをリアルタイム調整可能
 * - プリセット保存/読み込み対応
 * - Modular Avatar対応（オプション）
 * 
 * 使い方:
 * 1. アバターとネイルプレハブをセット
 * 2. 「Place Nails」ボタンで自動配置
 * 3. 調整タブで各指のネイルを微調整
 * 4. 必要に応じてプリセット保存/読み込み
 * 
 * 注意:
 * - Humanoidリグ専用
 * - Modular Avatar機能はMODULAR_AVATAR定義時のみ有効
 */
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if MODULAR_AVATAR
using nadena.dev.modular_avatar.core;
#endif




/// ネイル配置・調整用のEditorウィンドウ
public class NailToolWindow : EditorWindow
{
    #region Variables
    private int selectedTab = 0;
    private Animator avatarAnimator;
    private GameObject defaultNailPrefab;
    private float surfaceOffset = 0.0005f;
    private Vector3 positionOffset = Vector3.zero;
    private Vector3 rotationOffset = Vector3.zero;
    private NailAdjusterData data;
    private bool useSymmetry = true;
    private Vector2 scrollPos;
    private bool showHelp = true;
    private Dictionary<string, bool> fingerFoldouts = new Dictionary<string, bool>();
    private static readonly string[] fingerTypes = { "Thumb", "Index", "Middle", "Ring", "Little" };
    private bool showManualBoneSettings = false;
    [System.Serializable]
    private class BoneOverride { public HumanBodyBones bone; public Transform transform; }
    [SerializeField]
    private List<BoneOverride> boneOverrideList = new List<BoneOverride>();
    private Dictionary<HumanBodyBones, Transform> boneOverrides = new Dictionary<HumanBodyBones, Transform>();
    private bool showPrefabOverrides = false;
    [System.Serializable]
    private class PrefabOverride { public HumanBodyBones fingerBone; public GameObject prefab; }
    [SerializeField]
    private List<PrefabOverride> prefabOverrides = new List<PrefabOverride>();
    private static readonly Dictionary<HumanBodyBones, HumanBodyBones> symmetryMap = new Dictionary<HumanBodyBones, HumanBodyBones> { { HumanBodyBones.RightThumbDistal, HumanBodyBones.LeftThumbDistal }, { HumanBodyBones.LeftThumbDistal, HumanBodyBones.RightThumbDistal }, { HumanBodyBones.RightIndexDistal, HumanBodyBones.LeftIndexDistal }, { HumanBodyBones.LeftIndexDistal, HumanBodyBones.RightIndexDistal }, { HumanBodyBones.RightMiddleDistal, HumanBodyBones.LeftMiddleDistal }, { HumanBodyBones.LeftMiddleDistal, HumanBodyBones.RightMiddleDistal }, { HumanBodyBones.RightRingDistal, HumanBodyBones.LeftRingDistal }, { HumanBodyBones.LeftRingDistal, HumanBodyBones.RightRingDistal }, { HumanBodyBones.RightLittleDistal, HumanBodyBones.LeftLittleDistal }, { HumanBodyBones.LeftLittleDistal, HumanBodyBones.RightLittleDistal }, };
    #endregion


    /// Nail Toolウィンドウを表示
    [MenuItem("Tools/S101427 Nail Tool")]
    public static void ShowWindow() => GetWindow<NailToolWindow>("S101427 Nail Tool");

    private void OnEnable()
    {
        foreach (var type in fingerTypes) { if (!fingerFoldouts.ContainsKey(type)) fingerFoldouts.Add(type, false); }
        RebuildBoneOverridesDictionary();
        Selection.selectionChanged += OnSelectionChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
        OnSelectionChanged();
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }
    private void OnUndoRedo()
    {
        if (data != null)
        {
            foreach (var mapping in data.nailMappings)
            {
                if (mapping != null && mapping.nailObject != null) ApplyTransform(mapping);
            }
        }
        Repaint();
    }
    private void OnSelectionChanged() { if (Selection.activeGameObject == null) return; NailAdjusterData selectedData = Selection.activeGameObject.GetComponentInParent<NailAdjusterData>(); if (selectedData != null) { data = selectedData; selectedTab = 1; Repaint(); } }
    private void RebuildBoneOverridesDictionary()
    {
        boneOverrides.Clear();
        foreach (var item in boneOverrideList) { if (item != null) { boneOverrides[item.bone] = item.transform; } }
    }
    private void OnGUI()
    {
        showHelp = GUILayout.Toggle(showHelp, "? ヘルプ表示/非表示", "Button");
        EditorGUILayout.Space();
        selectedTab = GUILayout.Toolbar(selectedTab, new[] { "1.配置", "2.調整" });
        EditorGUILayout.Space();
        switch (selectedTab) { case 0: DrawPlacementGUI(); break; case 1: DrawAdjustmentGUI(); break; }
    }

    #region Placement Mode
    private void DrawPlacementGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("ステップ1：ネイルの自動配置", EditorStyles.boldLabel);
        if (showHelp) EditorGUILayout.HelpBox("アバターとネイルのプレハブをセットして「Place Nails」ボタンを押すと、ネイルが自動で配置され、調整モードに切り替わります。", MessageType.Info);
        avatarAnimator = (Animator)EditorGUILayout.ObjectField(new GUIContent("アバター (Animator)", "ネイルを付けたいアバターのAnimatorコンポーネント"), avatarAnimator, typeof(Animator), true);
        defaultNailPrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("ネイルプレハブ", "配置したいネイルのプレハブ"), defaultNailPrefab, typeof(GameObject), false);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("初期オフセット設定", EditorStyles.boldLabel);
        surfaceOffset = EditorGUILayout.FloatField(new GUIContent("めり込み調整 (Surface Offset)", "ネイルが浮いたり沈みすぎたりする場合に調整します。プラスで浮き、マイナスで沈みます。"), surfaceOffset);
        positionOffset = EditorGUILayout.Vector3Field(new GUIContent("位置オフセット (Position Offset)", "全体の位置を微調整します。"), positionOffset);
        rotationOffset = EditorGUILayout.Vector3Field(new GUIContent("回転オフセット (Rotation Offset)", "全体の回転を微調整します。4本指の骨の向きとプレハブの向きを合わせるための重要な設定です。"), rotationOffset);
        EditorGUILayout.Space();
        showPrefabOverrides = EditorGUILayout.Foldout(showPrefabOverrides, "▼上級設定：指ごとのプレハブ指定", true);
        if (showPrefabOverrides) { if (showHelp) EditorGUILayout.HelpBox("ここで左右の各指に個別のネイルプレハブを指定できます。指定がない指は、上の「ネイルプレハブ」で設定したものが使われます。", MessageType.Info); var allFingers = symmetryMap.Keys.Concat(symmetryMap.Values).Distinct().OrderBy(f => f.ToString()); foreach (var finger in allFingers) { var overrideEntry = prefabOverrides.FirstOrDefault(o => o.fingerBone == finger); GameObject currentOverridePrefab = overrideEntry?.prefab; EditorGUI.BeginChangeCheck(); GameObject newOverridePrefab = (GameObject)EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(finger.ToString()), currentOverridePrefab, typeof(GameObject), false); if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Change Prefab Override"); if (overrideEntry == null) { if (newOverridePrefab != null) { prefabOverrides.Add(new PrefabOverride { fingerBone = finger, prefab = newOverridePrefab }); } } else { overrideEntry.prefab = newOverridePrefab; if (newOverridePrefab == null) { prefabOverrides.Remove(overrideEntry); } } } } }
        EditorGUILayout.Space();
        showManualBoneSettings = EditorGUILayout.Foldout(showManualBoneSettings, "▼上級設定：手動ボーン指定", true);
        if (showManualBoneSettings) { if (showHelp) EditorGUILayout.HelpBox("自動検出がうまくいかない指がある場合、ここに直接ボーンのTransformをドラッグ＆ドロップして指定できます。空欄の場合は自動検出の結果が使われます。", MessageType.Info); if (GUILayout.Button("全てのボーン指定をリセット (再検出)")) { Undo.RecordObject(this, "Reset All Bone Overrides"); boneOverrideList.Clear(); RebuildBoneOverridesDictionary(); } var allFingers = symmetryMap.Keys.Concat(symmetryMap.Values).Distinct().OrderBy(f => f.ToString()); foreach (var finger in allFingers) { boneOverrides.TryGetValue(finger, out Transform currentOverride); Transform autoDetectedBone = (avatarAnimator != null) ? avatarAnimator.GetBoneTransform(finger) : null; Transform boneInField = currentOverride != null ? currentOverride : autoDetectedBone; EditorGUI.BeginChangeCheck(); Transform userInput = (Transform)EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(finger.ToString()), boneInField, typeof(Transform), true); if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(this, "Change Bone Override"); if (userInput == autoDetectedBone || userInput == null) { boneOverrideList.RemoveAll(item => item.bone == finger); } else { var existingOverride = boneOverrideList.FirstOrDefault(item => item.bone == finger); if (existingOverride != null) { existingOverride.transform = userInput; } else { boneOverrideList.Add(new BoneOverride { bone = finger, transform = userInput }); } } RebuildBoneOverridesDictionary(); } } }
        EditorGUILayout.Space(20);
        if (GUILayout.Button("Place Nails and Go to Adjuster")) { if (ValidateInputs()) PlaceNails(); }

        EditorGUILayout.EndScrollView();
    }
    /// ネイルの自動配置処理
    private void PlaceNails()
    {
        CleanupExistingNails();
        var renderers = avatarAnimator.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.sharedMesh != null && r.gameObject.activeInHierarchy).ToArray();
        if (renderers.Length == 0) { EditorUtility.DisplayDialog("Error", "アバターに有効なSkinnedMeshRendererが見つかりませんでした。", "OK"); return; }
        var parentObject = new GameObject("Generated Nails [By Tool]");
        parentObject.transform.SetParent(avatarAnimator.transform, false);
        var adjusterData = parentObject.AddComponent<NailAdjusterData>();
        int placedCount = 0;
        try
        {
            var fingerList = symmetryMap.Keys.Where(k => k.ToString().Contains("Right")).ToList();
            float totalFingers = fingerList.Count * 2;
            int currentFinger = 0;
            foreach (var rightFinger in fingerList)
            {
                var leftFinger = symmetryMap[rightFinger];
                foreach (var fingerId in new[] { rightFinger, leftFinger })
                {
                    string fingerName = ObjectNames.NicifyVariableName(fingerId.ToString());
                    EditorUtility.DisplayProgressBar("Placing Nails...", $"Processing: {fingerName}", currentFinger / totalFingers);
                    GameObject prefabToUse = defaultNailPrefab;
                    var overrideEntry = prefabOverrides.FirstOrDefault(o => o.fingerBone == fingerId);
                    if (overrideEntry != null && overrideEntry.prefab != null) { prefabToUse = overrideEntry.prefab; }
                    if (prefabToUse == null) continue;
                    GameObject nailInstance = PlaceNailOnFinger(fingerId, prefabToUse, parentObject.transform, renderers);
                    if (nailInstance != null)
                    {
                        placedCount++;
                        adjusterData.nailMappings.Add(new NailAdjusterData.NailMapping
                        {
                            finger = fingerId,
                            nailObject = nailInstance,
                            initialWorldPosition = nailInstance.transform.position,
                            initialWorldRotation = nailInstance.transform.rotation,
                            baseScale = nailInstance.transform.localScale,
                            positionOffset = Vector3.zero,
                            rotationOffset = Vector3.zero,
                            scaleOffset = Vector3.one
                        });
                    }
                    else { Debug.LogWarning($"{fingerName} の配置に失敗しました。"); }
                    currentFinger++;
                }
            }
        }
        finally { EditorUtility.ClearProgressBar(); }
        if (placedCount > 0) { EditorUtility.DisplayDialog("成功", $"{placedCount}個のネイルを配置しました。調整モードに移行します。", "OK"); Selection.activeGameObject = parentObject; data = adjusterData; selectedTab = 1; }
        else { DestroyImmediate(parentObject); EditorUtility.DisplayDialog("失敗", "ネイルを配置できませんでした。", "OK"); }
    }
    /// 指ごとのネイル配置
    private GameObject PlaceNailOnFinger(HumanBodyBones boneId, GameObject prefab, Transform parent, SkinnedMeshRenderer[] allRenderers)
    {
        boneOverrides.TryGetValue(boneId, out var distalOverride);
        Transform distalBone = (distalOverride != null) ? distalOverride : avatarAnimator.GetBoneTransform(boneId);
        Transform proximalBone = (distalOverride != null) ? distalOverride.parent : GetProximalBoneFor(boneId);
        if (distalBone == null || proximalBone == null) return null;
        Vector3 fingerForward = (distalBone.position - proximalBone.position).normalized; float fingerBoneLength = Vector3.Distance(distalBone.position, proximalBone.position); Vector3 tipVertexPos = Vector3.zero; float maxForwardDot = -2f; float tipSearchRadius = 0.02f; foreach (var renderer in allRenderers) { Mesh bakedMesh = new Mesh(); renderer.BakeMesh(bakedMesh); var vertices = bakedMesh.vertices; for (int i = 0; i < vertices.Length; i++) { Vector3 worldVertex = renderer.transform.TransformPoint(vertices[i]); if (Vector3.Distance(worldVertex, distalBone.position) < tipSearchRadius) { float dot = Vector3.Dot((worldVertex - distalBone.position).normalized, fingerForward); if (dot > maxForwardDot) { maxForwardDot = dot; tipVertexPos = worldVertex; } } } }
        if (tipVertexPos == Vector3.zero) return null; Vector3 nailBedSearchCenter = tipVertexPos - fingerForward * (fingerBoneLength * 0.25f); Vector3 basePos = Vector3.zero; Vector3 surfaceNormal = Vector3.zero; float minSqrDist = float.MaxValue; foreach (var renderer in allRenderers) { Mesh bakedMesh = new Mesh(); renderer.BakeMesh(bakedMesh); var vertices = bakedMesh.vertices; var normals = bakedMesh.normals; for (int i = 0; i < vertices.Length; i++) { Vector3 worldVertex = renderer.transform.TransformPoint(vertices[i]); float sqrDist = (worldVertex - nailBedSearchCenter).sqrMagnitude; if (sqrDist < minSqrDist) { minSqrDist = sqrDist; basePos = worldVertex; surfaceNormal = renderer.transform.TransformDirection(normals[i]); } } }
        if (basePos == Vector3.zero) return null;
        Vector3 upVec = surfaceNormal.normalized;
        Vector3 rightVec = Vector3.Cross(upVec, fingerForward).normalized;
        Vector3 correctedForward = Vector3.Cross(rightVec, upVec).normalized;
        Quaternion finalRot = Quaternion.LookRotation(upVec, correctedForward);
        GameObject nailInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        nailInstance.name = $"{prefab.name} ({ObjectNames.NicifyVariableName(boneId.ToString())})";
        nailInstance.transform.position = basePos + (surfaceNormal.normalized * surfaceOffset);
        nailInstance.transform.rotation = finalRot;
        nailInstance.transform.rotation *= Quaternion.Euler(rotationOffset);
        nailInstance.transform.Translate(positionOffset, Space.Self);
        return nailInstance;
    }
    #endregion

    #region Adjustment Mode
    private void DrawAdjustmentGUI()
    {
        EditorGUILayout.LabelField("ステップ2：リアルタイム微調整", EditorStyles.boldLabel);
        if (showHelp) EditorGUILayout.HelpBox("シーンで調整したいネイルの親オブジェクトを選択するか、下のスロットにドラッグすると調整を開始できます。\n「詳細プレビューウィンドウを開く」ボタンで専用ビューを開き、各指の「Left」「Right」ラベルをクリックするとその指をアップで確認できます。", MessageType.Info);

        data = (NailAdjusterData)EditorGUILayout.ObjectField("Nail Data Container", data, typeof(NailAdjusterData), true);
        if (data == null) { EditorGUILayout.HelpBox("データを待っています...", MessageType.Warning); return; }

        if (GUILayout.Button("詳細プレビューウィンドウを開く / 閉じる"))
        {
            if (EditorWindow.HasOpenInstances<NailPreviewWindow>()) { EditorWindow.GetWindow<NailPreviewWindow>().Close(); } else { NailPreviewWindow.ShowWindow(); }
        }
        EditorGUILayout.Space();
        useSymmetry = EditorGUILayout.Toggle(new GUIContent("左右対称 (Symmetry)", "片方の指を調整すると、もう片方の指も対称的に調整されます。"), useSymmetry);
        EditorGUILayout.Space();

        bool dataChanged = false;
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var fingerType in fingerTypes)
        {
            fingerFoldouts[fingerType] = EditorGUILayout.Foldout(fingerFoldouts[fingerType], fingerType, true, EditorStyles.foldoutHeader);
            if (fingerFoldouts[fingerType])
            {
                var leftMapping = data.nailMappings.FirstOrDefault(m => m.finger.ToString().Contains("Left") && m.finger.ToString().Contains(fingerType));
                var rightMapping = data.nailMappings.FirstOrDefault(m => m.finger.ToString().Contains("Right") && m.finger.ToString().Contains(fingerType));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MinWidth(position.width / 2 - 20));
                if (DrawFingerControls("Left", leftMapping)) dataChanged = true;
                EditorGUILayout.EndVertical();
                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.MinWidth(position.width / 2 - 20));
                if (DrawFingerControls("Right", rightMapping)) dataChanged = true;
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        if (dataChanged)
        {
            foreach (var mapping in data.nailMappings) ApplyTransform(mapping);

            if (data != null)
            {
                EditorUtility.SetDirty(data);
                if (data.gameObject.scene.isDirty)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(data.gameObject.scene);
                }
            }
        }

        EditorGUILayout.Space(20);

        // --- プリセット ---
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("プリセットの保存/読み込み", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Preset"))
        {
            SavePreset();
        }
        if (GUILayout.Button("Load Preset"))
        {
            LoadPreset();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        // -------------------------

#if MODULAR_AVATAR
        if (GUILayout.Button("Setup for Modular Avatar"))
        {
            SetupForModularAvatar();
        }
#else
        EditorGUI.BeginDisabledGroup(true);
        GUILayout.Button(new GUIContent("Setup for Modular Avatar", "プロジェクトにModular Avatarをインポートすると有効になります"));
        EditorGUI.EndDisabledGroup();
#endif
    }
    private void ResetFinger(NailAdjusterData.NailMapping mapping)
    {
        if (mapping == null) return;
        Undo.RecordObject(data, "Reset Nail Offsets");
        mapping.positionOffset = Vector3.zero;
        mapping.rotationOffset = Vector3.zero;
        mapping.scaleOffset = Vector3.one;
        var skinnedMesh = mapping.nailObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMesh != null)
        {
            Undo.RecordObject(skinnedMesh, "Reset BlendShapes");
            for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++) skinnedMesh.SetBlendShapeWeight(i, 0);
        }
    }
    private bool DrawFingerControls(string label, NailAdjusterData.NailMapping mapping)
    {
        if (mapping == null || mapping.nailObject == null) return false;
        bool changed = false;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(label, EditorStyles.boldLabel)) { Selection.activeGameObject = mapping.nailObject; NailPreviewWindow.targetFollowTransform = mapping.nailObject.transform; }
        if (GUILayout.Button("Reset", GUILayout.Width(50)))
        {
            ResetFinger(mapping);
            if (useSymmetry)
            {
                if (symmetryMap.TryGetValue(mapping.finger, out var symmetricFinger))
                {
                    var symmetricMapping = data.nailMappings.FirstOrDefault(m => m.finger == symmetricFinger);
                    if (symmetricMapping != null) ResetFinger(symmetricMapping);
                }
            }
            return true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.BeginChangeCheck();
        var posTitle = new GUIContent("Position Offset", "ネイル自身の向きを基準に位置を調整します。");
        var xPosLabel = new GUIContent("左右 (Left/Right)");
        var yPosLabel = new GUIContent("上下 (Up/Down)");
        var zPosLabel = new GUIContent("前後 (Forward/Back)");
        var newPositionOffset = EditorUtilities.LabeledAxesVector3WithSliders(posTitle, mapping.positionOffset, xPosLabel, yPosLabel, zPosLabel, -0.02f, 0.02f);
        var rotTitle = new GUIContent("Rotation Offset");
        var xRotLabel = new GUIContent("傾き (Pitch)");
        var yRotLabel = new GUIContent("向き (Yaw)");
        var zRotLabel = new GUIContent("ひねり (Roll)");
        var newRotationOffset = EditorUtilities.LabeledAxesVector3WithSliders(rotTitle, mapping.rotationOffset, xRotLabel, yRotLabel, zRotLabel, -180f, 180f);
        var newScaleOffset = EditorUtilities.Vector3WithSliders(new GUIContent("Scale", "スケール"), mapping.scaleOffset, 0.5f, 1.5f);
        var skinnedMesh = mapping.nailObject.GetComponentInChildren<SkinnedMeshRenderer>();
        List<float> newWeights = new List<float>();
        if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
        {
            int shapeCount = skinnedMesh.sharedMesh.blendShapeCount;
            if (shapeCount > 0)
            {
                if (showHelp) EditorGUILayout.HelpBox("ネイルプレハブが持つBlendShapeを調整できます。", MessageType.None);
                for (int i = 0; i < shapeCount; i++)
                {
                    string shapeName = skinnedMesh.sharedMesh.GetBlendShapeName(i);
                    float currentWeight = skinnedMesh.GetBlendShapeWeight(i);
                    newWeights.Add(EditorGUILayout.Slider(shapeName, currentWeight, 0f, 100f));
                }
            }
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(data, "Adjust Nail");
            mapping.positionOffset = newPositionOffset;
            mapping.rotationOffset = newRotationOffset;
            mapping.scaleOffset = newScaleOffset;
            if (skinnedMesh != null)
            {
                Undo.RecordObject(skinnedMesh, "Adjust BlendShape");
                for (int i = 0; i < newWeights.Count; i++) skinnedMesh.SetBlendShapeWeight(i, newWeights[i]);
            }
            if (useSymmetry) ApplySymmetry(mapping, newWeights);
            changed = true;
        }
        return changed;
    }
    #endregion

#if MODULAR_AVATAR
    private void SetupForModularAvatar()
    {
        if (data == null || data.GetComponentInParent<Animator>() == null)
        {
            EditorUtility.DisplayDialog("Error", "データまたはアバターが見つかりません。", "OK");
            return;
        }
        var animator = data.GetComponentInParent<Animator>();
        GameObject rootObject = data.gameObject;

        // --- MAの基本設定 ---
        var meshSettings = rootObject.GetComponent<ModularAvatarMeshSettings>();
        if (meshSettings == null) meshSettings = Undo.AddComponent<ModularAvatarMeshSettings>(rootObject);
        Undo.RecordObject(meshSettings, "Add MA Mesh Settings");
        meshSettings.InheritProbeAnchor = ModularAvatarMeshSettings.InheritMode.Inherit;
        meshSettings.InheritBounds = ModularAvatarMeshSettings.InheritMode.DontSet;

        // --- 補正コンポーネントの準備 ---
        var corrector = rootObject.GetComponent<NailPositionCorrector>();
        if (corrector == null) corrector = Undo.AddComponent<NailPositionCorrector>(rootObject);
        if (corrector == null)
        {
             // AddComponentが失敗した場合のエラーハンドリング
            Debug.LogError("NailPositionCorrectorコンポーネントの追加に失敗しました。スクリプトがEditorフォルダに入っていないか、コンパイルエラーがないか確認してください。");
            EditorUtility.DisplayDialog("エラー", "NailPositionCorrectorコンポーネントの追加に失敗しました。\nConsoleウィンドウを確認してください。", "OK");
            return;
        }
        Undo.RecordObject(corrector, "Setup Nail Corrector");
        corrector.corrections.Clear(); 

        int proxyCount = 0;
        foreach (var mapping in data.nailMappings)
        {
            if (mapping.nailObject != null)
            {
                var boneProxy = mapping.nailObject.GetComponent<ModularAvatarBoneProxy>();
                if (boneProxy == null)
                {
                    boneProxy = Undo.AddComponent<ModularAvatarBoneProxy>(mapping.nailObject);
                }
                Undo.RecordObject(boneProxy, "Add MA Bone Proxy");
                
                var distalBone = animator.GetBoneTransform(mapping.finger);
                if (distalBone != null)
                {
                    boneProxy.target = distalBone;
                    boneProxy.attachmentMode = BoneProxyAttachmentMode.AsChildKeepWorldPose;
                    Debug.Log($"boneProxy properties: target={boneProxy.target}, attachmentMode={boneProxy.attachmentMode}");

                    // --- 補正コンポーネントにデータを焼き付け ---
                    ApplyTransform(mapping);
                    var nailTransform = mapping.nailObject.transform;
                    
                    Vector3 correctLocalPos = distalBone.InverseTransformPoint(nailTransform.position);
                    Quaternion correctLocalRot = Quaternion.Inverse(distalBone.rotation) * nailTransform.rotation;
                    
                    Vector3 correctLocalScale = new Vector3(
                        nailTransform.lossyScale.x / distalBone.lossyScale.x,
                        nailTransform.lossyScale.y / distalBone.lossyScale.y,
                        nailTransform.lossyScale.z / distalBone.lossyScale.z
                    );

                    var correction = new NailPositionCorrector.CorrectionData
                    {
                        nailTransform = nailTransform,
                        correctLocalPosition = correctLocalPos,
                        correctLocalRotation = correctLocalRot,
                        correctLocalScale = correctLocalScale
                    };
                    corrector.corrections.Add(correction);
                    Debug.Log($"Added correction for {mapping.finger}:");
                    Debug.Log($"  distalBone: {distalBone.name}, bonePos: {distalBone.position}, boneRot: {distalBone.rotation.eulerAngles}, boneScale: {distalBone.lossyScale}");
                    Debug.Log($"  nailWorldPos: {nailTransform.position}, nailWorldRot: {nailTransform.rotation.eulerAngles}, nailWorldScale: {nailTransform.lossyScale}");
                    Debug.Log($"  computed nailLocalPos: {distalBone.InverseTransformPoint(nailTransform.position)}, nailLocalRot: {(Quaternion.Inverse(distalBone.rotation) * nailTransform.rotation).eulerAngles}");

                    proxyCount++;
                }
            }
        }
        
        EditorUtility.DisplayDialog("成功", $"Modular Avatarの設定と、再生時の位置補正セットアップが完了しました。\n- Bone Proxyをネイルに{proxyCount}個設定\n- 位置補正コンポーネントをルートに設定", "OK");
    }
#endif

    private void ApplyTransform(NailAdjusterData.NailMapping mapping)
    {
        if (mapping == null || mapping.nailObject == null) return;
        var t = mapping.nailObject.transform;
        Undo.RecordObject(t, "Adjust Nail Transform");
        Quaternion finalRotation = mapping.initialWorldRotation * Quaternion.Euler(mapping.rotationOffset);
        Vector3 worldSpacePositionOffset = finalRotation * mapping.positionOffset;
        Vector3 finalPosition = mapping.initialWorldPosition + worldSpacePositionOffset;
        t.position = finalPosition;
        t.rotation = finalRotation;
        t.localScale = new Vector3(
            mapping.baseScale.x * mapping.scaleOffset.x,
            mapping.baseScale.y * mapping.scaleOffset.y,
            mapping.baseScale.z * mapping.scaleOffset.z);
        EditorUtility.SetDirty(t);
    }
    private void ApplySymmetry(NailAdjusterData.NailMapping sourceMapping, List<float> weights)
    {
        if (!symmetryMap.ContainsKey(sourceMapping.finger)) return;
        var targetMapping = data.nailMappings.FirstOrDefault(m => m.finger == symmetryMap[sourceMapping.finger]);
        if (targetMapping == null) return;

        Undo.RecordObject(data, "Apply Symmetry Data");
        targetMapping.positionOffset = new Vector3(-sourceMapping.positionOffset.x, sourceMapping.positionOffset.y, sourceMapping.positionOffset.z);
        targetMapping.rotationOffset = new Vector3(sourceMapping.rotationOffset.x, -sourceMapping.rotationOffset.y, -sourceMapping.rotationOffset.z);
        targetMapping.scaleOffset = sourceMapping.scaleOffset;

        var targetSkinnedMesh = targetMapping.nailObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (targetSkinnedMesh != null)
        {
            Undo.RecordObject(targetSkinnedMesh, "Apply Symmetry BlendShape");
            for (int i = 0; i < weights.Count; i++) { targetSkinnedMesh.SetBlendShapeWeight(i, weights[i]); }
        }
    }
    private bool ValidateInputs() { if (avatarAnimator == null) { EditorUtility.DisplayDialog("Error", "Avatarが設定されていません。", "OK"); return false; } if (defaultNailPrefab == null) { EditorUtility.DisplayDialog("Error", "Default Nail Prefabが設定されていません。", "OK"); return false; } if (!avatarAnimator.isHuman) { EditorUtility.DisplayDialog("Error", "設定されたアバターはHumanoidリグではありません。", "OK"); return false; } return true; }
    private void CleanupExistingNails() { if (avatarAnimator == null) return; Transform existingParent = avatarAnimator.transform.Find("Generated Nails [By Tool]"); if (existingParent != null) DestroyImmediate(existingParent.gameObject); }
    private Transform GetProximalBoneFor(HumanBodyBones distalBoneId) { switch (distalBoneId) { case HumanBodyBones.LeftThumbDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.LeftThumbProximal); case HumanBodyBones.LeftIndexDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate); case HumanBodyBones.LeftMiddleDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.LeftMiddleIntermediate); case HumanBodyBones.LeftRingDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.LeftRingIntermediate); case HumanBodyBones.LeftLittleDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.LeftLittleIntermediate); case HumanBodyBones.RightThumbDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.RightThumbProximal); case HumanBodyBones.RightIndexDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate); case HumanBodyBones.RightMiddleDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate); case HumanBodyBones.RightRingDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.RightRingIntermediate); case HumanBodyBones.RightLittleDistal: return avatarAnimator.GetBoneTransform(HumanBodyBones.RightLittleIntermediate); default: return null; } }

    // --- プリセット保存・読み込み ---
    private void SavePreset()
    {
        if (data == null)
        {
            EditorUtility.DisplayDialog("エラー", "Nail Data Containerが選択されていません。", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("ネイルプリセットを保存", "", "MyNailPreset.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        NailPresetData presetData = new NailPresetData();
        foreach (var mapping in data.nailMappings)
        {
            if (mapping.nailObject == null) continue;

            var fingerData = new FingerPresetData
            {
                fingerBoneName = mapping.finger.ToString(),
                positionOffset = mapping.positionOffset,
                rotationOffset = mapping.rotationOffset,
                scaleOffset = mapping.scaleOffset
            };

            var skinnedMesh = mapping.nailObject.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
            {
                for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++)
                {
                    fingerData.blendShapes.Add(new BlendShapePresetData
                    {
                        name = skinnedMesh.sharedMesh.GetBlendShapeName(i),
                        weight = skinnedMesh.GetBlendShapeWeight(i)
                    });
                }
            }
            presetData.fingerPresets.Add(fingerData);
        }

        string json = JsonUtility.ToJson(presetData, true);
        System.IO.File.WriteAllText(path, json);

        EditorUtility.DisplayDialog("成功", $"プリセットを保存しました。\n{path}", "OK");
    }

    private void LoadPreset()
    {
        if (data == null)
        {
            EditorUtility.DisplayDialog("エラー", "Nail Data Containerが選択されていません。", "OK");
            return;
        }

        string path = EditorUtility.OpenFilePanel("ネイルプリセットを読み込む", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = System.IO.File.ReadAllText(path);
        NailPresetData presetData = JsonUtility.FromJson<NailPresetData>(json);

        if (presetData == null)
        {
            EditorUtility.DisplayDialog("エラー", "プリセットファイルの読み込みに失敗しました。", "OK");
            return;
        }

        Undo.RecordObject(data, "Load Nail Preset");

        foreach (var fingerPreset in presetData.fingerPresets)
        {
            HumanBodyBones fingerBone;
            try
            {
                fingerBone = (HumanBodyBones)System.Enum.Parse(typeof(HumanBodyBones), fingerPreset.fingerBoneName);
            }
            catch
            {
                Debug.LogWarning($"プリセット内の不明なボーン名をスキップしました: {fingerPreset.fingerBoneName}");
                continue;
            }

            var targetMapping = data.nailMappings.FirstOrDefault(m => m.finger == fingerBone);
            if (targetMapping != null)
            {
                targetMapping.positionOffset = fingerPreset.positionOffset;
                targetMapping.rotationOffset = fingerPreset.rotationOffset;
                targetMapping.scaleOffset = fingerPreset.scaleOffset;

                var skinnedMesh = targetMapping.nailObject.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skinnedMesh != null && skinnedMesh.sharedMesh != null)
                {
                    Undo.RecordObject(skinnedMesh, "Load BlendShapes");
                    for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++)
                    {
                        skinnedMesh.SetBlendShapeWeight(i, 0);
                    }
                    foreach (var bsPreset in fingerPreset.blendShapes)
                    {
                        int shapeIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(bsPreset.name);
                        if (shapeIndex != -1)
                        {
                            skinnedMesh.SetBlendShapeWeight(shapeIndex, bsPreset.weight);
                        }
                    }
                }
            }
        }

        foreach (var mapping in data.nailMappings)
        {
            ApplyTransform(mapping);
        }

        EditorUtility.SetDirty(data);
        SceneView.RepaintAll();
        EditorUtility.DisplayDialog("成功", "プリセットを読み込みました。", "OK");
    }
}