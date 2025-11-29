using System;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace VRCanoe.Game
{
    /// <summary>
    /// Oyuncu ve takim isimlerini yonetir.
    /// Isimler Photon Room Properties olarak saklanir.
    /// </summary>
    public class NameManager : MonoBehaviourPunCallbacks
    {
        public static NameManager Instance { get; private set; }

        [Header("Varsayilan Isimler")]
        [SerializeField] private string defaultPlayer1Name = "Oyuncu 1";
        [SerializeField] private string defaultPlayer2Name = "Oyuncu 2";
        [SerializeField] private string defaultTeamName = "Takim";

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action OnNamesSet;
        public event Action<string, string, string> OnNamesChanged; // player1, player2, team

        // Isimler
        private string _player1Name;
        private string _player2Name;
        private string _teamName;

        // Room property keys
        private const string PLAYER1_NAME_KEY = "Player1Name";
        private const string PLAYER2_NAME_KEY = "Player2Name";
        private const string TEAM_NAME_KEY = "TeamName";
        private const string NAMES_SET_KEY = "NamesSet";

        // Properties
        public string Player1Name => string.IsNullOrEmpty(_player1Name) ? defaultPlayer1Name : _player1Name;
        public string Player2Name => string.IsNullOrEmpty(_player2Name) ? defaultPlayer2Name : _player2Name;
        public string TeamName => string.IsNullOrEmpty(_teamName) ? defaultTeamName : _teamName;
        public bool AreNamesSet { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Varsayilan isimleri ata
            _player1Name = defaultPlayer1Name;
            _player2Name = defaultPlayer2Name;
            _teamName = defaultTeamName;
        }

        private void Start()
        {
            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameReset += OnGameReset;
            }
        }

        private void OnDestroy()
        {
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameReset -= OnGameReset;
            }
        }

        /// <summary>
        /// Isimleri ayarla ve tum clientlara sync et.
        /// </summary>
        public void SetNames(string player1Name, string player2Name, string teamName)
        {
            if (!PhotonNetwork.IsMasterClient && !IsPlayer1())
            {
                // Sadece MasterClient veya Player1 isim degistirebilir
                Debug.LogWarning("[NameManager] Isim degistirme yetkisi yok!");
                return;
            }

            // Bos isimleri varsayilan ile degistir
            if (string.IsNullOrWhiteSpace(player1Name)) player1Name = defaultPlayer1Name;
            if (string.IsNullOrWhiteSpace(player2Name)) player2Name = defaultPlayer2Name;
            if (string.IsNullOrWhiteSpace(teamName)) teamName = defaultTeamName;

            // Room properties olarak kaydet
            Hashtable props = new Hashtable
            {
                { PLAYER1_NAME_KEY, player1Name },
                { PLAYER2_NAME_KEY, player2Name },
                { TEAM_NAME_KEY, teamName },
                { NAMES_SET_KEY, true }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // RPC ile tum clientlara bildir
            photonView.RPC(nameof(RPC_SetNames), RpcTarget.All, player1Name, player2Name, teamName);

            if (showDebugInfo)
            {
                Debug.Log($"[NameManager] Isimler ayarlandi: {player1Name}, {player2Name}, {teamName}");
            }
        }

        /// <summary>
        /// Isimleri onayla ve oyunu baslat.
        /// </summary>
        public void ConfirmNames()
        {
            if (!AreNamesSet)
            {
                // Varsayilan isimlerle devam et
                SetNames(_player1Name, _player2Name, _teamName);
            }

            // GameManager'a bildir
            if (CanoeGameManager.Instance != null && PhotonNetwork.IsMasterClient)
            {
                CanoeGameManager.Instance.ConfirmNames();
            }
        }

        /// <summary>
        /// Isimleri onayla ve direkt countdown baslat.
        /// </summary>
        public void ConfirmNamesAndStart()
        {
            if (!AreNamesSet)
            {
                SetNames(_player1Name, _player2Name, _teamName);
            }

            // GameManager'a bildir - Ready state'ine gec ve countdown baslat
            if (CanoeGameManager.Instance != null)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    CanoeGameManager.Instance.ConfirmNames();
                    CanoeGameManager.Instance.StartCountdown();
                }
                else
                {
                    // MasterClient'a RPC gonder
                    photonView.RPC(nameof(RPC_RequestStartGame), RpcTarget.MasterClient);
                }
            }
        }

        /// <summary>
        /// Local oyuncu Player1 mi?
        /// </summary>
        private bool IsPlayer1()
        {
            if (Network.NetworkManager.Instance == null) return false;
            return Network.NetworkManager.Instance.LocalPlayerType == Network.PlayerType.Player1;
        }

        private void OnGameReset()
        {
            // Isimleri sifirla
            AreNamesSet = false;
            _player1Name = defaultPlayer1Name;
            _player2Name = defaultPlayer2Name;
            _teamName = defaultTeamName;
        }

        #region RPCs

        [PunRPC]
        private void RPC_SetNames(string player1Name, string player2Name, string teamName)
        {
            _player1Name = player1Name;
            _player2Name = player2Name;
            _teamName = teamName;
            AreNamesSet = true;

            OnNamesChanged?.Invoke(_player1Name, _player2Name, _teamName);
            OnNamesSet?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log($"[NameManager] RPC: Isimler alindi - {player1Name}, {player2Name}, {teamName}");
            }
        }

        [PunRPC]
        private void RPC_RequestStartGame()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ConfirmNamesAndStart();
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
            bool updated = false;

            if (propertiesThatChanged.TryGetValue(PLAYER1_NAME_KEY, out object p1))
            {
                _player1Name = (string)p1;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(PLAYER2_NAME_KEY, out object p2))
            {
                _player2Name = (string)p2;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(TEAM_NAME_KEY, out object team))
            {
                _teamName = (string)team;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(NAMES_SET_KEY, out object namesSet))
            {
                AreNamesSet = (bool)namesSet;
            }

            if (updated)
            {
                OnNamesChanged?.Invoke(_player1Name, _player2Name, _teamName);
            }
        }

        private void SyncFromRoom()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            var props = PhotonNetwork.CurrentRoom.CustomProperties;

            if (props.TryGetValue(PLAYER1_NAME_KEY, out object p1))
                _player1Name = (string)p1;
            if (props.TryGetValue(PLAYER2_NAME_KEY, out object p2))
                _player2Name = (string)p2;
            if (props.TryGetValue(TEAM_NAME_KEY, out object team))
                _teamName = (string)team;
            if (props.TryGetValue(NAMES_SET_KEY, out object namesSet))
                AreNamesSet = (bool)namesSet;

            if (AreNamesSet)
            {
                OnNamesChanged?.Invoke(_player1Name, _player2Name, _teamName);
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowAllDebugUI) return;

            GUILayout.BeginArea(new Rect(10, 210, 200, 100));
            GUILayout.Box("Name Manager");
            GUILayout.Label($"P1: {Player1Name}");
            GUILayout.Label($"P2: {Player2Name}");
            GUILayout.Label($"Team: {TeamName}");
            GUILayout.Label($"Set: {AreNamesSet}");
            GUILayout.EndArea();
        }
#endif
    }
}
