using UnityEngine;
using VRCanoe.Game;
using VRCanoe.VRPlayer;
using VRCanoe.Network;
using VRCanoe.Canoe;

namespace VRCanoe.UI
{
    /// <summary>
    /// Tum debug UI'larini merkezi olarak kontrol eder.
    /// Inspector'dan tek tikla tum debug panellerini ac/kapa.
    /// </summary>
    public class DebugUIManager : MonoBehaviour
    {
        public static DebugUIManager Instance { get; private set; }

        [Header("Master Kontrol")]
        [Tooltip("Tum debug UI'larini ac/kapa")]
        [SerializeField] private bool showAllDebugUI = true;

        [Header("Bireysel Kontroller")]
        [SerializeField] private bool showNetworkDebug = true;
        [SerializeField] private bool showGameManagerDebug = true;
        [SerializeField] private bool showTimerDebug = true;
        [SerializeField] private bool showScoreDebug = true;
        [SerializeField] private bool showPlayerSpawnerDebug = true;
        [SerializeField] private bool showSeatAssignmentDebug = true;
        [SerializeField] private bool showCanoeDebug = true;

        [Header("Klavye Kisayolu")]
        [Tooltip("Debug UI toggle tusu")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;

        // Properties
        public bool ShowAllDebugUI => showAllDebugUI;
        public bool ShowNetworkDebug => showAllDebugUI && showNetworkDebug;
        public bool ShowGameManagerDebug => showAllDebugUI && showGameManagerDebug;
        public bool ShowTimerDebug => showAllDebugUI && showTimerDebug;
        public bool ShowScoreDebug => showAllDebugUI && showScoreDebug;
        public bool ShowPlayerSpawnerDebug => showAllDebugUI && showPlayerSpawnerDebug;
        public bool ShowSeatAssignmentDebug => showAllDebugUI && showSeatAssignmentDebug;
        public bool ShowCanoeDebug => showAllDebugUI && showCanoeDebug;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // F12 ile toggle
            if (Input.GetKeyDown(toggleKey))
            {
                showAllDebugUI = !showAllDebugUI;
                Debug.Log($"[DebugUIManager] Debug UI: {(showAllDebugUI ? "ACIK" : "KAPALI")}");
            }
        }

        /// <summary>
        /// Tum debug UI'larini ac.
        /// </summary>
        public void EnableAll()
        {
            showAllDebugUI = true;
        }

        /// <summary>
        /// Tum debug UI'larini kapat.
        /// </summary>
        public void DisableAll()
        {
            showAllDebugUI = false;
        }

        /// <summary>
        /// Toggle tum debug UI'lar.
        /// </summary>
        public void ToggleAll()
        {
            showAllDebugUI = !showAllDebugUI;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Editor'da degerler degistiginde aninda uygula
        }

        private void OnGUI()
        {
            if (!showAllDebugUI) return;

            // Kucuk bilgi paneli
            GUILayout.BeginArea(new Rect(10, Screen.height - 30, 200, 25));
            GUILayout.Box($"Debug UI: ON (F12 toggle)");
            GUILayout.EndArea();
        }
#endif
    }
}
