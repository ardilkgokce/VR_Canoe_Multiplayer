using System;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace VRCanoe.Game
{
    /// <summary>
    /// Zaman yonetimi. Oyun suresi, geri sayim.
    /// MasterClient zamani yonetir, diger clientlar sync alir.
    /// </summary>
    public class TimerManager : MonoBehaviourPunCallbacks
    {
        public static TimerManager Instance { get; private set; }

        [Header("Ayarlar")]
        [SerializeField] private GameSettings gameSettings;

        [Header("Countdown")]
        [Tooltip("Geri sayim suresi (3-2-1)")]
        [SerializeField] private float countdownDuration = 3f;

        [Header("Sync")]
        [Tooltip("Kalan sureyi ne siklikla sync et (saniye)")]
        [SerializeField] private float syncInterval = 1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action<float> OnTimeChanged;      // Kalan sure
        public event Action<int> OnCountdownTick;      // 3, 2, 1
        public event Action OnCountdownFinished;       // GO!
        public event Action OnTimeUp;                  // Sure bitti

        // State
        private float _remainingTime;
        private float _elapsedTime;
        private float _countdownTimer;
        private int _lastCountdownValue;
        private bool _isPaused;
        private bool _isRunning;
        private float _lastSyncTime;

        // Room property keys
        private const string REMAINING_TIME_KEY = "RemainingTime";
        private const string ELAPSED_TIME_KEY = "ElapsedTime";
        private const string IS_RUNNING_KEY = "TimerRunning";
        private const string SERVER_TIME_KEY = "TimerServerTime";

        // Properties
        public float RemainingTime => _remainingTime;
        public float ElapsedTime => _elapsedTime;
        public float GameDuration => gameSettings != null ? gameSettings.gameDuration : 60f;
        public bool IsRunning => _isRunning && !_isPaused;
        public bool IsPaused => _isPaused;
        public float CountdownTimer => _countdownTimer;

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
            // GameSettings bul
            if (gameSettings == null && CanoeGameManager.Instance != null)
            {
                gameSettings = CanoeGameManager.Instance.Settings;
            }

            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                CanoeGameManager.Instance.OnGameReset += OnGameReset;
            }
        }

        private void OnDestroy()
        {
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                CanoeGameManager.Instance.OnGameReset -= OnGameReset;
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            UpdateCountdown();
            UpdateGameTimer();
        }

        #region Countdown

        /// <summary>
        /// Geri sayimi baslat (3-2-1-GO).
        /// </summary>
        public void StartCountdown()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _countdownTimer = countdownDuration;
            _lastCountdownValue = Mathf.CeilToInt(countdownDuration) + 1;

            if (showDebugInfo)
            {
                Debug.Log($"[TimerManager] Countdown basladi: {countdownDuration}s");
            }
        }

        private void UpdateCountdown()
        {
            if (_countdownTimer <= 0) return;
            if (CanoeGameManager.Instance == null) return;
            if (CanoeGameManager.Instance.CurrentState != GameState.Countdown) return;

            _countdownTimer -= Time.deltaTime;
            int currentValue = Mathf.CeilToInt(_countdownTimer);

            // Her saniye tick gonder
            if (currentValue != _lastCountdownValue && currentValue >= 0)
            {
                _lastCountdownValue = currentValue;
                photonView.RPC(nameof(RPC_CountdownTick), RpcTarget.All, currentValue);
            }

            // Countdown bitti
            if (_countdownTimer <= 0f)
            {
                _countdownTimer = 0f;
                photonView.RPC(nameof(RPC_CountdownFinished), RpcTarget.All);
            }
        }

        #endregion

        #region Game Timer

        /// <summary>
        /// Oyun zamanlayicisini baslat.
        /// </summary>
        public void StartTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _remainingTime = GameDuration;
            _elapsedTime = 0f;
            _isRunning = true;
            _isPaused = false;
            _lastSyncTime = Time.time;

            SyncTimer();

            if (showDebugInfo)
            {
                Debug.Log($"[TimerManager] Timer basladi: {_remainingTime}s");
            }
        }

        /// <summary>
        /// Zamanlayiciyi durdur.
        /// </summary>
        public void StopTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _isRunning = false;
            SyncTimer();

            if (showDebugInfo)
            {
                Debug.Log($"[TimerManager] Timer durduruldu. Gecen sure: {_elapsedTime:F1}s");
            }
        }

        /// <summary>
        /// Zamanlayiciyi duraklat.
        /// </summary>
        public void PauseTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _isPaused = true;
            SyncTimer();

            if (showDebugInfo)
            {
                Debug.Log("[TimerManager] Timer duraklatildi");
            }
        }

        /// <summary>
        /// Zamanlayiciyi devam ettir.
        /// </summary>
        public void ResumeTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _isPaused = false;
            SyncTimer();

            if (showDebugInfo)
            {
                Debug.Log("[TimerManager] Timer devam ediyor");
            }
        }

        /// <summary>
        /// Zamanlayiciyi sifirla.
        /// </summary>
        public void ResetTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _remainingTime = GameDuration;
            _elapsedTime = 0f;
            _isRunning = false;
            _isPaused = false;
            _countdownTimer = 0f;

            SyncTimer();

            if (showDebugInfo)
            {
                Debug.Log("[TimerManager] Timer sifirlandi");
            }
        }

        private void UpdateGameTimer()
        {
            if (!_isRunning || _isPaused) return;
            if (CanoeGameManager.Instance == null) return;
            if (CanoeGameManager.Instance.CurrentState != GameState.Playing) return;

            float deltaTime = Time.deltaTime;
            _remainingTime -= deltaTime;
            _elapsedTime += deltaTime;

            // Periyodik sync
            if (Time.time - _lastSyncTime >= syncInterval)
            {
                _lastSyncTime = Time.time;
                SyncTimer();
            }

            // Sure bitti
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _isRunning = false;

                SyncTimer();
                photonView.RPC(nameof(RPC_OnTimeUp), RpcTarget.All);
            }
        }

        private void SyncTimer()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // Room property olarak kaydet
            Hashtable props = new Hashtable
            {
                { REMAINING_TIME_KEY, _remainingTime },
                { ELAPSED_TIME_KEY, _elapsedTime },
                { IS_RUNNING_KEY, _isRunning && !_isPaused },
                { SERVER_TIME_KEY, PhotonNetwork.ServerTimestamp }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // RPC ile aninda bildir
            photonView.RPC(nameof(RPC_SyncTimer), RpcTarget.Others, _remainingTime, _elapsedTime, _isRunning, _isPaused);
        }

        #endregion

        #region Game State Events

        private void OnGameStateChanged(GameState newState)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            switch (newState)
            {
                case GameState.Countdown:
                    StartCountdown();
                    break;

                case GameState.Playing:
                    StartTimer();
                    break;

                case GameState.Finished:
                    StopTimer();
                    break;
            }
        }

        private void OnGameReset()
        {
            _remainingTime = GameDuration;
            _elapsedTime = 0f;
            _isRunning = false;
            _isPaused = false;
            _countdownTimer = 0f;

            OnTimeChanged?.Invoke(_remainingTime);
        }

        #endregion

        #region RPCs

        [PunRPC]
        private void RPC_CountdownTick(int value)
        {
            _lastCountdownValue = value;
            OnCountdownTick?.Invoke(value);

            if (showDebugInfo)
            {
                Debug.Log($"[TimerManager] Countdown: {value}");
            }
        }

        [PunRPC]
        private void RPC_CountdownFinished()
        {
            _countdownTimer = 0f;
            OnCountdownFinished?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("[TimerManager] GO!");
            }
        }

        [PunRPC]
        private void RPC_SyncTimer(float remaining, float elapsed, bool running, bool paused)
        {
            _remainingTime = remaining;
            _elapsedTime = elapsed;
            _isRunning = running;
            _isPaused = paused;

            OnTimeChanged?.Invoke(_remainingTime);
        }

        [PunRPC]
        private void RPC_OnTimeUp()
        {
            _remainingTime = 0f;
            _isRunning = false;

            OnTimeUp?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log("[TimerManager] Sure bitti!");
            }
        }

        #endregion

        #region Photon Callbacks

        public override void OnJoinedRoom()
        {
            SyncFromRoom();
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (PhotonNetwork.IsMasterClient) return;

            bool updated = false;

            if (propertiesThatChanged.TryGetValue(REMAINING_TIME_KEY, out object remainObj))
            {
                _remainingTime = (float)remainObj;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(ELAPSED_TIME_KEY, out object elapsedObj))
            {
                _elapsedTime = (float)elapsedObj;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(IS_RUNNING_KEY, out object runningObj))
            {
                _isRunning = (bool)runningObj;
            }

            if (updated)
            {
                OnTimeChanged?.Invoke(_remainingTime);
            }
        }

        private void SyncFromRoom()
        {
            if (PhotonNetwork.CurrentRoom == null) return;
            if (PhotonNetwork.IsMasterClient) return;

            var props = PhotonNetwork.CurrentRoom.CustomProperties;

            if (props.TryGetValue(REMAINING_TIME_KEY, out object remainObj))
                _remainingTime = (float)remainObj;
            if (props.TryGetValue(ELAPSED_TIME_KEY, out object elapsedObj))
                _elapsedTime = (float)elapsedObj;
            if (props.TryGetValue(IS_RUNNING_KEY, out object runningObj))
                _isRunning = (bool)runningObj;

            OnTimeChanged?.Invoke(_remainingTime);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Kalan sureyi formatli string olarak al (MM:SS).
        /// </summary>
        public string GetFormattedRemainingTime()
        {
            int minutes = Mathf.FloorToInt(_remainingTime / 60f);
            int seconds = Mathf.FloorToInt(_remainingTime % 60f);
            return $"{minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Gecen sureyi formatli string olarak al (MM:SS.mm).
        /// </summary>
        public string GetFormattedElapsedTime()
        {
            int minutes = Mathf.FloorToInt(_elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(_elapsedTime % 60f);
            int milliseconds = Mathf.FloorToInt((_elapsedTime % 1f) * 100f);
            return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }

        #endregion

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowTimerDebug) return;

            GUILayout.BeginArea(new Rect(Screen.width - 200, 140, 190, 100));
            GUILayout.Box("Timer Manager");

            if (_countdownTimer > 0)
            {
                GUILayout.Label($"Countdown: {Mathf.CeilToInt(_countdownTimer)}");
            }

            GUILayout.Label($"Remaining: {GetFormattedRemainingTime()}");
            GUILayout.Label($"Elapsed: {GetFormattedElapsedTime()}");
            GUILayout.Label($"Running: {_isRunning}, Paused: {_isPaused}");
            GUILayout.EndArea();
        }
#endif
    }
}
