using System;
using System.Collections.Generic;
using System.Linq;
using Alif.Campaign;
using Alif.Dialogue;
using Alif.Systems;
using Alif.UI;
using Alif.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.Adventure
{
    /// <summary>
    /// Side quest gaya "cari → berikan" (konten di SideQuestContent): NPC quest dimunculkan
    /// runtime, langkah talk / give / papan puzzle, hadiah barang & skor, hubungan NPC, penanda
    /// "!"/"?" di atas kepala NPC, serta isi Tas & pickup yang tersimpan di save.
    /// </summary>
    public sealed partial class AdventureGame
    {
        CityWorld.Built _city;
        readonly Dictionary<string, GameObject> _questNpcRoots = new Dictionary<string, GameObject>();
        /// <summary>Indeks area tiap tokoh/benda kota — dipakai panah penunjuk Kasus Warga.</summary>
        readonly Dictionary<string, int> _questNpcAreas = new Dictionary<string, int>();

        /// <summary>Kota Cempaka (6 map) ditambahkan setelah area scene; harus dibangun sebelum
        /// pemain ditempatkan agar AreaOf/Spawns mengenali posisi di kota.</summary>
        void BuildCity()
        {
            _city = CityWorld.Build();
            Centers = Centers.Concat(_city.Centers).ToArray();
            Spawns = Spawns.Concat(_city.Spawns).ToArray();
            if (Centers.Length != Content.Areas.Length)
                Debug.LogError($"[Alif] Jumlah area tidak cocok: {Centers.Length} pusat vs {Content.Areas.Length} nama area.");
            BuildSceneDoors();
            BuildEntranceMarkers();
            BuildRoadDoors();
            RetireUnusedAreas();
        }

        /// <summary>Penanda pintu masuk gedung kota: label ("IN") + panah bawah melayang bebas di
        /// atas serambi pintu, supaya pemain tahu pintu itu bisa dituju. Titik lantai & tinggi
        /// bebasnya dari <c>city.json</c> (lihat <c>entrances</c> di Tools/city-art/generate_city.py);
        /// geser penandanya dengan mengubah tinggi itu, bukan angka di sini.</summary>
        void BuildEntranceMarkers()
        {
            const float Gap = .12f;          // jarak panah dari puncak serambi/kusen
            const float ArrowScale = 1.4f;   // sprite 10 px pada PPU 33 ≈ .3 unit — dinaikkan agar terbaca
            foreach (var (area, foot, height, label, destination) in _city.Entrances)
            {
                var root = new GameObject("Entrance " + area + " " + label);
                root.transform.position = foot;
                // Penanda gedung yang terkunci disembunyikan (lihat RefreshEntranceMarkers).
                if (label != "OUT") _entranceMarkers.Add((root, Array.IndexOf(Content.Areas, destination)));

                var arrow = new GameObject("Arrow").AddComponent<SpriteRenderer>();
                arrow.transform.SetParent(root.transform, false);
                arrow.sprite = PixelSkin.EntranceArrow();
                arrow.transform.localScale = Vector3.one * ArrowScale;
                float arrowY = height + Gap + arrow.sprite.bounds.extents.y * ArrowScale;
                arrow.transform.localPosition = new Vector3(0f, arrowY, 0f);
                arrow.sortingOrder = YSortOrder.PromptOrderBase + 20;
                arrow.gameObject.AddComponent<FloatingPrompt>();

                var text = WorldLabel(root.transform, label, arrowY + arrow.sprite.bounds.extents.y * ArrowScale + .10f, .45f, PixelSkin.Cream);
                text.outlineColor = PixelSkin.Outline; text.outlineWidth = .3f;
            }
        }

        // ───────────────────── Pintu kota → interior di scene bab ─────────────────────

        /// <summary>Memasang pintu dari fasad toko di map kota ke interior yang tinggal di scene
        /// bab (<c>sceneDoors</c> di city.json). Warung Bu Siti dipakai begini: interiornya tetap
        /// yang digambar tangan di scene, tapi masuknya sekarang lewat etalase Toko Kelontong di
        /// Jalan Pasar — area luar lamanya ("Depan warung") dipensiunkan.</summary>
        void BuildSceneDoors()
        {
            var spec = CityWorld.Load();
            if (spec?.sceneDoors == null) return;
            var root = new GameObject("Pintu kota ke scene").transform;
            foreach (var entry in spec.sceneDoors)
            {
                int inside = Array.IndexOf(Content.Areas, entry.area);
                if (inside < 0 || !_city.Worlds.TryGetValue(entry.map, out var world)) continue;
                var inward = InwardDoor(inside);                     // pintu lama yang menuju interior
                var outward = SideDoor(inside, null);                // pintu keluar interior
                if (!inward || !outward) continue;

                // Sisi kota: trigger baru di fasad, mendarat di titik masuk interior yang sudah ada.
                Vector2 foot = world(entry.x, entry.y);
                var landing = new GameObject("Scene landing " + entry.area).transform;
                landing.SetParent(root, false);
                landing.position = inward.Destination.position;
                var go = new GameObject($"Door {entry.map} → {entry.area}");
                go.transform.SetParent(root, false);
                go.transform.position = foot;
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.1f, .5f);
                box.isTrigger = true;
                go.AddComponent<SceneDoor>().Configure(landing, "Masuk ke " + entry.area + "?", true);
                _city.Entrances.Add((entry.map, foot, entry.h / CityWorld.Load().ppu, entry.label, entry.area));

                // Sisi interior: pintu keluarnya diarahkan ke trotoar depan fasad itu.
                var back = new GameObject("Scene landing " + entry.map).transform;
                back.SetParent(root, false);
                back.position = world(entry.landX, entry.landY);
                outward.Configure(back, "Keluar ke " + entry.map + "?");
            }
        }

        /// <summary>Pintu mana pun yang tujuannya area ini (dipakai untuk meminjam titik
        /// mendaratnya), dan pintu mana pun yang ada di dalam area ini.</summary>
        SceneDoor InwardDoor(int area)
        {
            foreach (var door in FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
                if (door.Destination && AreaOf(door.Destination.position) == area && AreaOf(door.transform.position) != area)
                    return door;
            return null;
        }

        /// <summary>Area scene yang sudah tidak dipakai: latar, prop, dan NPC-nya dimatikan, pintu
        /// yang masih menuju ke sana dilucuti, dan pemain yang checkpoint-nya tersimpan di sana
        /// dipindahkan ke area terdekat yang masih hidup.</summary>
        void RetireUnusedAreas()
        {
            var spec = CityWorld.Load();
            if (spec == null) return;
            // Prop tempelan yang kalah oleh lukisan interiornya (rak saji bambu di Warung Bu Siti).
            if (spec.hiddenProps != null)
                foreach (var point in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (Array.IndexOf(spec.hiddenProps, point.name) >= 0) point.gameObject.SetActive(false);
            if (spec.retired == null || spec.retired.Length == 0) return;
            foreach (var retired in spec.retired)
            {
                int area = Array.IndexOf(Content.Areas, retired.area);
                int host = Array.IndexOf(Content.Areas, retired.to);
                if (area < 0 || host < 0 || !_city.Worlds.TryGetValue(retired.to, out var world)) continue;
                Vector2 spot = world(retired.x, retired.y);
                foreach (var door in FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
                    if (door.Destination && AreaOf(door.Destination.position) == area) door.gameObject.SetActive(false);

                // Hanya warga yang didaftarkan di 'keep' yang ikut pindah — sisanya duplikat dari
                // tokoh yang sudah berdiri di dalam gedungnya, jadi dimatikan bersama latar & prop
                // supaya trotoar penggantinya tidak sesak.
                foreach (var point in FindObjectsByType<AdventurePoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var go = point.transform.root.gameObject;
                    if (AreaOf(go.transform.position) != area) continue;
                    if (retired.keep == null || Array.IndexOf(retired.keep, point.Target) < 0) { go.SetActive(false); continue; }
                    go.transform.position = spot;                // di balik meja dagangan, bukan di jalur pemain
                    point.Area = host;
                }
                foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    var go = renderer.transform.root.gameObject;
                    // BuildCity() berjalan sebelum BindOriginalScene(), jadi _player masih null di
                    // sini — pemain dikenali lewat tag, bukan lewat referensinya.
                    if (go.CompareTag("Player") || go.GetComponentInChildren<Alif.Player.PlayerController>()) continue;
                    if (AreaOf(go.transform.position) == area) go.SetActive(false);
                }
                // Checkpoint yang tersimpan di area pensiunan: cukup pindahkan areanya dan lupakan
                // koordinatnya — BindOriginalScene() akan menaruh pemain di titik spawn area baru.
                if (State.Area == area) { State.Area = host; State.HasPosition = false; }
            }
        }

        // ───────────────────────── Jalur jalan kaki antar area ─────────────────────────

        /// <summary>Menyambung area yang bersebelahan di Peta HP (<c>roads</c> di city.json):
        /// berjalan sampai tepi map memunculkan pemain di tepi seberang tetangganya, jadi arah
        /// kiri-kanan di dunia sama dengan urutan pin di peta. Tepi map kota dipasangi strip
        /// pemicu setinggi lantainya; sisi area scene memakai pintu yang sudah digambar di scene
        /// dan hanya diarahkan ulang — dulu pintu ujung jalan itu saling menyambung langsung
        /// (Depan stasiun ⇄ Depan warung) sehingga seluruh kota terlompati.</summary>
        void BuildRoadDoors()
        {
            var spec = CityWorld.Load();
            if (spec?.roads == null) return;
            var root = new GameObject("Jalur jalan kaki").transform;
            foreach (var road in spec.roads)
            {
                if (!TryRoadSide(road.west, true, out var west) || !TryRoadSide(road.east, false, out var east)) continue;
                LinkRoad(root, west, east.Landing, road.east, true);
                LinkRoad(root, east, west.Landing, road.west, false);
            }
        }

        /// <summary>Tepi satu area: titik pemicunya, titik mendarat saat datang dari sisi itu, dan
        /// (kalau areanya bukan map kota) pintu scene yang sudah ada di sisi tersebut.</summary>
        struct RoadSide { public int Area; public Vector2 Foot, Landing, Size; public SceneDoor Door; }

        bool TryRoadSide(string area, bool east, out RoadSide side)
        {
            side = default;
            int index = Array.IndexOf(Content.Areas, area);
            if (index < 0) return false;                       // area itu tidak ada di bab ini
            var edges = east ? _city.EastEdges : _city.WestEdges;
            if (edges.TryGetValue(area, out var edge))
            {
                side = new RoadSide { Area = index, Foot = edge.Foot, Landing = edge.Landing, Size = edge.Size };
                return true;
            }
            // Area scene: geometri lantainya tidak ada di city.json, jadi jalurnya hanya dibuat
            // kalau sisi itu sudah punya pintu gambaran tangan (Halaman kos yang cuma punya pintu
            // balik ke kiri, misalnya, dilewati saja).
            var door = SideDoor(index, east);
            if (!door)
            {
                // Tidak ada pintu di sisi itu (mis. barat Depan stasiun → Pusat Kota): tepinya dibuat dari
                // lantai scene, sama seperti tepi map kota.
                if (!TrySceneEdge(index, east, out Vector2 edgeFoot, out Vector2 edgeLanding, out Vector2 edgeSize)) return false;
                side = new RoadSide { Area = index, Foot = edgeFoot, Landing = edgeLanding, Size = edgeSize };
                return true;
            }
            Vector2 foot = door.transform.position;
            side = new RoadSide { Area = index, Foot = foot, Landing = foot + new Vector2(east ? -1.5f : 1.5f, 0f), Door = door };
            return true;
        }

        /// <summary>Tepi kiri/kanan lantai sebuah area scene: pita pemicu selebar .5 unit yang
        /// merentang semua WalkableArea yang ujungnya (hampir) sejajar dengan ujung terluar.</summary>
        bool TrySceneEdge(int area, bool east, out Vector2 foot, out Vector2 landing, out Vector2 size)
        {
            foot = landing = size = default;
            var floors = FindObjectsByType<WalkableArea>(FindObjectsSortMode.None)
                .Select(w => w.GetComponent<Collider2D>()).Where(c => c && AreaOf(c.bounds.center) == area)
                .Select(c => c.bounds).ToList();
            if (floors.Count == 0) return false;
            float outer = east ? floors.Max(b => b.max.x) : floors.Min(b => b.min.x);
            var reach = floors.Where(b => Mathf.Abs((east ? b.max.x : b.min.x) - outer) < 1f).ToList();
            float bottom = reach.Min(b => b.min.y), top = reach.Max(b => b.max.y);
            foot = new Vector2(outer + (east ? -.35f : .35f), (bottom + top) / 2f);
            size = new Vector2(.5f, top - bottom);
            // Mendarat di tengah lantai terluas: titik tengah gabungan bisa jatuh di celah antar lantai.
            var widest = reach.OrderByDescending(b => b.size.x * b.size.y).First();
            landing = new Vector2(foot.x + (east ? -1.5f : 1.5f), widest.center.y);
            return true;
        }

        /// <summary>Pintu scene terluar di sisi kiri/kanan sebuah area (diukur dari pusat areanya).</summary>
        SceneDoor SideDoor(int area, bool? east)
        {
            SceneDoor best = null;
            foreach (var door in FindObjectsByType<SceneDoor>(FindObjectsSortMode.None))
            {
                if (AreaOf(door.transform.position) != area) continue;
                if (east == null) return door;                       // sisi mana pun (pintu tunggal)
                float offset = door.transform.position.x - Centers[area].x;
                if (east.Value ? offset <= .5f : offset >= -.5f) continue;
                if (!best || (east.Value ? door.transform.position.x > best.transform.position.x
                                         : door.transform.position.x < best.transform.position.x)) best = door;
            }
            return best;
        }

        void LinkRoad(Transform root, RoadSide side, Vector2 destination, string destinationArea, bool east)
        {
            var landing = new GameObject("Road landing " + destinationArea).transform;
            landing.SetParent(root, false);
            landing.position = destination;
            string message = "Jalan ke " + destinationArea + "?";
            if (side.Door) { side.Door.Configure(landing, message); return; }

            var go = new GameObject($"Road {Content.Areas[side.Area]} → {destinationArea}");
            go.transform.SetParent(root, false);
            go.transform.position = side.Foot;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = side.Size;
            box.isTrigger = true;
            go.AddComponent<SceneDoor>().Configure(landing, message);
            var label = WorldLabel(go.transform, (east ? "Kanan · " : "Kiri · ") + destinationArea, .9f, .42f, PixelSkin.Cream);
            label.outlineColor = PixelSkin.Outline; label.outlineWidth = .3f;
        }

        /// <summary>Penanda pintu masuk gedung + area tujuannya, untuk disembunyikan selama
        /// gedungnya masih terkunci (rantai tugas Buku Perjalanan belum sampai ke sana).</summary>
        readonly List<(GameObject root, int destination)> _entranceMarkers = new List<(GameObject, int)>();

        /// <summary>Sembunyikan panah "IN" di gedung yang belum ada urusannya, tampilkan lagi
        /// begitu tugasnya pindah ke sana atau rantai tugasnya selesai.</summary>
        void RefreshEntranceMarkers()
        {
            foreach (var (root, destination) in _entranceMarkers)
                if (root) root.SetActive(destination < 0 || !BuildingLocked(destination));
        }

        readonly Dictionary<string, TMP_Text> _questMarkers = new Dictionary<string, TMP_Text>();
        bool _restoringInventory;

        void StartSideQuests()
        {
            RestoreInventory();
            foreach (var pickup in FindObjectsByType<ItemPickup>(FindObjectsInactive.Include))
                if (State.PickedUp.Contains(pickup.ItemName)) pickup.MarkCollected();
            ItemPickup.Collected -= RecordPickup;
            ItemPickup.Collected += RecordPickup;
            // Sesi pertama sebuah bab: jamnya disetel ke jam kedatangan (Bab 1 tiba pukul 13.00).
            // Sesi lanjutan memakai jam berjalan supaya checkpoint tidak memundurkan waktu.
            if (State.HasPosition) SyncStoryDay(false);
            else TimeSystem.Instance?.SetStoryDay(State.Day, true, Content.StartHour);
            WatchClock();
            BuildQuestNpcs();
        }

        void StopSideQuests() => ItemPickup.Collected -= RecordPickup;

        void RecordPickup(string itemName)
        {
            if (!State.PickedUp.Contains(itemName)) State.PickedUp.Add(itemName);
        }

        /// <summary>Isi Tas dari save (nama & jumlah; ikon dari ItemCatalog). Tidak memunculkan
        /// popup "Aku mendapatkan …".</summary>
        void RestoreInventory()
        {
            var inventory = InventorySystem.Instance;
            if (!inventory || State.Items.Count == 0) return;
            _restoringInventory = true;
            inventory.InitializeSlots();
            foreach (var item in State.Items) inventory.AddItem(item.Name, ItemCatalog.Icon(item.Name), item.Quantity);
            _restoringInventory = false;
        }

        void SnapshotInventory()
        {
            var inventory = InventorySystem.Instance;
            if (!inventory) return;
            State.Items = inventory.Slots.Where(s => !s.IsEmpty).Select(s => new ItemStack { Name = s.ItemName, Quantity = s.Quantity }).ToList();
        }

        // ───────────────────────── NPC quest di dunia ─────────────────────────

        void BuildQuestNpcs()
        {
            foreach (var pair in _city.Npcs)
            {
                string npc = pair.Key;
                var (area, position) = pair.Value;
                int areaIndex = Array.IndexOf(Content.Areas, area);
                var info = SideQuestContent.Npc(npc);
                if (areaIndex < 0 || info == null) continue;
                // Warna pembeda sprite placeholder (dipinjam dari tokoh lama) sampai sprite final ada.
                Color tint = !string.IsNullOrEmpty(info.Tint) && ColorUtility.TryParseHtmlString("#" + info.Tint, out Color t) ? t : Color.white;
                Vector2 spot = position;
                if (!info.IsObject && !TryFindNpcSpot(position, out spot))
                {
                    Debug.LogWarning($"[Alif] NPC quest {info.Name} tidak dimunculkan: tidak ada lantai bebas di sekitar {position}.");
                    continue;
                }

                var root = new GameObject("QuestNpc " + info.Name);
                root.transform.position = spot;
                var body = root.AddComponent<SpriteRenderer>();
                if (!info.IsObject)
                {
                    var data = Cast.FirstOrDefault(c => c && c.name.EndsWith(info.Placeholder, StringComparison.OrdinalIgnoreCase));
                    body.sprite = data ? (data.WorldSprite ? data.WorldSprite : data.Portrait) : null;
                    body.color = tint;
                    root.AddComponent<YSortOrder>();
                    BlobShadow.Ensure(root.transform);
                    var feet = root.AddComponent<CapsuleCollider2D>();
                    feet.direction = CapsuleDirection2D.Horizontal; feet.size = new Vector2(.3f, .18f); feet.offset = new Vector2(0f, .09f);
                }

                var trigger = new GameObject("Adventure interaction") { layer = 7 };
                trigger.transform.SetParent(root.transform, false);
                var circle = trigger.AddComponent<CircleCollider2D>(); circle.isTrigger = true; circle.radius = .45f; circle.offset = new Vector2(0f, .3f);
                var point = trigger.AddComponent<AdventurePoint>(); point.Game = this; point.Target = npc; point.Area = areaIndex; point.Object = info.IsObject;

                // Benda sudah tergambar di latar: labelnya cukup setinggi papan, tanpa badan.
                float head = body.sprite ? body.sprite.bounds.max.y : info.IsObject ? .55f : 1f;
                var name = WorldLabel(root.transform, info.Name, head + .12f, .42f, PixelSkin.TextLight);
                name.outlineColor = PixelSkin.Outline; name.outlineWidth = .25f;
                var marker = WorldLabel(root.transform, "!", head + .42f, 1.1f, PixelSkin.Orange);
                marker.outlineColor = PixelSkin.Outline; marker.outlineWidth = .3f;
                _questMarkers[npc] = marker;
                _questNpcRoots[npc] = root;
                _questNpcAreas[npc] = areaIndex;
            }
            RefreshQuestMarkers();
        }

        bool TryFindNpcSpot(Vector2 desired, out Vector2 spot)
        {
            for (int ring = 0; ring < 8; ring++)
                for (int i = 0; i < (ring == 0 ? 1 : 16); i++)
                {
                    float angle = i * Mathf.PI / 8f;
                    spot = desired + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (ring * .2f);
                    if (WalkableArea.HasAreas && !WalkableArea.ContainsPoint(spot)) continue;
                    if (Physics2D.OverlapBoxAll(spot + new Vector2(0f, .1f), new Vector2(.4f, .22f), 0f).Any(c => !c.isTrigger)) continue;
                    return true;
                }
            spot = desired;
            return false;
        }

        static TMP_Text WorldLabel(Transform parent, string text, float y, float size, Color color)
        {
            var label = new GameObject("Label").AddComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = new Vector3(0f, y, 0f);
            if (PixelSkin.Font != null) label.font = PixelSkin.Font;
            label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3f, .6f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.GetComponent<MeshRenderer>().sortingOrder = YSortOrder.PromptOrderBase + 25;
            return label;
        }

        /// <summary>"!" = ada langkah untukmu; "?" = NPC menunggu barang yang belum kamu bawa.</summary>
        void RefreshQuestMarkers()
        {
            if (_questMarkers.Count == 0 || State == null) return;
            var inventory = InventorySystem.Instance;
            // Siapa yang hadir: gerbang cerita dulu (quest/hari kemunculan), lalu jadwal acak harian
            // per waktu (NpcSchedule) yang dijamin menyisakan minimal satu tokoh di tiap map.
            var present = ScheduledNpcs();
            foreach (var pair in _questNpcRoots)
                if (pair.Value) pair.Value.SetActive(present.Contains(pair.Key));
            foreach (var pair in _questMarkers)
            {
                if (!pair.Value) continue;
                var (_, step) = SideQuestRules.StepAt(State, SideQuestContent.All, pair.Key);
                bool waiting = step?.Kind == "give" && (!inventory || inventory.IndexOf(step.RequiredItem) < 0);
                pair.Value.gameObject.SetActive(step != null);
                pair.Value.text = waiting ? "?" : "!";
            }
        }

        // ───────────────────────── Interaksi ─────────────────────────

        void InteractQuestNpc(string npc)
        {
            if (TutorialActive) { Toast("Selesaikan atau lewati tutorial sebelum membantu warga", 4); return; }
            if (npc == CaseContent.Bed) { InteractBed(); return; }
            // Warga kota bisa sekaligus jadi sasaran tugas bab (Bu Tini dengan sewa kamar). Tugas
            // utama menang atas obrolan santai; Interact(nama) yang menangani dialog & papannya.
            var mainTask = CurrentTask;
            var resident = SideQuestContent.Npc(npc);
            if (mainTask != null && resident != null && mainTask.Target == resident.Name && mainTask.Area == State.Area)
            { Interact(resident.Name); return; }
            var intro = StoryContent.NpcIntro(npc);
            if (!StorySeen(intro)) { PlayStoryOnce(intro, () => InteractQuestNpc(npc)); return; }
            var info = SideQuestContent.Npc(npc);
            var (quest, step) = SideQuestRules.StepAt(State, SideQuestContent.All, npc);
            if (step == null) { SayLines(new[] { SideQuestRules.IdleLine(State, info) }, null); return; }

            if (step.Kind == "give")
            {
                var inventory = InventorySystem.Instance;
                int index = inventory ? inventory.IndexOf(step.RequiredItem) : -1;
                if (index < 0) { Toast($"{info.Name} menunggu: {step.RequiredItem}", 5); return; }
                ConfirmGive(quest, step, info, index);
                return;
            }
            if (step.Board != null) SayLines(step.Lines, () => { SideQuestRules.Prepare(State, quest); Save(false); OpenBoard(SideBoard(quest, step)); });
            else SayLines(step.Lines, () => FinishSideStep(quest, step));
        }

        void ConfirmGive(SideQuest quest, QuestStep step, QuestNpc npc, int slot)
        {
            ClearModal(CampaignActivity.Item);
            var panel = Panel(_modal, new Vector2(.3f, .2f), new Vector2(.7f, .8f), Color.white); ApplySprite(panel, PixelSkin.Panel());
            var p = panel.rectTransform;
            Text(p, $"Berikan {step.RequiredItem} kepada {npc.Name}?", new Vector2(.06f, .66f), new Vector2(.94f, .9f), 22).alignment = TextAlignmentOptions.Center;
            ItemTile(p, ItemCatalog.Icon(step.RequiredItem) ?? InventorySystem.Instance.Slots[slot].Icon, new Vector2(.5f, .47f), 104);
            Button(p, "Berikan", new Vector2(.08f, .08f), new Vector2(.48f, .24f), () =>
            {
                CloseModal();
                InventorySystem.Instance.RemoveItemAt(slot, 1);
                SayLines(step.Lines, () => FinishSideStep(quest, step));
            });
            Button(p, "Batal", new Vector2(.52f, .08f), new Vector2(.92f, .24f), CloseModal);
            FocusFirst();
        }

        BoardSession SideBoard(SideQuest quest, QuestStep step) => new BoardSession
        {
            Id = quest.Id, Title = step.Objective, Board = step.Board,
            Placements = () => SideQuestRules.Prepare(State, quest).Placements,
            Place = (card, slot) => (SideQuestRules.Place(State, quest, card, slot, out string feedback), feedback),
            Commit = () =>
            {
                if (!SideQuestRules.Commit(State, quest, out string feedback)) return (false, feedback);
                FinishSideStep(quest, step);
                return (true, feedback);
            },
        };

        /// <summary>Tutup langkah: majukan progres (papan sudah dimajukan oleh Commit), beri hadiah,
        /// jalankan dialog hasil, lalu kembali menjelajah.</summary>
        void FinishSideStep(SideQuest quest, QuestStep step)
        {
            if (_modal) CloseModal(); // kartu puzzle langkah ini
            if (step.Board == null && !SideQuestRules.Advance(State, quest, step.Target)) return;
            if (!string.IsNullOrEmpty(step.RewardItem)) InventorySystem.Instance?.AddItem(step.RewardItem, ItemCatalog.Icon(step.RewardItem), 1);
            if (step.Logic != 0) ScoreSystem.Instance?.AdjustFinancialLogic(step.Logic);
            if (step.Sharia != 0) ScoreSystem.Instance?.AdjustShariaCompliance(step.Sharia);
            SideQuestRules.AddBond(State, step.Target, step.Bond);
            if (!string.IsNullOrEmpty(step.Card)) UnlockCard(step.Card);
            bool questDone = SideQuestRules.Progress(State, quest.Id)?.Complete == true;
            Save(false);
            RefreshQuestMarkers();
            SayLines(step.Outcome, () =>
            {
                if (questDone && quest.Mandatory) Toast(SideQuestRules.CasesDone(State, Chapter) ? "Semua kasus warga selesai • kabari Bu Siti" : $"Kasus warga selesai • {quest.Title}", 7);
                else if (questDone) Toast($"Cerita sampingan selesai • {quest.Title}", 6);
                else if (step.Bond > 0 && string.IsNullOrEmpty(step.Card)) Toast($"Hubungan dengan {SideQuestContent.Npc(step.Target)?.Name} bertambah", 4);
                SetPlaying();
            });
        }

        /// <summary>Percakapan multi-baris "Pembicara|teks". Tokoh tanpa CharacterData (NPC baru)
        /// tampil tanpa portrait, bukan memakai portrait Alif.</summary>
        void SayLines(string[] lines, Action after)
        {
            if (lines == null || lines.Length == 0) { after?.Invoke(); return; }
            _afterDialogue = after;
            if (_dialogue) Destroy(_dialogue);
            _dialogue = ScriptableObject.CreateInstance<DialogueData>();
            _dialogue.Lines = lines.Select(line =>
            {
                var parts = line.Split(new[] { '|' }, 2);
                var speaker = Cast.FirstOrDefault(c => c && c.CharacterName.Equals(parts[0], StringComparison.OrdinalIgnoreCase));
                return new DialogueLine { SpeakerName = parts[0], Text = parts.Length > 1 ? parts[1] : "", SpeakerData = speaker };
            }).ToList();
            DialogueManager.Instance.StartDialogue(_dialogue, _dialogue.Lines[0].SpeakerData);
        }

        // ───────────────────────── Jurnal ─────────────────────────

        IEnumerable<string> SideQuestJournalEntries()
        {
            foreach (var quest in SideQuestContent.All)
            {
                var progress = SideQuestRules.Progress(State, quest.Id);
                bool unlocked = quest.Requires.All(id => SideQuestRules.Progress(State, id)?.Complete == true);
                if ((!unlocked || State.Day < quest.Day) && progress == null) continue;
                string kind = quest.Mandatory ? "KASUS WARGA" : "SAMPINGAN";
                if (progress?.Complete == true) { yield return $"✓  {kind}  /  {quest.Title} — selesai"; continue; }
                var step = quest.Steps[progress?.Step ?? 0];
                yield return $">  {kind}  /  {quest.Title} — {step.Objective} ({SideQuestContent.Npc(step.Target)?.Name})";
            }
            foreach (var npc in SideQuestContent.Npcs)
            {
                int hearts = SideQuestRules.Hearts(State, npc.Id);
                if (hearts > 0) yield return $"HUBUNGAN  /  {npc.Name}  " + HeartText(hearts);
            }
        }
    }
}
