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

            // 3. 相機元件：CameraDirector（模式權威）+ CameraController（sway/shake）
            //    + ObservationMode（右鍵觀測）+ ProjectileTrackingCamera（自動追彈）
            var cam = Camera.main;
            if (cam != null)
            {
                if (!cam.TryGetComponent<CameraDirector>(out _))
                    cam.gameObject.AddComponent<CameraDirector>();

                if (!cam.TryGetComponent<CameraController>(out _))
                    cam.gameObject.AddComponent<CameraController>();

                if (!cam.TryGetComponent<ProjectileTrackingCamera>(out var trk))
                    trk = cam.gameObject.AddComponent<ProjectileTrackingCamera>();
                // 強制關閉自動追彈（覆寫舊場景可能存下的 true），連發不被打斷
                var soTrk = new SerializedObject(trk);
                soTrk.FindProperty("autoTrackProjectile").boolValue = false;
                soTrk.ApplyModifiedProperties();

                if (!cam.TryGetComponent<ObservationMode>(out _))
                    cam.gameObject.AddComponent<ObservationMode>();
            }

            // 3b. 第一人稱炮台視角（隱藏底座 / 車輪 / 轉台）
            var artBaseGO = GameObject.Find("ArtilleryBase");
            if (artBaseGO != null && !artBaseGO.TryGetComponent<ArtilleryFrontier.Combat.FirstPersonCannonView>(out _))
                artBaseGO.AddComponent<ArtilleryFrontier.Combat.FirstPersonCannonView>();

            // 4. 更新砲彈 Prefab：確定性運動學砲彈（Projectile + VFX + Tracer，無 Rigidbody）
            UpdateProjectilePrefab();

            // 5. 修正相機位置（移出砲管幾何體）
            FixCameraPosition();

            // 6. 目標區域（礦脈 + 城堡）
            TargetZoneBuilder.Build();

            // 6b. 目標可見性系統（浮動標籤）
            foreach (var tvs in Object.FindObjectsByType<TargetVisibilitySystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(tvs.gameObject);
            new GameObject("TargetVisibilitySystem").AddComponent<TargetVisibilitySystem>();

            // 6b-2. 落點預測圓圈（調炮角時顯示命中位置 + 飛行時間）
            foreach (var lp in Object.FindObjectsByType<LandingPreview>(FindObjectsSortMode.None))
                Object.DestroyImmediate(lp.gameObject);
            new GameObject("LandingPreview").AddComponent<LandingPreview>();

            // 6c. 敵人波次系統（分波生成 + 基地 HP + 勝敗）
            foreach (var wm in Object.FindObjectsByType<WaveManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(wm.gameObject);
            new GameObject("WaveManager").AddComponent<WaveManager>();

            // 7. Screen Space HUD（核心視覺回饋）
            if (Object.FindAnyObjectByType<ArtilleryHUD>() == null)
                new GameObject("ArtilleryHUD").AddComponent<ArtilleryHUD>();

            // 7b. 彈種選擇列（底部）+ 熱鍵 1-5
            if (Object.FindAnyObjectByType<AmmoSelector>() == null)
                new GameObject("AmmoSelector").AddComponent<AmmoSelector>();

            // 8. 加入 UI 發射按鈕（Android 用）
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

                // 確定性運動學砲彈：Projectile 負責運動 / 碰撞 / 傷害
                if (!root.TryGetComponent<ArtilleryFrontier.Projectile.Projectile>(out _))
                    root.AddComponent<ArtilleryFrontier.Projectile.Projectile>();

                if (!root.TryGetComponent<ArtilleryFrontier.Projectile.ProjectileVFX>(out _))
                    root.AddComponent<ArtilleryFrontier.Projectile.ProjectileVFX>();

                // 高可視度砲彈追蹤器（亮黃 + Additive 閃動 Trail）
                if (!root.TryGetComponent<ArtilleryFrontier.Projectile.ProjectileTracer>(out _))
                    root.AddComponent<ArtilleryFrontier.Projectile.ProjectileTracer>();

                // 運動學砲彈自行處理碰撞，移除 Rigidbody（Projectile.Awake 也會於執行時清除）
                if (root.TryGetComponent<Rigidbody>(out var rb))
                    Object.DestroyImmediate(rb, true);
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

        private static void FixCameraPosition()
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.fieldOfView = GameConfig.AimFOV;

            var artBase = GameObject.Find("ArtilleryBase");
            if (artBase == null) return;

            // 輕度過肩第三人稱：掛在 ArtilleryBase（僅隨 Yaw 轉），與砲管俯仰脫耦，
            // 高仰角時不會看天；固定下俯讓前方地面與落點圈完整可見。
            cam.transform.SetParent(artBase.transform);
            cam.transform.localPosition = new Vector3(
                GameConfig.AimCamSide, GameConfig.AimCamUp, GameConfig.AimCamBack);
            cam.transform.localRotation = Quaternion.Euler(GameConfig.AimCamTilt, 0f, 0f);

            Debug.Log("[VerticalSliceSetup] 相機定位：輕度過肩第三人稱（隨 Yaw、脫離砲管俯仰）。");
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
