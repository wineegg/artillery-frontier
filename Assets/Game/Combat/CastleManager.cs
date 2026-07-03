using UnityEngine;
using ArtilleryFrontier.Core;

namespace ArtilleryFrontier.Combat
{
    public class CastleManager : MonoBehaviour
    {
        private int _total;
        private int _destroyed;

        private void OnEnable()  => GameEvents.TargetDestroyed += OnTargetDestroyed;
        private void OnDisable() => GameEvents.TargetDestroyed -= OnTargetDestroyed;

        private void Start()
        {
            _total     = FindObjectsByType<CastleSection>(FindObjectsSortMode.None).Length;
            _destroyed = 0;
        }

        private void OnTargetDestroyed(DestructibleTarget target)
        {
            if (target is not CastleSection) return;

            _destroyed++;
            if (_destroyed >= _total && _total > 0)
                GameEvents.RaiseAreaCleared();
        }
    }
}
