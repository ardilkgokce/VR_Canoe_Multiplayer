using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRCanoe.Paddle
{
    /// <summary>
    /// VR kurek kontrolu. 2 uclu kurek (double-bladed paddle).
    /// Controller kuregin ortasina baglanir, 2 uc zit yonlerde.
    /// </summary>
    public class PaddleController : MonoBehaviour
    {
        [Header("Kurek Uclari (2 Uclu Kurek)")]
        [Tooltip("Kurek ucu 1 (ust/on taraf)")]
        [SerializeField] private Transform paddleTip1;

        [Tooltip("Kurek ucu 2 (alt/arka taraf)")]
        [SerializeField] private Transform paddleTip2;

        [Header("Otomatik Olusturma (Uclar bos ise)")]
        [Tooltip("Controller'dan uca mesafe (her iki yon)")]
        [SerializeField] private float tipDistance = 0.6f;

        [Tooltip("Kurek ucu yonu (local space, Tip1 icin)")]
        [SerializeField] private Vector3 tipDirection = Vector3.forward;

        [Header("Controller Referansi")]
        [Tooltip("XR Controller (bos birak = parent'tan al)")]
        [SerializeField] private XRBaseController xrController;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // Properties
        public Transform Tip1 => paddleTip1;
        public Transform Tip2 => paddleTip2;
        public Vector3 Tip1PositionWorld => GetTipPositionWorld(paddleTip1, tipDirection);
        public Vector3 Tip2PositionWorld => GetTipPositionWorld(paddleTip2, -tipDirection);
        public Quaternion Rotation => transform.rotation;
        public bool IsTracking => _isTracking;
        public XRBaseController Controller => xrController;

        // Velocity tracking (her iki uc icin)
        private bool _isTracking;
        private Vector3 _lastTip1Position;
        private Vector3 _lastTip2Position;
        private Vector3 _tip1Velocity;
        private Vector3 _tip2Velocity;

        private void Awake()
        {
            // Controller referansini bul
            if (xrController == null)
            {
                xrController = GetComponentInParent<XRBaseController>();
            }

            // Kurek uclari yoksa olustur
            CreateTipsIfNeeded();
        }

        private void Start()
        {
            _lastTip1Position = Tip1PositionWorld;
            _lastTip2Position = Tip2PositionWorld;
        }

        private void Update()
        {
            UpdateTrackingState();
            CalculateTipVelocities();
        }

        /// <summary>
        /// Kurek uclari yoksa otomatik olustur.
        /// </summary>
        private void CreateTipsIfNeeded()
        {
            if (paddleTip1 == null)
            {
                GameObject tip1Obj = new GameObject("PaddleTip1");
                tip1Obj.transform.SetParent(transform);
                tip1Obj.transform.localPosition = tipDirection.normalized * tipDistance;
                tip1Obj.transform.localRotation = Quaternion.identity;
                paddleTip1 = tip1Obj.transform;
                Debug.Log($"[PaddleController] PaddleTip1 olusturuldu: {paddleTip1.localPosition}");
            }

            if (paddleTip2 == null)
            {
                GameObject tip2Obj = new GameObject("PaddleTip2");
                tip2Obj.transform.SetParent(transform);
                tip2Obj.transform.localPosition = -tipDirection.normalized * tipDistance;
                tip2Obj.transform.localRotation = Quaternion.identity;
                paddleTip2 = tip2Obj.transform;
                Debug.Log($"[PaddleController] PaddleTip2 olusturuldu: {paddleTip2.localPosition}");
            }
        }

        /// <summary>
        /// Controller tracking durumunu kontrol et.
        /// </summary>
        private void UpdateTrackingState()
        {
            if (xrController != null)
            {
                _isTracking = true;
            }
            else
            {
                var devices = new System.Collections.Generic.List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                _isTracking = devices.Count > 0;
            }
        }

        /// <summary>
        /// Her iki ucun hizini hesapla.
        /// </summary>
        private void CalculateTipVelocities()
        {
            // Tip 1
            Vector3 currentTip1 = Tip1PositionWorld;
            _tip1Velocity = (currentTip1 - _lastTip1Position) / Time.deltaTime;
            _lastTip1Position = currentTip1;

            // Tip 2
            Vector3 currentTip2 = Tip2PositionWorld;
            _tip2Velocity = (currentTip2 - _lastTip2Position) / Time.deltaTime;
            _lastTip2Position = currentTip2;
        }

        /// <summary>
        /// Kurek ucu pozisyonunu al (world space).
        /// </summary>
        private Vector3 GetTipPositionWorld(Transform tip, Vector3 fallbackDirection)
        {
            if (tip != null)
            {
                return tip.position;
            }
            return transform.TransformPoint(fallbackDirection.normalized * tipDistance);
        }

        /// <summary>
        /// Tip 1 world space hizi.
        /// </summary>
        public Vector3 GetTip1Velocity()
        {
            return _tip1Velocity;
        }

        /// <summary>
        /// Tip 2 world space hizi.
        /// </summary>
        public Vector3 GetTip2Velocity()
        {
            return _tip2Velocity;
        }

        /// <summary>
        /// Haptic feedback gonder.
        /// </summary>
        public void SendHapticFeedback(float amplitude = 0.3f, float duration = 0.1f)
        {
            if (xrController != null)
            {
                xrController.SendHapticImpulse(amplitude, duration);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Vector3 tip1Pos = paddleTip1 != null ? paddleTip1.position : transform.TransformPoint(tipDirection.normalized * tipDistance);
            Vector3 tip2Pos = paddleTip2 != null ? paddleTip2.position : transform.TransformPoint(-tipDirection.normalized * tipDistance);

            // Kurek cizgisi (tip1 -> tip2)
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(tip1Pos, tip2Pos);

            // Controller merkezi
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.03f);

            // Tip 1 (kirmizi)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tip1Pos, 0.05f);

            // Tip 2 (turuncu)
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawWireSphere(tip2Pos, 0.05f);

            // Velocity vektorleri
            if (Application.isPlaying)
            {
                if (_tip1Velocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(tip1Pos, tip1Pos + _tip1Velocity * 0.2f);
                }
                if (_tip2Velocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(tip2Pos, tip2Pos + _tip2Velocity * 0.2f);
                }
            }
        }
#endif
    }
}
