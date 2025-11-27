# Collectibles Scripts

Coin ve toplanabilir objeler sistemi.

## Scriptler

### Coin.cs
- Coin davranisi
- Toplama algilama
- Animasyon ve efektler

### CoinSpawner.cs
- Coin spawn pozisyonlari
- Rastgele veya sabit yerlesim
- Spawn zamanlama

### CollectibleBase.cs
- Tum toplanabilirler icin base class
- Ortak ozellikler
- Network senkronizasyonu

### CoinEffect.cs
- Toplama efektleri
- Ses ve partikuller
- Skor popup

## Coin Ozellikleri

- Deger: GameSettings.coinValue
- Donus animasyonu
- Parlama efekti
- Toplama sesi

## Network Senkronizasyonu

```csharp
// Coin toplandiktan sonra
[PunRPC]
void CollectCoin(int coinId, int playerId)
{
    // Tum clientlarda coin'i kaldir
    // Skoru guncelle
}
```

## Yerlesim

Coinler su yuzeyinde veya hafif yukarida:
- Parkur boyunca dagilmis
- Zorluk seviyesine gore
- Bazi coinler bonuslu olabilir

## Notlar

- Trigger collider kullan
- Object pooling onerilir
- Master client spawn'i yonetir
