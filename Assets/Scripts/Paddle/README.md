# Paddle Scripts

Kurek mekanigi ve VR input sistemi.

## Scriptler

### PaddleController.cs
- Kurek fizigi
- Suyla temas algilama
- Kuvvet hesaplama

### VRPaddleInput.cs
- VR controller pozisyon/rotasyon
- Hand tracking destegi
- Kurek tutma/birakma

### PaddleStroke.cs
- Kurek cekisi algilama
- Stroke gucu hesaplama
- Senkronize stroke tespiti

### PaddleVisuals.cs
- Kurek gorselleri
- Su efektleri
- Haptic feedback

## VR Input

```csharp
// Controller pozisyonu
Vector3 paddlePosition = VRPaddleInput.GetPaddlePosition();

// Stroke kuvveti
float strokeForce = PaddleStroke.CalculateForce(velocity);
```

## Senkronize Kurek Bonusu

Iki oyuncu ayni anda kurek cekerse:
- Timing window: 0.3 saniye
- Bonus carpan: GameSettings.syncPaddleMultiplier

## Notlar

- XR Interaction Toolkit gerektirir
- Her el icin ayri paddle instance
- Haptic feedback suresi ayarlanabilir
