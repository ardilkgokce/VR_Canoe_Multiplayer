using UnityEngine;
using Photon.Pun;
using VRCanoe.Network;

namespace VRCanoe.Paddle
{
    /// <summary>
    /// 2 uclu kurek pozisyon/rotasyon senkronizasyonu (gorsel).
    /// Her oyuncunun kuregi diger clientlarda gorulur.
    /// Fizik hesabi her client'ta kendi controller'indan yapilir.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PaddleController))]
    public class PaddleSync : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Interpolasyon Ayarlari")]
        [Tooltip("Pozisyon interpolasyon hizi")]
        [SerializeField] private float positionLerpSpeed = 15f;

        [Tooltip("Rotasyon interpolasyon hizi")]
        [SerializeField] private float rotationLerpSpeed = 15f;

        [Header("Sahiplik")]
        [Tooltip("Bu kurek hangi oyuncuya ait? (Player1 = 0, Player2 = 1)")]
        [SerializeField] private int ownerPlayerIndex = 0;

        // Network state
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private bool _networkTip1InWater;
        private bool _networkTip2InWater;

        // Components
        private PaddleController _paddleController;
        private PaddlePhysics _paddlePhysics;

        // Local state
        private bool _isLocalPaddle;
        private bool _isInitialized;

        // Properties
        public bool IsLocalPaddle => _isLocalPaddle;
        public int OwnerPlayerIndex => ownerPlayerIndex;

        private void Awake()
        {
            _paddleController = GetComponent<PaddleController>();
            _paddlePhysics = GetComponent<PaddlePhysics>();
        }

        private void Start()
        {
            DetermineOwnership();
            InitializeNetworkState();
            _isInitialized = true;
        }

        /// <summary>
        /// Bu kurek local oyuncuya mi ait?
        /// </summary>
        private void DetermineOwnership()
        {
            if (NetworkManager.Instance == null)
            {
                // Offline mod - her zaman local
                _isLocalPaddle = true;
                return;
            }

            PlayerType localType = NetworkManager.Instance.LocalPlayerType;

            // Player1 -> index 0, Player2 -> index 1
            if (localType == PlayerType.Player1 && ownerPlayerIndex == 0)
            {
                _isLocalPaddle = true;
            }
            else if (localType == PlayerType.Player2 && ownerPlayerIndex == 1)
            {
                _isLocalPaddle = true;
            }
            else
            {
                _isLocalPaddle = false;
            }

            // Local degilse fizik ve controller'i devre disi birak
            if (!_isLocalPaddle)
            {
                if (_paddleController != null)
                    _paddleController.enabled = false;

                if (_paddlePhysics != null)
                    _paddlePhysics.enabled = false;
            }

            Debug.Log($"[PaddleSync] Owner: {ownerPlayerIndex}, Local: {_isLocalPaddle}, PlayerType: {localType}");
        }

        private void InitializeNetworkState()
        {
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
            _networkTip1InWater = false;
            _networkTip2InWater = false;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            // Local degilse interpolasyon uygula
            if (!_isLocalPaddle)
            {
                ApplyInterpolation();
            }
        }

        /// <summary>
        /// Network pozisyonuna dogru interpolasyon.
        /// </summary>
        private void ApplyInterpolation()
        {
            transform.position = Vector3.Lerp(transform.position, _networkPosition, positionLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, rotationLerpSpeed * Time.deltaTime);
        }

        #region IPunObservable

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Local paddle - veri gonder
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);

                // Her iki ucun su durumunu gonder
                bool tip1InWater = _paddlePhysics != null && _paddlePhysics.Tip1InWater;
                bool tip2InWater = _paddlePhysics != null && _paddlePhysics.Tip2InWater;
                stream.SendNext(tip1InWater);
                stream.SendNext(tip2InWater);
            }
            else
            {
                // Remote paddle - veri al
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                _networkTip1InWater = (bool)stream.ReceiveNext();
                _networkTip2InWater = (bool)stream.ReceiveNext();
            }
        }

        #endregion

        /// <summary>
        /// Kurek sahibini ayarla (runtime).
        /// </summary>
        public void SetOwnerPlayerIndex(int index)
        {
            ownerPlayerIndex = index;
            DetermineOwnership();
        }

        /// <summary>
        /// Network uzerinden Tip1 suda mi?
        /// </summary>
        public bool GetNetworkTip1InWater()
        {
            return _isLocalPaddle
                ? (_paddlePhysics != null && _paddlePhysics.Tip1InWater)
                : _networkTip1InWater;
        }

        /// <summary>
        /// Network uzerinden Tip2 suda mi?
        /// </summary>
        public bool GetNetworkTip2InWater()
        {
            return _isLocalPaddle
                ? (_paddlePhysics != null && _paddlePhysics.Tip2InWater)
                : _networkTip2InWater;
        }

        /// <summary>
        /// Network uzerinden herhangi bir uc suda mi?
        /// </summary>
        public bool GetNetworkAnyTipInWater()
        {
            return GetNetworkTip1InWater() || GetNetworkTip2InWater();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Sahiplik gostergesi
            Gizmos.color = _isLocalPaddle ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);

            // Network su durumu (remote paddle icin)
            if (!_isLocalPaddle && Application.isPlaying)
            {
                if (_paddleController != null)
                {
                    Gizmos.color = _networkTip1InWater ? Color.cyan : Color.gray;
                    Gizmos.DrawWireSphere(_paddleController.Tip1PositionWorld, 0.04f);

                    Gizmos.color = _networkTip2InWater ? Color.cyan : Color.gray;
                    Gizmos.DrawWireSphere(_paddleController.Tip2PositionWorld, 0.04f);
                }
            }
        }
#endif
    }
}
