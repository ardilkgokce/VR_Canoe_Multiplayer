using System;
using UnityEngine;
using Photon.Pun;
using VRCanoe.Canoe;

namespace VRCanoe.Game
{
    /// <summary>
    /// Bitis cizgisi. Kano gectiginde oyunu bitirir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FinishLine : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Sadece bir kez tetiklensin")]
        [SerializeField] private bool triggerOnce = true;

        [Tooltip("Gecerli tag (bos = her sey)")]
        [SerializeField] private string requiredTag = "Canoe";

        [Header("Gorseller")]
        [Tooltip("Gecis efekti (particle vb.)")]
        [SerializeField] private GameObject crossingEffect;

        [Tooltip("Gecis sesi")]
        [SerializeField] private AudioClip crossingSound;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Events
        public static event Action<float> OnFinishLineCrossed; // Bitis suresi

        // State
        private bool _hasTriggered;
        private Collider _collider;
        private AudioSource _audioSource;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            // AudioSource ekle (yoksa)
            if (crossingSound != null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
                _audioSource.playOnAwake = false;
            }

            // Efekti baslangicta gizle
            if (crossingEffect != null)
            {
                crossingEffect.SetActive(false);
            }
        }

        private void Start()
        {
            // GameManager eventlerini dinle
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

        private void OnTriggerEnter(Collider other)
        {
            // Zaten tetiklendi mi?
            if (triggerOnce && _hasTriggered) return;

            // Oyun devam ediyor mu?
            if (CanoeGameManager.Instance == null) return;
            if (CanoeGameManager.Instance.CurrentState != GameState.Playing) return;

            // Tag kontrolu
            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
            {
                // Parent'ta da bak
                if (other.transform.parent == null || !other.transform.parent.CompareTag(requiredTag))
                {
                    return;
                }
            }

            // Kano mu?
            CanoeController canoe = other.GetComponent<CanoeController>();
            if (canoe == null)
            {
                canoe = other.GetComponentInParent<CanoeController>();
            }

            if (canoe == null)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[FinishLine] Kano degil: {other.name}");
                }
                return;
            }

            // Bitis!
            HandleFinishLineCrossed();
        }

        private void HandleFinishLineCrossed()
        {
            _hasTriggered = true;

            // Gecen sureyi al
            float finishTime = 0f;
            if (TimerManager.Instance != null)
            {
                finishTime = TimerManager.Instance.ElapsedTime;
            }

            if (showDebugInfo)
            {
                Debug.Log($"[FinishLine] Bitis cizgisi gecildi! Sure: {finishTime:F2}s");
            }

            // Efektleri oynat
            PlayEffects();

            // Event firlat
            OnFinishLineCrossed?.Invoke(finishTime);

            // Sadece MasterClient oyunu bitirir
            if (PhotonNetwork.IsMasterClient)
            {
                // Skor hesapla
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.CalculateTimeBonus(finishTime);
                }

                // Oyunu bitir
                if (CanoeGameManager.Instance != null)
                {
                    CanoeGameManager.Instance.FinishGame();
                }
            }
        }

        private void PlayEffects()
        {
            // Particle efekti
            if (crossingEffect != null)
            {
                crossingEffect.SetActive(true);

                // Particle system varsa oynat
                var particles = crossingEffect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    particles.Play();
                }
            }

            // Ses efekti
            if (_audioSource != null && crossingSound != null)
            {
                _audioSource.PlayOneShot(crossingSound);
            }
        }

        private void OnGameReset()
        {
            _hasTriggered = false;

            // Efekti gizle
            if (crossingEffect != null)
            {
                crossingEffect.SetActive(false);
            }
        }

        /// <summary>
        /// Manuel olarak bitis cizgisini tetikle (test icin).
        /// </summary>
        public void TriggerFinish()
        {
            if (_hasTriggered && triggerOnce) return;
            HandleFinishLineCrossed();
        }

        /// <summary>
        /// Bitis cizgisini sifirla.
        /// </summary>
        public void Reset()
        {
            _hasTriggered = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Bitis cizgisini goster
            Collider col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = _hasTriggered ? Color.green : Color.red;

            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }

            // Label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, "FINISH LINE");
        }
#endif
    }
}
