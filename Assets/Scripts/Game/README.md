# Game Scripts

Oyun yonetimi, skor ve state management.

## Scriptler

### GameManager.cs
- Oyun state yonetimi (Lobby, Playing, Finished)
- Skor takibi
- Oyun suresi kontrolu
- Kazanan belirleme

### ScoreManager.cs
- Coin toplama skorlari
- Takim skorlari
- Skor senkronizasyonu

### TimerManager.cs
- Geri sayim
- Oyun suresi (default 60 saniye)
- UI guncellemeleri

### RaceManager.cs
- Yaris baslangici
- Bitis cizgisi kontrolu
- Siralama

### GameState.cs
- State enum tanimlari
- State degisim eventleri

## Oyun Akisi

```
1. Lobby -> Oyuncular hazir
2. Countdown -> 3-2-1
3. Playing -> Yaris basladi
4. Finished -> Sonuclar
```

## State Yonetimi

```csharp
public enum GameState
{
    Lobby,
    Countdown,
    Playing,
    Finished
}
```

## Notlar

- GameManager singleton pattern kullanir
- Master client state degisikliklerini yonetir
- Skorlar PhotonNetwork uzerinden senkronize edilir
