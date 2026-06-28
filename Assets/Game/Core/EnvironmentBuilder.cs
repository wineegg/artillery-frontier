using UnityEngine;

namespace ArtilleryFrontier.Core
{
    public class EnvironmentBuilder : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] private int terrainSize = 500;
        [SerializeField] private float terrainMaxHeight = 30f;
        [SerializeField] private float noiseScale = 3.5f;

        [Header("Fog")]
        [SerializeField] private Color fogColor = new Color(0.72f, 0.65f, 0.52f, 1f);
        [SerializeField] private float fogDensity = 0.004f;

        private void Start()
        {
            RemoveLegacyPlane();
            BuildTerrain();
            BuildDistantHills();
            ConfigureLighting();
            ConfigureFog();
            ConfigureSkybox();
        }

        private void RemoveLegacyPlane()
        {
            var plane = GameObject.Find("Ground");
            if (plane != null) Destroy(plane);
        }

        private void BuildTerrain()
        {
            var td = new TerrainData();
            td.heightmapResolution = 257;
            td.size = new Vector3(terrainSize, terrainMaxHeight, terrainSize);

            int res = td.heightmapResolution;
            float[,] heights = new float[res, res];

            for (int x = 0; x < res; x++)
            {
                for (int z = 0; z < res; z++)
                {
                    float nx = x / (float)res;
                    float nz = z / (float)res;

                    float h = Mathf.PerlinNoise(nx * noiseScale, nz * noiseScale) * 0.4f
                            + Mathf.PerlinNoise(nx * 8f + 100f, nz * 8f) * 0.08f;

                    // 中央平台（炮台站立位置）
                    float dx = nx - 0.5f;
                    float dz = nz - 0.5f;
                    float centerDist = Mathf.Sqrt(dx * dx + dz * dz);
                    float flatMask = Mathf.SmoothStep(0.12f, 0.22f, centerDist);
                    heights[x, z] = Mathf.Lerp(0.018f, h, flatMask);
                }
            }

            td.SetHeights(0, 0, heights);

            var terrainGO = Terrain.CreateTerrainGameObject(td);
            terrainGO.name = "Terrain";
            terrainGO.transform.position = new Vector3(-terrainSize * 0.5f, 0f, -terrainSize * 0.5f);

            // 簡單草綠色（URP Terrain 材質）
            if (terrainGO.TryGetComponent<Terrain>(out var terrain))
            {
                terrain.materialTemplate = null; // 使用預設
            }
        }

        private void BuildDistantHills()
        {
            var parent = new GameObject("DistantHills");
            var darkColor = new Color(0.13f, 0.15f, 0.18f);

            // 環繞炮台的遠景山丘
            (float angle, float dist, float w, float h, float d)[] hills = new[]
            {
                (  0f, 190f, 80f, 35f, 45f),
                ( 35f, 210f, 60f, 28f, 38f),
                ( 70f, 180f, 90f, 40f, 50f),
                (105f, 220f, 55f, 25f, 35f),
                (145f, 195f, 75f, 32f, 42f),
                (185f, 205f, 65f, 38f, 40f),
                (230f, 185f, 85f, 30f, 48f),
                (270f, 215f, 70f, 36f, 44f),
                (310f, 190f, 60f, 42f, 38f),
                (345f, 200f, 78f, 29f, 46f),
            };

            foreach (var (angle, dist, w, h, d) in hills)
            {
                float rad = angle * Mathf.Deg2Rad;
                var hill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hill.name = "Hill";
                hill.transform.SetParent(parent.transform);
                hill.transform.position = new Vector3(Mathf.Sin(rad) * dist, h * 0.5f, Mathf.Cos(rad) * dist);
                hill.transform.localScale = new Vector3(w, h, d);
                hill.transform.rotation = Quaternion.Euler(0f, angle, 0f);

                hill.GetComponent<Renderer>().material = MakeLitMaterial(darkColor, 0f, 0.1f);
                Destroy(hill.GetComponent<Collider>());
            }
        }

        private void ConfigureLighting()
        {
            Light dirLight = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { dirLight = l; break; }
            }

            if (dirLight == null)
            {
                var go = new GameObject("Directional Light");
                dirLight = go.AddComponent<Light>();
                dirLight.type = LightType.Directional;
            }

            dirLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            dirLight.color = new Color(1f, 0.93f, 0.80f);
            dirLight.intensity = 1.1f;
            dirLight.shadows = LightShadows.Soft;
            dirLight.shadowStrength = 0.65f;
            dirLight.shadowBias = 0.05f;
        }

        private void ConfigureFog()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }

        private void ConfigureSkybox()
        {
            var sky = new Material(Shader.Find("Skybox/Procedural"));
            if (sky == null) return;

            sky.SetFloat("_SunSize", 0.04f);
            sky.SetFloat("_SunSizeConvergence", 5f);
            sky.SetFloat("_AtmosphereThickness", 1.05f);
            sky.SetColor("_SkyTint", new Color(0.48f, 0.56f, 0.72f));
            sky.SetColor("_GroundColor", new Color(0.38f, 0.34f, 0.28f));
            sky.SetFloat("_Exposure", 1.15f);

            RenderSettings.skybox = sky;
            RenderSettings.ambientIntensity = 0.7f;
            DynamicGI.UpdateEnvironment();
        }

        public static Material MakeLitMaterial(Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                               ?? Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
    }
}
