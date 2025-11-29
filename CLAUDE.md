# VR Canoe Multiplayer - Claude Code Guide

## Project Overview

VR Kano Yarisi oyunu. Iki kisilik kanolarla yaris yapilan multiplayer VR oyunu.

- **Unity:** 2022.3.62f2 (LTS)
- **Rendering:** Universal Render Pipeline (URP 14.0.12)
- **Networking:** Photon PUN2
- **VR:** XR Interaction Toolkit
- **Target:** Windows, Android (Quest)

## Oyun Mekanikleri

- 2 kisilik kanolar (on ve arka kurekci)
- Senkronize kurek cekme bonus verir
- 60 saniyelik yaris suresi
- Coin toplama sistemi
- Izleyici modu

## Proje Yapisi

```
Assets/
├── Scripts/
│   ├── Network/        # Photon baglanti, lobby, senkronizasyon
│   ├── Canoe/          # Kano fizigi, hareket
│   ├── Paddle/         # Kurek mekanigi, VR input
│   ├── Game/           # GameManager, skor, state management
│   ├── UI/             # Scoreboard, isim girisi, izleyici UI
│   ├── Spectator/      # Izleyici kamera sistemi
│   ├── Collectibles/   # Coin sistemi
│   └── VRPlayer/       # VR rig, seat assignment
├── Prefabs/
│   ├── Network/        # Network prefablar (PhotonView)
│   ├── Player/         # VR oyuncu, kurekler
│   ├── Environment/    # Su, parkur, dekor
│   └── UI/             # Canvas prefablar
├── Scenes/
│   ├── MainMenu        # Ana menu
│   ├── Lobby           # Bekleme odasi
│   └── Game            # Yaris sahnesi
├── ScriptableObjects/
│   └── GameSettings    # Oyun ayarlari
└── Settings/           # URP ayarlari
```

## Namespace Yapisi

Tum scriptler `VRCanoe` namespace altinda:
- `VRCanoe.Network` - NetworkManager, PhotonCallbacks
- `VRCanoe.Canoe` - CanoeMovement, CanoePhysics
- `VRCanoe.Paddle` - PaddleController, PaddlePhysics
- `VRCanoe.Game` - CanoeGameManager, ScoreManager, TimerManager, NameManager, ScoreboardManager
- `VRCanoe.UI` - WorldSpaceScoreboard, CongratulationsDisplay, NameEntryUI, DebugUIManager
- `VRCanoe.Spectator` - SpectatorCamera
- `VRCanoe.Collectibles` - Collectible, CollectibleManager
- `VRCanoe.VRPlayer` - PlayerSpawner, SeatAssignment (NOT: VRCanoe.Player degil, Photon conflict)

## Onemli Manager'lar

### CanoeGameManager
- GameState yonetimi (WaitingForPlayers, EnteringNames, Countdown, Playing, Finished)
- Events: OnGameStateChanged, OnGameStarted, OnGameFinished, OnGameReset

### ScoreManager
- Coin toplama, sync bonus, time bonus
- Photon Room Properties ile senkronize
- PhotonView gerekli

### TimerManager
- Countdown ve oyun suresi
- Events: OnCountdownTick, OnCountdownFinished, OnTimeUp
- PhotonView gerekli

### NameManager
- Oyuncu ve takim isimleri
- Photon Room Properties ile senkronize
- Events: OnNamesChanged, OnNamesConfirmed

### ScoreboardManager
- JSON dosyasina tum skorlari kaydeder
- Top 10 gosterimi
- Photon RPC ile senkronize (oyun bitiminde)
- PhotonView gerekli

### CollectibleManager
- Tum collectible'lari takip eder
- Reset fonksiyonu

### DebugUIManager
- F12 ile debug UI toggle
- Inspector'dan kontrol

## Network Mimarisi

- Master Client: Oyun state, timer, fizik hesaplamalari
- Clients: Input gonderme, interpolasyon
- Senkronizasyon:
  - Room Properties: GameState, Score, Timer, Names
  - RPC: Collectible toplama, skor ekleme
  - PhotonView: Transform, Rigidbody

## VR Setup

- XR Origin ile oyuncu tracking
- Scene-based VR players (kanoya child olarak yerlestirilmis)
- Controller-based paddle input
- World Space UI (Scoreboard, Congratulations)

## PhotonView Gereken Objeler

- CanoeGameManager
- ScoreManager
- TimerManager
- ScoreboardManager
- Collectible (her biri)
- Canoe

## UI Sistemleri

### WorldSpaceScoreboard
- 10 TeamName text + 10 Points text (Inspector'dan atanir)
- # (siralama) elle yazilir
- Oyun bitiminde otomatik gosterilir

### CongratulationsDisplay
- Ayri text'ler veya tek combined text
- High score vurgulama
- Oyun bitiminde otomatik gosterilir

### NameEntryUI
- Player1, Player2, Team isim girisi
- MasterClient veya Player1 duzenleyebilir

## JSON Kayit

Skorlar `Application.persistentDataPath/scoreboard.json` dosyasina kaydedilir.
Tum oyuncu skorlari saklanir, sadece top 10 gosterilir.

## Coding Standards

```csharp
namespace VRCanoe.Game
{
    public class ExampleManager : MonoBehaviourPunCallbacks
    {
        public static ExampleManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private GameSettings settings;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        public event Action OnSomethingHappened;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}
```

## Build

```
File > Build Settings > Build
- Windows: Standalone
- Quest: Android + OpenXR
```
