# Spectator Scripts

Izleyici kamera sistemi.

## Scriptler

### SpectatorManager.cs
- Izleyici modu yonetimi
- Kamera gecisleri
- Takip edilecek hedef secimi

### SpectatorCamera.cs
- Kamera kontrolleri
- Smooth follow
- Orbit kamera

### CameraPositions.cs
- Sabit kamera noktalari
- Drone gorunumu
- Bitis cizgisi kamerasi

### SpectatorInput.cs
- Kamera degistirme inputu
- Zoom kontrolleri
- Takim secimi

## Kamera Modlari

1. **Follow Cam**: Secili kanoyu takip eder
2. **Orbit Cam**: Kano etrafinda doner
3. **Fixed Cam**: Sabit noktalardan izleme
4. **Free Cam**: Serbest hareket
5. **Drone Cam**: Yukaridan kus bakisi

## Kullanim

```csharp
// Kamera modunu degistir
SpectatorManager.Instance.SetCameraMode(CameraMode.Follow);

// Hedef degistir
SpectatorManager.Instance.SetTarget(canoeTransform);
```

## VR Izleyici

- VR'da izleyici olarak katilabilir
- Teleport ile pozisyon degistirme
- Rahat izleme acilari

## Notlar

- Cinemachine kullanilabilir
- Smooth gecisler icin Lerp
- Izleyiciler oyunu etkilemez
