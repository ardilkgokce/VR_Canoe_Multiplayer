using System;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace VRCanoe.Network
{
    public enum PlayerType
    {
        Player1,
        Player2,
        Spectator
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        ConnectedToMaster,
        JoiningRoom,
        InRoom
    }

    /// <summary>
    /// Photon PUN2 baglanti yonetimi.
    /// Oyun acildiginda otomatik baglanir ve sabit room'a katilir.
    /// </summary>
    [DefaultExecutionOrder(-100)] // PhotonView'dan once calissin
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Room Ayarlari")]
        [SerializeField] private string roomName = "CanoeRoom";
        [SerializeField] private byte maxPlayers = 3; // 2 oyuncu + 1 izleyici

        [Header("Oyuncu Ayarlari")]
        [SerializeField] private PlayerType playerType = PlayerType.Player1;

        // Events
        public event Action OnConnectedToMasterEvent;
        public event Action OnJoinedRoomEvent;
        public event Action<Player> OnPlayerJoinedEvent;
        public event Action<Player> OnPlayerLeftEvent;
        public event Action<ConnectionState> OnConnectionStateChanged;

        // Properties
        public ConnectionState CurrentState { get; private set; } = ConnectionState.Disconnected;
        public PlayerType LocalPlayerType => playerType;
        public int PlayerCount => GetPlayerCount();
        public int SpectatorCount => GetSpectatorCount();
        public bool IsRoomFull => PlayerCount >= 2;
        public bool AllPlayersReady => PlayerCount == 2;

        private const string PLAYER_TYPE_KEY = "PlayerType";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // Sahne yenilendi, eski NetworkManager zaten var
                // PhotonView duplicate hatasini onlemek icin hemen yok et
                DestroyImmediate(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Connect();
        }

        /// <summary>
        /// Photon sunucusuna baglan.
        /// </summary>
        public void Connect()
        {
            // Zaten room'daysak bir sey yapma (sahne yenilendi)
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[NetworkManager] Zaten room'da, yeniden baglanmaya gerek yok");
                SetState(ConnectionState.InRoom);

                // PlayerType'i yeniden set et (sahne yenilendi, sahne objeleri yeniden olusturuldu)
                SetPlayerTypeProperty();
                OnJoinedRoomEvent?.Invoke();
                return;
            }

            if (PhotonNetwork.IsConnected)
            {
                Debug.Log("[NetworkManager] Zaten bagli, room'a katiliniyor...");
                JoinRoom();
                return;
            }

            SetState(ConnectionState.Connecting);
            Debug.Log("[NetworkManager] Photon'a baglaniyor...");

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }

        /// <summary>
        /// Room'a katil veya olustur.
        /// </summary>
        private void JoinRoom()
        {
            SetState(ConnectionState.JoiningRoom);

            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = true,
                IsOpen = true,
                // Room asla otomatik kapanmasin
                EmptyRoomTtl = 0,
                PlayerTtl = -1 // Oyuncu ayrilinca hemen cikarilsin ama room kalsin
            };

            Debug.Log($"[NetworkManager] Room'a katiliniyor: {roomName}");
            PhotonNetwork.JoinOrCreateRoom(roomName, roomOptions, TypedLobby.Default);
        }

        private void SetState(ConnectionState newState)
        {
            if (CurrentState == newState) return;

            CurrentState = newState;
            Debug.Log($"[NetworkManager] State: {newState}");
            OnConnectionStateChanged?.Invoke(newState);
        }

        private void SetPlayerTypeProperty()
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { PLAYER_TYPE_KEY, (int)playerType }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        private int GetPlayerCount()
        {
            if (!PhotonNetwork.InRoom) return 0;

            int count = 0;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // Sadece aktif oyunculari say (PlayerList zaten sadece aktif oyunculari icerir)
                if (player.CustomProperties.TryGetValue(PLAYER_TYPE_KEY, out object typeObj))
                {
                    PlayerType type = (PlayerType)(int)typeObj;
                    if (type == PlayerType.Player1 || type == PlayerType.Player2)
                    {
                        count++;
                    }
                }
            }

            // Maksimum 2 oyuncu olabilir - guvenlik kontrolu
            return Mathf.Min(count, 2);
        }

        private int GetSpectatorCount()
        {
            if (!PhotonNetwork.InRoom) return 0;

            int count = 0;
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.TryGetValue(PLAYER_TYPE_KEY, out object typeObj))
                {
                    PlayerType type = (PlayerType)(int)typeObj;
                    if (type == PlayerType.Spectator)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        /// <summary>
        /// Belirli bir oyuncunun tipini al.
        /// </summary>
        public PlayerType GetPlayerType(Player player)
        {
            if (player.CustomProperties.TryGetValue(PLAYER_TYPE_KEY, out object typeObj))
            {
                return (PlayerType)(int)typeObj;
            }
            return PlayerType.Spectator;
        }

        /// <summary>
        /// Oyuncu tipini degistir (sadece room'da degilken).
        /// </summary>
        public void SetPlayerType(PlayerType type)
        {
            playerType = type;
            if (PhotonNetwork.InRoom)
            {
                SetPlayerTypeProperty();
            }
        }

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("[NetworkManager] Master sunucusuna baglandi");
            SetState(ConnectionState.ConnectedToMaster);
            OnConnectedToMasterEvent?.Invoke();

            JoinRoom();
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"[NetworkManager] Room'a katildi: {PhotonNetwork.CurrentRoom.Name}");
            Debug.Log($"[NetworkManager] Oyuncu sayisi: {PhotonNetwork.CurrentRoom.PlayerCount}");

            SetState(ConnectionState.InRoom);

            // Oyuncu tipini dinamik olarak ata (bos slot bul)
            AssignPlayerTypeAutomatically();

            OnJoinedRoomEvent?.Invoke();
        }

        /// <summary>
        /// Oyuncu tipini otomatik olarak ata (bos slota).
        /// </summary>
        private void AssignPlayerTypeAutomatically()
        {
            bool player1Taken = false;
            bool player2Taken = false;

            // Diger oyuncularin tiplerini kontrol et
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                // Kendimizi atlayalim
                if (player.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber) continue;

                if (player.CustomProperties.TryGetValue(PLAYER_TYPE_KEY, out object typeObj))
                {
                    PlayerType type = (PlayerType)(int)typeObj;
                    if (type == PlayerType.Player1) player1Taken = true;
                    else if (type == PlayerType.Player2) player2Taken = true;
                }
            }

            // Bos slotu bul ve ata
            if (!player1Taken)
            {
                playerType = PlayerType.Player1;
            }
            else if (!player2Taken)
            {
                playerType = PlayerType.Player2;
            }
            else
            {
                playerType = PlayerType.Spectator;
            }

            Debug.Log($"[NetworkManager] Oyuncu tipi atandi: {playerType}");
            SetPlayerTypeProperty();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Room'a katilamadi: {message}");
            SetState(ConnectionState.ConnectedToMaster);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"[NetworkManager] Room olusturulamadi: {message}");
            SetState(ConnectionState.ConnectedToMaster);
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"[NetworkManager] Oyuncu katildi: {newPlayer.NickName}");
            OnPlayerJoinedEvent?.Invoke(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[NetworkManager] Oyuncu ayrildi: {otherPlayer.NickName}");
            OnPlayerLeftEvent?.Invoke(otherPlayer);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"[NetworkManager] Baglanti kesildi: {cause}");
            SetState(ConnectionState.Disconnected);

            // Otomatik yeniden baglan (opsiyonel)
            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                Invoke(nameof(Connect), 2f);
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Oyuncu tipi degistiginde UI guncellensin
            if (changedProps.ContainsKey(PLAYER_TYPE_KEY))
            {
                Debug.Log($"[NetworkManager] Oyuncu tipi guncellendi: {targetPlayer.NickName} -> {GetPlayerType(targetPlayer)}");
                Debug.Log($"[NetworkManager] Guncel oyuncu sayisi: {PlayerCount}");

                // ConnectionUI gibi dinleyicilerin guncellenmesi icin event tetikle
                // OnPlayerJoinedEvent ile ayni handler'lari calistirir
                OnConnectionStateChanged?.Invoke(CurrentState);
            }
        }

        #endregion
    }
}
