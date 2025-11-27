using UnityEngine;
using Photon.Pun;
using VRCanoe.Canoe;
using VRCanoe.Game;

namespace VRCanoe.Paddle
{
    /// <summary>
    /// 2 uclu kurek fizik hesaplamalari.
    /// Her iki ucu da kontrol eder, hangisi suda ve hareket ediyorsa kuvvet uygular.
    /// Kano'ya gore relative velocity hesaplar.
    /// </summary>
    [RequireComponent(typeof(PaddleController))]
    public class PaddlePhysics : MonoBehaviour
    {
        [Header("Su Ayarlari")]
        [Tooltip("Su seviyesi (Y pozisyonu)")]
        [SerializeField] private float waterLevel = 0f;

        [Tooltip("Kuregin suda olup olmadigini kontrol etmek icin tolerans")]
        [SerializeField] private float waterTolerance = 0.1f;

        [Header("Fizik Ayarlari")]
        [Tooltip("Minimum hareket hizi (bu altindaki hareketler ignore edilir)")]
        [SerializeField] private float minVelocityThreshold = 0.3f;

        [Tooltip("Maksimum kuvvet carpani")]
        [SerializeField] private float maxForceMultiplier = 5f;

        [Tooltip("Kuvvet carpani (velocity -> force)")]
        [SerializeField] private float forceMultiplier = 2f;

        [Tooltip("Torque carpani")]
        [SerializeField] private float torqueMultiplier = 1f;

        [Header("Referanslar")]
        [Tooltip("Hedef kano (bos birak = otomatik bul)")]
        [SerializeField] private CanoeMovement targetCanoe;

        [Header("Taraf Belirleme")]
        [Tooltip("Kurek kano'nun hangi tarafinda? (Tip X pozisyonuna gore otomatik belirle)")]
        [SerializeField] private bool autoDetectSide = true;

        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float hapticAmplitude = 0.2f;
        [SerializeField] private float hapticDuration = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Components
        private PaddleController _paddleController;
        private Rigidbody _canoeRigidbody;
        private Transform _canoeTransform;

        // Tip 1 relative velocity
        private Vector3 _lastTip1LocalPosition;
        private Vector3 _tip1RelativeVelocity;
        private bool _tip1InWater;
        private bool _tip1WasInWater;

        // Tip 2 relative velocity
        private Vector3 _lastTip2LocalPosition;
        private Vector3 _tip2RelativeVelocity;
        private bool _tip2InWater;
        private bool _tip2WasInWater;

        // Properties
        public bool Tip1InWater => _tip1InWater;
        public bool Tip2InWater => _tip2InWater;
        public bool AnyTipInWater => _tip1InWater || _tip2InWater;
        public Vector3 Tip1RelativeVelocity => _tip1RelativeVelocity;
        public Vector3 Tip2RelativeVelocity => _tip2RelativeVelocity;

        private void Awake()
        {
            _paddleController = GetComponent<PaddleController>();
        }

        private void Start()
        {
            FindCanoe();
            InitializeLocalPositions();
        }

        private void FindCanoe()
        {
            if (targetCanoe == null)
            {
                targetCanoe = FindObjectOfType<CanoeMovement>();
            }

            if (targetCanoe != null)
            {
                _canoeRigidbody = targetCanoe.GetComponent<Rigidbody>();
                _canoeTransform = targetCanoe.transform;

                // Su seviyesini kanodan al
                var canoeController = targetCanoe.GetComponent<CanoeController>();
                if (canoeController != null)
                {
                    waterLevel = canoeController.WaterLevel;
                }
            }
            else
            {
                Debug.LogWarning("[PaddlePhysics] Kano bulunamadi!");
            }
        }

        private void InitializeLocalPositions()
        {
            if (_canoeTransform != null)
            {
                _lastTip1LocalPosition = _canoeTransform.InverseTransformPoint(_paddleController.Tip1PositionWorld);
                _lastTip2LocalPosition = _canoeTransform.InverseTransformPoint(_paddleController.Tip2PositionWorld);
            }
        }

        private void FixedUpdate()
        {
            if (targetCanoe == null || _canoeTransform == null) return;

            // Her iki ucu da kontrol et
            UpdateTip1();
            UpdateTip2();
        }

        #region Tip 1

        private void UpdateTip1()
        {
            UpdateTip1WaterState();
            CalculateTip1RelativeVelocity();

            if (_tip1InWater && _tip1RelativeVelocity.magnitude > minVelocityThreshold)
            {
                ApplyPaddleForce(_tip1RelativeVelocity, _paddleController.Tip1PositionWorld, 1);
            }
        }

        private void UpdateTip1WaterState()
        {
            _tip1WasInWater = _tip1InWater;
            float tipY = _paddleController.Tip1PositionWorld.y;
            _tip1InWater = tipY <= (waterLevel + waterTolerance);

            if (_tip1InWater && !_tip1WasInWater)
            {
                OnTipEnterWater(1);
            }
        }

        private void CalculateTip1RelativeVelocity()
        {
            Vector3 currentLocalPosition = _canoeTransform.InverseTransformPoint(_paddleController.Tip1PositionWorld);
            _tip1RelativeVelocity = (currentLocalPosition - _lastTip1LocalPosition) / Time.fixedDeltaTime;
            _lastTip1LocalPosition = currentLocalPosition;
        }

        #endregion

        #region Tip 2

        private void UpdateTip2()
        {
            UpdateTip2WaterState();
            CalculateTip2RelativeVelocity();

            if (_tip2InWater && _tip2RelativeVelocity.magnitude > minVelocityThreshold)
            {
                ApplyPaddleForce(_tip2RelativeVelocity, _paddleController.Tip2PositionWorld, 2);
            }
        }

        private void UpdateTip2WaterState()
        {
            _tip2WasInWater = _tip2InWater;
            float tipY = _paddleController.Tip2PositionWorld.y;
            _tip2InWater = tipY <= (waterLevel + waterTolerance);

            if (_tip2InWater && !_tip2WasInWater)
            {
                OnTipEnterWater(2);
            }
        }

        private void CalculateTip2RelativeVelocity()
        {
            Vector3 currentLocalPosition = _canoeTransform.InverseTransformPoint(_paddleController.Tip2PositionWorld);
            _tip2RelativeVelocity = (currentLocalPosition - _lastTip2LocalPosition) / Time.fixedDeltaTime;
            _lastTip2LocalPosition = currentLocalPosition;
        }

        #endregion

        #region Force Application

        /// <summary>
        /// Kurek kuvvetini kanoya uygula.
        /// </summary>
        private void ApplyPaddleForce(Vector3 relativeVelocity, Vector3 tipWorldPosition, int tipIndex)
        {
            // Oyun oynamiyorsa kuvvet uygulama
            if (CanoeGameManager.Instance != null && !CanoeGameManager.Instance.IsPlaying)
            {
                return;
            }

            // Geri cekme hareketi (Z negatif) kuvvet uygular
            float forwardComponent = relativeVelocity.z;

            if (forwardComponent > -minVelocityThreshold)
            {
                // Ileri itme veya yatay hareket - etki yok
                return;
            }

            // Kuvvet hesapla
            float forceMagnitude = Mathf.Abs(forwardComponent) * forceMultiplier;
            forceMagnitude = Mathf.Clamp(forceMagnitude, 0f, maxForceMultiplier);

            // Ileri kuvvet
            Vector3 force = Vector3.forward * forceMagnitude;

            // Yatay hareket donus etkisi
            float lateralComponent = relativeVelocity.x;
            Vector3 torque = Vector3.up * lateralComponent * torqueMultiplier;

            // Kurek hangi tarafta? (Sag mi sol mu)
            bool isRightSide = DetermineSide(tipWorldPosition);

            // Kanoya kuvvet uygula
            targetCanoe.AddPaddleForce(force, torque, isRightSide);

            // Haptic feedback
            if (enableHaptics && _paddleController != null)
            {
                float hapticStrength = Mathf.Clamp01(forceMagnitude / maxForceMultiplier) * hapticAmplitude;
                _paddleController.SendHapticFeedback(hapticStrength, hapticDuration);
            }

            if (showDebugInfo)
            {
                Debug.Log($"[PaddlePhysics] Tip{tipIndex} Force: {forceMagnitude:F2}, Side: {(isRightSide ? "Right" : "Left")}");
            }
        }

        /// <summary>
        /// Kurek ucunun kano'nun hangi tarafinda oldugunu belirle.
        /// </summary>
        private bool DetermineSide(Vector3 tipWorldPosition)
        {
            if (!autoDetectSide || _canoeTransform == null)
            {
                return true; // Default: sag
            }

            // Tip pozisyonunu kano local space'ine cevir
            Vector3 localTipPos = _canoeTransform.InverseTransformPoint(tipWorldPosition);

            // X pozitif = sag, X negatif = sol
            return localTipPos.x > 0f;
        }

        private void OnTipEnterWater(int tipIndex)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[PaddlePhysics] Tip{tipIndex} suya girdi");
            }

            if (enableHaptics && _paddleController != null)
            {
                _paddleController.SendHapticFeedback(0.1f, 0.02f);
            }
        }

        #endregion

        /// <summary>
        /// Su seviyesini ayarla.
        /// </summary>
        public void SetWaterLevel(float level)
        {
            waterLevel = level;
        }

        /// <summary>
        /// Hedef kanoyu ayarla.
        /// </summary>
        public void SetTargetCanoe(CanoeMovement canoe)
        {
            targetCanoe = canoe;
            if (canoe != null)
            {
                _canoeRigidbody = canoe.GetComponent<Rigidbody>();
                _canoeTransform = canoe.transform;
                InitializeLocalPositions();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_paddleController == null) return;

            // Su seviyesi cizgisi
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Vector3 center = (_paddleController.Tip1PositionWorld + _paddleController.Tip2PositionWorld) / 2f;
            Vector3 waterPos = new Vector3(center.x, waterLevel, center.z);
            Gizmos.DrawWireCube(waterPos, new Vector3(1f, 0.02f, 1f));

            if (Application.isPlaying)
            {
                // Tip 1 su durumu
                Gizmos.color = _tip1InWater ? Color.cyan : Color.gray;
                Gizmos.DrawWireSphere(_paddleController.Tip1PositionWorld, 0.06f);

                // Tip 2 su durumu
                Gizmos.color = _tip2InWater ? Color.cyan : Color.gray;
                Gizmos.DrawWireSphere(_paddleController.Tip2PositionWorld, 0.06f);

                // Relative velocity vektorleri
                if (_canoeTransform != null)
                {
                    if (_tip1InWater && _tip1RelativeVelocity.magnitude > minVelocityThreshold)
                    {
                        Gizmos.color = Color.yellow;
                        Vector3 worldVel = _canoeTransform.TransformDirection(_tip1RelativeVelocity);
                        Gizmos.DrawLine(_paddleController.Tip1PositionWorld, _paddleController.Tip1PositionWorld + worldVel * 0.3f);
                    }

                    if (_tip2InWater && _tip2RelativeVelocity.magnitude > minVelocityThreshold)
                    {
                        Gizmos.color = Color.magenta;
                        Vector3 worldVel = _canoeTransform.TransformDirection(_tip2RelativeVelocity);
                        Gizmos.DrawLine(_paddleController.Tip2PositionWorld, _paddleController.Tip2PositionWorld + worldVel * 0.3f);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 180));
            GUILayout.Box("Paddle (2 Tips)");

            GUILayout.Label($"Tip1 In Water: {_tip1InWater}");
            GUILayout.Label($"Tip1 RelVel: {_tip1RelativeVelocity.magnitude:F2} (Z: {_tip1RelativeVelocity.z:F2})");

            GUILayout.Space(5);

            GUILayout.Label($"Tip2 In Water: {_tip2InWater}");
            GUILayout.Label($"Tip2 RelVel: {_tip2RelativeVelocity.magnitude:F2} (Z: {_tip2RelativeVelocity.z:F2})");

            GUILayout.EndArea();
        }
#endif
    }
}
