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
        // Warna pembeda sprite placeholder (dipinjam dari tokoh lama) sampai sprite final ada.
        static readonly Dictionary<string, Color> PlaceholderTints = new Dictionary<string, Color>
        {
            [SideQuestContent.Kirana] = new Color(1f, .86f, 1f),
            [SideQuestContent.Bank] = new Color(.84f, 1f, .88f),
        };
        CityWorld.Built _city;

        /// <summary>Kota Cempaka (6 map) ditambahkan setelah area scene; harus dibangun sebelum
        /// pemain ditempatkan agar AreaOf/Spawns mengenali posisi di kota.</summary>
        void BuildCity()
        {
            _city = CityWorld.Build();
            Centers = Centers.Concat(_city.Centers).ToArray();
            Spawns = Spawns.Concat(_city.Spawns).ToArray();
            if (Centers.Length != Content.Areas.Length)
                Debug.LogError($"[Alif] Jumlah area tidak cocok: {Centers.Length} pusat vs {Content.Areas.Length} nama area.");
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
                Color tint = PlaceholderTints.TryGetValue(npc, out Color t) ? t : Color.white;
                if (!TryFindNpcSpot(position, out Vector2 spot))
                {
                    Debug.LogWarning($"[Alif] NPC quest {info.Name} tidak dimunculkan: tidak ada lantai bebas di sekitar {position}.");
                    continue;
                }

                var root = new GameObject("QuestNpc " + info.Name);
                root.transform.position = spot;
                var body = root.AddComponent<SpriteRenderer>();
                var data = Cast.FirstOrDefault(c => c && c.name.EndsWith(info.Placeholder, StringComparison.OrdinalIgnoreCase));
                body.sprite = data ? (data.WorldSprite ? data.WorldSprite : data.Portrait) : null;
                body.color = tint;
                root.AddComponent<YSortOrder>();
                BlobShadow.Ensure(root.transform);
                var feet = root.AddComponent<CapsuleCollider2D>();
                feet.direction = CapsuleDirection2D.Horizontal; feet.size = new Vector2(.3f, .18f); feet.offset = new Vector2(0f, .09f);

                var trigger = new GameObject("Adventure interaction") { layer = 7 };
                trigger.transform.SetParent(root.transform, false);
                var circle = trigger.AddComponent<CircleCollider2D>(); circle.isTrigger = true; circle.radius = .45f; circle.offset = new Vector2(0f, .3f);
                var point = trigger.AddComponent<AdventurePoint>(); point.Game = this; point.Target = npc; point.Area = areaIndex;

                float head = body.sprite ? body.sprite.bounds.max.y : 1f;
                var name = WorldLabel(root.transform, info.Name, head + .12f, .42f, PixelSkin.TextLight);
                name.outlineColor = PixelSkin.Outline; name.outlineWidth = .25f;
                var marker = WorldLabel(root.transform, "!", head + .42f, 1.1f, PixelSkin.Orange);
                marker.outlineColor = PixelSkin.Outline; marker.outlineWidth = .3f;
                _questMarkers[npc] = marker;
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
            var info = SideQuestContent.Npc(npc);
            var (quest, step) = SideQuestRules.StepAt(State, SideQuestContent.All, npc);
            if (step == null) { SayLines(new[] { info.Idle }, null); return; }

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
            bool questDone = SideQuestRules.Progress(State, quest.Id)?.Complete == true;
            Save(false);
            RefreshQuestMarkers();
            SayLines(step.Outcome, () =>
            {
                if (questDone) Toast($"Cerita sampingan selesai • {quest.Title}", 6);
                else if (step.Bond > 0) Toast($"Hubungan dengan {SideQuestContent.Npc(step.Target)?.Name} bertambah", 4);
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
                if (!unlocked && progress == null) continue;
                if (progress?.Complete == true) { yield return $"✓  SAMPINGAN  /  {quest.Title} — selesai"; continue; }
                var step = quest.Steps[progress?.Step ?? 0];
                yield return $">  SAMPINGAN  /  {quest.Title} — {step.Objective} ({SideQuestContent.Npc(step.Target)?.Name})";
            }
            foreach (var npc in SideQuestContent.Npcs)
            {
                int hearts = SideQuestRules.Hearts(State, npc.Id);
                if (hearts > 0) yield return $"HUBUNGAN  /  {npc.Name}  " + new string('♥', hearts) + new string('♡', SideQuestRules.MaxHearts - hearts);
            }
        }
    }
}
