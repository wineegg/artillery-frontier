using UnityEngine;

namespace ArtilleryFrontier.Combat
{
    public class CastleManager : MonoBehaviour
    {
        public static CastleManager Instance { get; private set; }

        private int _total;
        private int _destroyed;

        private void Awake() => Instance = this;

        private void Start()
        {
            _total     = FindObjectsByType<CastleSection>(FindObjectsSortMode.None).Length;
            _destroyed = 0;
        }

        public void OnSectionDestroyed()
        {
            _destroyed++;
            if (_destroyed >= _total && _total > 0)
                ArtilleryFrontier.UI.ArtilleryHUD.ShowAreaCleared();
        }
    }
}
