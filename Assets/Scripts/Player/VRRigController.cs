using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRCanoe.VRPlayer
{
    /// <summary>
    /// VR Rig yardimci scripti.
    /// VR Rig sahnede kano koltuguna child olarak yerlestirilmistir.
    /// Bu script sadece recenter ve yardimci islemler icin kullanilir.
    /// Local/Remote player ayrimi yapar - remote player icin sadece gorseller aktif kalir.
    /// </summary>
    public class VRRigController : MonoBehaviour
    {
        [Header("Referanslar")]
        [Tooltip("XR Origin (bos birak = bu obje veya child'da ara)")]
        [SerializeField] private XROrigin xrOrigin;

        [Tooltip("Main Camera (bos birak = otomatik bul)")]
        [SerializeField] private Camera mainCamera;

        [Header("Controller Referanslari")]
        [Tooltip("Sol controller (bos birak = otomatik bul)")]
        [SerializeField] private GameObject leftController;

        [Tooltip("Sag controller (bos birak = otomatik bul)")]
        [SerializeField] private GameObject rightController;

        [Header("XR System Referanslari (Otomatik Bulunur)")]
        [Tooltip("Input Action Manager (bos birak = otomatik bul)")]
        [SerializeField] private PlayerInput inputActionManager;

        [Tooltip("XR Interaction Manager (bos birak = otomatik bul)")]
        [SerializeField] private XRInteractionManager xrInteractionManager;

        [Tooltip("Event System (bos birak = otomatik bul)")]
        [SerializeField] private EventSystem eventSystem;

        [Header("Recenter Ayarlari")]
        [Tooltip("Recenter yapildiginda hedef yukseklik (oturma pozisyonu)")]
        [SerializeField] private float targetEyeHeight = 1.1f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Local player mi?
        private bool _isLocalPlayer = false;
        public bool IsLocalPlayer => _isLocalPlayer;

        // InputActionAsset referansi (disable/enable icin)
        private InputActionAsset _inputActionAsset;

        private void Awake()
        {
            Debug.Log($"[VRRigController] Awake: {gameObject.name}");

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

            // Controller'lari bul
            FindControllers();

            // XR System componentlerini bul
            FindXRSystemComponents();
        }

        private void OnDisable()
        {
            Debug.LogWarning($"[VRRigController] OnDisable CALLED: {gameObject.name} - KIM KAPATTI?", this);
        }

        private void OnEnable()
        {
            Debug.Log($"[VRRigController] OnEnable: {gameObject.name}");
        }

        /// <summary>
        /// Controller referanslarini otomatik bul.
        /// </summary>
        private void FindControllers()
        {
            if (leftController == null || rightController == null)
            {
                var controllers = GetComponentsInChildren<XRBaseController>(true);
                foreach (var controller in controllers)
                {
                    string name = controller.gameObject.name.ToLower();
                    if (name.Contains("left") && leftController == null)
                    {
                        leftController = controller.gameObject;
                    }
                    else if (name.Contains("right") && rightController == null)
                    {
                        rightController = controller.gameObject;
                    }
                }
            }
        }

        /// <summary>
        /// XR System componentlerini otomatik bul (InputActionManager, XRInteractionManager, EventSystem).
        /// </summary>
        private void FindXRSystemComponents()
        {
            // PlayerInput (Input Action Manager) - genelde ayni obje veya child'da
            if (inputActionManager == null)
            {
                inputActionManager = GetComponent<PlayerInput>();
                if (inputActionManager == null)
                {
                    inputActionManager = GetComponentInChildren<PlayerInput>(true);
                }
            }

            // InputActionAsset'i PlayerInput'tan al
            if (inputActionManager != null)
            {
                _inputActionAsset = inputActionManager.actions;
            }

            // XR Interaction Manager
            if (xrInteractionManager == null)
            {
                xrInteractionManager = GetComponent<XRInteractionManager>();
                if (xrInteractionManager == null)
                {
                    xrInteractionManager = GetComponentInChildren<XRInteractionManager>(true);
                }
            }

            // Event System
            if (eventSystem == null)
            {
                eventSystem = GetComponent<EventSystem>();
                if (eventSystem == null)
                {
                    eventSystem = GetComponentInChildren<EventSystem>(true);
                }
            }
        }

        /// <summary>
        /// Bu rig'i local player olarak aktive et.
        /// XR tracking, kamera ve input aktif olur.
        /// </summary>
        public void SetAsLocalPlayer()
        {
            Debug.Log($"[VRRigController] SetAsLocalPlayer CALLED: {gameObject.name}, activeInHierarchy: {gameObject.activeInHierarchy}");
            _isLocalPlayer = true;

            // XR Origin aktif
            if (xrOrigin != null)
            {
                xrOrigin.enabled = true;
            }

            // Kamera aktif
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
                mainCamera.tag = "MainCamera";

                // AudioListener aktif
                var audioListener = mainCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = true;
                }
            }

            // XR System componentlerini aktif et
            SetXRSystemComponentsActive(true);

            // Controller'lar tam aktif (tracking + input + gorseller)
            SetControllersActive(true, true);

            if (showDebugInfo)
            {
                Debug.Log($"[VRRigController] {gameObject.name} LOCAL player olarak ayarlandi");
            }
        }

        /// <summary>
        /// Bu rig'i remote player olarak ayarla.
        /// Sadece gorseller aktif, XR tracking ve input kapali.
        /// </summary>
        public void SetAsRemotePlayer()
        {
            Debug.Log($"[VRRigController] SetAsRemotePlayer CALLED: {gameObject.name}, activeInHierarchy: {gameObject.activeInHierarchy}");
            _isLocalPlayer = false;

            // XR Origin kapali - tracking yapmasin
            if (xrOrigin != null)
            {
                xrOrigin.enabled = false;
            }

            // Kamera kapali - renderlamasin
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
                mainCamera.tag = "Untagged";

                // AudioListener kapali
                var audioListener = mainCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }
            }

            // XR System componentlerini kapat
            SetXRSystemComponentsActive(false);

            // Controller'lar: tracking/input kapali, gorseller acik
            SetControllersActive(false, true);

            if (showDebugInfo)
            {
                Debug.Log($"[VRRigController] {gameObject.name} REMOTE player olarak ayarlandi (gorseller aktif)");
            }
        }

        /// <summary>
        /// XR System componentlerini (InputActionManager, XRInteractionManager, EventSystem) aktif/pasif yap.
        /// </summary>
        private void SetXRSystemComponentsActive(bool active)
        {
            // Input Action Manager / PlayerInput
            if (inputActionManager != null)
            {
                inputActionManager.enabled = active;
            }

            // Input Action Asset - tum actionlari enable/disable
            if (_inputActionAsset != null)
            {
                if (active)
                {
                    _inputActionAsset.Enable();
                }
                else
                {
                    _inputActionAsset.Disable();
                }
            }

            // XR Interaction Manager
            if (xrInteractionManager != null)
            {
                xrInteractionManager.enabled = active;
            }

            // Event System
            if (eventSystem != null)
            {
                eventSystem.enabled = active;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[VRRigController] XR System components {(active ? "ENABLED" : "DISABLED")}");
            }
        }

        /// <summary>
        /// Controller'larin durumunu ayarla.
        /// </summary>
        /// <param name="trackingEnabled">XR tracking ve input aktif mi?</param>
        /// <param name="visualsEnabled">Gorsel meshler aktif mi?</param>
        private void SetControllersActive(bool trackingEnabled, bool visualsEnabled)
        {
            SetControllerState(leftController, trackingEnabled, visualsEnabled);
            SetControllerState(rightController, trackingEnabled, visualsEnabled);
        }

        /// <summary>
        /// Tek bir controller'in durumunu ayarla.
        /// </summary>
        private void SetControllerState(GameObject controller, bool trackingEnabled, bool visualsEnabled)
        {
            if (controller == null) return;

            // XR Controller component - tracking/input
            var xrController = controller.GetComponent<XRBaseController>();
            if (xrController != null)
            {
                xrController.enabled = trackingEnabled;
            }

            // ActionBasedController
            var actionController = controller.GetComponent<ActionBasedController>();
            if (actionController != null)
            {
                actionController.enabled = trackingEnabled;
            }

            // XR Interactor'lar
            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>(true);
            foreach (var interactor in interactors)
            {
                interactor.enabled = trackingEnabled;
            }

            // Gorseller (MeshRenderer, SkinnedMeshRenderer) - her zaman kontrol edilebilir
            // Paddle uzerindeki renderlar ayri oldugu icin burada sadece controller renderlarini etkiliyoruz
            // Ama genelde controller gorselleri de acik kalmali ki paddle gorunsun
        }

        /// <summary>
        /// Rig'i tamamen deaktif et.
        /// </summary>
        public void DisableCompletely()
        {
            _isLocalPlayer = false;
            gameObject.SetActive(false);

            if (showDebugInfo)
            {
                Debug.Log($"[VRRigController] {gameObject.name} tamamen deaktif edildi");
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
