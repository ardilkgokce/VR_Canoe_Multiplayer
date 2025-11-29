using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VRCanoe.Game;

namespace VRCanoe.UI
{
    /// <summary>
    /// World Space'te skor tablosu gosterimi.
    /// 10 tane TeamName ve 10 tane Points text'i Inspector'dan atanir.
    /// Prefab kullanmaz, sabit text'leri gunceller.
    /// </summary>
    public class WorldSpaceScoreboard : MonoBehaviour
    {
        [Header("Canvas Referansi")]
        [Tooltip("World Space Canvas (bos birak = bu objede ara)")]
        [SerializeField] private Canvas worldSpaceCanvas;

        [Header("Baslik")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string headerText = "EN IYI SKORLAR";

        [Header("Takim Isimleri (10 adet)")]
        [Tooltip("Sirasyla 1-10 takim ismi text'leri")]
        [SerializeField] private List<TMP_Text> teamNameTexts = new List<TMP_Text>();

        [Header("Puanlar (10 adet)")]
        [Tooltip("Sirasyla 1-10 puan text'leri")]
        [SerializeField] private List<TMP_Text> pointsTexts = new List<TMP_Text>();

        [Header("Bos Satir Ayarlari")]
        [Tooltip("Skor yoksa gosterilecek takim ismi")]
        [SerializeField] private string emptyTeamName = "---";

        [Tooltip("Skor yoksa gosterilecek puan")]
        [SerializeField] private string emptyPoints = "-";

        [Header("Gosterim Ayarlari")]
        [Tooltip("Oyun bitiminde otomatik guncelle")]
        [SerializeField] private bool autoUpdateOnGameEnd = true;

        [Tooltip("Oyun bitiminde canvas'i aktif et")]
        [SerializeField] private bool showOnGameEnd = true;

        [Tooltip("Baslangicta gizle")]
        [SerializeField] private bool hideOnStart = true;

        private void Awake()
        {
            if (worldSpaceCanvas == null)
            {
                worldSpaceCanvas = GetComponent<Canvas>();
                if (worldSpaceCanvas == null)
                {
                    worldSpaceCanvas = GetComponentInChildren<Canvas>();
                }
            }
        }

        private void Start()
        {
            if (hideOnStart && worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(false);
            }

            // ScoreboardManager eventlerini dinle
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.OnScoresLoaded += RefreshDisplay;
                ScoreboardManager.Instance.OnNewScoreAdded += OnNewScore;
            }

            // GameManager eventlerini dinle
            if (autoUpdateOnGameEnd && CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished += OnGameFinished;
                CanoeGameManager.Instance.OnGameReset += OnGameReset;
            }

            // Baslik ayarla
            if (titleText != null)
            {
                titleText.text = headerText;
            }

            // Baslangicta mevcut skorlari goster (JSON'dan yuklenmis)
            // Eger skor yoksa bos gosterir
            RefreshDisplay();
        }

        private void OnDestroy()
        {
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.OnScoresLoaded -= RefreshDisplay;
                ScoreboardManager.Instance.OnNewScoreAdded -= OnNewScore;
            }

            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished -= OnGameFinished;
                CanoeGameManager.Instance.OnGameReset -= OnGameReset;
            }
        }

        /// <summary>
        /// Oyun bittiginde.
        /// </summary>
        private void OnGameFinished()
        {
            if (showOnGameEnd)
            {
                Show();
            }

            // Biraz bekle, skor eklendikten sonra guncelle
            Invoke(nameof(RefreshDisplay), 0.5f);
        }

        /// <summary>
        /// Oyun resetlendiginde.
        /// </summary>
        private void OnGameReset()
        {
            if (hideOnStart)
            {
                Hide();
            }
        }

        /// <summary>
        /// Yeni skor eklendiginde.
        /// </summary>
        private void OnNewScore(ScoreEntry entry)
        {
            RefreshDisplay();
        }

        /// <summary>
        /// Skor tablosunu guncelle.
        /// </summary>
        public void RefreshDisplay()
        {
            if (ScoreboardManager.Instance == null)
            {
                ClearAllRows();
                return;
            }

            List<ScoreEntry> topScores = ScoreboardManager.Instance.GetTopTenScores();
            UpdateRows(topScores);
        }

        /// <summary>
        /// Satirlari skorlarla guncelle.
        /// </summary>
        private void UpdateRows(List<ScoreEntry> scores)
        {
            int maxRows = Mathf.Min(teamNameTexts.Count, pointsTexts.Count);

            for (int i = 0; i < maxRows; i++)
            {
                if (i < scores.Count)
                {
                    // Skor var - doldur
                    ScoreEntry entry = scores[i];

                    if (teamNameTexts[i] != null)
                    {
                        teamNameTexts[i].text = entry.teamName;
                    }

                    if (pointsTexts[i] != null)
                    {
                        pointsTexts[i].text = entry.score.ToString();
                    }
                }
                else
                {
                    // Skor yok - bos goster
                    if (teamNameTexts[i] != null)
                    {
                        teamNameTexts[i].text = emptyTeamName;
                    }

                    if (pointsTexts[i] != null)
                    {
                        pointsTexts[i].text = emptyPoints;
                    }
                }
            }
        }

        /// <summary>
        /// Tum satirlari temizle (bos goster).
        /// </summary>
        public void ClearAllRows()
        {
            int maxRows = Mathf.Min(teamNameTexts.Count, pointsTexts.Count);

            for (int i = 0; i < maxRows; i++)
            {
                if (teamNameTexts[i] != null)
                {
                    teamNameTexts[i].text = emptyTeamName;
                }

                if (pointsTexts[i] != null)
                {
                    pointsTexts[i].text = emptyPoints;
                }
            }
        }

        /// <summary>
        /// Belirli bir satiri manuel guncelle.
        /// </summary>
        public void SetRow(int index, string teamName, int points)
        {
            if (index < 0 || index >= teamNameTexts.Count || index >= pointsTexts.Count)
            {
                Debug.LogWarning($"[WorldSpaceScoreboard] Gecersiz index: {index}");
                return;
            }

            if (teamNameTexts[index] != null)
            {
                teamNameTexts[index].text = teamName;
            }

            if (pointsTexts[index] != null)
            {
                pointsTexts[index].text = points.ToString();
            }
        }

        /// <summary>
        /// Belirli bir satiri temizle.
        /// </summary>
        public void ClearRow(int index)
        {
            if (index < 0 || index >= teamNameTexts.Count || index >= pointsTexts.Count)
            {
                return;
            }

            if (teamNameTexts[index] != null)
            {
                teamNameTexts[index].text = emptyTeamName;
            }

            if (pointsTexts[index] != null)
            {
                pointsTexts[index].text = emptyPoints;
            }
        }

        /// <summary>
        /// Scoreboard'u goster.
        /// </summary>
        public void Show()
        {
            if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(true);
            }
            RefreshDisplay();
        }

        /// <summary>
        /// Scoreboard'u gizle.
        /// </summary>
        public void Hide()
        {
            if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Toggle gosterim.
        /// </summary>
        public void Toggle()
        {
            if (worldSpaceCanvas != null)
            {
                if (worldSpaceCanvas.gameObject.activeSelf)
                {
                    Hide();
                }
                else
                {
                    Show();
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test: Fill With Sample Data")]
        private void TestFillSampleData()
        {
            string[] teams = { "Kartallar", "Aslanlar", "Kaplanlar", "Delfin", "Yildizlar",
                              "Atmacalar", "Kurtlar", "Sahinler", "Akrepler", "Bozkurtlar" };

            int[] scores = { 950, 875, 820, 780, 720, 680, 620, 580, 540, 500 };

            int count = Mathf.Min(teamNameTexts.Count, pointsTexts.Count, teams.Length);

            for (int i = 0; i < count; i++)
            {
                SetRow(i, teams[i], scores[i]);
            }
        }

        [ContextMenu("Clear All Rows")]
        private void TestClearAllRows()
        {
            ClearAllRows();
        }
#endif
    }
}
