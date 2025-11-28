using UnityEngine;
using Photon.Pun;
using VRCanoe.Game;

namespace VRCanoe.Collectibles
{
    /// <summary>
    /// Toplanabilir obje (coin vb.).
    /// Kano degdiginde puan ekler ve kaybolur.
    /// MasterClient yonetir, diger clientlar sync alir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviourPunCallbacks
    {
        [Header("Ayarlar")]
        [Tooltip("Toplandiginda kac coin eklensin (1 = GameSettings.coinValue)")]
        [SerializeField] private int coinCount = 1;

        [Tooltip("Gecerli tag (Canoe)")]
        [SerializeField] private string requiredTag = "Canoe";

        [Header("Gorseller")]
        [Tooltip("Toplama efekti (particle vb.)")]
        [SerializeField] private GameObject collectEffect;

        [Tooltip("Toplama sesi")]
        [SerializeField] private AudioClip collectSound;

        [Header("Animasyon")]
        [Tooltip("Yukari asagi hareket et")]
        [SerializeField] private bool floatAnimation = true;

        [Tooltip("Yukari asagi hareket miktari")]
        [SerializeField] private float floatAmplitude = 0.2f;

        [Tooltip("Yukari asagi hareket hizi")]
        [SerializeField] private float floatSpeed = 2f;

        [Tooltip("Kendi etrafinda don")]
        [SerializeField] private bool rotateAnimation = true;

        [Tooltip("Donme hizi (derece/saniye)")]
        [SerializeField] private float rotateSpeed = 90f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Components
        private MeshRenderer _meshRenderer;
        private Collider _collider;
        private AudioSource _audioSource;

        // State
        private bool _isCollected;
        private Vector3 _startPosition;

        // Properties
        public bool IsCollected => _isCollected;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
            }

            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            _startPosition = transform.position;

            // AudioSource ekle (yoksa)
            if (collectSound != null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
            }

            // Efekti baslangicta gizle
            if (collectEffect != null)
            {
                collectEffect.SetActive(false);
            }
        }

        private void Start()
        {
            // CollectibleManager'a kayit ol
            if (CollectibleManager.Instance != null)
            {
                CollectibleManager.Instance.RegisterCollectible(this);
            }
        }

        private void OnDestroy()
        {
            // CollectibleManager'dan kaydi kaldir
            if (CollectibleManager.Instance != null)
            {
                CollectibleManager.Instance.UnregisterCollectible(this);
            }
        }

        private void Update()
        {
            if (_isCollected) return;

            // Float animasyonu
            if (floatAnimation)
            {
                float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
                transform.position = _startPosition + Vector3.up * yOffset;
            }

            // Rotate animasyonu
            if (rotateAnimation)
            {
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Zaten toplandi mi?
            if (_isCollected) return;

            // Sadece MasterClient isler
            if (!PhotonNetwork.IsMasterClient) return;

            // Oyun devam ediyor mu?
            if (CanoeGameManager.Instance == null) return;
            if (CanoeGameManager.Instance.CurrentState != GameState.Playing) return;

            // Tag kontrolu
            if (!string.IsNullOrEmpty(requiredTag))
            {
                bool hasTag = other.CompareTag(requiredTag);

                // Parent'ta da bak
                if (!hasTag && other.transform.parent != null)
                {
                    hasTag = other.transform.parent.CompareTag(requiredTag);
                }

                if (!hasTag)
                {
                    return;
                }
            }

            // Topla!
            Collect();
        }

        /// <summary>
        /// Collectible'i topla.
        /// </summary>
        private void Collect()
        {
            if (_isCollected) return;

            _isCollected = true;

            if (showDebugInfo)
            {
                Debug.Log($"[Collectible] Toplandi: {gameObject.name}");
            }

            // Skor ekle
            if (ScoreManager.Instance != null)
            {
                for (int i = 0; i < coinCount; i++)
                {
                    ScoreManager.Instance.AddCoin();
                }
            }

            // Tum clientlara bildir
            photonView.RPC(nameof(RPC_OnCollected), RpcTarget.All);
        }

        [PunRPC]
        private void RPC_OnCollected()
        {
            _isCollected = true;

            // Efektleri oynat (obje kapanmadan once)
            PlayEffects();

            // Objeyi kapat
            gameObject.SetActive(false);

            if (showDebugInfo)
            {
                Debug.Log($"[Collectible] RPC: {gameObject.name} toplandi");
            }
        }

        private void PlayEffects()
        {
            // Particle efekti
            if (collectEffect != null)
            {
                collectEffect.SetActive(true);

                // Particle system varsa oynat
                var particles = collectEffect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    particles.Play();
                }
            }

            // Ses efekti
            if (_audioSource != null && collectSound != null)
            {
                _audioSource.PlayOneShot(collectSound);
            }
        }

        /// <summary>
        /// Collectible'i sifirla (CollectibleManager tarafindan cagirilir).
        /// </summary>
        public void Reset()
        {
            _isCollected = false;

            // Objeyi ac
            gameObject.SetActive(true);

            // Efekti gizle
            if (collectEffect != null)
            {
                collectEffect.SetActive(false);
            }

            // Pozisyonu sifirla
            transform.position = _startPosition;

            if (showDebugInfo)
            {
                Debug.Log($"[Collectible] Reset: {gameObject.name}");
            }
        }

        /// <summary>
        /// Manuel olarak topla (test icin).
        /// </summary>
        public void ForceCollect()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                Collect();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Collectible'i goster
            Gizmos.color = _isCollected ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            if (!_isCollected)
            {
                // Float range goster
                if (floatAnimation)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 pos = Application.isPlaying ? _startPosition : transform.position;
                    Gizmos.DrawLine(pos + Vector3.up * floatAmplitude, pos - Vector3.up * floatAmplitude);
                }
            }
        }
#endif
    }
}
