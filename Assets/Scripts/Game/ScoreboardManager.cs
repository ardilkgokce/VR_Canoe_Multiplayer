using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Photon.Pun;

namespace VRCanoe.Game
{
    /// <summary>
    /// Skor tablosu yonetimi.
    /// Skorlari JSON dosyasina kaydeder.
    /// Photon RPC ile tum client'lara senkronize eder.
    /// Tum oyuncu skorlari saklanir, sadece top 10 gosterilir.
    /// </summary>
    public class ScoreboardManager : MonoBehaviourPunCallbacks
    {
        public static ScoreboardManager Instance { get; private set; }

        [Header("Kayit Ayarlari")]
        [Tooltip("JSON dosya adi")]
        [SerializeField] private string jsonFileName = "scoreboard.json";

        [Tooltip("Gosterilecek maksimum skor sayisi")]
        [SerializeField] private int maxDisplayScores = 10;

        [Tooltip("PlayerPrefs'e de kaydet (yedek)")]
        [SerializeField] private bool usePlayerPrefs = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action<ScoreEntry> OnNewScoreAdded;
        public event Action<ScoreEntry, int> OnNewHighScore; // entry, rank
        public event Action OnScoresLoaded;

        // Data
        private ScoreboardData _data;
        private ScoreEntry _lastAddedEntry;

        // Keys
        private const string PLAYERPREFS_KEY = "ScoreboardData";

        // Properties
        public ScoreboardData Data => _data;
        public ScoreEntry LastAddedEntry => _lastAddedEntry;
        public List<ScoreEntry> TopScores => _data?.GetTopScores(maxDisplayScores) ?? new List<ScoreEntry>();
        public int TotalScoreCount => _data?.TotalCount ?? 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _data = new ScoreboardData { maxDisplayEntries = maxDisplayScores };
        }

        private void Start()
        {
            LoadScores();

            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished += OnGameFinished;
            }
        }

        private void OnDestroy()
        {
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished -= OnGameFinished;
            }
        }

        /// <summary>
        /// Oyun bittiginde skoru kaydet.
        /// Sadece MasterClient skoru toplar ve tum client'lara gonderir.
        /// </summary>
        private void OnGameFinished()
        {
            // Sadece MasterClient skor ekler
            if (!PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnected) return;

            // Skor bilgilerini al
            int score = 0;
            float finishTime = 0f;

            if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.TotalScore;
                finishTime = ScoreManager.Instance.FinishTime;
            }

            if (TimerManager.Instance != null && finishTime <= 0)
            {
                finishTime = TimerManager.Instance.ElapsedTime;
            }

            // Isim bilgilerini al
            string teamName = "Takim";
            string player1 = "Oyuncu 1";
            string player2 = "Oyuncu 2";

            if (NameManager.Instance != null)
            {
                teamName = NameManager.Instance.TeamName;
                player1 = NameManager.Instance.Player1Name;
                player2 = NameManager.Instance.Player2Name;
            }

            // Tarih
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            // Tum client'lara gonder
            if (PhotonNetwork.IsConnected)
            {
                photonView.RPC(nameof(RPC_AddScore), RpcTarget.All,
                    teamName, score, finishTime, player1, player2, date);
            }
            else
            {
                // Offline mod
                AddScoreLocal(teamName, score, finishTime, player1, player2, date);
            }
        }

        /// <summary>
        /// RPC: Tum client'larda skoru ekle.
        /// </summary>
        [PunRPC]
        private void RPC_AddScore(string teamName, int score, float finishTime,
            string player1, string player2, string date)
        {
            AddScoreLocal(teamName, score, finishTime, player1, player2, date);
        }

        /// <summary>
        /// Lokal olarak skor ekle ve kaydet.
        /// </summary>
        private void AddScoreLocal(string teamName, int score, float finishTime,
            string player1, string player2, string date)
        {
            ScoreEntry entry = new ScoreEntry
            {
                teamName = teamName,
                score = score,
                finishTime = finishTime,
                player1Name = player1,
                player2Name = player2,
                date = date
            };

            bool madeTopList = _data.AddScore(entry);
            _lastAddedEntry = entry;

            if (showDebugInfo)
            {
                Debug.Log($"[ScoreboardManager] Skor eklendi: {teamName} - {score} puan (Toplam: {_data.TotalCount} kayit)");
            }

            // JSON'a kaydet
            SaveScores();

            // Event firlat
            OnNewScoreAdded?.Invoke(entry);

            if (madeTopList)
            {
                int rank = _data.GetRank(entry);
                OnNewHighScore?.Invoke(entry, rank);

                if (showDebugInfo)
                {
                    Debug.Log($"[ScoreboardManager] Top {maxDisplayScores}'a girdi! Siralama: {rank}");
                }
            }
        }

        /// <summary>
        /// Manuel skor ekleme (test veya ozel durumlar icin).
        /// </summary>
        public void AddScore(string teamName, int score, float finishTime, string player1, string player2)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_AddScore), RpcTarget.All,
                    teamName, score, finishTime, player1, player2, date);
            }
            else
            {
                AddScoreLocal(teamName, score, finishTime, player1, player2, date);
            }
        }

        /// <summary>
        /// Skorlari JSON'a kaydet.
        /// </summary>
        public void SaveScores()
        {
            string json = JsonUtility.ToJson(_data, true);

            // JSON dosyasina kaydet
            try
            {
                string path = GetJsonFilePath();
                File.WriteAllText(path, json);

                if (showDebugInfo)
                {
                    Debug.Log($"[ScoreboardManager] JSON kaydedildi: {path} ({_data.TotalCount} kayit)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ScoreboardManager] JSON kayit hatasi: {e.Message}");
            }

            // PlayerPrefs'e yedek kaydet
            if (usePlayerPrefs)
            {
                try
                {
                    PlayerPrefs.SetString(PLAYERPREFS_KEY, json);
                    PlayerPrefs.Save();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ScoreboardManager] PlayerPrefs kayit hatasi: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Skorlari yukle.
        /// </summary>
        public void LoadScores()
        {
            bool loaded = false;

            // Oncelik JSON dosyasinda
            try
            {
                string path = GetJsonFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    _data = JsonUtility.FromJson<ScoreboardData>(json);
                    _data.maxDisplayEntries = maxDisplayScores;
                    loaded = true;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[ScoreboardManager] JSON'dan yuklendi: {_data.TotalCount} kayit");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ScoreboardManager] JSON yukleme hatasi: {e.Message}");
            }

            // JSON yoksa PlayerPrefs'ten dene
            if (!loaded && usePlayerPrefs && PlayerPrefs.HasKey(PLAYERPREFS_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PLAYERPREFS_KEY);
                    _data = JsonUtility.FromJson<ScoreboardData>(json);
                    _data.maxDisplayEntries = maxDisplayScores;
                    loaded = true;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[ScoreboardManager] PlayerPrefs'ten yuklendi: {_data.TotalCount} kayit");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ScoreboardManager] PlayerPrefs yukleme hatasi: {e.Message}");
                }
            }

            // Hicbiri yoksa bos data olustur
            if (!loaded)
            {
                _data = new ScoreboardData { maxDisplayEntries = maxDisplayScores };

                if (showDebugInfo)
                {
                    Debug.Log("[ScoreboardManager] Bos scoreboard olusturuldu");
                }
            }

            OnScoresLoaded?.Invoke();
        }

        /// <summary>
        /// Tum skorlari sil.
        /// </summary>
        public void ClearAllScores()
        {
            _data.ClearScores();
            SaveScores();

            if (showDebugInfo)
            {
                Debug.Log("[ScoreboardManager] Tum skorlar silindi");
            }

            OnScoresLoaded?.Invoke();
        }

        /// <summary>
        /// JSON dosya yolunu al.
        /// </summary>
        private string GetJsonFilePath()
        {
            return Path.Combine(Application.persistentDataPath, jsonFileName);
        }

        /// <summary>
        /// Top 10 skorlari getir.
        /// </summary>
        public List<ScoreEntry> GetTopTenScores()
        {
            return _data.GetTopScores(10);
        }

        /// <summary>
        /// Top N skorlari getir.
        /// </summary>
        public List<ScoreEntry> GetTopScores(int count)
        {
            return _data.GetTopScores(count);
        }

        /// <summary>
        /// Tum skorlari getir.
        /// </summary>
        public List<ScoreEntry> GetAllScores()
        {
            return _data.GetAllScores();
        }

        /// <summary>
        /// Belirli bir skorun sirasini getir.
        /// </summary>
        public int GetRank(ScoreEntry entry)
        {
            return _data.GetRank(entry);
        }

        /// <summary>
        /// Mevcut skorun top 10'a girip giremeyecegini kontrol et.
        /// </summary>
        public bool WouldMakeTopTen(int score)
        {
            return _data.IsInTopTen(score);
        }

#if UNITY_EDITOR
        [ContextMenu("Test: Add Random Score")]
        private void TestAddRandomScore()
        {
            string[] teamNames = { "Kartallar", "Aslanlar", "Kaplanlar", "Delfin", "Yildizlar" };
            string teamName = teamNames[UnityEngine.Random.Range(0, teamNames.Length)];
            int score = UnityEngine.Random.Range(100, 1000);
            float time = UnityEngine.Random.Range(30f, 60f);

            AddScore(teamName, score, time, "Test P1", "Test P2");
        }

        [ContextMenu("Clear All Scores")]
        private void TestClearScores()
        {
            ClearAllScores();
        }

        [ContextMenu("Show JSON Path")]
        private void ShowJsonPath()
        {
            Debug.Log($"JSON Path: {GetJsonFilePath()}");
        }

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowAllDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 320, 280, 220));
            GUILayout.Box($"Scoreboard (Top 5 / Toplam: {_data.TotalCount})");

            var topScores = _data.GetTopScores(5);
            for (int i = 0; i < topScores.Count; i++)
            {
                var entry = topScores[i];
                GUILayout.Label($"{i + 1}. {entry.teamName}: {entry.score} ({entry.GetFormattedTime()})");
                GUILayout.Label($"   {entry.player1Name} & {entry.player2Name} - {entry.date}");
            }

            if (topScores.Count == 0)
            {
                GUILayout.Label("Henuz skor yok");
            }

            GUILayout.EndArea();
        }
#endif
    }
}
