using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 按住滑鼠右鍵放大精瞄（ADS）。取代舊的自由觀測模式。
    /// 停留在 Aim 模式（不影響瞄準與落點預覽），只改變 FOV。
    /// 掛在 Main Camera 上。（觸控平台目前無右鍵，之後可接一個放大按鈕。）
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ObservationMode : MonoBehaviour
    {
        private Camera _cam;
        private float  _fovVel;
        private const float FovTime = 0.16f;

        private void Awake() => _cam = GetComponent<Camera>();

        private void LateUpdate()
        {
            bool zoom = CameraDirector.IsAiming
                     && Mouse.current != null
                     && Mouse.current.rightButton.isPressed;

            float target = zoom ? GameConfig.ObserveFOV : GameConfig.AimFOV;
            _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, target, ref _fovVel, FovTime);
        }
    }
}
