# ScriptableObjects

Oyun verileri ve ayarlari.

## ScriptableObjects

### GameSettings.asset
- Oyun suresi
- Coin degeri
- Hiz carpanlari
- Bitis mesafesi

### CanoeSettings.asset
- Kano fizik parametreleri
- Kurek gucu
- Buoyancy ayarlari

### AudioSettings.asset
- Ses seviyeleri
- Muzik/SFX ayri
- Spatial audio

### VRSettings.asset
- Comfort ayarlari
- Hareket hassasiyeti
- Snap turn acisi

## Kullanim

```csharp
// Inspector'dan referans
[SerializeField] private GameSettings gameSettings;

// Kullanim
float gameTime = gameSettings.gameDuration;
```

## Avantajlari

- Runtime degistirilebilir
- Prefab bagimsiz
- Kolay balans ayari
- Version control friendly

## Notlar

- Assets/ScriptableObjects/ klasorunde tut
- Anlamli isimler ver
- Default degerler ayarla
