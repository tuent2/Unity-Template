# GameUp Unity Starter Template

Unity 6 starter với **GameUp Core** (UPM), AI tooling (Cursor/Claude), và shell `_MainProject` mỏng — dùng để tạo game mới, không phải full game.

Repo: https://github.com/tuent2/Unity-Template

---

## Cách dùng — tạo project mới

### Cách 1: Use this template (khuyên dùng)

1. Trên GitHub: bật **Settings → General → Template repository** (nếu chưa).
2. Mở https://github.com/tuent2/Unity-Template → **Use this template → Create a new repository**.
3. Clone repo mới về máy:

```bash
git clone https://github.com/<ban>/<ten-game-moi>.git
cd <ten-game-moi>
```

4. Mở folder đó bằng **Unity Hub** (Unity **6000.3.x** — xem `ProjectSettings/ProjectVersion.txt`).

Mỗi lần làm vậy = 1 game mới, git history sạch.

### Cách 2: Clone thẳng

```bash
git clone https://github.com/tuent2/Unity-Template.git MyNewGame
cd MyNewGame
git remote set-url origin https://github.com/<ban>/MyNewGame.git
git push -u origin main
```

---

## Setup lần đầu trong Unity

1. Mở project → chờ UPM resolve packages (cần mạng: `com.ohze.gameup.core`, SDK…).
2. Chạy **GameUp → Project → GameUpCore Installer** (define, Addressables shell, Core nếu cần).
3. Mở scene **`Assets/_MainProject/Scenes/Boot.unity`**.
4. Nếu chưa có trong scene, kéo vào:
   - `Assets/_MainProject/Prefabs/Core/====Manager====.prefab`
   - `Assets/_MainProject/Prefabs/Core/=====UI=====.prefab`
5. Khi có audio: **GameUp → Audio → Setup AudioManager**.
6. Đăng ký màn/popup trong:
   - `Assets/_MainProject/Resources/Data/ScreenData`
   - `Assets/_MainProject/Resources/Data/PopupData`

Viết code game trong `Assets/_MainProject/Scripts/` (theo convention trong `.cursorrules` / `CLAUDE.md` nếu có).

---

## Không dùng như thế nào

| Việc | Nên / Không |
|------|-------------|
| Tạo game **mới** từ template | Có — Use this template / clone |
| “Add template” vào project **đã có** như package UPM | Không — đây là cả Unity project |
| Copy đè `ProjectSettings` / `Packages` vào project cũ | Không — dễ hỏng project |
| Chỉ lấy vài phần (rules, Core prefab…) vào project cũ | Có — copy **có chọn lọc** từng folder |

Template này **không** phải dependency để gắn vào project khác. Muốn tái dùng Core thì dùng UPM `com.ohze.gameup.core` trong `Packages/manifest.json` của project đó.

---

## Có gì trong template

| Area | Nội dung |
|------|----------|
| Framework | `com.ohze.gameup.core` + `com.ohze.gameup.sdk` (Git UPM) |
| Tooling | `.cursor/`, `.claude/` (nếu có trong bản push), rules AI |
| Render | URP 2D, TextMesh Pro, DOTween |
| Core prefabs | `====Manager====`, `=====UI=====` (+ Loading / Toast) |
| Scripts | `Scripts/Core/` utilities, `BlockCanvas` |
| Data | `ScreenData`, `PopupData`, `AudioDatabase`, `AddressableHolder` (rỗng / tối thiểu) |
| Scene | `Boot.unity` |

**Không kèm:** art/audio/VFX game, battle/perk, Firebase secrets, popup/screen gameplay đầy đủ.

---

## Cấu trúc thư mục

```
Assets/
  _MainProject/
    Art/                 ← art game (placeholder)
    Audio/               ← audio game (placeholder)
    Data/Singletons/     ← AudioDatabase, …
    Prefabs/Core/        ← Manager + UI roots
    Prefabs/UI/          ← Screens, Popups, Helpers
    Resources/Data/      ← ScreenData, PopupData
    Scenes/              ← Boot.unity
    ScriptableObjects/
    Scripts/Core/        ← tiện ích dùng chung
    Scripts/Gameplay/    ← feature code
  Plugins/Demigiant/     ← DOTween
Packages/
  manifest.json          ← GameUp Core/SDK + Unity packages
```

---

## Pin version GameUp Core (nên làm)

Trong `Packages/manifest.json`, gắn tag thay vì trôi theo `main`:

```json
"com.ohze.gameup.core": "https://github.com/ohze/gameup-unity-template.git?path=Assets/GameUpCore#v1.x.x",
"com.ohze.gameup.sdk": "https://github.com/ohze/gameup-unity-template.git?path=Assets/GameUpSDK#v1.x.x"
```

---

## Checklist nhanh sau khi tạo game mới

- [ ] Unity 6 đúng version mở được project
- [ ] Package Manager không lỗi đỏ (GameUp Core/SDK)
- [ ] Chạy GameUpCore Installer
- [ ] `Boot` chạy được, có Manager + UI
- [ ] Đổi remote / tên product trong `ProjectSettings`
- [ ] Bắt đầu feature trong `_MainProject` (không sửa package Core nếu dùng UPM)

---

## License / third-party

Theo policy studio. Third-party: DOTween (Demigiant), TextMesh Pro (Unity), GameUp Core/SDK (ohze).
