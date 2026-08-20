# Unity Template — Tuent Core

Starter Unity 6 với **Tuent Core** — framework riêng, 100% trong repo.

Repo: https://github.com/tuent2/Unity-Template

---

## Framework

| Path | Nội dung |
|------|----------|
| `Assets/TuentCore/` | Logger, pool, UI, audio, save, editor installer |
| `Assets/_MainProject/` | Code & asset game |

**Namespace:** `Tuent.Core`, `Tuent.Core.UI`  
**API chính:** `TLogger`, `TPool`, `TPoolers`, `MonoSingleton`, `UIScreen`, `UIPopup`

Không còn GameUp / ohze UPM.

---

## Setup project mới

1. **Use this template** trên GitHub → clone.
2. Unity **6000.3.x** → mở project.
3. **Tuent → Project → TuentCore Installer** → DOTween + define.
4. **Tuent → Project → Folder Setup** → **Core setup**.
5. Mở `Assets/_MainProject/Scenes/Boot.unity`.

---

## Menu Editor

| Menu | Việc |
|------|------|
| `Tuent/Project/TuentCore Installer` | DOTween + define |
| `Tuent/Project/Folder Setup` | Tạo folder `_MainProject` |
| `Tuent/Project/Core setup` | Copy prefab Manager + UI |
| `Tuent/Audio/Setup AudioManager` | Audio pipeline |
| `Tuent/Logger/Enable Logs` | Bật/tắt log |

---

## Viết code game

```csharp
using Tuent.Core;
using Tuent.Core.UI;

TLogger.Log("Shop", "Opened");
public class ShopPopup : UIPopup { }
```

Đặt script trong `Assets/_MainProject/Scripts/` — namespace `Gameplay.*`, không `Tuent.Core*`.

---

## Cấu trúc

```
Assets/
  TuentCore/Runtime/Core/     TLogger, TPool, AudioManager…
  TuentCore/Runtime/UI/       UIScreen, Loading, Toast…
  TuentCore/Editor/           Installer, setup windows
  _MainProject/               Game của bạn
```

---

## Checklist

- [ ] Compile OK, menu **Tuent/** hiện đủ
- [ ] Không folder `GameUpSDK` / `GameUpCore`
- [ ] Boot scene chạy với Manager + UI
- [ ] Chỉ feature game trong `_MainProject`
