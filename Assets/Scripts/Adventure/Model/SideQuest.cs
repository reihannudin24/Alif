using System;
using System.Collections.Generic;
using System.Linq;

namespace Alif.Adventure
{
    /// <summary>
    /// Satu langkah side quest. Jenis: "talk" (bicara), "give" (serahkan barang ke Target),
    /// atau jenis papan puzzle (budget/match/sort/flow/inspect) yang memakai <see cref="Board"/>.
    /// Target adalah id titik interaksi, mis. "npc:kirana".
    /// </summary>
    [Serializable] public sealed class QuestStep
    {
        public string Kind = "talk", Target, Objective;
        /// <summary>Dialog sebelum langkah dijalankan, format "Pembicara|teks" per baris.</summary>
        public string[] Lines = Array.Empty<string>();
        /// <summary>Dialog setelah langkah selesai, format sama dengan <see cref="Lines"/>.</summary>
        public string[] Outcome = Array.Empty<string>();
        public string RequiredItem, RewardItem;
        public ActivityBoard Board;
        public int Bond;
        public float Logic, Sharia;
    }

    public sealed class SideQuest
    {
        public string Id, Title, Theme, Summary;
        public string[] Requires = Array.Empty<string>();
        public QuestStep[] Steps = Array.Empty<QuestStep>();
    }

    /// <summary>NPC pemberi quest: nama tampilan, id titik interaksi, dan baris santai saat
    /// tidak ada langkah quest untuknya.</summary>
    public sealed class QuestNpc
    {
        public string Id, Name, Placeholder, Idle;
    }

    [Serializable] public sealed class SideQuestProgress
    {
        public string Id;
        public int Step;
        public bool Complete;
        public List<int> Placements = new List<int>();
    }

    [Serializable] public sealed class ItemStack
    {
        public string Name;
        public int Quantity;
    }

    [Serializable] public sealed class NpcBond
    {
        public string Npc;
        public int Hearts;
    }

    /// <summary>Aturan side quest yang tidak bergantung pada Unity (dipakai AdventureState & test).</summary>
    public static class SideQuestRules
    {
        public const int MaxHearts = 5;

        public static SideQuestProgress Progress(AdventureState state, string questId) =>
            state.SideQuests.Find(p => p.Id == questId);

        public static bool Available(AdventureState state, SideQuest quest) =>
            state.TutorialStep == 0 && Progress(state, quest.Id)?.Complete != true &&
            quest.Requires.All(id => Progress(state, id)?.Complete == true);

        public static QuestStep CurrentStep(AdventureState state, SideQuest quest)
        {
            if (!Available(state, quest)) return null;
            int step = Progress(state, quest.Id)?.Step ?? 0;
            return step < quest.Steps.Length ? quest.Steps[step] : null;
        }

        /// <summary>Langkah pertama (dari semua quest yang tersedia) yang dijalankan di titik ini.</summary>
        public static (SideQuest quest, QuestStep step) StepAt(AdventureState state, IEnumerable<SideQuest> quests, string target)
        {
            foreach (var quest in quests)
            {
                var step = CurrentStep(state, quest);
                if (step != null && step.Target == target) return (quest, step);
            }
            return (null, null);
        }

        public static SideQuestProgress Prepare(AdventureState state, SideQuest quest)
        {
            var p = Progress(state, quest.Id);
            if (p == null) { p = new SideQuestProgress { Id = quest.Id }; state.SideQuests.Add(p); }
            var step = p.Step < quest.Steps.Length ? quest.Steps[p.Step] : null;
            if (step?.Board != null && p.Placements.Count != step.Board.Cards.Length)
                p.Placements = Enumerable.Repeat(-1, step.Board.Cards.Length).ToList();
            return p;
        }

        /// <summary>Selesaikan langkah talk/give yang sedang aktif. Barang untuk give diperiksa
        /// pemanggil (inventory ada di runtime); di sini hanya urutan & status.</summary>
        public static bool Advance(AdventureState state, SideQuest quest, string target)
        {
            var step = CurrentStep(state, quest);
            if (step == null || step.Target != target || step.Board != null) return false;
            Complete(state, quest);
            return true;
        }

        public static bool Place(AdventureState state, SideQuest quest, int card, int slot, out string feedback)
        {
            feedback = "Langkah ini belum tersedia.";
            var step = CurrentStep(state, quest);
            var board = step?.Board;
            if (board == null || card < 0 || card >= board.Cards.Length || slot < 0 || slot >= (board.Kind == "budget" ? 2 : board.Slots.Length)) return false;
            var p = Prepare(state, quest);
            if (board.Kind != "budget" && p.Placements[card] == board.Answers[card]) { feedback = "Bagian ini sudah tepat."; return false; }
            if (board.Kind != "budget" && slot != board.Answers[card]) { feedback = board.Notes[card]; return false; }
            p.Placements[card] = slot;
            feedback = board.Kind == "budget" ? "Alokasi diperbarui; periksa total sebelum menyetujui." : board.Notes[card];
            return true;
        }

        public static bool Commit(AdventureState state, SideQuest quest, out string feedback)
        {
            feedback = "Lengkapi papan terlebih dahulu.";
            var step = CurrentStep(state, quest);
            var p = Progress(state, quest.Id);
            if (step?.Board == null || p == null || !step.Board.Solved(p.Placements)) return false;
            Complete(state, quest);
            feedback = "Aktivitas selesai.";
            return true;
        }

        static void Complete(AdventureState state, SideQuest quest)
        {
            var p = Prepare(state, quest);
            p.Step++;
            p.Placements = new List<int>();
            if (p.Step >= quest.Steps.Length) p.Complete = true;
            else Prepare(state, quest);
        }

        public static int Hearts(AdventureState state, string npc) => state.Bonds.Find(b => b.Npc == npc)?.Hearts ?? 0;

        public static void AddBond(AdventureState state, string npc, int hearts)
        {
            if (string.IsNullOrEmpty(npc) || hearts == 0) return;
            var bond = state.Bonds.Find(b => b.Npc == npc);
            if (bond == null) { bond = new NpcBond { Npc = npc }; state.Bonds.Add(bond); }
            bond.Hearts = Math.Max(0, Math.Min(MaxHearts, bond.Hearts + hearts));
        }

        /// <summary>Validasi save: id & langkah harus cocok dengan konten yang ada.</summary>
        public static bool Valid(AdventureState state)
        {
            if (state.SideQuests == null || state.Items == null || state.Bonds == null || state.PickedUp == null) return false;
            if (state.SideQuests.Any(p => p == null || p.Id == null || p.Placements == null)) return false;
            if (state.SideQuests.Select(p => p.Id).Distinct().Count() != state.SideQuests.Count) return false;
            foreach (var p in state.SideQuests)
            {
                var quest = SideQuestContent.Find(p.Id);
                if (quest == null || p.Step < 0 || p.Step > quest.Steps.Length || p.Complete != (p.Step == quest.Steps.Length)) return false;
            }
            return state.Items.All(i => i != null && !string.IsNullOrEmpty(i.Name) && i.Quantity > 0) &&
                state.Bonds.All(b => b != null && !string.IsNullOrEmpty(b.Npc) && b.Hearts >= 0 && b.Hearts <= MaxHearts);
        }
    }
}
