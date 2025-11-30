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
        [SerializeField] private float positionLerpSpeed = 25f;

        [Tooltip("Rotasyon interpolasyon hizi")]
        [SerializeField] private float rotationLerpSpeed = 25f;

        [Tooltip("Pozisyon snap mesafesi (bu mesafeden uzaksa direkt snap)")]
        [SerializeField] private float positionSnapThreshold = 1f;

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
            InitializeNetworkState();

            // NetworkManager hazirsa hemen belirle, degilse event bekle
            if (NetworkManager.Instance != null && PhotonNetwork.InRoom)
            {
                // Kucuk gecikme - CustomProperties'in gelmesi icin
                Invoke(nameof(DetermineOwnership), 0.3f);
            }

            // Player properties guncellendiginde yeniden kontrol et
            // Bu onemli cunku AssignPlayerTypeAutomatically() room'a katildiktan sonra calisir
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Kendi property'miz guncellendiyse ownership'i yeniden belirle
            if (targetPlayer == PhotonNetwork.LocalPlayer && !_isInitialized)
            {
                DetermineOwnership();
                _isInitialized = true;
            }
        }

        public override void OnJoinedRoom()
        {
            // Room'a katildiginda kucuk gecikme ile ownership belirle
            Invoke(nameof(DetermineOwnership), 0.3f);
        }

        /// <summary>
        /// Bu kurek local oyuncuya mi ait?
        /// </summary>
        private void DetermineOwnership()
        {
            // Zaten belirlenmisse tekrar yapma
            if (_isInitialized) return;

            if (NetworkManager.Instance == null)
            {
                // Offline mod - her zaman local
                _isLocalPaddle = true;
                _isInitialized = true;
                Debug.Log($"[PaddleSync] Offline mod - Owner: {ownerPlayerIndex}, Local: true");
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
            else
            {
                // Local ise fizik ve controller'i aktif tut
                if (_paddleController != null)
                    _paddleController.enabled = true;

                if (_paddlePhysics != null)
                    _paddlePhysics.enabled = true;
            }

            _isInitialized = true;
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
            // Cok uzaktaysa direkt snap yap (teleport durumu)
            float distance = Vector3.Distance(transform.position, _networkPosition);
            if (distance > positionSnapThreshold)
            {
                transform.position = _networkPosition;
                transform.rotation = _networkRotation;
                return;
            }

            // Yumusak interpolasyon
            float lerpFactor = 1f - Mathf.Exp(-positionLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _networkPosition, lerpFactor);

            float rotLerpFactor = 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, rotLerpFactor);
        }

        #region IPunObservable

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // ONEMLI: _isLocalPaddle kontrolu yapiyoruz, stream.IsWriting PhotonView owner'a bagli
            // Eger bu paddle local degilse, veri gondermemeli
            if (stream.IsWriting)
            {
                // Sadece local paddle veri gonderebilir
                if (!_isLocalPaddle)
                {
                    // Bu durumda olmamali ama guvenlik icin
                    stream.SendNext(transform.position);
                    stream.SendNext(transform.rotation);
                    stream.SendNext(false);
                    stream.SendNext(false);
                    return;
                }

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
