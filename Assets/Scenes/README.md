# Scenes

Oyun sahneleri.

## Sahneler

### MainMenu.unity
- Ana menu
- Photon baglantisi
- Oda olusturma/katilma

### Lobby.unity
- Oyuncu bekleme alani
- Hazir durumu
- Oyun baslatma

### Game.unity
- Ana oyun sahnesi
- Yaris parkuru
- Su ve cevre

### GameOver.unity (opsiyonel)
- Sonuc ekrani
- Ayri sahne olarak
- veya Game icinde panel

## Sahne Akisi

```
MainMenu -> Lobby -> Game -> (GameOver) -> MainMenu
```

## Network Sahne Gecisi

```csharp
// Master client sahne degistirir
PhotonNetwork.LoadLevel("Game");
```

## Build Settings Sirasi

1. MainMenu (index 0)
2. Lobby (index 1)
3. Game (index 2)

## Notlar

- PhotonNetwork.AutomaticallySyncScene = true
- Ayni sahne tum clientlarda yuklenir
- Loading ekrani goster
