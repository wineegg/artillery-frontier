using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using ArtilleryFrontier.Projectile;
#endif

namespace ArtilleryFrontier.Combat
{
    // ── Runtime: 砲管後座力 ────────────────────────────────────────────
    public class CannonRecoil : MonoBehaviour
    {
        [SerializeField] private Transform barrel;
        [SerializeField] private float recoilDistance = 0.35f;
        [SerializeField] private float recoilTime = 0.08f;
        [SerializeField] private float returnTime = 0.14f;

        private Vector3 _originLocalPos;
        private Coroutine _routine;

        private void Awake()
        {
            if (barrel != null) _originLocalPos = barrel.localPosition;
        }

        public void Trigger()
        {
            if (barrel == null) return;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(DoRecoil());
        }

        private IEnumerator DoRecoil()
        {
            Vector3 recoilPos = _originLocalPos - Vector3.forward * recoilDistance;

            for (float t = 0f; t < recoilTime; t += Time.deltaTime)
            {
                barrel.localPosition = Vector3.Lerp(_originLocalPos, recoilPos, t / recoilTime);
                yield return null;
            }
            barrel.localPosition = recoilPos;

            for (float t = 0f; t < returnTime; t += Time.deltaTime)
            {
                barrel.localPosition = Vector3.Lerp(recoilPos, _originLocalPos, t / returnTime);
                yield return null;
            }
            barrel.localPosition = _originLocalPos;
            _routine = null;
        }
    }

    // ── Editor: 炮台外觀建造器 ────────────────────────────────────────
#if UNITY_EDITOR
    public static class CannonVisualBuilder
    {
        [MenuItem("ArtilleryFrontier/Build Cannon Visual")]
        public static void Build()
        {
            var artBase = GameObject.Find("ArtilleryBase");
            if (artBase == null)
            {
                Debug.LogError("[CannonVisualBuilder] 找不到 ArtilleryBase，請先執行 Setup Battle Scene。");
                return;
            }

            var barrelPivot = artBase.transform.Find("BarrelPivot");
            if (barrelPivot == null)
            {
                Debug.LogError("[CannonVisualBuilder] 找不到 BarrelPivot。");
                return;
            }

            ClearVisualChildren(artBase.transform, barrelPivot);
            ClearVisualChildren(barrelPivot, null);

            // 各部件用明顯不同的 Sprites/Default 顏色（URP 下不會顯示粉紅）
            var matBase    = MakeMat(new Color(0.12f, 0.12f, 0.13f));  // 近黑灰（底座/車軸）
            var matMount   = MakeMat(new Color(0.18f, 0.24f, 0.12f));  // 橄欖深綠（砲管轉台）
            var matMetal   = MakeMat(new Color(0.08f, 0.13f, 0.20f));  // 深藍鋼（砲管+環）
            var matWood    = MakeMat(new Color(0.40f, 0.22f, 0.08f));  // 暖棕木（車輪）
            var matMuzzle  = MakeMat(new Color(0.42f, 0.45f, 0.48f));  // 亮灰銀（砲口鐘）

            // ── Base：砲台底座 ───────────────────────────────────────
            var baseCyl = MakePrim(PrimitiveType.Cylinder, "Base", artBase.transform,
                pos: new Vector3(0f, -0.25f, 0f),
                scale: new Vector3(1.8f, 0.25f, 1.8f),
                mat: matBase);

            // Base 上蓋板
            var topPlate = MakePrim(PrimitiveType.Cube, "TopPlate", artBase.transform,
                pos: new Vector3(0f, 0.05f, 0.1f),
                scale: new Vector3(1.4f, 0.12f, 1.8f),
                mat: matBase);

            // ── Wheels ──────────────────────────────────────────────
            float wheelY = -0.05f;
            float wheelZ = 0.5f;
            MakeWheel("WheelLeft",  artBase.transform, new Vector3(-0.95f, wheelY, wheelZ), matWood);
            MakeWheel("WheelRight", artBase.transform, new Vector3( 0.95f, wheelY, wheelZ), matWood);

            // 車軸
            var axle = MakePrim(PrimitiveType.Cylinder, "Axle", artBase.transform,
                pos: new Vector3(0f, wheelY, wheelZ),
                scale: new Vector3(0.12f, 1.0f, 0.12f),
                mat: matBase);
            axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // ── Barrel Mount（轉台）────────────────────────────────
            var mount = MakePrim(PrimitiveType.Cube, "BarrelMount", barrelPivot,
                pos: new Vector3(0f, 0f, 0.4f),
                scale: new Vector3(0.7f, 0.65f, 0.9f),
                mat: matMount);

            // ── Barrel（砲管主體）──────────────────────────────────
            // Cylinder height=2 units, scale Y=2.5 → 5m length
            var barrel = MakePrim(PrimitiveType.Cylinder, "Barrel", barrelPivot,
                pos: new Vector3(0f, 0f, 3.1f),
                scale: new Vector3(0.22f, 2.5f, 0.22f),
                mat: matMetal);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // 砲管加強環
            for (int i = 0; i < 3; i++)
            {
                float ringZ = 1.5f + i * 1.1f;
                var ring = MakePrim(PrimitiveType.Cylinder, $"BarrelRing_{i}", barrelPivot,
                    pos: new Vector3(0f, 0f, ringZ),
                    scale: new Vector3(0.30f, 0.10f, 0.30f),
                    mat: matMetal);
                ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // ── Muzzle Bell（砲口加大）────────────────────────────
            var muzzleBell = MakePrim(PrimitiveType.Cylinder, "MuzzleBell", barrelPivot,
                pos: new Vector3(0f, 0f, 5.5f),
                scale: new Vector3(0.36f, 0.28f, 0.36f),
                mat: matMuzzle);
            muzzleBell.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // ── Muzzle（砲口空點，保留原有引用）────────────────────
            var muzzleGO = barrelPivot.Find("Muzzle");
            if (muzzleGO == null)
            {
                muzzleGO = new GameObject("Muzzle").transform;
                muzzleGO.SetParent(barrelPivot);
            }
            muzzleGO.localPosition = new Vector3(0f, 0f, 5.9f);
            muzzleGO.localRotation = Quaternion.identity;

            // ClearVisualChildren 可能已刪除舊 Muzzle，須補回 ProjectileLauncher 的引用
            var launcher = barrelPivot.GetComponent<ProjectileLauncher>();
            if (launcher != null)
            {
                var soL = new SerializedObject(launcher);
                soL.FindProperty("muzzle").objectReferenceValue = muzzleGO;
                soL.ApplyModifiedProperties();
            }

            // ── CannonRecoil ────────────────────────────────────────
            if (!barrelPivot.TryGetComponent<CannonRecoil>(out _))
            {
                var recoil = barrelPivot.gameObject.AddComponent<CannonRecoil>();
                var so = new SerializedObject(recoil);
                so.FindProperty("barrel").objectReferenceValue = barrel.transform;
                so.ApplyModifiedProperties();
            }

            Debug.Log("[CannonVisualBuilder] 炮台外觀建立完成。");
            Selection.activeGameObject = artBase;
        }

        private static void ClearVisualChildren(Transform parent, Transform preserve)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == preserve) continue;
                // 保留有 MonoBehaviour（邏輯腳本）的物件
                if (child.GetComponents<MonoBehaviour>().Length > 0) continue;
                Object.DestroyImmediate(child.gameObject);
            }
        }

        private static GameObject MakePrim(PrimitiveType type, string name, Transform parent,
            Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.identity;
            go.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void MakeWheel(string name, Transform parent, Vector3 localPos, Material mat)
        {
            var wheel = MakePrim(PrimitiveType.Cylinder, name, parent,
                pos: localPos,
                scale: new Vector3(0.18f, 0.75f, 0.75f),
                mat: mat);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // 輪輻（3根）
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 60f;
                var spoke = MakePrim(PrimitiveType.Cylinder, $"Spoke_{i}", wheel.transform,
                    pos: Vector3.zero,
                    scale: new Vector3(0.08f, 0.85f, 0.08f),
                    mat: mat);
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static Material MakeMat(Color color)
        {
            // Sprites/Default 在所有 Unity 渲染管線（URP/Built-in）均存在，不顯示粉紅
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }
    }
#endif
}
