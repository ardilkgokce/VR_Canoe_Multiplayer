using UnityEngine;
using Photon.Pun;
using VRCanoe.Network;
using VRCanoe.Canoe;
using PhotonPlayer = Photon.Realtime.Player;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// Oyuncularin kano koltuk atamasini yonetir.
    /// Player1 -> on koltuk, Player2 -> arka koltuk.
    /// Kurek tarafi bilgisini de saglar.
    /// </summary>
    public class SeatAssignment : MonoBehaviourPunCallbacks
    {
        public static SeatAssignment Instance { get; private set; }

        [Header("Koltuk Referanslari")]
        [Tooltip("On koltuk (Player1)")]
        [SerializeField] private Transform frontSeat;

        [Tooltip("Arka koltuk (Player2)")]
        [SerializeField] private Transform backSeat;

        [Header("Kurek Tarafi")]
        [Tooltip("Player1 hangi taraftan kurek ceker?")]
        [SerializeField] private PaddleSide player1PaddleSide = PaddleSide.Right;

        [Tooltip("Player2 hangi taraftan kurek ceker?")]
        [SerializeField] private PaddleSide player2PaddleSide = PaddleSide.Left;

        [Header("Kano Referansi")]
        [Tooltip("Kano controller (bos birak = otomatik bul)")]
        [SerializeField] private CanoeController canoe;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Atamalar
        private PhotonPlayer _frontSeatPlayer;
        private PhotonPlayer _backSeatPlayer;

        // Properties
        public Transform FrontSeat => frontSeat;
        public Transform BackSeat => backSeat;
        public PhotonPlayer FrontSeatPlayer => _frontSeatPlayer;
        public PhotonPlayer BackSeatPlayer => _backSeatPlayer;
        public bool IsFrontSeatOccupied => _frontSeatPlayer != null;
        public bool IsBackSeatOccupied => _backSeatPlayer != null;
        public bool BothSeatsOccupied => IsFrontSeatOccupied && IsBackSeatOccupied;

        public enum PaddleSide
        {
            Left,
            Right,
            Both // Tek kisilik modda veya kayak kurek icin
        }

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
            // Kano bul
            if (canoe == null)
            {
                canoe = FindObjectOfType<CanoeController>();
            }

            // Koltuk referanslarini kanodan al
            if (canoe != null)
            {
                if (frontSeat == null) frontSeat = canoe.Player1Seat;
                if (backSeat == null) backSeat = canoe.Player2Seat;
            }

            // NetworkManager event'lerini dinle
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnJoinedRoomEvent += OnLocalPlayerJoined;
                NetworkManager.Instance.OnPlayerJoinedEvent += OnRemotePlayerJoined;
                NetworkManager.Instance.OnPlayerLeftEvent += OnPlayerLeft;
            }

            // Mevcut oyunculari kontrol et
            if (PhotonNetwork.InRoom)
            {
                UpdateAllAssignments();
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnJoinedRoomEvent -= OnLocalPlayerJoined;
                NetworkManager.Instance.OnPlayerJoinedEvent -= OnRemotePlayerJoined;
                NetworkManager.Instance.OnPlayerLeftEvent -= OnPlayerLeft;
            }
        }

        /// <summary>
        /// Local oyuncu room'a katildi.
        /// </summary>
        private void OnLocalPlayerJoined()
        {
            UpdateAllAssignments();
        }

        /// <summary>
        /// Uzak oyuncu room'a katildi.
        /// </summary>
        private void OnRemotePlayerJoined(PhotonPlayer newPlayer)
        {
            // Biraz bekle - custom properties gelmis olsun
            Invoke(nameof(UpdateAllAssignments), 0.5f);
        }

        /// <summary>
        /// Oyuncu ayrildi.
        /// </summary>
        private void OnPlayerLeft(PhotonPlayer leftPlayer)
        {
            // Ayrilani temizle
            if (_frontSeatPlayer == leftPlayer) _frontSeatPlayer = null;
            if (_backSeatPlayer == leftPlayer) _backSeatPlayer = null;

            if (showDebugInfo)
            {
                Debug.Log($"[SeatAssignment] Oyuncu ayrildi: {leftPlayer.NickName}");
            }
        }

        /// <summary>
        /// Tum koltuk atamalarini guncelle.
        /// </summary>
        public void UpdateAllAssignments()
        {
            if (!PhotonNetwork.InRoom) return;

            _frontSeatPlayer = null;
            _backSeatPlayer = null;

            foreach (PhotonPlayer player in PhotonNetwork.PlayerList)
            {
                PlayerType type = GetPlayerType(player);

                if (type == PlayerType.Player1)
                {
                    _frontSeatPlayer = player;
                }
                else if (type == PlayerType.Player2)
                {
                    _backSeatPlayer = player;
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[SeatAssignment] Atamalar guncellendi - On: {_frontSeatPlayer?.NickName ?? "Bos"}, Arka: {_backSeatPlayer?.NickName ?? "Bos"}");
            }
        }

        /// <summary>
        /// Oyuncu tipini al.
        /// </summary>
        private PlayerType GetPlayerType(PhotonPlayer player)
        {
            if (NetworkManager.Instance != null)
            {
                return NetworkManager.Instance.GetPlayerType(player);
            }
            return PlayerType.Spectator;
        }

        /// <summary>
        /// Local oyuncunun koltuk pozisyonunu al.
        /// </summary>
        public Transform GetLocalPlayerSeat()
        {
            if (NetworkManager.Instance == null) return null;

            PlayerType localType = NetworkManager.Instance.LocalPlayerType;

            switch (localType)
            {
                case PlayerType.Player1:
                    return frontSeat;
                case PlayerType.Player2:
                    return backSeat;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Local oyuncunun kurek tarafini al.
        /// </summary>
        public PaddleSide GetLocalPlayerPaddleSide()
        {
            if (NetworkManager.Instance == null) return PaddleSide.Both;

            PlayerType localType = NetworkManager.Instance.LocalPlayerType;

            switch (localType)
            {
                case PlayerType.Player1:
                    return player1PaddleSide;
                case PlayerType.Player2:
                    return player2PaddleSide;
                default:
                    return PaddleSide.Both;
            }
        }

        /// <summary>
        /// Belirli bir oyuncu tipinin koltuk pozisyonunu al.
        /// </summary>
        public Transform GetSeatForPlayerType(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player1:
                    return frontSeat;
                case PlayerType.Player2:
                    return backSeat;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Belirli bir oyuncu tipinin kurek tarafini al.
        /// </summary>
        public PaddleSide GetPaddleSideForPlayerType(PlayerType playerType)
        {
            switch (playerType)
            {
                case PlayerType.Player1:
                    return player1PaddleSide;
                case PlayerType.Player2:
                    return player2PaddleSide;
                default:
                    return PaddleSide.Both;
            }
        }

        /// <summary>
        /// Kurek tarafini bool olarak al (sag = true, sol = false).
        /// </summary>
        public bool IsRightSide(PaddleSide side)
        {
            return side == PaddleSide.Right;
        }

        /// <summary>
        /// Local oyuncunun kurek tarafini bool olarak al.
        /// </summary>
        public bool IsLocalPlayerRightSide()
        {
            return IsRightSide(GetLocalPlayerPaddleSide());
        }

        /// <summary>
        /// Kurek tarafi ayarlarini degistir.
        /// </summary>
        public void SetPaddleSides(PaddleSide player1Side, PaddleSide player2Side)
        {
            player1PaddleSide = player1Side;
            player2PaddleSide = player2Side;

            if (showDebugInfo)
            {
                Debug.Log($"[SeatAssignment] Kurek taraflari guncellendi - P1: {player1Side}, P2: {player2Side}");
            }
        }

        /// <summary>
        /// On ve arka koltuk arasindaki mesafe.
        /// </summary>
        public float GetSeatDistance()
        {
            if (frontSeat == null || backSeat == null) return 0f;
            return Vector3.Distance(frontSeat.position, backSeat.position);
        }

        /// <summary>
        /// Koltuklar arasi orta nokta.
        /// </summary>
        public Vector3 GetSeatCenter()
        {
            if (frontSeat == null || backSeat == null)
            {
                return canoe != null ? canoe.transform.position : Vector3.zero;
            }
            return (frontSeat.position + backSeat.position) / 2f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // On koltuk
            if (frontSeat != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(frontSeat.position, 0.3f);
                Gizmos.DrawLine(frontSeat.position, frontSeat.position + frontSeat.forward * 0.5f);

                // Kurek tarafi goster
                Vector3 paddleDir = player1PaddleSide == PaddleSide.Right ? frontSeat.right : -frontSeat.right;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(frontSeat.position, frontSeat.position + paddleDir * 0.5f);

                UnityEditor.Handles.Label(frontSeat.position + Vector3.up * 0.5f, "Player1 (Front)");
            }

            // Arka koltuk
            if (backSeat != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(backSeat.position, 0.3f);
                Gizmos.DrawLine(backSeat.position, backSeat.position + backSeat.forward * 0.5f);

                // Kurek tarafi goster
                Vector3 paddleDir = player2PaddleSide == PaddleSide.Right ? backSeat.right : -backSeat.right;
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(backSeat.position, backSeat.position + paddleDir * 0.5f);

                UnityEditor.Handles.Label(backSeat.position + Vector3.up * 0.5f, "Player2 (Back)");
            }

            // Koltuklar arasi cizgi
            if (frontSeat != null && backSeat != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(frontSeat.position, backSeat.position);
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 280, 200, 270, 150));
            GUILayout.Box("Seat Assignment");

            GUILayout.Label($"Front Seat: {(_frontSeatPlayer?.NickName ?? "Empty")}");
            GUILayout.Label($"Back Seat: {(_backSeatPlayer?.NickName ?? "Empty")}");
            GUILayout.Label($"Both Occupied: {BothSeatsOccupied}");

            if (NetworkManager.Instance != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Local Type: {NetworkManager.Instance.LocalPlayerType}");
                GUILayout.Label($"Local Side: {GetLocalPlayerPaddleSide()}");
            }

            GUILayout.EndArea();
        }
#endif
    }
}
