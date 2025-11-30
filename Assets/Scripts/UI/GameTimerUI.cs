using UnityEngine;
using TMPro;
using VRCanoe.Game;

namespace VRCanoe.UI
{
    /// <summary>
    /// World Space UI - Countdown ve Sure Bitti panellerini yonetir.
    /// Main Camera'ya child olarak eklenir, oyuncunun onunde sabit durur.
    /// </summary>
    public class GameTimerUI : MonoBehaviour
    {
        [Header("Countdown Panel")]
        [Tooltip("Countdown paneli (3-2-1-GO!)")]
        [SerializeField] private GameObject countdownPanel;

        [Tooltip("Countdown sayi text'i")]
        [SerializeField] private TextMeshProUGUI countdownText;

        [Header("Kalan Sure (Oyun Icinde)")]
        [Tooltip("Kalan sure paneli")]
        [SerializeField] private GameObject remainingTimePanel;

        [Tooltip("Kalan sure text'i")]
        [SerializeField] private TextMeshProUGUI remainingTimeText;

        [Header("Sure Bitti Panel")]
        [Tooltip("Sure bitti paneli")]
        [SerializeField] private GameObject timeUpPanel;

        [Tooltip("Sure bitti ana text")]
        [SerializeField] private TextMeshProUGUI timeUpText;

        [Tooltip("Final skor text (opsiyonel)")]
        [SerializeField] private TextMeshProUGUI finalScoreText;

        [Header("Animasyon Ayarlari")]
        [Tooltip("Countdown text buyume efekti")]
        [SerializeField] private bool animateCountdown = true;

        [Tooltip("Countdown text baslangic boyutu")]
        [SerializeField] private float countdownStartScale = 2f;

        [Tooltip("Countdown text bitis boyutu")]
        [SerializeField] private float countdownEndScale = 1f;

        [Tooltip("Countdown animasyon suresi")]
        [SerializeField] private float countdownAnimDuration = 0.5f;

        [Header("GO! Ayarlari")]
        [Tooltip("GO! yazisi")]
        [SerializeField] private string goText = "GO!";

        [Tooltip("GO! ne kadar sure gosterilsin")]
        [SerializeField] private float goDisplayDuration = 1f;

        [Header("Sure Bitti Ayarlari")]
        [Tooltip("Sure bitti yazisi")]
        [SerializeField] private string timeUpMessage = "SÜRE BİTTİ!";

        [Tooltip("Sure bitti paneli ne kadar sure gosterilsin (0 = surekli)")]
        [SerializeField] private float timeUpDisplayDuration = 0f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Animasyon state
        private float _countdownAnimTime;
        private bool _isAnimatingCountdown;
        private Vector3 _countdownOriginalScale;

        private void Awake()
        {
            // Original scale kaydet
            if (countdownText != null)
            {
                _countdownOriginalScale = countdownText.transform.localScale;
            }
        }

        private void Start()
        {
            // Baslangicta panelleri kapat
            HideAllPanels();

            // TimerManager eventlerini dinle
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnCountdownTick += OnCountdownTick;
                TimerManager.Instance.OnCountdownFinished += OnCountdownFinished;
                TimerManager.Instance.OnTimeUp += OnTimeUp;
                TimerManager.Instance.OnTimeChanged += OnTimeChanged;

                if (showDebugInfo)
                {
                    Debug.Log($"[GameTimerUI] TimerManager baglandi. GameDuration: {TimerManager.Instance.GameDuration}s");
                }
            }
            else
            {
                Debug.LogWarning("[GameTimerUI] TimerManager.Instance bulunamadi!");
            }

            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                CanoeGameManager.Instance.OnGameReset += OnGameReset;
            }
            else
            {
                Debug.LogWarning("[GameTimerUI] CanoeGameManager.Instance bulunamadi!");
            }
        }

        private void OnDestroy()
        {
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnCountdownTick -= OnCountdownTick;
                TimerManager.Instance.OnCountdownFinished -= OnCountdownFinished;
                TimerManager.Instance.OnTimeUp -= OnTimeUp;
                TimerManager.Instance.OnTimeChanged -= OnTimeChanged;
            }

            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                CanoeGameManager.Instance.OnGameReset -= OnGameReset;
            }
        }

        private void Update()
        {
            // Countdown animasyonu
            if (_isAnimatingCountdown && countdownText != null)
            {
                _countdownAnimTime += Time.deltaTime;
                float t = Mathf.Clamp01(_countdownAnimTime / countdownAnimDuration);

                // Scale: buyukten kucuge
                float scale = Mathf.Lerp(countdownStartScale, countdownEndScale, t);
                countdownText.transform.localScale = _countdownOriginalScale * scale;

                // Alpha: saydamdan opak'a
                Color color = countdownText.color;
                color.a = t;
                countdownText.color = color;

                if (t >= 1f)
                {
                    _isAnimatingCountdown = false;
                }
            }

            // Kalan sureyi surekli guncelle (Playing state'de)
            UpdateRemainingTimeDisplay();
        }

        /// <summary>
        /// Kalan sure text'ini guncelle
        /// </summary>
        private void UpdateRemainingTimeDisplay()
        {
            if (remainingTimeText == null) return;
            if (remainingTimePanel == null || !remainingTimePanel.activeInHierarchy) return;

            if (TimerManager.Instance != null)
            {
                float remainingTime = TimerManager.Instance.RemainingTime;
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                remainingTimeText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        #region Event Handlers

        /// <summary>
        /// Countdown tick (3, 2, 1)
        /// </summary>
        private void OnCountdownTick(int value)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[GameTimerUI] Countdown: {value}");
            }

            // Countdown panelini goster
            ShowCountdownPanel();

            // Text guncelle
            if (countdownText != null)
            {
                countdownText.text = value.ToString();

                // Animasyon baslat
                if (animateCountdown)
                {
                    StartCountdownAnimation();
                }
            }
        }

        /// <summary>
        /// Countdown bitti - GO!
        /// </summary>
        private void OnCountdownFinished()
        {
            if (showDebugInfo)
            {
                Debug.Log("[GameTimerUI] GO!");
            }

            // GO! goster
            if (countdownText != null)
            {
                countdownText.text = goText;

                if (animateCountdown)
                {
                    StartCountdownAnimation();
                }
            }

            // Belirli sure sonra countdown panelini kapat
            Invoke(nameof(HideCountdownPanel), goDisplayDuration);
        }

        /// <summary>
        /// Sure bitti
        /// </summary>
        private void OnTimeUp()
        {
            if (showDebugInfo)
            {
                Debug.Log("[GameTimerUI] Sure bitti!");
            }

            // Kalan sure panelini kapat
            HideRemainingTimePanel();

            // Sure bitti panelini goster
            ShowTimeUpPanel();

            // Skor goster (opsiyonel)
            if (finalScoreText != null && ScoreManager.Instance != null)
            {
                finalScoreText.text = $"Skor: {ScoreManager.Instance.TotalScore}";
            }

            // Belirli sure sonra kapat (0 = surekli)
            if (timeUpDisplayDuration > 0)
            {
                Invoke(nameof(HideTimeUpPanel), timeUpDisplayDuration);
            }
        }

        /// <summary>
        /// Kalan sure degisti
        /// </summary>
        private void OnTimeChanged(float remainingTime)
        {
            if (remainingTimeText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                remainingTimeText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        /// <summary>
        /// Oyun state degisti
        /// </summary>
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.WaitingForPlayers:
                case GameState.EnteringNames:
                case GameState.Ready:
                    HideAllPanels();
                    break;

                case GameState.Countdown:
                    // Countdown event'leri ile yonetilecek
                    HideRemainingTimePanel();
                    HideTimeUpPanel();
                    break;

                case GameState.Playing:
                    HideCountdownPanel();
                    ShowRemainingTimePanel();
                    HideTimeUpPanel();
                    break;

                case GameState.Finished:
                    // TimeUp event'i ile yonetilecek
                    break;
            }
        }

        /// <summary>
        /// Oyun resetlendi
        /// </summary>
        private void OnGameReset()
        {
            HideAllPanels();
        }

        #endregion

        #region Panel Control

        private void ShowCountdownPanel()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true);
            }
        }

        private void HideCountdownPanel()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
        }

        private void ShowRemainingTimePanel()
        {
            if (remainingTimePanel != null)
            {
                remainingTimePanel.SetActive(true);

                // Baslangic suresini hemen goster
                if (TimerManager.Instance != null && remainingTimeText != null)
                {
                    // GameDuration'dan al (RemainingTime henuz set edilmemis olabilir)
                    float initialTime = TimerManager.Instance.RemainingTime;
                    if (initialTime <= 0)
                    {
                        initialTime = TimerManager.Instance.GameDuration;
                    }

                    int minutes = Mathf.FloorToInt(initialTime / 60f);
                    int seconds = Mathf.FloorToInt(initialTime % 60f);
                    remainingTimeText.text = $"{minutes:00}:{seconds:00}";

                    if (showDebugInfo)
                    {
                        Debug.Log($"[GameTimerUI] RemainingTimePanel acildi, sure: {minutes:00}:{seconds:00}");
                    }
                }
            }
        }

        private void HideRemainingTimePanel()
        {
            if (remainingTimePanel != null)
            {
                remainingTimePanel.SetActive(false);
            }
        }

        private void ShowTimeUpPanel()
        {
            if (timeUpPanel != null)
            {
                timeUpPanel.SetActive(true);

                if (timeUpText != null)
                {
                    timeUpText.text = timeUpMessage;
                }
            }
        }

        private void HideTimeUpPanel()
        {
            if (timeUpPanel != null)
            {
                timeUpPanel.SetActive(false);
            }
        }

        private void HideAllPanels()
        {
            HideCountdownPanel();
            HideRemainingTimePanel();
            HideTimeUpPanel();
        }

        #endregion

        #region Animation

        private void StartCountdownAnimation()
        {
            _countdownAnimTime = 0f;
            _isAnimatingCountdown = true;

            // Baslangic scale ve alpha
            if (countdownText != null)
            {
                countdownText.transform.localScale = _countdownOriginalScale * countdownStartScale;
                Color color = countdownText.color;
                color.a = 0f;
                countdownText.color = color;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manuel olarak countdown panelini goster (test icin)
        /// </summary>
        public void TestShowCountdown(int value)
        {
            OnCountdownTick(value);
        }

        /// <summary>
        /// Manuel olarak sure bitti panelini goster (test icin)
        /// </summary>
        public void TestShowTimeUp()
        {
            OnTimeUp();
        }

        #endregion
    }
}
