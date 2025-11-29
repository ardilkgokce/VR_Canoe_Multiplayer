using UnityEngine;
using TMPro;
using VRCanoe.Game;

namespace VRCanoe.UI
{
    /// <summary>
    /// Oyun sonu tebrik mesaji gosterimi.
    /// "Tebrikler <takim ismi>! Puaniniz: <puan>"
    /// Birden fazla World Space Canvas'a yerlestirilebilir.
    /// </summary>
    public class CongratulationsDisplay : MonoBehaviour
    {
        [Header("UI Elemanlari")]
        [Tooltip("Ana mesaj text (Tebrikler <takim>!)")]
        [SerializeField] private TMP_Text congratsText;

        [Tooltip("Skor text (Puaniniz: XXX)")]
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("Sure text (Sure: XX:XX.XX)")]
        [SerializeField] private TMP_Text timeText;

        [Tooltip("Oyuncu isimleri text")]
        [SerializeField] private TMP_Text playersText;

        [Tooltip("Siralama text (Top 10'a girdiniz!)")]
        [SerializeField] private TMP_Text rankText;

        [Header("Tek Text Modu")]
        [Tooltip("Tum bilgiyi tek text'te goster")]
        [SerializeField] private TMP_Text combinedText;

        [Header("Canvas Referansi")]
        [Tooltip("Root canvas veya panel (bos birak = bu obje)")]
        [SerializeField] private GameObject displayRoot;

        [Header("Format")]
        [SerializeField] private string congratsFormat = "Tebrikler {0}!";
        [SerializeField] private string scoreFormat = "Puaniniz: {0}";
        [SerializeField] private string timeFormat = "Sure: {0}";
        [SerializeField] private string playersFormat = "{0} & {1}";
        [SerializeField] private string rankFormat = "{0}. sirada!";
        [SerializeField] private string newHighScoreText = "YENI REKOR!";

        [Header("Kombinasyon Format")]
        [SerializeField] [TextArea(3, 5)] private string combinedFormat =
            "Tebrikler {0}!\n\nPuaniniz: {1}\nSure: {2}\n\n{3}";

        [Header("Ayarlar")]
        [Tooltip("Oyun bitiminde otomatik goster")]
        [SerializeField] private bool showOnGameEnd = true;

        [Tooltip("Baslangicta gizle")]
        [SerializeField] private bool hideOnStart = true;

        [Tooltip("High score durumunda farkli gosterim")]
        [SerializeField] private bool highlightHighScore = true;

        [Header("High Score Gorselleri")]
        [Tooltip("High score durumunda aktif olacak obje")]
        [SerializeField] private GameObject highScoreEffect;

        [Tooltip("Normal durum rengi")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("High score rengi")]
        [SerializeField] private Color highScoreColor = Color.yellow;

        // Son gosterilen bilgiler
        private string _lastTeamName;
        private int _lastScore;
        private float _lastTime;
        private int _lastRank;

        private void Awake()
        {
            if (displayRoot == null)
            {
                displayRoot = gameObject;
            }
        }

        private void Start()
        {
            if (hideOnStart)
            {
                Hide();
            }

            // High score efektini gizle
            if (highScoreEffect != null)
            {
                highScoreEffect.SetActive(false);
            }

            // GameManager eventlerini dinle
            if (showOnGameEnd && CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished += OnGameFinished;
                CanoeGameManager.Instance.OnGameReset += OnGameReset;
            }

            // ScoreboardManager eventlerini dinle
            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.OnNewHighScore += OnNewHighScore;
            }
        }

        private void OnDestroy()
        {
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameFinished -= OnGameFinished;
                CanoeGameManager.Instance.OnGameReset -= OnGameReset;
            }

            if (ScoreboardManager.Instance != null)
            {
                ScoreboardManager.Instance.OnNewHighScore -= OnNewHighScore;
            }
        }

        /// <summary>
        /// Oyun bittiginde.
        /// </summary>
        private void OnGameFinished()
        {
            // Bilgileri topla
            string teamName = "Takim";
            string player1 = "Oyuncu 1";
            string player2 = "Oyuncu 2";
            int score = 0;
            float time = 0f;

            if (NameManager.Instance != null)
            {
                teamName = NameManager.Instance.TeamName;
                player1 = NameManager.Instance.Player1Name;
                player2 = NameManager.Instance.Player2Name;
            }

            if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.TotalScore;
                time = ScoreManager.Instance.FinishTime;
            }

            if (TimerManager.Instance != null && time <= 0)
            {
                time = TimerManager.Instance.ElapsedTime;
            }

            // Goster
            ShowResult(teamName, score, time, player1, player2);
        }

        /// <summary>
        /// Oyun resetlendiginde.
        /// </summary>
        private void OnGameReset()
        {
            Hide();
        }

        /// <summary>
        /// Yeni high score yapildiginda.
        /// </summary>
        private void OnNewHighScore(ScoreEntry entry, int rank)
        {
            _lastRank = rank;
            UpdateRankDisplay(rank, true);
        }

        /// <summary>
        /// Sonuc bilgilerini goster.
        /// </summary>
        public void ShowResult(string teamName, int score, float time, string player1 = "", string player2 = "")
        {
            _lastTeamName = teamName;
            _lastScore = score;
            _lastTime = time;

            // Formatlı sure
            string formattedTime = FormatTime(time);

            // Ayri text'ler
            if (congratsText != null)
            {
                congratsText.text = string.Format(congratsFormat, teamName);
            }

            if (scoreText != null)
            {
                scoreText.text = string.Format(scoreFormat, score);
            }

            if (timeText != null)
            {
                timeText.text = string.Format(timeFormat, formattedTime);
            }

            if (playersText != null && !string.IsNullOrEmpty(player1))
            {
                playersText.text = string.Format(playersFormat, player1, player2);
            }

            // Kombine text
            if (combinedText != null)
            {
                string rankStr = _lastRank > 0 ? string.Format(rankFormat, _lastRank) : "";
                combinedText.text = string.Format(combinedFormat, teamName, score, formattedTime, rankStr);
            }

            Show();
        }

        /// <summary>
        /// Siralama gosterimini guncelle.
        /// </summary>
        private void UpdateRankDisplay(int rank, bool isHighScore)
        {
            if (rankText != null)
            {
                if (isHighScore && rank <= 3)
                {
                    rankText.text = newHighScoreText;
                }
                else if (rank > 0 && rank <= 10)
                {
                    rankText.text = string.Format(rankFormat, rank);
                }
                else
                {
                    rankText.text = "";
                }
            }

            // High score efekti
            if (highlightHighScore && isHighScore)
            {
                if (highScoreEffect != null)
                {
                    highScoreEffect.SetActive(true);
                }

                // Renkleri degistir
                if (congratsText != null) congratsText.color = highScoreColor;
                if (scoreText != null) scoreText.color = highScoreColor;
                if (combinedText != null) combinedText.color = highScoreColor;
            }
        }

        /// <summary>
        /// Sureyi formatla.
        /// </summary>
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
            return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }

        /// <summary>
        /// Goster.
        /// </summary>
        public void Show()
        {
            if (displayRoot != null)
            {
                displayRoot.SetActive(true);
            }
        }

        /// <summary>
        /// Gizle.
        /// </summary>
        public void Hide()
        {
            if (displayRoot != null)
            {
                displayRoot.SetActive(false);
            }

            // High score efektini gizle
            if (highScoreEffect != null)
            {
                highScoreEffect.SetActive(false);
            }

            // Renkleri sifirla
            if (congratsText != null) congratsText.color = normalColor;
            if (scoreText != null) scoreText.color = normalColor;
            if (combinedText != null) combinedText.color = normalColor;

            _lastRank = 0;
        }

        /// <summary>
        /// Manuel test icin.
        /// </summary>
        public void TestShow()
        {
            ShowResult("Test Takim", 500, 45.5f, "Player A", "Player B");
        }
    }
}
