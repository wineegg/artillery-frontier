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
            var old = GameObject.Find("TargetZone");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("TargetZone");

            var mgr = new GameObject("GameManager");
            mgr.transform.SetParent(root.transform);
            mgr.AddComponent<Inventory>();
            mgr.AddComponent<CastleManager>();

            BuildResourceZone(root.transform);
            BuildCastleZone(root.transform);

            Debug.Log("[TargetZoneBuilder] 完成。礦脈 7 個（126-156m），城堡 6 段（152-163m）。");
        }

        // ── 礦脈佈局 ─────────────────────────────────────────────────
        // 全部拉近到 ~125-156m（180m 山丘之前，襯著山丘/天空更好辨識）、體積放大，
        // 配合螢幕標記，玩家看得到方向與距離、可用落點距離對照瞄準。
        private static void BuildResourceZone(Transform parent)
        {
            var zone = new GameObject("ResourceZone");
            zone.transform.SetParent(parent);

            Color flagStone  = new Color(0.55f, 0.55f, 0.60f);
            Color flagIron   = new Color(0.25f, 0.50f, 0.90f);
            Color flagSulfur = new Color(1.00f, 0.88f, 0.10f);

            // Stone — 灰色大方塊，角度 15-37°，dist ~126-138m
            SpawnNode(zone.transform, "Stone_A", ResourceType.Stone,
                new Vector3( 58f, 2f, 119f), new Color(0.48f, 0.48f, 0.50f),
                PrimitiveType.Cube, new Vector3(20f, 20f, 20f), flagStone);
            SpawnNode(zone.transform, "Stone_B", ResourceType.Stone,
                new Vector3( 83f, 2f, 110f), new Color(0.44f, 0.43f, 0.46f),
                PrimitiveType.Cube, new Vector3(20f, 20f, 20f), flagStone);
            SpawnNode(zone.transform, "Stone_C", ResourceType.Stone,
                new Vector3( 33f, 2f, 122f), new Color(0.50f, 0.50f, 0.52f),
                PrimitiveType.Cube, new Vector3(20f, 20f, 20f), flagStone);

            // Iron — 高藍色圓柱，角度 ±50°，dist ~156m
            SpawnNode(zone.transform, "Iron_A", ResourceType.Iron,
                new Vector3( 120f, 2f, 100f), new Color(0.18f, 0.38f, 0.72f),
                PrimitiveType.Cylinder, new Vector3(10f, 20f, 10f), flagIron);
            SpawnNode(zone.transform, "Iron_B", ResourceType.Iron,
                new Vector3(-120f, 2f, 100f), new Color(0.15f, 0.34f, 0.68f),
                PrimitiveType.Cylinder, new Vector3(10f, 20f, 10f), flagIron);

            // Sulfur — 亮黃球，角度 -19 ~ -28°，dist ~149-155m
            SpawnNode(zone.transform, "Sulfur_A", ResourceType.Sulfur,
                new Vector3(-73f, 2f, 137f), new Color(0.95f, 0.88f, 0.06f),
                PrimitiveType.Sphere, new Vector3(20f, 20f, 20f), flagSulfur);
            SpawnNode(zone.transform, "Sulfur_B", ResourceType.Sulfur,
                new Vector3(-48f, 2f, 141f), new Color(0.92f, 0.85f, 0.06f),
                PrimitiveType.Sphere, new Vector3(20f, 20f, 20f), flagSulfur);
        }

        private static void SpawnNode(Transform parent, string nodeName,
            ResourceType type, Vector3 pos, Color color,
            PrimitiveType shape, Vector3 scale, Color flagColor)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = nodeName;
            go.transform.SetParent(parent);
            go.transform.position   = pos;
            go.transform.localScale = scale;

            var mat = new Material(Shader.Find("Sprites/Default")) { color = color };
            go.GetComponent<Renderer>().material = mat;

            var node = go.AddComponent<ResourceNode>();
            var so   = new SerializedObject(node);
            so.FindProperty("nodeType").enumValueIndex = (int)type;
            so.ApplyModifiedProperties();

            // 計算節點頂部高度：center.y + halfHeight（scale的y軸半徑）
            float topY = pos.y + scale.y * 0.5f;

            AddShadow(parent, pos, scale.x * 0.55f);
            AddFlag(parent, pos, topY, flagColor);
        }

        // 地面陰影：扁平深色圓柱，平貼地面
        private static void AddShadow(Transform parent, Vector3 center, float radius)
        {
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow";
            shadow.transform.SetParent(parent);
            shadow.transform.position   = new Vector3(center.x, 0.3f, center.z);
            shadow.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0f, 0f, 0f, 0.55f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = 3000;
            shadow.GetComponent<Renderer>().material = mat;
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
        }

        // 旗幟：旗桿 + 彩旗（子物件，不隨節點Scale變形）
        private static void AddFlag(Transform parent, Vector3 center, float topY, Color flagColor)
        {
            float poleRadius = 0.22f;
            float poleHeight = 9f;
            float poleBaseY  = topY;
            float poleMidY   = poleBaseY + poleHeight * 0.5f;

            // 旗桿
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "FlagPole";
            pole.transform.SetParent(parent);
            pole.transform.position   = new Vector3(center.x, poleMidY, center.z);
            pole.transform.localScale = new Vector3(poleRadius, poleHeight * 0.5f, poleRadius);
            pole.GetComponent<Renderer>().material =
                new Material(Shader.Find("Sprites/Default")) { color = new Color(0.88f, 0.88f, 0.88f) };
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            // 旗面
            float flagW  = 3.2f;
            float flagH  = 1.8f;
            float flagTopY = poleBaseY + poleHeight;
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(parent);
            flag.transform.position   = new Vector3(center.x + flagW * 0.5f, flagTopY - flagH * 0.5f, center.z);
            flag.transform.localScale = new Vector3(flagW, flagH, 0.18f);
            flag.GetComponent<Renderer>().material =
                new Material(Shader.Find("Sprites/Default")) { color = flagColor };
            Object.DestroyImmediate(flag.GetComponent<Collider>());
        }

        // ── 城堡佈局（維持原距離 152-163m）─────────────────────────────
        private static void BuildCastleZone(Transform parent)
        {
            var zone = new GameObject("CastleZone");
            zone.transform.SetParent(parent);

            Color wallColor  = new Color(0.62f, 0.60f, 0.55f);
            Color towerColor = new Color(0.50f, 0.48f, 0.44f);
            Color keepColor  = new Color(0.35f, 0.33f, 0.30f);

            SpawnSection(zone.transform, "Wall_L",  StructureType.Wall,
                new Vector3(-18f, 2f, 152f), wallColor,  new Vector3(15f, 7f, 3f));
            SpawnSection(zone.transform, "Wall_C",  StructureType.Wall,
                new Vector3(  0f, 2f, 152f), wallColor,  new Vector3(15f, 7f, 3f));
            SpawnSection(zone.transform, "Wall_R",  StructureType.Wall,
                new Vector3( 18f, 2f, 152f), wallColor,  new Vector3(15f, 7f, 3f));
            SpawnSection(zone.transform, "Tower_L", StructureType.Tower,
                new Vector3(-30f, 2f, 152f), towerColor, new Vector3(5f,  7f, 5f));
            SpawnSection(zone.transform, "Tower_R", StructureType.Tower,
                new Vector3( 30f, 2f, 152f), towerColor, new Vector3(5f,  7f, 5f));
            SpawnSection(zone.transform, "Keep",    StructureType.Keep,
                new Vector3(  0f, 2f, 163f), keepColor,  new Vector3(14f, 10f, 12f));
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
