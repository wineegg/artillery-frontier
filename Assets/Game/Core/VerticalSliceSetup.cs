#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ArtilleryFrontier.Combat;
using ArtilleryFrontier.Projectile;
using ArtilleryFrontier.UI;

namespace ArtilleryFrontier.Core
{
    public static class VerticalSliceSetup
    {
        [MenuItem("ArtilleryFrontier/Setup Vertical Slice (All Phases)")]
        public static void SetupAll()
        {
            // 0. 清除多餘的 ArtilleryBase（多次執行 Setup Battle Scene 會留下殘留）
            PurgeDuplicateBases();

            // 1. 刪除舊 Plane，加入 EnvironmentBuilder（Start 時自動建立地形）
            var plane = GameObject.Find("Ground");
            if (plane != null) Object.DestroyImmediate(plane);

            // 刪除多餘 EnvironmentBuilder（多次執行 VerticalSlice 也會重複）
            foreach (var eb in Object.FindObjectsByType<EnvironmentBuilder>(FindObjectsSortMode.None))
                Object.DestroyImmediate(eb.gameObject);

            var envGO = new GameObject("EnvironmentBuilder");
            envGO.AddComponent<EnvironmentBuilder>();

            // 2. 建立炮台外觀
            CannonVisualBuilder.Build();

            // 3. 加 CameraController + ProjectileCamera 到 Main Camera
            var cam = Camera.main;
            if (cam != null)
            {
                if (!cam.TryGetComponent<CameraController>(out _))
                    cam.gameObject.AddComponent<CameraController>();
                if (!cam.TryGetComponent<ProjectileCamera>(out _))
                    cam.gameObject.AddComponent<ProjectileCamera>();
            }

            // 4. 更新砲彈 Prefab：確保有 ProjectileVFX + Sphere Collider
            UpdateProjectilePrefab();

            // 5. 修正相機位置（移出砲管幾何體）
            FixCameraPosition();

            // 5c. 關閉彈道預覽（舊版場景 showTrajectoryAlways 可能為 true）
            DisableTrajectoryPreview();

            // 5b. 移除 AngleVisualizer（TextMesh 在 URP 下顯示粉紅；角度已由 ArtilleryHUD 呈現）
            foreach (var av in Object.FindObjectsByType<AngleVisualizer>(FindObjectsSortMode.None))
                Object.DestroyImmediate(av.gameObject.GetComponent<AngleVisualizer>());

            // 6. Screen Space HUD（核心視覺回饋）
            if (Object.FindAnyObjectByType<ArtilleryHUD>() == null)
                new GameObject("ArtilleryHUD").AddComponent<ArtilleryHUD>();

            // 7. 加入 UI 發射按鈕（Android 用）
            BuildFireUI();

            Debug.Log("[VerticalSliceSetup] 完成。按 Play 即可體驗 Vertical Slice。");
        }

        private static void UpdateProjectilePrefab()
        {
            const string prefabPath = "Assets/Game/Projectile/ProjectilePrefab.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogWarning("[VerticalSliceSetup] 找不到 ProjectilePrefab，請先執行 Setup Battle Scene。");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                if (!root.TryGetComponent<ArtilleryFrontier.Projectile.ProjectileVFX>(out _))
                    root.AddComponent<ArtilleryFrontier.Projectile.ProjectileVFX>();

                // 確保 Rigidbody 設定正確
                if (root.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.mass = 1f;
                    rb.useGravity = true;
                }
            }
        }

        private static void BuildFireUI()
        {
            // 已存在則跳過
            if (GameObject.Find("FireButton") != null) return;

            var canvasGO = new GameObject("HUD_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 發射按鈕（右下角）
            var btnGO = new GameObject("FireButton");
            btnGO.transform.SetParent(canvasGO.transform);

            var img = btnGO.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.9f, 0.3f, 0.1f, 0.75f);

            var btn = btnGO.AddComponent<UnityEngine.UI.Button>();
            var rect = btnGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot     = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f);
            rect.sizeDelta = new Vector2(160f, 160f);

            // 按鈕文字
            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(btnGO.transform);
            var txt = txtGO.AddComponent<UnityEngine.UI.Text>();
            txt.text = "FIRE";
            txt.fontSize = 32;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            var tRect = txtGO.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            // 連接 Launcher
            var launcher = Object.FindAnyObjectByType<ArtilleryFrontier.Projectile.ProjectileLauncher>();
            if (launcher != null)
            {
                var so = new SerializedObject(btn);
                btn.onClick.AddListener(launcher.OnFireButtonPressed);
            }
        }

        private static void DisableTrajectoryPreview()
        {
            var preview = Object.FindAnyObjectByType<TrajectoryPreview>();
            if (preview == null) return;

            // 用 SerializedObject 覆寫場景已儲存的 showTrajectoryAlways=true
            var so = new SerializedObject(preview);
            so.FindProperty("showTrajectoryAlways").boolValue = false;
            so.ApplyModifiedProperties();

            Debug.Log("[VerticalSliceSetup] 彈道預覽已關閉。");
        }

        private static void FixCameraPosition()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // 找 BarrelPivot
            Transform barrelPivot = null;
            var artBase = GameObject.Find("ArtilleryBase");
            if (artBase != null) barrelPivot = artBase.transform.Find("BarrelPivot");

            if (barrelPivot != null)
            {
                cam.transform.SetParent(barrelPivot);
                // 砲架後方 0.35m，上方 0.55m — 在所有幾何體外
                cam.transform.localPosition = new Vector3(0f, 0.55f, -0.35f);
                cam.transform.localRotation = Quaternion.identity;
                Debug.Log("[VerticalSliceSetup] 相機重新定位至砲架後方。");
            }
        }

        // 多次執行 Setup Battle Scene 會在場景留下多個 ArtilleryBase（含舊的粉紅砲管），全數清除
        private static void PurgeDuplicateBases()
        {
            // 找出含 Camera.main 的 root ArtilleryBase（我們要保留的那個）
            Transform keeper = null;
            var cam = Camera.main;
            if (cam != null)
            {
                var t = cam.transform;
                while (t.parent != null) t = t.parent;
                if (t.name == "ArtilleryBase") keeper = t;
            }

            // 取得場景所有 GameObject 的快照後刪除多餘的
            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int removed = 0;
            foreach (var go in all)
            {
                if (go == null) continue;
                if (go.name != "ArtilleryBase" || go.transform.parent != null) continue;
                if (go.transform == keeper) continue;
                Object.DestroyImmediate(go);
                removed++;
            }
            if (removed > 0)
                Debug.Log($"[VerticalSliceSetup] 清除了 {removed} 個重複的 ArtilleryBase（殘留物）。");
        }
    }
}
#endif
