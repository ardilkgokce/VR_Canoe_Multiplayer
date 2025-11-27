using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// VR Rig yardimci scripti.
    /// VR Rig sahnede kano koltuguna child olarak yerlestirilmistir.
    /// Bu script sadece recenter ve yardimci islemler icin kullanilir.
    /// </summary>
    public class VRRigController : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("XR Origin (bos birak = bu obje veya child'da ara)")]
        [SerializeField] private XROrigin xrOrigin;

        [Tooltip("Main Camera (bos birak = otomatik bul)")]
        [SerializeField] private Camera mainCamera;

        [Header("Recenter Ayarlari")]
        [Tooltip("Recenter yapildiginda hedef yukseklik (oturma pozisyonu)")]
        [SerializeField] private float targetEyeHeight = 1.1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private void Awake()
        {
            // XR Origin bul
            if (xrOrigin == null)
            {
                xrOrigin = GetComponent<XROrigin>();
                if (xrOrigin == null)
                {
                    xrOrigin = GetComponentInChildren<XROrigin>();
                }
            }

            // Main Camera bul
            if (mainCamera == null)
            {
                mainCamera = GetComponentInChildren<Camera>();
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }
            }
        }

        /// <summary>
        /// VR view'i recenter et (kafayi referans noktasina sifirla).
        /// </summary>
        public void RecenterView()
        {
            // XR API ile recenter dene
            var xrInputSubsystem = GetXRInputSubsystem();
            if (xrInputSubsystem != null && xrInputSubsystem.TryRecenter())
            {
                if (showDebugInfo)
                {
                    Debug.Log("[VRRigController] XR Recenter basarili");
                }
                return;
            }

            // Manuel recenter
            ManualRecenter();
        }

        /// <summary>
        /// Manuel recenter - kamerayi merkeze hizala.
        /// </summary>
        private void ManualRecenter()
        {
            if (mainCamera == null || xrOrigin == null) return;

            // Kameranin XR Origin'e gore local pozisyonunu al
            Vector3 cameraLocalPos = xrOrigin.transform.InverseTransformPoint(mainCamera.transform.position);

            // XR Origin'i kameranin tam tersine kaydir (sadece X ve Z)
            Vector3 offset = new Vector3(-cameraLocalPos.x, 0f, -cameraLocalPos.z);
            xrOrigin.transform.localPosition += offset;

            // Y rotasyonunu sifirla
            float cameraYaw = mainCamera.transform.eulerAngles.y;
            float originYaw = xrOrigin.transform.eulerAngles.y;
            float yawDiff = originYaw - cameraYaw;

            xrOrigin.transform.Rotate(Vector3.up, yawDiff, Space.World);

            if (showDebugInfo)
            {
                Debug.Log("[VRRigController] Manuel recenter yapildi");
            }
        }

        /// <summary>
        /// XR Input Subsystem'i al.
        /// </summary>
        private XRInputSubsystem GetXRInputSubsystem()
        {
            var xrInputSubsystems = new System.Collections.Generic.List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(xrInputSubsystems);

            if (xrInputSubsystems.Count > 0)
            {
                return xrInputSubsystems[0];
            }
            return null;
        }

        /// <summary>
        /// Oturma yuksekligini ayarla.
        /// </summary>
        public void SetTargetEyeHeight(float height)
        {
            targetEyeHeight = height;
        }

        /// <summary>
        /// XR Origin referansini al.
        /// </summary>
        public XROrigin GetXROrigin()
        {
            return xrOrigin;
        }

        /// <summary>
        /// Main Camera referansini al.
        /// </summary>
        public Camera GetMainCamera()
        {
            return mainCamera;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Hedef goz yuksekligini goster
            Gizmos.color = Color.yellow;
            Vector3 eyePos = transform.position + Vector3.up * targetEyeHeight;
            Gizmos.DrawWireSphere(eyePos, 0.1f);

            // Forward yonu
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(eyePos, eyePos + transform.forward * 0.5f);
        }
#endif
    }
}
