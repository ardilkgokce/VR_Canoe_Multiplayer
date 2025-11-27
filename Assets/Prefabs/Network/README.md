# Network Prefabs

Photon network prefablari.

## Prefablar

### NetworkPlayer.prefab
- PhotonView component
- PlayerSync script
- Oyuncu network kimlik bilgileri

### NetworkCanoe.prefab
- PhotonView component
- CanoeSync script
- PhotonRigidbodyView
- Iki oyunculu kano

### NetworkCoin.prefab
- PhotonView component
- Coin script
- Senkronize toplanabilir

## PhotonView Ayarlari

- Ownership: Takeable veya Fixed
- Synchronization: Reliable Delta Compressed
- Observable Components: Transform, Rigidbody, Custom

## Notlar

- Tum network prefablar Resources klasorunde olmali
- PhotonNetwork.Instantiate ile olusturulur
- ViewID otomatik atanir
