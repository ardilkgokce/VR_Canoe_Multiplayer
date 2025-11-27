using UnityEngine;
using Photon.Pun;
using VRCanoe.Game;

namespace VRCanoe.Canoe
{
    /// <summary>
    /// Kano fizik kontrolu. Rigidbody tabanli hareket ve stabilite.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class CanoeController : MonoBehaviourPunCallbacks
    {
        [Header("Su Ayarlari")]
        [Tooltip("Suyun Y pozisyonu")]
        [SerializeField] private float waterLevel = 0f;

        [Tooltip("Kano su seviyesinden ne kadar yukarda kalsin")]
        [SerializeField] private float floatHeight = 0.3f;

        [Tooltip("Floating yumusatma hizi")]
        [SerializeField] private float floatDamping = 2f;

        [Header("Stabilite Ayarlari")]
        [Tooltip("Rotasyon duzeltme hizi")]
        [SerializeField] private float stabilizationSpeed = 5f;

        [Tooltip("Maksimum izin verilen X rotasyonu (derece)")]
        [SerializeField] private float maxTiltAngle = 5f;

        [Header("Fizik Ayarlari")]
        [Tooltip("Su direnci (drag)")]
        [SerializeField] private float waterDrag = 1.5f;

        [Tooltip("Rotasyon direnci (angular drag)")]
        [SerializeField] private float waterAngularDrag = 2f;

        [Header("Referanslar")]
        [SerializeField] private Transform leftPaddlePoint;
        [SerializeField] private Transform rightPaddlePoint;
        [SerializeField] private Transform player1Seat;
        [SerializeField] private Transform player2Seat;

        // Components
        private Rigidbody _rigidbody;
        private CanoeMovement _movement;
        private CanoePositionSync _positionSync;

        // Baslangic pozisyonu
        private Vector3 _startPosition;
        private Quaternion _startRotation;

        // Properties
        public Rigidbody Rigidbody => _rigidbody;
        public Transform LeftPaddlePoint => leftPaddlePoint;
        public Transform RightPaddlePoint => rightPaddlePoint;
        public Transform Player1Seat => player1Seat;
        public Transform Player2Seat => player2Seat;
        public float WaterLevel => waterLevel;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _movement = GetComponent<CanoeMovement>();
            _positionSync = GetComponent<CanoePositionSync>();

            // Baslangic pozisyonunu kaydet
            _startPosition = transform.position;
            _startRotation = transform.rotation;

            SetupRigidbody();
        }

        private void Start()
        {
            // GameManager reset event'ini dinle
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

        private void OnGameReset()
        {
            // Sadece Master resetler ve senkronize eder
            if (!PhotonNetwork.IsMasterClient) return;

            Debug.Log("[CanoeController] Kano resetleniyor...");

            // PositionSync uzerinden teleport (tum clientlarda)
            if (_positionSync != null)
            {
                _positionSync.Teleport(_startPosition, _startRotation);
            }
            else
            {
                ResetPosition(_startPosition, _startRotation);
            }
        }

        private void SetupRigidbody()
        {
            // Rigidbody ayarlari
            _rigidbody.useGravity = false; // Kendi floating sistemimizi kullanacagiz
            _rigidbody.drag = waterDrag;
            _rigidbody.angularDrag = waterAngularDrag;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Constraints - Y ekseninde donusu serbest birak (yaw), diger rotasyonlari kisitla
            // Not: Constraints yerine kod ile stabilize ediyoruz daha smooth sonuc icin
        }

        private void FixedUpdate()
        {
            // Sadece fizik simulasyonu yapan client calistirsin
            // (MasterClient veya network sync yoksa local)
            if (!ShouldSimulatePhysics()) return;

            ApplyFloating();
            StabilizeRotation();
        }

        /// <summary>
        /// Fizik simulasyonu bu client'ta mi yapilmali?
        /// </summary>
        private bool ShouldSimulatePhysics()
        {
            var photonView = GetComponent<PhotonView>();
            if (photonView == null) return true; // Local test

            // Sadece sahibi fizik simulasyonu yapar
            return photonView.IsMine || (PhotonNetwork.IsMasterClient && photonView.Owner == null);
        }

        /// <summary>
        /// Kanoyu su seviyesinde tut.
        /// </summary>
        private void ApplyFloating()
        {
            Vector3 position = transform.position;
            float targetY = waterLevel + floatHeight;

            // Yumusak gecis ile Y pozisyonunu duzelt
            if (Mathf.Abs(position.y - targetY) > 0.001f)
            {
                float newY = Mathf.Lerp(position.y, targetY, floatDamping * Time.fixedDeltaTime);
                _rigidbody.MovePosition(new Vector3(position.x, newY, position.z));
            }

            // Y eksenindeki velocity'yi sifirla (saga sola kaymayi engelle)
            Vector3 velocity = _rigidbody.velocity;
            velocity.y = 0f;
            _rigidbody.velocity = velocity;
        }

        /// <summary>
        /// Kano devrilmesin, X ve Z rotasyonlarini duzelt.
        /// </summary>
        private void StabilizeRotation()
        {
            Vector3 currentEuler = transform.eulerAngles;

            // Euler acilarini -180 ile 180 arasina normalize et
            float xAngle = NormalizeAngle(currentEuler.x);
            float zAngle = NormalizeAngle(currentEuler.z);

            // Eger cok fazla egilmisse hizli duzelt
            float xTarget = Mathf.Clamp(xAngle, -maxTiltAngle, maxTiltAngle);
            float zTarget = Mathf.Clamp(zAngle, -maxTiltAngle, maxTiltAngle);

            // Hedefe dogru lerp
            float newX = Mathf.LerpAngle(xAngle, xTarget * 0.1f, stabilizationSpeed * Time.fixedDeltaTime);
            float newZ = Mathf.LerpAngle(zAngle, zTarget * 0.1f, stabilizationSpeed * Time.fixedDeltaTime);

            // Sadece Y rotasyonunu koru (yaw - donus)
            Quaternion targetRotation = Quaternion.Euler(newX, currentEuler.y, newZ);
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, stabilizationSpeed * Time.fixedDeltaTime));

            // Angular velocity'nin X ve Z bilesenlerini azalt
            Vector3 angularVel = _rigidbody.angularVelocity;
            angularVel.x *= 0.9f;
            angularVel.z *= 0.9f;
            _rigidbody.angularVelocity = angularVel;
        }

        /// <summary>
        /// Aciyi -180 ile 180 arasina normalize et.
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Su seviyesini ayarla.
        /// </summary>
        public void SetWaterLevel(float level)
        {
            waterLevel = level;
        }

        /// <summary>
        /// Kanoyu baslangic pozisyonuna resetle.
        /// </summary>
        public void ResetPosition(Vector3 position, Quaternion rotation)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            transform.position = new Vector3(position.x, waterLevel + floatHeight, position.z);
            transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Nehir kenarina carpinca
            if (collision.gameObject.CompareTag("Boundary"))
            {
                HandleBoundaryCollision(collision);
            }
        }

        private void HandleBoundaryCollision(Collision collision)
        {
            // Basit geri sekme
            Vector3 normal = collision.contacts[0].normal;
            Vector3 bounceDirection = Vector3.Reflect(_rigidbody.velocity.normalized, normal);

            // Hizi azalt ve geri sek
            _rigidbody.velocity = bounceDirection * _rigidbody.velocity.magnitude * 0.3f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Su seviyesini goster
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            Vector3 waterPos = new Vector3(transform.position.x, waterLevel, transform.position.z);
            Gizmos.DrawCube(waterPos, new Vector3(5f, 0.1f, 10f));

            // Kurek noktalarini goster
            Gizmos.color = Color.yellow;
            if (leftPaddlePoint != null)
                Gizmos.DrawWireSphere(leftPaddlePoint.position, 0.2f);
            if (rightPaddlePoint != null)
                Gizmos.DrawWireSphere(rightPaddlePoint.position, 0.2f);

            // Oturma pozisyonlarini goster
            Gizmos.color = Color.green;
            if (player1Seat != null)
                Gizmos.DrawWireSphere(player1Seat.position, 0.3f);
            if (player2Seat != null)
                Gizmos.DrawWireSphere(player2Seat.position, 0.3f);
        }
#endif
    }
}
