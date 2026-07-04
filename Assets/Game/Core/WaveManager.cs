using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ArtilleryFrontier.Combat;

namespace ArtilleryFrontier.Core
{
    /// <summary>
    /// 戰局大腦：分波生成敵人、追蹤存活數、基地 HP、勝敗判定、重開。
    /// 敵人朝原點（基地=砲台）前進；攻到基地扣 HP，HP 歸零=失敗；全波清完=勝利。
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [SerializeField] private int   baseMaxHP        = 100;
        [SerializeField] private int   totalWaves       = 5;
        [SerializeField] private float startDelay       = 3f;
        [SerializeField] private float spawnInterval    = 1.4f;
        [SerializeField] private float betweenWaveDelay = 4f;
        [SerializeField] private float spawnDistance    = 200f;

        private int  _baseHP;
        private int  _wave;
        private int  _alive;
        private bool _gameOver;
        private bool _won;

        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Start()
        {
            _baseHP = baseMaxHP;
            GameEvents.RaiseBaseChanged(_baseHP, baseMaxHP);
            GameEvents.RaiseWaveChanged(0, totalWaves);
            StartCoroutine(RunGame());
        }

        private void Update()
        {
            // 結束後按 R 重開
            if ((_gameOver || _won) && Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private IEnumerator RunGame()
        {
            yield return new WaitForSeconds(startDelay);

            for (int w = 1; w <= totalWaves; w++)
            {
                if (_gameOver) yield break;
                _wave = w;
                GameEvents.RaiseWaveChanged(w, totalWaves);

                foreach (var type in Composition(w))
                {
                    if (_gameOver) yield break;
                    SpawnOne(type);
                    yield return new WaitForSeconds(spawnInterval);
                }

                // 等本波清空（打死或到達皆算清）
                while (_alive > 0 && !_gameOver) yield return null;
                if (_gameOver) yield break;

                if (w < totalWaves)
                    yield return new WaitForSeconds(betweenWaveDelay);
            }

            if (!_gameOver) Victory();
        }

        private void SpawnOne(EnemyType type)
        {
            // 前方 ±60° 隨機方位，距離 spawnDistance 附近
            float bearing = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
            float dist    = spawnDistance + Random.Range(-15f, 15f);
            Vector3 pos   = new Vector3(Mathf.Sin(bearing) * dist, 0f, Mathf.Cos(bearing) * dist);

            Enemy.Spawn(type, pos);
            _alive++;
        }

        // ── Enemy 回呼 ───────────────────────────────────────────────
        public void OnEnemyKilled()
        {
            _alive = Mathf.Max(0, _alive - 1);
        }

        public void OnEnemyReachedBase(int dmg)
        {
            _alive = Mathf.Max(0, _alive - 1);
            if (_gameOver || _won) return;

            _baseHP = Mathf.Max(0, _baseHP - dmg);
            GameEvents.RaiseBaseChanged(_baseHP, baseMaxHP);
            if (_baseHP <= 0) GameOver();
        }

        private void GameOver()
        {
            if (_gameOver) return;
            _gameOver = true;
            GameEvents.RaiseGameOver();
        }

        private void Victory()
        {
            if (_won) return;
            _won = true;
            GameEvents.RaiseVictory();
        }

        // ── 波次組成（手調，隨波加重）────────────────────────────────
        private static IEnumerable<EnemyType> Composition(int wave)
        {
            switch (wave)
            {
                case 1: return new[] { G, G, G, G, G };
                case 2: return new[] { G, G, O, G, O, G };
                case 3: return new[] { G, O, G, O, B, G, O };
                case 4: return new[] { O, O, B, O, B, O, B };
                case 5: return new[] { B, O, B, D, B, O };   // boss 波
                default: return new[] { G, G, O };
            }
        }

        private const EnemyType G = EnemyType.Goblin;
        private const EnemyType O = EnemyType.Orc;
        private const EnemyType B = EnemyType.Beetle;
        private const EnemyType D = EnemyType.Demon;
    }
}
