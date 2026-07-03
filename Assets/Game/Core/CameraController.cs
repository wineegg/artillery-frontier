using UnityEngine;

namespace ArtilleryFrontier.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Header("Micro Sway")]
        [SerializeField] private float swayAmplitude = 0.18f;
        [SerializeField] private float swaySpeed = 0.7f;

        [Header("Fire Shake")]
        [SerializeField] private float maxShakeDegrees = 2.8f;
        [SerializeField] private float traumaDecay = 4.5f;
        [SerializeField] private float shakeFrequency = 28f;

        private Camera _cam;
        private Quaternion _baseLocalRotation;
        private float _trauma;   // 0~1, squared → shake magnitude
        private float _swayT;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.fieldOfView = GameConfig.AimFOV;
            _baseLocalRotation = transform.localRotation;
        }

        private void Update()
        {
            _trauma = Mathf.Max(0f, _trauma - traumaDecay * Time.deltaTime);
            _swayT += Time.deltaTime * swaySpeed;

            Quaternion sway = CalcSway();
            Quaternion shake = CalcShake();

            transform.localRotation = _baseLocalRotation * sway * shake;
        }

        private Quaternion CalcSway()
        {
            float x = Mathf.Sin(_swayT * 1.27f) * swayAmplitude;
            float y = Mathf.Sin(_swayT * 0.83f) * swayAmplitude * 0.5f;
            float z = Mathf.Sin(_swayT * 0.61f) * swayAmplitude * 0.25f;
            return Quaternion.Euler(x, y, z);
        }

        private Quaternion CalcShake()
        {
            float mag = _trauma * _trauma;
            float t = Time.time * shakeFrequency;
            float x = (Mathf.PerlinNoise(t,       0.5f) * 2f - 1f) * maxShakeDegrees * mag;
            float y = (Mathf.PerlinNoise(0.5f,    t)    * 2f - 1f) * maxShakeDegrees * mag;
            float z = (Mathf.PerlinNoise(t + 0.3f, t)   * 2f - 1f) * maxShakeDegrees * 0.3f * mag;
            return Quaternion.Euler(x, y, z);
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }
    }
}
