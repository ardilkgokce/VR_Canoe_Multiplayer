using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using VRCanoe.Network;

namespace VRCanoe.UI
{
    /// <summary>
    /// Baglanti durumunu gosteren UI.
    /// </summary>
    public class ConnectionUI : MonoBehaviourPunCallbacks
    {
        [Header("UI Referanslari")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI spectatorCountText;
        [SerializeField] private GameObject readyPanel;

        [Header("Durum Metinleri")]
        [SerializeField] private string connectingText = "Baglaniyor...";
        [SerializeField] private string connectedText = "Baglandi";
        [SerializeField] private string waitingPlayersText = "Oyuncu bekleniyor...";
        [SerializeField] private string readyText = "Hazir!";

        private NetworkManager _networkManager;

        private void Start()
        {
            _networkManager = NetworkManager.Instance;

            if (_networkManager != null)
            {
                _networkManager.OnConnectionStateChanged += OnConnectionStateChanged;
                _networkManager.OnPlayerJoinedEvent += OnPlayerUpdated;
                _networkManager.OnPlayerLeftEvent += OnPlayerUpdated;

                // Baslangic durumunu goster
                OnConnectionStateChanged(_networkManager.CurrentState);
            }

            if (readyPanel != null)
            {
                readyPanel.SetActive(false);
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnConnectionStateChanged -= OnConnectionStateChanged;
                _networkManager.OnPlayerJoinedEvent -= OnPlayerUpdated;
                _networkManager.OnPlayerLeftEvent -= OnPlayerUpdated;
            }
        }

        private void OnConnectionStateChanged(ConnectionState state)
        {
            UpdateStatusText(state);
            UpdateUI();
        }

        private void OnPlayerUpdated(Player player)
        {
            UpdateUI();
        }

        private void UpdateStatusText(ConnectionState state)
        {
            if (statusText == null) return;

            switch (state)
            {
                case ConnectionState.Disconnected:
                    statusText.text = "Baglanti kesildi";
                    statusText.color = Color.red;
                    break;

                case ConnectionState.Connecting:
                case ConnectionState.JoiningRoom:
                    statusText.text = connectingText;
                    statusText.color = Color.yellow;
                    break;

                case ConnectionState.ConnectedToMaster:
                    statusText.text = connectedText;
                    statusText.color = Color.green;
                    break;

                case ConnectionState.InRoom:
                    UpdateRoomStatus();
                    break;
            }
        }

        private void UpdateRoomStatus()
        {
            if (statusText == null || _networkManager == null) return;

            if (_networkManager.AllPlayersReady)
            {
                statusText.text = readyText;
                statusText.color = Color.green;

                if (readyPanel != null)
                {
                    readyPanel.SetActive(true);
                }
            }
            else
            {
                statusText.text = waitingPlayersText;
                statusText.color = Color.yellow;

                if (readyPanel != null)
                {
                    readyPanel.SetActive(false);
                }
            }
        }

        private void UpdateUI()
        {
            UpdatePlayerCount();
            UpdateSpectatorCount();
            UpdateRoomStatus();
        }

        private void UpdatePlayerCount()
        {
            if (playerCountText == null || _networkManager == null) return;

            int currentPlayers = _networkManager.PlayerCount;
            playerCountText.text = $"{currentPlayers}/2 Oyuncu";

            // Renk ayarla
            if (currentPlayers == 2)
            {
                playerCountText.color = Color.green;
            }
            else if (currentPlayers == 1)
            {
                playerCountText.color = Color.yellow;
            }
            else
            {
                playerCountText.color = Color.white;
            }
        }

        private void UpdateSpectatorCount()
        {
            if (spectatorCountText == null || _networkManager == null) return;

            int spectators = _networkManager.SpectatorCount;
            spectatorCountText.text = $"{spectators}/1 Izleyici";

            // Renk ayarla
            spectatorCountText.color = spectators > 0 ? Color.green : Color.white;
        }

        // Photon callbacks - oyuncu property degisikliklerini dinle
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            UpdateUI();
        }

        public override void OnJoinedRoom()
        {
            UpdateUI();
        }
    }
}
