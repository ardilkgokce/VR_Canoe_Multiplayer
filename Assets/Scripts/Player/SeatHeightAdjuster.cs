using UnityEngine;
using Photon.Pun;
using VRCanoe.Network;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// Local oyuncunun seat yuksekligini ok tuslariyla ayarlamasini saglar.
    /// Her client sadece kendi seat'ini kontrol eder.
    /// Etkinlikte hizli pozisyon duzenlemesi icin.
    /// </summary>
    public class SeatHeightAdjuster : MonoBehaviourPunCallbacks
    {
        [Header("Seat Referanslari")]
        [Tooltip("Player1 seat objesi (icerisinde VR player olan)")]
        [SerializeField] private Transform player1Seat;

        [Tooltip("Player2 seat objesi (icerisinde VR player olan)")]
        [SerializeField] private Transform player2Seat;

        [Header("Ayar Degerleri")]
        [Tooltip("Her tusla ne kadar yukselir/alçalir (metre)")]
        [SerializeField] private float heightStep = 0.05f;

        [Tooltip("Minimum yukseklik limiti (baslangictan)")]
        [SerializeField] private float minHeightOffset = -0.5f;

        [Tooltip("Maximum yukseklik limiti (baslangictan)")]
        [SerializeField] private float maxHeightOffset = 0.5f;

        [Tooltip("Surekli basildiginda hiz (saniyede metre)")]
        [SerializeField] private float continuousSpeed = 0.2f;

        [Header("Kontroller")]
        [Tooltip("Yukari tus")]
        [SerializeField] private KeyCode upKey = KeyCode.UpArrow;

        [Tooltip("Asagi tus")]
        [SerializeField] private KeyCode downKey = KeyCode.DownArrow;

        [Tooltip("Reset tus (baslangic yuksekligine don)")]
        [SerializeField] private KeyCode resetKey = KeyCode.Home;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Baslangic yukseklikleri
        private float _player1InitialY;
        private float _player2InitialY;
        private float _currentOffset = 0f;

        // Local player'in seat'i
        private Transform _localSeat;
        private float _localInitialY;
        private bool _isInitialized = false;
        private PlayerType _assignedType = PlayerType.Spectator;

        private void Start()
        {
            // Baslangic yuksekliklerini kaydet
            if (player1Seat != null)
            {
                _player1InitialY = player1Seat.localPosition.y;
            }

            if (player2Seat != null)
            {
                _player2InitialY = player2Seat.localPosition.y;
            }

            // Eger zaten room'daysak hemen initialize et
            if (PhotonNetwork.InRoom)
            {
                // Biraz bekle - properties gelmis olsun
                Invoke(nameof(InitializeLocalSeat), 0.5f);
            }
        }

        public override void OnJoinedRoom()
        {
            // Room'a katilinca biraz bekle ve initialize et
            Invoke(nameof(InitializeLocalSeat), 0.5f);
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Kendi PlayerType'imiz degistiyse yeniden initialize et
            if (targetPlayer.IsLocal && changedProps.ContainsKey("PlayerType"))
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[SeatHeightAdjuster] PlayerType degisti, yeniden initialize ediliyor...");
                }
                InitializeLocalSeat();
            }
        }

        private void InitializeLocalSeat()
        {
            // NetworkManager yoksa offline mod
            if (NetworkManager.Instance == null)
            {
                _localSeat = player1Seat;
                _localInitialY = _player1InitialY;
                _assignedType = PlayerType.Player1;
                _isInitialized = true;

                if (showDebugInfo)
                {
                    Debug.Log("[SeatHeightAdjuster] Offline mod - Player1 seat kullaniliyor");
                }
                return;
            }

            // Photon'dan PlayerType al
            PlayerType localType = PlayerType.Spectator;

            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("PlayerType", out object typeObj))
            {
                localType = (PlayerType)(int)typeObj;
            }
            else
            {
                // Property henuz gelmemis, NetworkManager'dan al
                localType = NetworkManager.Instance.LocalPlayerType;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[SeatHeightAdjuster] LocalPlayerType: {localType}");
            }

            // Ayni tip zaten atanmissa tekrar yapma
            if (_isInitialized && _assignedType == localType)
            {
                return;
            }

            _assignedType = localType;

            switch (localType)
            {
                case PlayerType.Player1:
                    _localSeat = player1Seat;
                    _localInitialY = _player1InitialY;
                    _isInitialized = true;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[SeatHeightAdjuster] Player1 - Seat: {player1Seat?.name ?? "NULL"}");
                    }
                    break;

                case PlayerType.Player2:
                    _localSeat = player2Seat;
                    _localInitialY = _player2InitialY;
                    _isInitialized = true;

                    if (showDebugInfo)
                    {
                        Debug.Log($"[SeatHeightAdjuster] Player2 - Seat: {player2Seat?.name ?? "NULL"}");
                    }
                    break;

                default:
                    _localSeat = null;
                    _isInitialized = true;

                    if (showDebugInfo)
                    {
                        Debug.Log("[SeatHeightAdjuster] Spectator - seat ayari yok");
                    }
                    return;
            }

            // Offset'i sifirla (yeni seat icin)
            _currentOffset = 0f;
        }

        private void Update()
        {
            if (!_isInitialized || _localSeat == null) return;

            // Tek basilista adim adim
            if (Input.GetKeyDown(upKey))
            {
                AdjustHeight(heightStep);
            }
            else if (Input.GetKeyDown(downKey))
            {
                AdjustHeight(-heightStep);
            }

            // Surekli basili tutunca
            if (Input.GetKey(upKey) && !Input.GetKeyDown(upKey))
            {
                AdjustHeight(continuousSpeed * Time.deltaTime);
            }
            else if (Input.GetKey(downKey) && !Input.GetKeyDown(downKey))
            {
                AdjustHeight(-continuousSpeed * Time.deltaTime);
            }

            // Reset
            if (Input.GetKeyDown(resetKey))
            {
                ResetHeight();
            }
        }

        /// <summary>
        /// Seat yuksekligini ayarla.
        /// </summary>
        private void AdjustHeight(float delta)
        {
            if (_localSeat == null) return;

            // Yeni offset hesapla
            float newOffset = _currentOffset + delta;
            newOffset = Mathf.Clamp(newOffset, minHeightOffset, maxHeightOffset);

            // Degisiklik var mi?
            if (Mathf.Approximately(newOffset, _currentOffset)) return;

            _currentOffset = newOffset;

            // Pozisyonu guncelle
            Vector3 pos = _localSeat.localPosition;
            pos.y = _localInitialY + _currentOffset;
            _localSeat.localPosition = pos;

            if (showDebugInfo)
            {
                Debug.Log($"[SeatHeightAdjuster] {_assignedType} - Yukseklik: {_currentOffset:F2}m");
            }
        }

        /// <summary>
        /// Yuksekligi baslangica dondur.
        /// </summary>
        public void ResetHeight()
        {
            if (_localSeat == null) return;

            _currentOffset = 0f;

            Vector3 pos = _localSeat.localPosition;
            pos.y = _localInitialY;
            _localSeat.localPosition = pos;

            if (showDebugInfo)
            {
                Debug.Log("[SeatHeightAdjuster] Yukseklik resetlendi");
            }
        }

        /// <summary>
        /// Belirli bir yukseklik ayarla.
        /// </summary>
        public void SetHeightOffset(float offset)
        {
            if (_localSeat == null) return;

            _currentOffset = Mathf.Clamp(offset, minHeightOffset, maxHeightOffset);

            Vector3 pos = _localSeat.localPosition;
            pos.y = _localInitialY + _currentOffset;
            _localSeat.localPosition = pos;
        }

        /// <summary>
        /// Mevcut offset'i al.
        /// </summary>
        public float GetCurrentOffset()
        {
            return _currentOffset;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowAllDebugUI) return;

            GUILayout.BeginArea(new Rect(Screen.width - 280, 360, 270, 120));
            GUILayout.Box("Seat Height Adjuster");

            GUILayout.Label($"PlayerType: {_assignedType}");

            if (_localSeat != null)
            {
                GUILayout.Label($"Seat: {_localSeat.name}");
                GUILayout.Label($"Offset: {_currentOffset:F2}m");
                GUILayout.Label($"[{upKey}] Yukari | [{downKey}] Asagi");
            }
            else
            {
                GUILayout.Label("Seat atanmamis (Spectator?)");
            }

            GUILayout.EndArea();
        }
#endif
    }
}
