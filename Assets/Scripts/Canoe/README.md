# Canoe Scripts

Kano fizigi ve hareket sistemi.

## Scriptler

### CanoeController.cs
- Kano Rigidbody kontrolu
- Hareket ve donus
- Su fizigi (buoyancy)

### CanoePhysics.cs
- Suya tepki
- Dalga efektleri
- Drag ve surtunme

### CanoeBalance.cs
- Denge mekanigi
- Devrilme kontrolu
- Agirlik merkezi

### TwoPlayerCanoe.cs
- Iki oyunculu kano yonetimi
- Senkronize kurek bonusu
- Ortak hareket hesaplama

## Fizik Parametreleri

- Mass: Kano agirligi
- Drag: Su direnci
- Angular Drag: Donus direnci
- Buoyancy Force: Yukari itme kuvveti

## Notlar

- Rigidbody gerektirir
- Su yuzeyiyle collision layer ayarla
- GameSettings'ten hiz carpanini al
