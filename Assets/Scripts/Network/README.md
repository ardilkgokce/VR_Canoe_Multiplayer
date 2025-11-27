# Network Scripts

Photon PUN2 network sistemi scriptleri.

## Scriptler

### PhotonManager.cs
- Photon sunucusuna baglanma
- Lobby yonetimi
- Oda olusturma/katilma

### RoomManager.cs
- Oda state yonetimi
- Oyuncu listesi
- Oyun baslatma/bitirme

### PlayerSync.cs
- Oyuncu pozisyon/rotasyon senkronizasyonu
- Kurek hareketleri senkronizasyonu
- Interpolasyon

### CanoeSync.cs
- Kano fizik state senkronizasyonu
- Network ownership yonetimi

## Kullanim

```csharp
// Photon'a baglanma
PhotonManager.Instance.Connect();

// Odaya katilma
PhotonManager.Instance.JoinOrCreateRoom("RoomName");
```

## Notlar

- Photon PUN2 paketi gerekli
- AppId'yi PhotonServerSettings'te ayarla
- Master client oyun state'ini yonetir
