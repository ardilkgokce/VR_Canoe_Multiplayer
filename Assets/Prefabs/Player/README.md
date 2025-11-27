# Player Prefabs

Oyuncu ve VR prefablari.

## Prefablar

### VRPlayer.prefab
- XR Origin
- Head tracking
- Hand controllers
- Paddle attachment points

### PlayerAvatar.prefab
- Oyuncu gorsel temsili
- Animasyon controller
- IK hedefleri

### LeftPaddle.prefab
- Sol el kuregi
- Collider
- VR grab

### RightPaddle.prefab
- Sag el kuregi
- Collider
- VR grab

### HandModel.prefab
- El modeli
- Poz animasyonlari
- Haptic feedback

## VR Hierarchy

```
VRPlayer
├── XR Origin
│   ├── Camera Offset
│   │   ├── Main Camera (Head)
│   │   ├── Left Controller
│   │   │   └── LeftPaddle
│   │   └── Right Controller
│   │       └── RightPaddle
│   └── Locomotion System
└── PlayerAvatar
```

## Notlar

- XR Interaction Toolkit kullan
- Hand tracking opsiyonel
- Avatar network senkronize
