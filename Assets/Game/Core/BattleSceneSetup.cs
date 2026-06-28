#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArtilleryFrontier.Core
{
    public static class BattleSceneSetup
    {
        [MenuItem("ArtilleryFrontier/Setup Battle Scene")]
        public static void Setup()
        {
            // --- 地面 ---
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(100f, 1f, 100f);

            // --- 炮台基座 ---
            GameObject artilleryBase = new GameObject("ArtilleryBase");
            artilleryBase.transform.position = new Vector3(0f, 0.5f, 0f);

            // --- 砲管 Pivot ---
            GameObject barrelPivot = new GameObject("BarrelPivot");
            barrelPivot.transform.SetParent(artilleryBase.transform);
            barrelPivot.transform.localPosition = Vector3.zero;

            // 視覺砲管
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(barrelPivot.transform);
            barrel.transform.localPosition = new Vector3(0f, 0f, 1f);
            barrel.transform.localScale = new Vector3(0.15f, 1f, 0.15f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 砲口
            GameObject muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(barrelPivot.transform);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 2f);

            // ArtilleryController
            var controller = artilleryBase.AddComponent<ArtilleryFrontier.Combat.ArtilleryController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("barrelPivot").objectReferenceValue = barrelPivot.transform;
            so.ApplyModifiedProperties();

            // --- 砲彈 Prefab（執行時期建立）---
            GameObject projectilePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectilePrefab.name = "ProjectilePrefab";
            projectilePrefab.transform.localScale = Vector3.one * 0.2f;
            projectilePrefab.AddComponent<Rigidbody>();

            // 存成 Prefab
            string prefabPath = "Assets/Game/Projectile/ProjectilePrefab.prefab";
            bool success;
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(projectilePrefab, prefabPath, out success);
            Object.DestroyImmediate(projectilePrefab);

            // ProjectileLauncher
            var launcher = barrelPivot.AddComponent<ArtilleryFrontier.Projectile.ProjectileLauncher>();
            SerializedObject soLauncher = new SerializedObject(launcher);
            soLauncher.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            if (success)
                soLauncher.FindProperty("projectilePrefab").objectReferenceValue = savedPrefab;
            soLauncher.ApplyModifiedProperties();

            // TrajectoryPreview
            var preview = barrelPivot.AddComponent<ArtilleryFrontier.Projectile.TrajectoryPreview>();
            SerializedObject soPreview = new SerializedObject(preview);
            soPreview.FindProperty("launcher").objectReferenceValue = launcher;
            soPreview.ApplyModifiedProperties();

            // --- Camera ---
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }
            mainCam.transform.SetParent(barrelPivot.transform);
            // 放在砲架後方，避免相機進入砲管幾何體內
            mainCam.transform.localPosition = new Vector3(0f, 0.55f, -0.35f);
            mainCam.transform.localRotation = Quaternion.identity;

            // --- 平行光 ---
            if (Object.FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Debug.Log("[BattleSceneSetup] 場景建立完成。按 Play 即可測試。");
            Selection.activeGameObject = artilleryBase;
        }
    }
}
#endif
