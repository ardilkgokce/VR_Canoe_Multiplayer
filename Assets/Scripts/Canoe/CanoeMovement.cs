using UnityEngine;
using Photon.Pun;
using VRCanoe.Game;

namespace VRCanoe.Canoe
{
    /// <summary>
    /// Kano hareket sistemi. Kureklerden gelen kuvvetleri uygular.
    /// Tum oyuncular kurek cekebilir, kuvvetler RPC ile Master'a gonderilir.
    /// </summary>
    [RequireComponent(typeof(CanoeController))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PhotonView))]
    public class CanoeMovement : MonoBehaviourPun
    {
        [Header("Hareket Ayarlari")]
        [Tooltip("Kurek kuvvet carpani")]
        [SerializeField] private float forceMultiplier = 10f;

        [Tooltip("Donus kuvvet carpani")]
        [SerializeField] private float torqueMultiplier = 5f;

        [Tooltip("Maksimum ileri hiz (m/s)")]
        [SerializeField] private float maxForwardSpeed = 8f;

        [Tooltip("Maksimum donus hizi (derece/s)")]
        [SerializeField] private float maxAngularSpeed = 90f;

        [Header("Senkronize Kurek Bonusu")]
        [Tooltip("Senkronize kurek zamanlama penceresi (saniye)")]
        [SerializeField] private float syncWindow = 0.3f;

        [Tooltip("Senkronize kurek bonus carpani")]
        [SerializeField] private float syncBonusMultiplier = 1.5f;

        [Header("Geri Sekme")]
        [Tooltip("Carpisma geri sekme kuvveti")]
        [SerializeField] private float bounceForce = 5f;

        // Components
        private Rigidbody _rigidbody;
        private CanoeController _controller;
        private PhotonView _photonView;

        // Senkronize kurek tracking
        private float _lastLeftPaddleTime;
        private float _lastRightPaddleTime;
        private bool _syncBonusApplied;

        // Properties
        public float CurrentSpeed => _rigidbody.velocity.magnitude;
        public float CurrentForwardSpeed => Vector3.Dot(_rigidbody.velocity, transform.forward);
        public bool IsSyncBonusActive => _syncBonusApplied;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _controller = GetComponent<CanoeController>();
            _photonView = GetComponent<PhotonView>();
        }

        private void Start()
        {
            // GameSettings'ten ayarlari al
            ApplyGameSettings();
        }

        private void ApplyGameSettings()
        {
            if (CanoeGameManager.Instance != null && CanoeGameManager.Instance.Settings != null)
            {
                var settings = CanoeGameManager.Instance.Settings;
                forceMultiplier *= settings.canoeSpeedMultiplier;
                syncBonusMultiplier = settings.syncPaddleMultiplier;
                syncWindow = settings.syncPaddleWindow;
            }
        }

        private void FixedUpdate()
        {
            ClampVelocity();
            ResetSyncBonus();
        }

        /// <summary>
        /// Kurekten gelen kuvveti uygula.
        /// Herhangi bir client cagirabilir, RPC ile Master'a gonderilir.
        /// </summary>
        /// <param name="force">Kurek kuvveti (local space)</param>
        /// <param name="torque">Donus kuvveti</param>
        /// <param name="isRightSide">Sag taraftan mi?</param>
        public void AddPaddleForce(Vector3 force, Vector3 torque, bool isRightSide)
        {
            // Oyun Playing state'inde degilse hareket etme
            if (CanoeGameManager.Instance != null && !CanoeGameManager.Instance.IsPlaying)
            {
                return;
            }

            // Master degilsek, RPC ile Master'a gonder
            if (!ShouldSimulatePhysics())
            {
                photonView.RPC(nameof(RPC_AddPaddleForce), RpcTarget.MasterClient, force, torque, isRightSide);
                return;
            }

            // Master ise direkt uygula
            ApplyPaddleForceInternal(force, torque, isRightSide);
        }

        [PunRPC]
        private void RPC_AddPaddleForce(Vector3 force, Vector3 torque, bool isRightSide)
        {
            // Sadece Master bu RPC'yi isler
            if (!PhotonNetwork.IsMasterClient) return;

            ApplyPaddleForceInternal(force, torque, isRightSide);
        }

        /// <summary>
        /// Kurek kuvvetini gercekten uygula (sadece Master).
        /// </summary>
        private void ApplyPaddleForceInternal(Vector3 force, Vector3 torque, bool isRightSide)
        {
            // Senkronize kurek kontrolu
            float currentTime = Time.time;
            float bonusMultiplier = 1f;

            if (isRightSide)
            {
                _lastRightPaddleTime = currentTime;

                // Sol kurekle senkronize mi?
                if (currentTime - _lastLeftPaddleTime <= syncWindow)
                {
                    bonusMultiplier = syncBonusMultiplier;
                    _syncBonusApplied = true;
                }
            }
            else
            {
                _lastLeftPaddleTime = currentTime;

                // Sag kurekle senkronize mi?
                if (currentTime - _lastRightPaddleTime <= syncWindow)
                {
                    bonusMultiplier = syncBonusMultiplier;
                    _syncBonusApplied = true;
                }
            }

            // Kuvveti world space'e cevir ve uygula
            Vector3 worldForce = transform.TransformDirection(force) * forceMultiplier * bonusMultiplier;
            _rigidbody.AddForce(worldForce, ForceMode.Impulse);

            // Torque uygula (sag taraftan cekince sola don, sol taraftan cekince saga don)
            Vector3 worldTorque = torque * torqueMultiplier;

            // Sag taraftan kurek cekilince sola donus (negatif Y)
            // Sol taraftan kurek cekilince saga donus (pozitif Y)
            float turnDirection = isRightSide ? -1f : 1f;
            _rigidbody.AddTorque(Vector3.up * worldTorque.magnitude * turnDirection * bonusMultiplier, ForceMode.Impulse);
        }

        /// <summary>
        /// Direkt kuvvet uygula (test veya ozel durumlar icin).
        /// </summary>
        public void AddForce(Vector3 worldForce, ForceMode mode = ForceMode.Force)
        {
            if (!ShouldSimulatePhysics()) return;

            _rigidbody.AddForce(worldForce, mode);
        }

        /// <summary>
        /// Direkt torque uygula.
        /// </summary>
        public void AddTorque(Vector3 worldTorque, ForceMode mode = ForceMode.Force)
        {
            if (!ShouldSimulatePhysics()) return;

            _rigidbody.AddTorque(worldTorque, mode);
        }

        /// <summary>
        /// Hizi maksimum degerlere sinirla.
        /// </summary>
        private void ClampVelocity()
        {
            // Lineer hiz siniri
            if (_rigidbody.velocity.magnitude > maxForwardSpeed)
            {
                _rigidbody.velocity = _rigidbody.velocity.normalized * maxForwardSpeed;
            }

            // Acisal hiz siniri (Y ekseni - yaw)
            Vector3 angularVel = _rigidbody.angularVelocity;
            float maxAngularRad = maxAngularSpeed * Mathf.Deg2Rad;

            if (Mathf.Abs(angularVel.y) > maxAngularRad)
            {
                angularVel.y = Mathf.Sign(angularVel.y) * maxAngularRad;
                _rigidbody.angularVelocity = angularVel;
            }
        }

        /// <summary>
        /// Senkronize bonus flag'ini resetle.
        /// </summary>
        private void ResetSyncBonus()
        {
            // Her frame sonunda bonus flag'ini resetle
            // (Bir sonraki kurek darbesinde tekrar kontrol edilecek)
            if (_syncBonusApplied && Time.time - Mathf.Max(_lastLeftPaddleTime, _lastRightPaddleTime) > syncWindow)
            {
                _syncBonusApplied = false;
            }
        }

        /// <summary>
        /// Fizik simulasyonu bu client'ta mi yapilmali?
        /// </summary>
        private bool ShouldSimulatePhysics()
        {
            if (_photonView == null) return true; // Local test

            return _photonView.IsMine || (PhotonNetwork.IsMasterClient && _photonView.Owner == null);
        }

        /// <summary>
        /// Hareketi durdur.
        /// </summary>
        public void Stop()
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Nehir kenarina carpisma.
        /// </summary>
        public void ApplyBounce(Vector3 normal)
        {
            if (!ShouldSimulatePhysics()) return;

            // Mevcut hizi yansit ve azalt
            Vector3 bounceVelocity = Vector3.Reflect(_rigidbody.velocity, normal);
            _rigidbody.velocity = bounceVelocity * 0.3f;

            // Ek geri itme kuvveti
            _rigidbody.AddForce(normal * bounceForce, ForceMode.Impulse);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Boundary"))
            {
                Vector3 normal = collision.contacts[0].normal;
                ApplyBounce(normal);
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (UI.DebugUIManager.Instance != null && !UI.DebugUIManager.Instance.ShowCanoeDebug) return;

            // Debug bilgileri
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Speed: {CurrentSpeed:F2} m/s");
            GUILayout.Label($"Forward: {CurrentForwardSpeed:F2} m/s");
            GUILayout.Label($"Sync Bonus: {(_syncBonusApplied ? "ACTIVE" : "inactive")}");
            GUILayout.EndArea();
        }
#endif
    }
}
