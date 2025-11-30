using UnityEngine;
using Photon.Pun;
using VRCanoe.Network;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// Oyuncu room'a girdiginde PlayerType'a gore VR Rig ayarlarini yapar.
    /// VR Rigler sahnede kano koltuguna child olarak hazir bekler.
    /// Local player: XR tracking + kamera aktif
    /// Remote player: Sadece gorseller aktif (paddle gorunur)
    /// </summary>
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        public static PlayerSpawner Instance { get; private set; }

        [Header("VR Rig Referanslari")]
        [Tooltip("Player1 (on koltuk) VR Rig - Kano icinde child olarak")]
        [SerializeField] private GameObject player1VRRig;

        [Tooltip("Player2 (arka koltuk) VR Rig - Kano icinde child olarak")]
        [SerializeField] private GameObject player2VRRig;

        [Header("Spectator Referansi")]
        [Tooltip("Izleyici kamerasi")]
        [SerializeField] private GameObject spectatorCamera;

        [Header("Ayarlar")]
        [Tooltip("Baslangicta tum rigleri remote moda al")]
        [SerializeField] private bool setupOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Aktif local rig
        private GameObject _activeLocalRig;

        // VRRigController referanslari
        private VRRigController _player1RigController;
        private VRRigController _player2RigController;

        // Properties
        public GameObject ActiveRig => _activeLocalRig;
        public bool IsLocalPlayerActive => _activeLocalRig != null && _activeLocalRig.activeInHierarchy;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // VRRigController referanslarini al
            CacheRigControllers();
        }

        private void Start()
        {
            // Baslangicta tum rigleri remote moda al (gorseller acik, tracking kapali)
            if (setupOnStart)
            {
                SetupAllRigsAsRemote();
            }

            // NetworkManager event'lerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnJoinedRoomEvent += OnLocalPlayerJoinedRoom;
            }

            // Eger zaten room'daysak ayarla
            if (PhotonNetwork.InRoom)
            {
                OnLocalPlayerJoinedRoom();
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnJoinedRoomEvent -= OnLocalPlayerJoinedRoom;
            }
        }

        /// <summary>
        /// VRRigController referanslarini cache'le.
        /// </summary>
        private void CacheRigControllers()
        {
            if (player1VRRig != null)
            {
                _player1RigController = player1VRRig.GetComponent<VRRigController>();
                if (_player1RigController == null)
                {
                    Debug.LogWarning("[PlayerSpawner] Player1 VR Rig'de VRRigController bulunamadi!");
                }
            }

            if (player2VRRig != null)
            {
                _player2RigController = player2VRRig.GetComponent<VRRigController>();
                if (_player2RigController == null)
                {
                    Debug.LogWarning("[PlayerSpawner] Player2 VR Rig'de VRRigController bulunamadi!");
                }
            }
        }

        /// <summary>
        /// Tum VR rigleri remote moda al (gorseller acik, tracking kapali).
        /// Spectator kamerayi kapat.
        /// </summary>
        private void SetupAllRigsAsRemote()
        {
            // Her iki VR rig'i de aktif yap ve remote moda al
            if (player1VRRig != null)
            {
                player1VRRig.SetActive(true);
                if (_player1RigController != null)
                {
                    _player1RigController.SetAsRemotePlayer();
                }
            }

            if (player2VRRig != null)
            {
                player2VRRig.SetActive(true);
                if (_player2RigController != null)
                {
                    _player2RigController.SetAsRemotePlayer();
                }
            }

            // Spectator kamera kapali
            if (spectatorCamera != null)
            {
                spectatorCamera.SetActive(false);
            }

            if (showDebugInfo)
            {
                Debug.Log("[PlayerSpawner] Tum VR Rigler remote moda alindi (gorseller aktif)");
            }
        }

        /// <summary>
        /// Local oyuncu room'a katildiginda cagirilir.
        /// </summary>
        private void OnLocalPlayerJoinedRoom()
        {
            if (NetworkManager.Instance == null) return;

            PlayerType localType = NetworkManager.Instance.LocalPlayerType;

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerSpawner] Local oyuncu tipi: {localType}");
            }

            SetupRigsForPlayerType(localType);
        }

        /// <summary>
        /// Oyuncu tipine gore rigleri ayarla.
        /// Local player'in rig'i tam aktif, diger rig remote modda (sadece gorseller).
        /// </summary>
        public void SetupRigsForPlayerType(PlayerType playerType)
        {
            // Onceki local rig'i remote moda al
            if (_activeLocalRig != null)
            {
                var prevController = _activeLocalRig.GetComponent<VRRigController>();
                if (prevController != null)
                {
                    prevController.SetAsRemotePlayer();
                }
                _activeLocalRig = null;
            }

            switch (playerType)
            {
                case PlayerType.Player1:
                    SetupAsLocalPlayer(player1VRRig, _player1RigController, "Player1");
                    SetupAsRemotePlayer(player2VRRig, _player2RigController, "Player2");
                    if (spectatorCamera != null) spectatorCamera.SetActive(false);
                    break;

                case PlayerType.Player2:
                    SetupAsLocalPlayer(player2VRRig, _player2RigController, "Player2");
                    SetupAsRemotePlayer(player1VRRig, _player1RigController, "Player1");
                    if (spectatorCamera != null) spectatorCamera.SetActive(false);
                    break;

                case PlayerType.Spectator:
                    // Spectator: her iki VR rig de remote modda, spectator kamera aktif
                    SetupAsRemotePlayer(player1VRRig, _player1RigController, "Player1");
                    SetupAsRemotePlayer(player2VRRig, _player2RigController, "Player2");
                    if (spectatorCamera != null)
                    {
                        spectatorCamera.SetActive(true);
                        _activeLocalRig = spectatorCamera;
                        if (showDebugInfo)
                        {
                            Debug.Log("[PlayerSpawner] Spectator Camera aktive edildi");
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Rig'i local player olarak ayarla (tam aktif).
        /// </summary>
        private void SetupAsLocalPlayer(GameObject rig, VRRigController controller, string playerName)
        {
            if (rig == null)
            {
                Debug.LogError($"[PlayerSpawner] {playerName} VR Rig referansi eksik!");
                return;
            }

            rig.SetActive(true);
            _activeLocalRig = rig;

            if (controller != null)
            {
                controller.SetAsLocalPlayer();
            }

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerSpawner] {playerName} VR Rig LOCAL olarak ayarlandi");
            }
        }

        /// <summary>
        /// Rig'i remote player olarak ayarla (sadece gorseller aktif).
        /// </summary>
        private void SetupAsRemotePlayer(GameObject rig, VRRigController controller, string playerName)
        {
            if (rig == null) return;

            rig.SetActive(true);

            if (controller != null)
            {
                controller.SetAsRemotePlayer();
            }

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerSpawner] {playerName} VR Rig REMOTE olarak ayarlandi (gorseller aktif)");
            }
        }

        /// <summary>
        /// Oyuncu tipini degistir ve rigleri guncelle.
        /// </summary>
        public void ChangePlayerType(PlayerType newType)
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SetPlayerType(newType);
                SetupRigsForPlayerType(newType);
            }
        }

        /// <summary>
        /// Aktif local VR Rig'deki VRRigController'i al (varsa).
        /// </summary>
        public VRRigController GetActiveRigController()
        {
            if (_activeLocalRig == null) return null;
            return _activeLocalRig.GetComponent<VRRigController>();
        }

        /// <summary>
        /// VR view'i recenter et.
        /// </summary>
        public void RecenterView()
        {
            var controller = GetActiveRigController();
            if (controller != null)
            {
                controller.RecenterView();
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (Game.CanoeGameManager.Instance != null)
            {
                if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowPlayerSpawnerDebug) return;
            }

            GUILayout.BeginArea(new Rect(10, 120, 250, 120));
            GUILayout.Box("Player Spawner");
            GUILayout.Label($"Local Rig: {(_activeLocalRig != null ? _activeLocalRig.name : "None")}");

            if (NetworkManager.Instance != null)
            {
                GUILayout.Label($"Player Type: {NetworkManager.Instance.LocalPlayerType}");
            }

            // Rig durumlarini goster
            string p1Status = _player1RigController != null
                ? (_player1RigController.IsLocalPlayer ? "LOCAL" : "REMOTE")
                : "N/A";
            string p2Status = _player2RigController != null
                ? (_player2RigController.IsLocalPlayer ? "LOCAL" : "REMOTE")
                : "N/A";

            GUILayout.Label($"Player1 Rig: {p1Status}");
            GUILayout.Label($"Player2 Rig: {p2Status}");

            GUILayout.EndArea();
        }
#endif
    }
}
