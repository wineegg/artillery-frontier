using UnityEngine;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 相機模式的單一權威。取代散落各處的 ObservationMode.IsObserving /
    /// ProjectileTrackingCamera.IsTracking 兩個 static bool 互戳。
    ///
    /// 只有從 Aim 才能進入 Observe 或 Track（互斥）；子行為透過 TryEnter/Exit 回報。
    /// 其他系統一律讀 CameraDirector.IsAiming / IsObserving / IsTracking。
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        public enum Mode { Aim, Observe, Track }

        public static CameraDirector Instance { get; private set; }

        public Mode Current { get; private set; } = Mode.Aim;

        public static bool IsAiming    => Instance == null || Instance.Current == Mode.Aim;
        public static bool IsObserving => Instance != null && Instance.Current == Mode.Observe;
        public static bool IsTracking  => Instance != null && Instance.Current == Mode.Track;

        private void Awake()   => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// 嘗試進入模式：同模式視為成功；僅能從 Aim 切入其他模式，否則拒絕（互斥）。
        public bool TryEnter(Mode mode)
        {
            if (Current == mode)      return true;
            if (Current != Mode.Aim)  return false;
            Current = mode;
            return true;
        }

        /// 退出指定模式，回到 Aim（僅在目前正處於該模式時生效）。
        public void Exit(Mode mode)
        {
            if (Current == mode) Current = Mode.Aim;
        }
    }
}
