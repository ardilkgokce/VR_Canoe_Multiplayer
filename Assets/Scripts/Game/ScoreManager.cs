using System;
using UnityEngine;
using Photon.Pun;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace VRCanoe.Game
{
    /// <summary>
    /// Skor yonetimi. Coin toplama, bonus hesaplama.
    /// MasterClient skorlari yonetir, diger clientlar sync alir.
    /// </summary>
    public class ScoreManager : MonoBehaviourPunCallbacks
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Ayarlar")]
        [SerializeField] private GameSettings gameSettings;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public event Action<int> OnScoreChanged;
        public event Action<int> OnCoinCollected; // Coin degeri

        // Skorlar
        private int _coinScore;
        private int _syncBonus;
        private int _timeBonus;
        private float _finishTime;

        // Room property keys
        private const string COIN_SCORE_KEY = "CoinScore";
        private const string SYNC_BONUS_KEY = "SyncBonus";
        private const string TIME_BONUS_KEY = "TimeBonus";
        private const string FINISH_TIME_KEY = "FinishTime";

        // Properties
        public int CoinScore => _coinScore;
        public int SyncBonus => _syncBonus;
        public int TimeBonus => _timeBonus;
        public float FinishTime => _finishTime;
        public int TotalScore => _coinScore + _syncBonus + _timeBonus;

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
            // GameSettings bul
            if (gameSettings == null && CanoeGameManager.Instance != null)
            {
                gameSettings = CanoeGameManager.Instance.Settings;
            }

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
        /// Coin toplandi.
        /// </summary>
        public void AddCoin()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                // MasterClient'a bildir
                photonView.RPC(nameof(RPC_RequestAddCoin), RpcTarget.MasterClient);
                return;
            }

            int coinValue = gameSettings != null ? gameSettings.coinValue : 10;
            _coinScore += coinValue;

            SyncScores();
            photonView.RPC(nameof(RPC_OnCoinCollected), RpcTarget.All, coinValue);

            if (showDebugInfo)
            {
                Debug.Log($"[ScoreManager] Coin toplandi: +{coinValue}, Toplam: {_coinScore}");
            }
        }

        /// <summary>
        /// Senkronize kurek bonusu ekle.
        /// </summary>
        public void AddSyncBonus(int bonus)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_RequestAddSyncBonus), RpcTarget.MasterClient, bonus);
                return;
            }

            _syncBonus += bonus;
            SyncScores();

            if (showDebugInfo)
            {
                Debug.Log($"[ScoreManager] Sync bonus: +{bonus}, Toplam: {_syncBonus}");
            }
        }

        /// <summary>
        /// Bitis suresine gore zaman bonusu hesapla.
        /// </summary>
        public void CalculateTimeBonus(float finishTime)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            _finishTime = finishTime;

            float gameDuration = gameSettings != null ? gameSettings.gameDuration : 60f;

            // Erken bitirme bonusu: kalan sure * 10 puan
            float remainingTime = gameDuration - finishTime;
            if (remainingTime > 0)
            {
                _timeBonus = Mathf.RoundToInt(remainingTime * 10f);
            }
            else
            {
                _timeBonus = 0;
            }

            SyncScores();

            if (showDebugInfo)
            {
                Debug.Log($"[ScoreManager] Bitis suresi: {finishTime:F1}s, Zaman bonusu: {_timeBonus}");
            }
        }

        /// <summary>
        /// Tum skorlari sifirla.
        /// </summary>
        public void ResetScores()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(RPC_RequestResetScores), RpcTarget.MasterClient);
                return;
            }

            _coinScore = 0;
            _syncBonus = 0;
            _timeBonus = 0;
            _finishTime = 0f;

            SyncScores();

            if (showDebugInfo)
            {
                Debug.Log("[ScoreManager] Skorlar sifirlandi");
            }
        }

        /// <summary>
        /// Skorlari Room property olarak kaydet ve sync et.
        /// </summary>
        private void SyncScores()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Hashtable props = new Hashtable
            {
                { COIN_SCORE_KEY, _coinScore },
                { SYNC_BONUS_KEY, _syncBonus },
                { TIME_BONUS_KEY, _timeBonus },
                { FINISH_TIME_KEY, _finishTime }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            // Tum clientlara bildir
            photonView.RPC(nameof(RPC_SyncScores), RpcTarget.All, _coinScore, _syncBonus, _timeBonus, _finishTime);
        }

        private void OnGameReset()
        {
            // Local skorlari sifirla
            _coinScore = 0;
            _syncBonus = 0;
            _timeBonus = 0;
            _finishTime = 0f;

            OnScoreChanged?.Invoke(0);
        }

        #region RPCs

        [PunRPC]
        private void RPC_RequestAddCoin()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                AddCoin();
            }
        }

        [PunRPC]
        private void RPC_RequestAddSyncBonus(int bonus)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                AddSyncBonus(bonus);
            }
        }

        [PunRPC]
        private void RPC_RequestResetScores()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ResetScores();
            }
        }

        [PunRPC]
        private void RPC_SyncScores(int coinScore, int syncBonus, int timeBonus, float finishTime)
        {
            _coinScore = coinScore;
            _syncBonus = syncBonus;
            _timeBonus = timeBonus;
            _finishTime = finishTime;

            OnScoreChanged?.Invoke(TotalScore);
        }

        [PunRPC]
        private void RPC_OnCoinCollected(int coinValue)
        {
            OnCoinCollected?.Invoke(coinValue);
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

            if (propertiesThatChanged.TryGetValue(COIN_SCORE_KEY, out object coinObj))
            {
                _coinScore = (int)coinObj;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(SYNC_BONUS_KEY, out object syncObj))
            {
                _syncBonus = (int)syncObj;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(TIME_BONUS_KEY, out object timeObj))
            {
                _timeBonus = (int)timeObj;
                updated = true;
            }
            if (propertiesThatChanged.TryGetValue(FINISH_TIME_KEY, out object finishObj))
            {
                _finishTime = (float)finishObj;
                updated = true;
            }

            if (updated)
            {
                OnScoreChanged?.Invoke(TotalScore);
            }
        }

        private void SyncFromRoom()
        {
            if (PhotonNetwork.CurrentRoom == null) return;

            var props = PhotonNetwork.CurrentRoom.CustomProperties;

            if (props.TryGetValue(COIN_SCORE_KEY, out object coinObj))
                _coinScore = (int)coinObj;
            if (props.TryGetValue(SYNC_BONUS_KEY, out object syncObj))
                _syncBonus = (int)syncObj;
            if (props.TryGetValue(TIME_BONUS_KEY, out object timeObj))
                _timeBonus = (int)timeObj;
            if (props.TryGetValue(FINISH_TIME_KEY, out object finishObj))
                _finishTime = (float)finishObj;

            OnScoreChanged?.Invoke(TotalScore);
        }

        #endregion

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 120));
            GUILayout.Box("Score Manager");
            GUILayout.Label($"Coins: {_coinScore}");
            GUILayout.Label($"Sync Bonus: {_syncBonus}");
            GUILayout.Label($"Time Bonus: {_timeBonus}");
            GUILayout.Label($"TOTAL: {TotalScore}");
            if (_finishTime > 0)
            {
                GUILayout.Label($"Finish: {_finishTime:F1}s");
            }
            GUILayout.EndArea();
        }
#endif
    }
}
