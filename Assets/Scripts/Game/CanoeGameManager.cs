using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using VRCanoe.Network;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace VRCanoe.Game
{
    /// <summary>
    /// Oyun state yonetimi. MasterClient tum state gecislerini kontrol eder.
    /// </summary>
    public class CanoeGameManager : MonoBehaviourPunCallbacks
    {
        public static CanoeGameManager Instance { get; private set; }

        [Header("Ayarlar")]
        [SerializeField] private GameSettings gameSettings;
        [SerializeField] private float countdownDuration = 3f;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action OnGameReset;
        public event Action<int> OnCountdownTick; // 3, 2, 1, 0 (GO!)

        // Properties
        public GameState CurrentState { get; private set; } = GameState.WaitingForPlayers;
        public GameSettings Settings => gameSettings;
        public float RemainingTime { get; private set; }
        public bool IsPlaying => CurrentState == GameState.Playing;

        // Room property key
        private const string GAME_STATE_KEY = "GameState";

        // Countdown
        private float _countdownTimer;
        private int _lastCountdownValue;

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
            // NetworkManager eventlerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoinedEvent += OnPlayerCountChanged;
                NetworkManager.Instance.OnPlayerLeftEvent += OnPlayerCountChanged;
                NetworkManager.Instance.OnJoinedRoomEvent += OnNetworkJoinedRoom;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoinedEvent -= OnPlayerCountChanged;
                NetworkManager.Instance.OnPlayerLeftEvent -= OnPlayerCountChanged;
                NetworkManager.Instance.OnJoinedRoomEvent -= OnNetworkJoinedRoom;
            }
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
            UpdateCountdown();
            UpdateGameTimer();
        }

        #region Keyboard Shortcuts

        private void HandleKeyboardShortcuts()
        {
            // Sadece MasterClient veya Spectator kontrol edebilir
            if (!CanControl()) return;

            if (Input.GetKeyDown(KeyCode.F1))
            {
                StartEnteringNames();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                QuickStart();
            }
            else if (Input.GetKeyDown(KeyCode.F5))
            {
                ResetGame();
            }
            else if (Input.GetKeyDown(KeyCode.F9))
            {
                EmergencyStop();
            }
        }

        private bool CanControl()
        {
            if (!PhotonNetwork.InRoom) return false;

            // MasterClient her zaman kontrol edebilir
            if (PhotonNetwork.IsMasterClient) return true;

            // Spectator da kontrol edebilir
            if (NetworkManager.Instance != null &&
                NetworkManager.Instance.LocalPlayerType == PlayerType.Spectator)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region State Transitions

        /// <summary>
        /// Isim girisi ekranina gec.
        /// </summary>
        public void StartEnteringNames()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.WaitingForPlayers && CurrentState != GameState.Finished) return;

            // 2 oyuncu gerekli
            if (NetworkManager.Instance == null || !NetworkManager.Instance.AllPlayersReady)
            {
                Debug.LogWarning("[CanoeGameManager] 2 oyuncu gerekli!");
                return;
            }

            SetState(GameState.EnteringNames);
        }

        /// <summary>
        /// Isim girisi tamamlandi, hazir durumuna gec.
        /// </summary>
        public void ConfirmNames()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.EnteringNames) return;

            SetState(GameState.Ready);
        }

        /// <summary>
        /// Geri sayimi baslat.
        /// </summary>
        public void StartCountdown()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.Ready) return;

            _countdownTimer = countdownDuration;
            _lastCountdownValue = Mathf.CeilToInt(countdownDuration);
            SetState(GameState.Countdown);
        }

        /// <summary>
        /// Isim girisi olmadan direkt baslat.
        /// </summary>
        public void QuickStart()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.WaitingForPlayers && CurrentState != GameState.Finished) return;

            // 2 oyuncu gerekli
            if (NetworkManager.Instance == null || !NetworkManager.Instance.AllPlayersReady)
            {
                Debug.LogWarning("[CanoeGameManager] 2 oyuncu gerekli!");
                return;
            }

            // Direkt Ready'ye gec ve countdown baslat
            SetState(GameState.Ready);
            StartCountdown();
        }

        /// <summary>
        /// Oyunu baslat (countdown bittikten sonra).
        /// </summary>
        private void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            RemainingTime = gameSettings != null ? gameSettings.gameDuration : 60f;
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Oyunu bitir.
        /// </summary>
        public void FinishGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Finished);
        }

        /// <summary>
        /// Acil durdur - hangi state'de olursa olsun Finished'a gec.
        /// </summary>
        public void EmergencyStop()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // Spectator ise MasterClient'a RPC gonder
                photonView.RPC(nameof(RPC_RequestEmergencyStop), RpcTarget.MasterClient);
                return;
            }

            Debug.LogWarning("[CanoeGameManager] Acil durdurma!");
            SetState(GameState.Finished);
        }

        /// <summary>
        /// Oyunu sifirla - skorlar, pozisyonlar, coinler.
        /// </summary>
        public void ResetGame()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // Spectator ise MasterClient'a RPC gonder
                photonView.RPC(nameof(RPC_RequestReset), RpcTarget.MasterClient);
                return;
            }

            Debug.Log("[CanoeGameManager] Oyun resetleniyor...");

            // State'i sifirla
            RemainingTime = 0f;
            _countdownTimer = 0f;

            // Reset event'i firlat (skorlar, pozisyonlar vs. resetlensin)
            photonView.RPC(nameof(RPC_OnGameReset), RpcTarget.All);

            // WaitingForPlayers'a don
            SetState(GameState.WaitingForPlayers);
        }

        #endregion

        #region State Management

        private void SetState(GameState newState)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState == newState) return;

            Debug.Log($"[CanoeGameManager] State: {CurrentState} -> {newState}");

            // Room property olarak kaydet (yeni katilan oyuncular icin)
            Hashtable props = new Hashtable { { GAME_STATE_KEY, (int)newState } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // Tum clientlara RPC ile bildir
            photonView.RPC(nameof(RPC_SetState), RpcTarget.All, (int)newState);
        }

        [PunRPC]
        private void RPC_SetState(int stateValue)
        {
            GameState newState = (GameState)stateValue;
            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[CanoeGameManager] State guncellendi: {newState}");
            OnGameStateChanged?.Invoke(newState);

            // State'e ozel islemler
            if (newState == GameState.Countdown && oldState != GameState.Countdown)
            {
                _countdownTimer = countdownDuration;
                _lastCountdownValue = Mathf.CeilToInt(countdownDuration);
            }
        }

        [PunRPC]
        private void RPC_OnGameReset()
        {
            Debug.Log("[CanoeGameManager] Oyun resetlendi");
            OnGameReset?.Invoke();
        }

        [PunRPC]
        private void RPC_RequestEmergencyStop()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                EmergencyStop();
            }
        }

        [PunRPC]
        private void RPC_RequestReset()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ResetGame();
            }
        }

        [PunRPC]
        private void RPC_CountdownTick(int value)
        {
            OnCountdownTick?.Invoke(value);
        }

        #endregion

        #region Timers

        private void UpdateCountdown()
        {
            if (CurrentState != GameState.Countdown) return;
            if (!PhotonNetwork.IsMasterClient) return;

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
                StartGame();
            }
        }

        private void UpdateGameTimer()
        {
            if (CurrentState != GameState.Playing) return;
            if (!PhotonNetwork.IsMasterClient) return;

            RemainingTime -= Time.deltaTime;

            // Sync remaining time periodically (her saniye)
            // Basit tutmak icin su an sadece MasterClient'ta tutuluyor
            // Gerekirse RPC ile senkronize edilebilir

            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                FinishGame();
            }
        }

        #endregion

        #region Photon Callbacks

        private void OnNetworkJoinedRoom()
        {
            // NetworkManager event'inden gelen callback
            SyncStateFromRoom();
        }

        public override void OnJoinedRoom()
        {
            // Photon callback
            SyncStateFromRoom();
        }

        private void SyncStateFromRoom()
        {
            // Room'a katildiginda mevcut state'i al
            if (PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(GAME_STATE_KEY, out object stateObj))
            {
                CurrentState = (GameState)(int)stateObj;
                Debug.Log($"[CanoeGameManager] Mevcut state alindi: {CurrentState}");
                OnGameStateChanged?.Invoke(CurrentState);
            }
        }

        private void OnPlayerCountChanged(Player player)
        {
            // Oyuncu sayisi degistiginde kontrol et
            if (!PhotonNetwork.IsMasterClient) return;

            // Oyun sirasinda oyuncu ayrilirsa
            if (CurrentState == GameState.Playing || CurrentState == GameState.Countdown)
            {
                if (NetworkManager.Instance != null && NetworkManager.Instance.PlayerCount < 2)
                {
                    Debug.LogWarning("[CanoeGameManager] Oyuncu ayrildi, oyun durduruluyor!");
                    SetState(GameState.WaitingForPlayers);
                }
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            // Room property degistiginde state'i guncelle (yedek mekanizma)
            if (propertiesThatChanged.TryGetValue(GAME_STATE_KEY, out object stateObj))
            {
                GameState newState = (GameState)(int)stateObj;
                if (CurrentState != newState)
                {
                    CurrentState = newState;
                    OnGameStateChanged?.Invoke(newState);
                }
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.Log($"[CanoeGameManager] Yeni MasterClient: {newMasterClient.NickName}");

            // Yeni MasterClient oyunu devam ettirebilir
            // Gerekirse ek logic eklenebilir
        }

        #endregion
    }
}
