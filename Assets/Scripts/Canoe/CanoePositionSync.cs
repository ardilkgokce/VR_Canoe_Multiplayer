using UnityEngine;
using Photon.Pun;

namespace VRCanoe.Canoe
{
    /// <summary>
    /// Kano pozisyon ve rotasyon senkronizasyonu.
    /// MasterClient fizigi simule eder, diger clientlar interpolasyon ile takip eder.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(Rigidbody))]
    public class CanoePositionSync : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Interpolasyon Ayarlari")]
        [Tooltip("Pozisyon interpolasyon hizi")]
        [SerializeField] private float positionLerpSpeed = 10f;

        [Tooltip("Rotasyon interpolasyon hizi")]
        [SerializeField] private float rotationLerpSpeed = 10f;

        [Tooltip("Teleport mesafesi (bu mesafeden fazlaysa aninda atla)")]
        [SerializeField] private float teleportDistance = 5f;

        [Header("Extrapolasyon")]
        [Tooltip("Extrapolasyon aktif mi?")]
        [SerializeField] private bool useExtrapolation = true;

        [Tooltip("Maksimum extrapolasyon suresi (saniye)")]
        [SerializeField] private float maxExtrapolationTime = 0.2f;

        // Network state
        private Vector3 _networkPosition;
        private Quaternion _networkRotation;
        private Vector3 _networkVelocity;
        private Vector3 _networkAngularVelocity;
        private double _lastReceiveTime;

        // Components
        private Rigidbody _rigidbody;
        private CanoeController _controller;

        // Local state
        private bool _isInitialized;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _controller = GetComponent<CanoeController>();
        }

        private void Start()
        {
            // Baslangic pozisyonunu kaydet
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
            _networkVelocity = Vector3.zero;
            _networkAngularVelocity = Vector3.zero;
            _lastReceiveTime = PhotonNetwork.Time;
            _isInitialized = true;

            // Fizik ayarlari
            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (photonView.IsMine || IsMasterClientOwned())
            {
                // Fizik simulasyonu yapan client - normal Rigidbody
                _rigidbody.isKinematic = false;
            }
            else
            {
                // Takip eden client - kinematic Rigidbody
                _rigidbody.isKinematic = true;
            }
        }

        private bool IsMasterClientOwned()
        {
            return PhotonNetwork.IsMasterClient && (photonView.Owner == null || photonView.Owner == PhotonNetwork.MasterClient);
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            // Fizik simulasyonu bu client'ta mi?
            if (photonView.IsMine || IsMasterClientOwned())
            {
                // Bu client fizik simulasyonu yapiyor, bir sey yapma
                return;
            }

            // Diger clientlar - interpolasyon/extrapolasyon uygula
            ApplyInterpolation();
        }

        private void ApplyInterpolation()
        {
            // Gecen sure
            float timeSinceLastUpdate = (float)(PhotonNetwork.Time - _lastReceiveTime);

            // Hedef pozisyon (extrapolasyon ile)
            Vector3 targetPosition = _networkPosition;
            Quaternion targetRotation = _networkRotation;

            if (useExtrapolation && timeSinceLastUpdate < maxExtrapolationTime)
            {
                // Velocity tabanli extrapolasyon
                targetPosition += _networkVelocity * timeSinceLastUpdate;

                // Angular velocity tabanli extrapolasyon
                if (_networkAngularVelocity.magnitude > 0.01f)
                {
                    float angle = _networkAngularVelocity.magnitude * Mathf.Rad2Deg * timeSinceLastUpdate;
                    Quaternion extraRotation = Quaternion.AngleAxis(angle, _networkAngularVelocity.normalized);
                    targetRotation = extraRotation * _networkRotation;
                }
            }

            // Teleport kontrolu
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance > teleportDistance)
            {
                // Aninda teleport
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
            else
            {
                // Smooth interpolasyon
                transform.position = Vector3.Lerp(transform.position, targetPosition, positionLerpSpeed * Time.fixedDeltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
            }
        }

        #region IPunObservable

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Bu client veri gonderiyor (fizik simulasyonu yapan)
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_rigidbody.velocity);
                stream.SendNext(_rigidbody.angularVelocity);
            }
            else
            {
                // Veri aliniyor
                _networkPosition = (Vector3)stream.ReceiveNext();
                _networkRotation = (Quaternion)stream.ReceiveNext();
                _networkVelocity = (Vector3)stream.ReceiveNext();
                _networkAngularVelocity = (Vector3)stream.ReceiveNext();
                _lastReceiveTime = info.SentServerTime;
            }
        }

        #endregion

        /// <summary>
        /// Kanoyu belirli bir pozisyona teleport et (tum clientlarda).
        /// </summary>
        [PunRPC]
        public void RPC_Teleport(Vector3 position, Quaternion rotation)
        {
            _networkPosition = position;
            _networkRotation = rotation;
            _networkVelocity = Vector3.zero;
            _networkAngularVelocity = Vector3.zero;

            transform.position = position;
            transform.rotation = rotation;

            if (!_rigidbody.isKinematic)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// Tum clientlarda teleport cagir.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (photonView.IsMine || IsMasterClientOwned())
            {
                photonView.RPC(nameof(RPC_Teleport), RpcTarget.All, position, rotation);
            }
        }

        /// <summary>
        /// Sahiplik degistiginde fizik ayarlarini guncelle.
        /// </summary>
        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            ConfigurePhysics();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;

            // Network pozisyonunu goster
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_networkPosition, 0.5f);

            // Velocity vektorunu goster
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_networkPosition, _networkPosition + _networkVelocity);
        }
#endif
    }
}
