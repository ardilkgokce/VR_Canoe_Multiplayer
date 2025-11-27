using UnityEngine;
using Photon.Pun;
using VRCanoe.Network;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// Oyuncu room'a girdiginde PlayerType'a gore VR Rig veya Spectator kamera aktive eder.
    /// VR Rigler sahnede kano koltuguna child olarak hazir bekler.
    /// Bu script sadece dogru rig'i aktive/deaktive eder.
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
        [Tooltip("Baslangicta tum rigleri deaktif et")]
        [SerializeField] private bool disableAllOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Aktif rig
        private GameObject _activeRig;

        // Properties
        public GameObject ActiveRig => _activeRig;
        public bool IsLocalPlayerActive => _activeRig != null && _activeRig.activeInHierarchy;

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
            // Baslangicta tum rigleri deaktif et
            if (disableAllOnStart)
            {
                DisableAllRigs();
            }

            // NetworkManager event'lerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnJoinedRoomEvent += OnLocalPlayerJoinedRoom;
            }

            // Eger zaten room'daysak aktive et
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
        /// Tum rigleri deaktif et.
        /// </summary>
        private void DisableAllRigs()
        {
            if (player1VRRig != null) player1VRRig.SetActive(false);
            if (player2VRRig != null) player2VRRig.SetActive(false);
            if (spectatorCamera != null) spectatorCamera.SetActive(false);
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

            ActivateRigForPlayerType(localType);
        }

        /// <summary>
        /// Oyuncu tipine gore uygun rig'i aktive et.
        /// </summary>
        public void ActivateRigForPlayerType(PlayerType playerType)
        {
            // Onceki aktif rig'i deaktif et
            if (_activeRig != null)
            {
                _activeRig.SetActive(false);
                _activeRig = null;
            }

            switch (playerType)
            {
                case PlayerType.Player1:
                    ActivateRig(player1VRRig, "Player1 VR Rig");
                    break;

                case PlayerType.Player2:
                    ActivateRig(player2VRRig, "Player2 VR Rig");
                    break;

                case PlayerType.Spectator:
                    ActivateRig(spectatorCamera, "Spectator Camera");
                    break;
            }
        }

        /// <summary>
        /// Belirtilen rig'i aktive et.
        /// </summary>
        private void ActivateRig(GameObject rig, string rigName)
        {
            if (rig == null)
            {
                Debug.LogError($"[PlayerSpawner] {rigName} referansi eksik!");
                return;
            }

            _activeRig = rig;
            rig.SetActive(true);

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerSpawner] {rigName} aktive edildi");
            }
        }

        /// <summary>
        /// Oyuncu tipini degistir ve rig'i guncelle.
        /// </summary>
        public void ChangePlayerType(PlayerType newType)
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SetPlayerType(newType);
                ActivateRigForPlayerType(newType);
            }
        }

        /// <summary>
        /// Aktif VR Rig'deki VRRigController'i al (varsa).
        /// </summary>
        public VRRigController GetActiveRigController()
        {
            if (_activeRig == null) return null;
            return _activeRig.GetComponent<VRRigController>();
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

            GUILayout.BeginArea(new Rect(10, 120, 200, 80));
            GUILayout.Box("Player Spawner");
            GUILayout.Label($"Active Rig: {(_activeRig != null ? _activeRig.name : "None")}");

            if (NetworkManager.Instance != null)
            {
                GUILayout.Label($"Player Type: {NetworkManager.Instance.LocalPlayerType}");
            }
            GUILayout.EndArea();
        }
#endif
    }
}
