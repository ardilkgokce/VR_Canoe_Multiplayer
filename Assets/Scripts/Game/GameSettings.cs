using UnityEngine;

namespace VRCanoe.Game
{
    /// <summary>
    /// Oyun ayarlarini tutan ScriptableObject.
    /// Assets > Create > VR Canoe > Game Settings ile olusturulabilir.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "VR Canoe/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Zaman Ayarlari")]
        [Tooltip("Oyun suresi (saniye)")]
        [Range(30f, 300f)]
        public float gameDuration = 60f;

        [Header("Skor Ayarlari")]
        [Tooltip("Her coin'in puan degeri")]
        [Range(1, 100)]
        public int coinValue = 10;

        [Header("Kano Ayarlari")]
        [Tooltip("Kano hiz carpani")]
        [Range(0.5f, 3f)]
        public float canoeSpeedMultiplier = 1f;

        [Tooltip("Senkronize kurek bonus carpani")]
        [Range(1f, 3f)]
        public float syncPaddleMultiplier = 1.5f;

        [Header("Yaris Ayarlari")]
        [Tooltip("Bitis cizgisi mesafesi (metre)")]
        [Range(100f, 1000f)]
        public float finishLineDistance = 500f;

        [Header("Kurek Ayarlari")]
        [Tooltip("Senkronize kurek icin zaman penceresi (saniye)")]
        [Range(0.1f, 0.5f)]
        public float syncPaddleWindow = 0.3f;

        [Tooltip("Kurek kuvvet carpani")]
        [Range(0.5f, 3f)]
        public float paddleForceMultiplier = 1f;

        [Header("Fizik Ayarlari")]
        [Tooltip("Su direnci")]
        [Range(0.1f, 2f)]
        public float waterDrag = 0.5f;

        [Tooltip("Kano kutlesi (kg)")]
        [Range(50f, 200f)]
        public float canoeMass = 100f;
    }
}
