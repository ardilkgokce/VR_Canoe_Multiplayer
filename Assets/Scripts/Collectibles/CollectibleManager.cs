using System.Collections.Generic;
using UnityEngine;
using VRCanoe.Game;

namespace VRCanoe.Collectibles
{
    /// <summary>
    /// Tum collectible'lari yonetir.
    /// Oyun resetlendiginde tum collectible'lari geri getirir.
    /// </summary>
    public class CollectibleManager : MonoBehaviour
    {
        public static CollectibleManager Instance { get; private set; }

        [Header("Ayarlar")]
        [Tooltip("Sahnedeki tum collectible'lari otomatik bul")]
        [SerializeField] private bool autoFindCollectibles = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Tum collectible'lar
        private List<Collectible> _collectibles = new List<Collectible>();

        // Properties
        public int TotalCount => _collectibles.Count;
        public int CollectedCount => GetCollectedCount();
        public int RemainingCount => TotalCount - CollectedCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Collectible'lari bul
            if (autoFindCollectibles)
            {
                FindAllCollectibles();
            }

            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameReset += ResetAllCollectibles;
            }
        }

        private void OnDestroy()
        {
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameReset -= ResetAllCollectibles;
            }
        }

        /// <summary>
        /// Sahnedeki tum collectible'lari bul.
        /// </summary>
        public void FindAllCollectibles()
        {
            _collectibles.Clear();
            _collectibles.AddRange(FindObjectsOfType<Collectible>(true)); // true = inactive dahil

            if (showDebugInfo)
            {
                Debug.Log($"[CollectibleManager] {_collectibles.Count} collectible bulundu");
            }
        }

        /// <summary>
        /// Collectible ekle (runtime'da olusturulanlar icin).
        /// </summary>
        public void RegisterCollectible(Collectible collectible)
        {
            if (!_collectibles.Contains(collectible))
            {
                _collectibles.Add(collectible);
            }
        }

        /// <summary>
        /// Collectible kaldir.
        /// </summary>
        public void UnregisterCollectible(Collectible collectible)
        {
            _collectibles.Remove(collectible);
        }

        /// <summary>
        /// Tum collectible'lari resetle.
        /// </summary>
        public void ResetAllCollectibles()
        {
            foreach (var collectible in _collectibles)
            {
                if (collectible != null)
                {
                    collectible.Reset();
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[CollectibleManager] {_collectibles.Count} collectible resetlendi");
            }
        }

        /// <summary>
        /// Toplanan collectible sayisi.
        /// </summary>
        private int GetCollectedCount()
        {
            int count = 0;
            foreach (var collectible in _collectibles)
            {
                if (collectible != null && collectible.IsCollected)
                {
                    count++;
                }
            }
            return count;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowAllDebugUI) return;

            GUILayout.BeginArea(new Rect(Screen.width - 200, 250, 190, 60));
            GUILayout.Box("Collectibles");
            GUILayout.Label($"Collected: {CollectedCount} / {TotalCount}");
            GUILayout.EndArea();
        }
#endif
    }
}
