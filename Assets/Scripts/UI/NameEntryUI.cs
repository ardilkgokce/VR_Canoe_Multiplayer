using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using VRCanoe.Game;
using VRCanoe.Network;

namespace VRCanoe.UI
{
    /// <summary>
    /// Isim girisi UI paneli.
    /// EnteringNames state'inde gosterilir.
    /// </summary>
    public class NameEntryUI : MonoBehaviour
    {
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField player1NameInput;
        [SerializeField] private TMP_InputField player2NameInput;
        [SerializeField] private TMP_InputField teamNameInput;

        [Header("Butonlar")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button skipButton;

        [Header("Text Elemanlari")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Varsayilan Isimler")]
        [SerializeField] private string defaultPlayer1Name = "Oyuncu 1";
        [SerializeField] private string defaultPlayer2Name = "Oyuncu 2";
        [SerializeField] private string defaultTeamName = "Takim";

        [Header("Ayarlar")]
        [Tooltip("Sadece kontrol yetkisi olanlar duzenleyebilsin")]
        [SerializeField] private bool restrictEditing = true;

        // Events
        public event Action<string, string, string> OnNamesSubmitted;
        public event Action OnSkipped;

        private bool _canEdit;

        private void Awake()
        {
            // Baslangicta gizle
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Start()
        {
            // Varsayilan isimleri ayarla
            SetDefaultNames();

            // Buton eventlerini bagla
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipButtonClicked);
            }

            // GameManager eventlerini dinle
            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }

            // NameManager eventlerini dinle
            if (NameManager.Instance != null)
            {
                NameManager.Instance.OnNamesChanged += OnNamesChangedFromNetwork;
            }

            // Kontrol yetkisini belirle
            UpdateEditPermission();
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(OnSkipButtonClicked);
            }

            if (CanoeGameManager.Instance != null)
            {
                CanoeGameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }

            if (NameManager.Instance != null)
            {
                NameManager.Instance.OnNamesChanged -= OnNamesChangedFromNetwork;
            }
        }

        /// <summary>
        /// Varsayilan isimleri input field'lara yaz.
        /// </summary>
        private void SetDefaultNames()
        {
            if (player1NameInput != null)
            {
                player1NameInput.text = defaultPlayer1Name;
            }
            if (player2NameInput != null)
            {
                player2NameInput.text = defaultPlayer2Name;
            }
            if (teamNameInput != null)
            {
                teamNameInput.text = defaultTeamName;
            }

            // NameManager'dan mevcut isimleri al
            if (NameManager.Instance != null && NameManager.Instance.AreNamesSet)
            {
                if (player1NameInput != null) player1NameInput.text = NameManager.Instance.Player1Name;
                if (player2NameInput != null) player2NameInput.text = NameManager.Instance.Player2Name;
                if (teamNameInput != null) teamNameInput.text = NameManager.Instance.TeamName;
            }
        }

        /// <summary>
        /// Kontrol yetkisini guncelle.
        /// </summary>
        private void UpdateEditPermission()
        {
            if (!restrictEditing)
            {
                _canEdit = true;
            }
            else
            {
                // MasterClient veya Player1 kontrol edebilir
                _canEdit = PhotonNetwork.IsMasterClient || IsPlayer1();
            }

            // Input field'lari guncelle
            if (player1NameInput != null) player1NameInput.interactable = _canEdit;
            if (player2NameInput != null) player2NameInput.interactable = _canEdit;
            if (teamNameInput != null) teamNameInput.interactable = _canEdit;
            if (startButton != null) startButton.interactable = _canEdit;
            if (skipButton != null) skipButton.interactable = _canEdit;

            // Status text guncelle
            if (statusText != null)
            {
                if (_canEdit)
                {
                    statusText.text = "Isimleri girin ve Basla'ya basin";
                }
                else
                {
                    statusText.text = "Kaptan isim giriyor, bekleyin...";
                }
            }
        }

        /// <summary>
        /// Local oyuncu Player1 mi?
        /// </summary>
        private bool IsPlayer1()
        {
            if (NetworkManager.Instance == null) return false;
            return NetworkManager.Instance.LocalPlayerType == PlayerType.Player1;
        }

        /// <summary>
        /// Paneli goster.
        /// </summary>
        public void Show()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            SetDefaultNames();
            UpdateEditPermission();
        }

        /// <summary>
        /// Paneli gizle.
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        /// <summary>
        /// Basla butonuna basildi.
        /// </summary>
        private void OnStartButtonClicked()
        {
            if (!_canEdit) return;

            string p1Name = player1NameInput != null ? player1NameInput.text : defaultPlayer1Name;
            string p2Name = player2NameInput != null ? player2NameInput.text : defaultPlayer2Name;
            string teamName = teamNameInput != null ? teamNameInput.text : defaultTeamName;

            // NameManager'a gonder
            if (NameManager.Instance != null)
            {
                NameManager.Instance.SetNames(p1Name, p2Name, teamName);
                NameManager.Instance.ConfirmNamesAndStart();
            }

            // Event firlat
            OnNamesSubmitted?.Invoke(p1Name, p2Name, teamName);

            // Paneli gizle
            Hide();
        }

        /// <summary>
        /// Atla butonuna basildi (varsayilan isimlerle devam).
        /// </summary>
        private void OnSkipButtonClicked()
        {
            if (!_canEdit) return;

            // Varsayilan isimlerle devam et
            if (NameManager.Instance != null)
            {
                NameManager.Instance.ConfirmNamesAndStart();
            }

            OnSkipped?.Invoke();
            Hide();
        }

        /// <summary>
        /// GameState degistiginde UI'i guncelle.
        /// </summary>
        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.EnteringNames)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Network'ten isim degisikligi geldi.
        /// </summary>
        private void OnNamesChangedFromNetwork(string p1, string p2, string team)
        {
            if (player1NameInput != null && !_canEdit) player1NameInput.text = p1;
            if (player2NameInput != null && !_canEdit) player2NameInput.text = p2;
            if (teamNameInput != null && !_canEdit) teamNameInput.text = team;
        }

        /// <summary>
        /// Input field'lar degistiginde network'e gonder (real-time sync).
        /// </summary>
        public void OnInputFieldChanged()
        {
            if (!_canEdit) return;

            // Her degisiklikte network'e gonderme, sadece Basla'da gonder
            // Eger real-time sync istenirse asagidaki kodu aktive et:
            /*
            if (NameManager.Instance != null)
            {
                string p1 = player1NameInput != null ? player1NameInput.text : "";
                string p2 = player2NameInput != null ? player2NameInput.text : "";
                string team = teamNameInput != null ? teamNameInput.text : "";
                NameManager.Instance.SetNames(p1, p2, team);
            }
            */
        }
    }
}
