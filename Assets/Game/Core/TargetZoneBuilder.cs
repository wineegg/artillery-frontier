#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    public static class TargetZoneBuilder
    {
        [MenuItem("ArtilleryFrontier/Build Target Zone")]
        public static void Build()
        {
            // 清除舊 TargetZone 避免重複
            var old = GameObject.Find("TargetZone");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("TargetZone");

            // ── GameManager（Inventory + CastleManager）───────────────
            var mgr = new GameObject("GameManager");
            mgr.transform.SetParent(root.transform);
            mgr.AddComponent<Inventory>();
            mgr.AddComponent<CastleManager>();

            // ── 礦脈區（z = 40-75m）─────────────────────────────────
            BuildResourceZone(root.transform);

            // ── 城堡區（z = 110-128m）───────────────────────────────
            BuildCastleZone(root.transform);

            Debug.Log("[TargetZoneBuilder] 場景建立完成：礦脈 7 個，城堡 6 段。");
        }

        // ── 礦脈 ─────────────────────────────────────────────────────
        private static void BuildResourceZone(Transform parent)
        {
            var zone = new GameObject("ResourceZone");
            zone.transform.SetParent(parent);

            // Stone — 灰色方塊
            SpawnNode(zone.transform, "Stone_A", ResourceType.Stone,
                new Vector3(-15f, 2f, 50f), new Color(0.45f, 0.45f, 0.48f), PrimitiveType.Cube,
                new Vector3(2.5f, 2.5f, 2.5f));
            SpawnNode(zone.transform, "Stone_B", ResourceType.Stone,
                new Vector3( 0f,  2f, 54f), new Color(0.40f, 0.40f, 0.43f), PrimitiveType.Cube,
                new Vector3(2f,   3f,  2f));
            SpawnNode(zone.transform, "Stone_C", ResourceType.Stone,
                new Vector3( 14f, 2f, 48f), new Color(0.48f, 0.47f, 0.50f), PrimitiveType.Cube,
                new Vector3(2.5f, 2f, 2.5f));

            // Iron — 深紅棕圓柱
            SpawnNode(zone.transform, "Iron_A", ResourceType.Iron,
                new Vector3(-10f, 2f, 70f), new Color(0.55f, 0.22f, 0.10f), PrimitiveType.Cylinder,
                new Vector3(2f,   1.5f, 2f));
            SpawnNode(zone.transform, "Iron_B", ResourceType.Iron,
                new Vector3( 10f, 2f, 73f), new Color(0.58f, 0.25f, 0.12f), PrimitiveType.Cylinder,
                new Vector3(2f,   2f,   2f));

            // Sulfur — 亮黃球
            SpawnNode(zone.transform, "Sulfur_A", ResourceType.Sulfur,
                new Vector3(-22f, 2f, 42f), new Color(0.92f, 0.85f, 0.06f), PrimitiveType.Sphere,
                new Vector3(2.2f, 2.2f, 2.2f));
            SpawnNode(zone.transform, "Sulfur_B", ResourceType.Sulfur,
                new Vector3( 21f, 2f, 44f), new Color(0.95f, 0.88f, 0.08f), PrimitiveType.Sphere,
                new Vector3(2f,   2f,   2f));
        }

        private static void SpawnNode(Transform parent, string nodeName,
            ResourceType type, Vector3 pos, Color color,
            PrimitiveType shape, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = nodeName;
            go.transform.SetParent(parent);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<BoxCollider>();  // 統一用 BoxCollider 供彈道碰撞

            var mat = new Material(Shader.Find("Sprites/Default")) { color = color };
            go.GetComponent<Renderer>().material = mat;

            var node = go.AddComponent<ResourceNode>();
            var so   = new SerializedObject(node);
            so.FindProperty("nodeType").enumValueIndex = (int)type;
            so.ApplyModifiedProperties();
        }

        // ── 城堡 ─────────────────────────────────────────────────────
        private static void BuildCastleZone(Transform parent)
        {
            var zone = new GameObject("CastleZone");
            zone.transform.SetParent(parent);

            Color wallColor  = new Color(0.62f, 0.60f, 0.55f);  // 石灰色
            Color towerColor = new Color(0.50f, 0.48f, 0.44f);  // 深石色
            Color keepColor  = new Color(0.35f, 0.33f, 0.30f);  // 近黑石

            // 城牆（3 段）
            SpawnSection(zone.transform, "Wall_L",  StructureType.Wall,
                new Vector3(-18f, 2f, 110f), wallColor,  new Vector3(13f, 5f, 2.5f));
            SpawnSection(zone.transform, "Wall_C",  StructureType.Wall,
                new Vector3(  0f, 2f, 110f), wallColor,  new Vector3(13f, 5f, 2.5f));
            SpawnSection(zone.transform, "Wall_R",  StructureType.Wall,
                new Vector3( 18f, 2f, 110f), wallColor,  new Vector3(13f, 5f, 2.5f));

            // 塔樓（兩翼）— Cylinder：height = 2*scaleY
            SpawnSection(zone.transform, "Tower_L", StructureType.Tower,
                new Vector3(-30f, 2f, 110f), towerColor, new Vector3(4.5f, 6f, 4.5f));
            SpawnSection(zone.transform, "Tower_R", StructureType.Tower,
                new Vector3( 30f, 2f, 110f), towerColor, new Vector3(4.5f, 6f, 4.5f));

            // 城堡主塔
            SpawnSection(zone.transform, "Keep",    StructureType.Keep,
                new Vector3(  0f, 2f, 126f), keepColor,  new Vector3(12f, 8f, 10f));
        }

        private static void SpawnSection(Transform parent, string sectionName,
            StructureType type, Vector3 pos, Color color, Vector3 scale)
        {
            bool isTower = (type == StructureType.Tower);
            var go = GameObject.CreatePrimitive(isTower ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            go.name = sectionName;
            go.transform.SetParent(parent);
            go.transform.position   = pos;
            go.transform.localScale = scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.AddComponent<BoxCollider>();

            var mat = new Material(Shader.Find("Sprites/Default")) { color = color };
            go.GetComponent<Renderer>().material = mat;

            var cs = go.AddComponent<CastleSection>();
            var so = new SerializedObject(cs);
            so.FindProperty("structureType").enumValueIndex = (int)type;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
