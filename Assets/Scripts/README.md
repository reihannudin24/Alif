# Alif — Setup Guide (Unity Editor)

Panduan singkat untuk merangkai GameObject dan component di Unity Editor agar seluruh script
di `Assets/Scripts` berfungsi. Struktur namespace: `Alif.Core`, `Alif.Player`, `Alif.Systems`,
`Alif.Dialogue`, `Alif.Characters`, `Alif.UI`.

## Cara tercepat: tool otomatis

Ada 2 menu editor (`Assets/Editor/`) yang membangun semua langkah di bawah secara otomatis:

1. **`Alif > 1) Import Character Art`** — baca folder `Assets/Sprites/Characters/<nama>/` (hasil
   export AI generator 8-arah: `Idle/rotations/<arah>.png` + `Idle/animations/...`), lalu bikin
   AnimationClip + AnimatorController + isi `CharacterData` untuk tiap karakter. Karakter bernama
   `alif` diperlakukan sebagai Player (walk cycle 8 arah); sisanya (`dimas`, `naya`, `raka`,
   `bu_siti`, `pak_ustad`) jadi NPC idle-loop. Tambah/kurangi daftar NPC di
   `AlifCharacterAnimationBuilder.NpcCharacters`.
2. **`Alif > 2) Build Demo Scene`** — bikin `Background`, `_GameManagers`, `Player`, 5 NPC, dan
   `Canvas` (HUD + inventory + dialogue) di scene aktif, sudah terhubung ke asset dari langkah 1.
   Background dibaca dari `Assets/Sprites/Backgrounds/Stasiun_Interior.png` kalau ada (sorting
   layer `Ground`, skala PPU 200 supaya proporsional sama karakter). Ganti isi konstanta
   `BackgroundSpritePath` di `AlifDemoSceneBuilder.cs` kalau mau pakai file/nama lain.

Jalankan urutan 1 lalu 2, simpan scene, tekan Play. Aman dijalankan berulang kali (idempotent).
Bagian di bawah ini menjelaskan APA yang dibuat tool tersebut, kalau mau setup manual/custom.

## 0. Prasyarat
- Package **Input System** (`com.unity.inputsystem`) sudah terpasang (cek di `Packages/manifest.json`).
- Di **Edit > Project Settings > Player > Active Input Handling**, set ke **Input System Package (New)**
  atau **Both** — kalau tidak, `PlayerInput` tidak akan berfungsi.
- Sorting Layer sudah dibuat otomatis di `ProjectSettings/TagManager.asset` dengan urutan:
  `Background → Ground → Characters → UI`. Assign layer ini ke tiap `SpriteRenderer`
  (Background/Tilemap = `Background` atau `Ground`, karakter/NPC = `Characters`, elemen 2D
  di world seperti UI diegetic = `UI`).
- Layer fisik `Interactable` juga sudah ditambahkan (`Edit > Project Settings > Tags and Layers`),
  dipakai untuk deteksi NPC oleh `PlayerController`.

## 1. Persistent Managers (satu scene khusus, atau di scene pertama)

Buat GameObject kosong bernama **`_GameManagers`**, lalu buat child GameObject untuk tiap manager
(atau attach semua ke satu GameObject `_GameManagers` juga boleh, karena semuanya singleton):

| GameObject           | Component            | Catatan |
|-----------------------|-----------------------|---------|
| `GameManager`         | `GameManager.cs`      | `DontDestroyOnLoad` otomatis di `Awake`. |
| `SceneLoader`         | `SceneLoader.cs`      | Opsional, hanya jika perlu pindah scene. |
| `TimeSystem`          | `TimeSystem.cs`       | Atur `Game Minutes Per Real Second` & jam awal di Inspector. |
| `EnergySystem`        | `EnergySystem.cs`     | Atur `Max Energy` di Inspector. |
| `CurrencySystem`      | `CurrencySystem.cs`   | Atur `Current Money` awal di Inspector. |
| `InventorySystem`     | `InventorySystem.cs`  | Atur `Slot Count` (default 5). |
| `DialogueManager`     | `DialogueManager.cs`  | Drag Player (`PlayerController`) ke field `_playerController`. |

> Semua script di atas pakai pola Singleton (`Instance`), jadi cukup satu instance per game,
> idealnya di scene pertama yang di-load (misal scene `Bootstrap` atau `MainMenu`).

## 2. Player

Buat GameObject **`Player`** dengan susunan component berikut:

1. **`Rigidbody2D`** — set `Gravity Scale = 0` (top-down, tidak butuh gravitasi), `Collision Detection = Continuous`.
2. **`Collider2D`** (misal `CapsuleCollider2D` atau `BoxCollider2D`) sesuai bentuk sprite.
3. **`Animator`** — assign Animator Controller yang punya parameter:
   - `Float MoveX`, `Float MoveY` — dipakai sebagai input Blend Tree 2D Freeform Directional untuk memilih clip idle/walk sesuai arah (South, North, East, West, + 4 diagonal).
   - `Bool IsMoving` — untuk pindah antara state Idle dan Walk (misal via Blend Tree bersarang atau 2 Blend Tree terpisah yang di-switch lewat transition `IsMoving`).
4. **`PlayerAnimation.cs`** — drag `Animator` ke field `_animator` (atau biarkan kosong, akan auto-`GetComponent`).
5. **`PlayerController.cs`**:
   - `Move Speed` sesuai kebutuhan (default 4).
   - `Interact Radius` (default 1.2).
   - `Interactable Layer` → pilih layer **`Interactable`** (dibuat otomatis di step 0).
   - `_playerAnimation` → drag component `PlayerAnimation` di GameObject yang sama.
6. **`PlayerInput`** (component bawaan Input System):
   - `Actions` → drag **Input Action Asset** kamu (buat lewat `Assets > Create > Input Actions`
     jika belum ada, lalu isi 2 action: `Move` (Value/Vector2, binding WASD + Left Stick) dan
     `Interact` (Button, binding tombol E / South Button gamepad)).
   - `Behavior` → pilih **`Send Messages`** (paling sederhana; `PlayerController` sudah punya
     method `OnMove(InputAction.CallbackContext)` dan `OnInteract(InputAction.CallbackContext)`
     yang otomatis terpanggil).
7. **`SpriteRenderer`** — Sorting Layer = `Characters`.

## 3. NPC

Buat prefab di `Assets/Prefabs/Characters`, GameObject **`NPC_<Nama>`**:

1. **`SpriteRenderer`** — Sorting Layer = `Characters`.
2. **`Animator`** — opsional, akan di-override otomatis oleh `CharacterData.AnimatorController` jika di-assign.
3. **`Collider2D`** — set **Layer = `Interactable`** (bukan default) supaya terdeteksi `PlayerController`.
   `Is Trigger` boleh dicentang tergantung apakah NPC juga perlu menghalangi jalan pemain
   (kalau perlu solid collision, tambahkan collider kedua khusus untuk fisik, bukan trigger).
4. **`NPCController.cs`**:
   - `_characterData` → drag asset `CharacterData` (lihat langkah 5).
   - `_defaultDialogue` → drag asset `DialogueData` (lihat langkah 6).

## 4. ScriptableObject Data

### CharacterData
`Assets/ScriptableObjects/Characters` → klik kanan → **Create > Alif > Character Data**.
Isi `Character Name`, `Description`, `Portrait`, `Animator Controller`.

### DialogueData
`Assets/ScriptableObjects/Dialogue` → klik kanan → **Create > Alif > Dialogue Data**.
Isi list `Lines`: tiap baris punya `Speaker Name` + `Text`. Untuk dialog bercabang, isi
`Choices` pada baris tersebut (tiap choice punya `Choice Text` + `Next Line Index` yang
menunjuk ke index baris tujuan di list `Lines` yang sama).

## 5. UI Canvas

Buat **Canvas** (`GameObject > UI > Canvas`), `Render Mode = Screen Space - Overlay`,
tambahkan **Canvas Scaler** (`UI Scale Mode = Scale With Screen Size`) supaya UI konsisten
di berbagai resolusi. Sorting Layer canvas = `UI`.

Struktur child yang disarankan:

```
Canvas
├── UIManager.cs  (attach di root Canvas)
├── HUD_Panel                  (pojok kiri atas)
│   ├── DayWeekText (TMP_Text)
│   ├── ClockText (TMP_Text)
│   ├── EnergySlider (Slider)
│   └── MoneyText (TMP_Text)
│   → attach HUDController.cs di GameObject HUD_Panel, drag ke-4 elemen di atas ke field-nya
├── Inventory_Panel             (5 slot horizontal di bawah layar)
│   ├── SlotContainer (Horizontal Layout Group)
│   │   ├── Slot_0 .. Slot_4    (masing-masing: Image icon + Text jumlah + InventorySlotUI.cs)
│   → attach InventoryUI.cs di GameObject Inventory_Panel, drag ke-5 `InventorySlotUI` ke list `_slotViews`
└── Dialogue_Panel
    ├── SpeakerNameText (TMP_Text)
    ├── DialogueText (TMP_Text)
    ├── NextButton (Button)
    └── ChoiceButtonContainer (Vertical Layout Group, awalnya kosong)
    → attach DialogueUI.cs, drag semua referensi + prefab tombol pilihan (`ChoiceButtonPrefab`)
```

Setelah semua panel dibuat, drag `HUD_Panel`, `Inventory_Panel`, `Dialogue_Panel` ke field
terkait di `UIManager.cs`.

> **Catatan TextMeshPro**: jika field `TMP_Text` belum bisa di-assign / muncul error,
> jalankan `Window > TextMeshPro > Import TMP Essential Resources` sekali di awal project.

## 6. Urutan Testing Cepat

1. Play scene yang berisi `_GameManagers`, `Player`, minimal 1 `NPC`, dan `Canvas` UI.
2. Gerakkan Player dengan WASD/stick — cek animasi berganti sesuai 8 arah.
3. Dekati NPC, tekan tombol Interact — `Dialogue_Panel` harus muncul dan menampilkan baris pertama.
4. Klik "Next" sampai dialog selesai — pastikan `Dialogue_Panel` hilang lagi dan Player bisa gerak lagi.
5. Cek HUD: jam berjalan otomatis, energy bar & uang berubah sesuai pemanggilan
   `EnergySystem.Instance.ConsumeEnergy(...)` / `CurrencySystem.Instance.AddMoney(...)` dari script lain.
