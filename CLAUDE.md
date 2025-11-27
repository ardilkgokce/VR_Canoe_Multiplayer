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
│   └── Collectibles/   # Coin sistemi
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

## GameSettings

`Assets/ScriptableObjects/GameSettings.asset` dosyasi:

| Ayar | Default | Aciklama |
|------|---------|----------|
| gameDuration | 60s | Oyun suresi |
| coinValue | 10 | Coin puan degeri |
| canoeSpeedMultiplier | 1.0 | Hiz carpani |
| syncPaddleMultiplier | 1.5 | Senkronize kurek bonusu |
| finishLineDistance | 500m | Bitis mesafesi |

## Namespace

Tum scriptler `VRCanoe` namespace altinda:
- `VRCanoe.Network`
- `VRCanoe.Canoe`
- `VRCanoe.Paddle`
- `VRCanoe.Game`
- `VRCanoe.UI`
- `VRCanoe.Spectator`
- `VRCanoe.Collectibles`

## Key Technologies

| Package | Purpose |
|---------|---------|
| Photon PUN2 | Multiplayer networking |
| XR Interaction Toolkit | VR input ve interaction |
| URP 14.0.12 | Rendering |
| TextMeshPro | UI text |

## Oyun Akisi

```
MainMenu -> Photon Connect -> Lobby -> Ready -> Game -> Finish -> Results
```

## Network Mimarisi

- Master Client: Oyun state, timer, spawn yonetimi
- Clients: Input gonderme, interpolasyon
- Senkronizasyon: Transform, Rigidbody, Custom properties

## VR Setup

- XR Origin ile oyuncu tracking
- Controller-based paddle input
- World Space UI
- Comfort settings

## Coding Standards

```csharp
namespace VRCanoe.Game
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameSettings settings;

        private void Start() { }
    }
}
```

## Build

```
File > Build Settings > Build
- Windows: Standalone
- Quest: Android + OpenXR
```
