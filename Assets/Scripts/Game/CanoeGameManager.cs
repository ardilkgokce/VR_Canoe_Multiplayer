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
    /// TimerManager ve ScoreManager ile birlikte calisir.
    /// </summary>
    public class CanoeGameManager : MonoBehaviourPunCallbacks
    {
        public static CanoeGameManager Instance { get; private set; }

        [Header("Ayarlar")]
        [SerializeField] private GameSettings gameSettings;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action<GameState> OnGameStateChanged;
        public event Action OnGameReset;
        public event Action<int> OnCountdownTick; // 3, 2, 1, 0 (GO!)
        public event Action OnGameFinished;

        // Properties
        public GameState CurrentState { get; private set; } = GameState.WaitingForPlayers;
        public GameSettings Settings => gameSettings;
        public float RemainingTime => TimerManager.Instance != null ? TimerManager.Instance.RemainingTime : 0f;
        public bool IsPlaying => CurrentState == GameState.Playing;

        // Room property key
        private const string GAME_STATE_KEY = "GameState";

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

            // TimerManager eventlerini dinle
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnCountdownTick += HandleCountdownTick;
                TimerManager.Instance.OnCountdownFinished += HandleCountdownFinished;
                TimerManager.Instance.OnTimeUp += HandleTimeUp;
            }

            // FinishLine eventini dinle
            FinishLine.OnFinishLineCrossed += HandleFinishLineCrossed;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoinedEvent -= OnPlayerCountChanged;
                NetworkManager.Instance.OnPlayerLeftEvent -= OnPlayerCountChanged;
                NetworkManager.Instance.OnJoinedRoomEvent -= OnNetworkJoinedRoom;
            }

            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.OnCountdownTick -= HandleCountdownTick;
                TimerManager.Instance.OnCountdownFinished -= HandleCountdownFinished;
                TimerManager.Instance.OnTimeUp -= HandleTimeUp;
            }

            FinishLine.OnFinishLineCrossed -= HandleFinishLineCrossed;
        }

        private void Update()
        {
            HandleKeyboardShortcuts();
        }

        #region Timer Events

        private void HandleCountdownTick(int value)
        {
            OnCountdownTick?.Invoke(value);
        }

        private void HandleCountdownFinished()
        {
            // GO! - Oyunu baslat
            if (PhotonNetwork.IsMasterClient)
            {
                StartGame();
            }
        }

        private void HandleTimeUp()
        {
            // Sure bitti - Oyunu bitir
            if (PhotonNetwork.IsMasterClient)
            {
                if (showDebugInfo)
                {
                    Debug.Log("[CanoeGameManager] Sure bitti!");
                }
                FinishGame();
            }
        }

        private void HandleFinishLineCrossed(float finishTime)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[CanoeGameManager] Bitis cizgisi gecildi: {finishTime:F2}s");
            }
            // FinishLine zaten FinishGame cagiriyor
        }

        #endregion

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
                if (showDebugInfo)
                {
                    Debug.LogWarning("[CanoeGameManager] 2 oyuncu gerekli!");
                }
                return;
            }

            SetState(GameState.EnteringNames);

            if (showDebugInfo)
            {
                Debug.Log("[CanoeGameManager] Isim girisi basladi");
            }
        }

        /// <summary>
        /// Isim girisi tamamlandi, hazir durumuna gec.
        /// </summary>
        public void ConfirmNames()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.EnteringNames) return;

            SetState(GameState.Ready);

            if (showDebugInfo)
            {
                Debug.Log("[CanoeGameManager] Isimler onaylandi, Ready state");
            }
        }

        /// <summary>
        /// Geri sayimi baslat.
        /// </summary>
        public void StartCountdown()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.Ready) return;

            SetState(GameState.Countdown);
            // TimerManager OnGameStateChanged event'ini dinleyerek countdown'u baslatacak
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

            SetState(GameState.Playing);
            // TimerManager OnGameStateChanged event'ini dinleyerek timer'i baslatacak
        }

        /// <summary>
        /// Oyunu bitir.
        /// </summary>
        public void FinishGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (CurrentState != GameState.Playing) return;

            SetState(GameState.Finished);

            // Final skoru logla
            if (showDebugInfo && ScoreManager.Instance != null)
            {
                Debug.Log($"[CanoeGameManager] Oyun bitti! Toplam Skor: {ScoreManager.Instance.TotalScore}");
            }

            // Event firlat
            photonView.RPC(nameof(RPC_OnGameFinished), RpcTarget.All);
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

            if (showDebugInfo)
            {
                Debug.Log("[CanoeGameManager] Oyun resetleniyor...");
            }

            // Timer'i sifirla
            if (TimerManager.Instance != null)
            {
                TimerManager.Instance.ResetTimer();
            }

            // Skorlari sifirla
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScores();
            }

            // Reset event'i firlat (pozisyonlar, coinler vs. resetlensin)
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
            CurrentState = newState;

            if (showDebugInfo)
            {
                Debug.Log($"[CanoeGameManager] State guncellendi: {newState}");
            }

            OnGameStateChanged?.Invoke(newState);
        }

        [PunRPC]
        private void RPC_OnGameFinished()
        {
            OnGameFinished?.Invoke();
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
