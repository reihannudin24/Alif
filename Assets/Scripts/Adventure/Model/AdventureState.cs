using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Alif.Adventure
{
    [Serializable] public sealed class PuzzleStep
    {
        public string Prompt, Evidence, Explanation;
        public string[] Options;
        public int Answer;
        public PuzzleStep(string prompt, string evidence, string explanation, int answer, params string[] options)
        { Prompt = prompt; Evidence = evidence; Explanation = explanation; Answer = answer; Options = options; }
    }
    [Serializable] public sealed class AdventureTask
    {
        public string Id, Title, Target, Speaker, Introduction, Outcome, Kind;
        public int Area, Cost;
        public ActivityBoard Board;
        public string[] Prerequisites = Array.Empty<string>();
        public PuzzleStep[] Steps = Array.Empty<PuzzleStep>();
    }
    public sealed class AdventureChapter
    {
        public int Number, StartArea;
        public string Title, Subtitle, Introduction, Ending;
        public string[] Areas, Optional;
        public AdventureTask[] Tasks;
    }
    [Serializable] public sealed class TaskProgress
    {
        public string Id;
        public int Step;
        public bool Complete;
        public List<int> Choices = new List<int>();
        public List<int> Placements = new List<int>();
    }
    [Serializable] public sealed class AdventureState
    {
        public int Version = 3, Chapter = 1, Area, Money = 100000, Bank = 1000000, HighestUnlocked = 1;
        public int TutorialStep;
        [NonSerialized] public bool ReadOnlySave;
        public bool HasPosition;
        public float X = 0, Y = -2.5f;
        public bool IntroductionSeen, Completed;
        public List<int> CompletedChapters = new List<int>();
        public List<TaskProgress> Tasks = new List<TaskProgress>();
        public List<string> Evidence = new List<string>();
        public List<string> Discoveries = new List<string>();
        // Lintas bab: side quest, isi tas, hubungan NPC, dan pickup dunia yang sudah diambil.
        public List<SideQuestProgress> SideQuests = new List<SideQuestProgress>();
        public List<ItemStack> Items = new List<ItemStack>();
        public List<NpcBond> Bonds = new List<NpcBond>();
        public List<string> PickedUp = new List<string>();
        public float PlaySeconds;
        public TaskProgress Progress(string id) => Tasks.Find(t => t.Id == id);
        public bool CanStart(AdventureTask task) => !Completed && task.Prerequisites.All(id => Progress(id)?.Complete == true);
        public bool Accept(AdventureTask task, int option, out string feedback)
        {
            feedback = "Selesaikan tugas sebelumnya dahulu.";
            if (task.Kind == "encounter" || task.Board != null || !CanStart(task)) return false;
            var p = Progress(task.Id);
            if (p == null) { p = new TaskProgress { Id = task.Id, Placements = task.Board == null ? new List<int>() : Enumerable.Repeat(-1,task.Board.Cards.Length).ToList() }; Tasks.Add(p); }
            if (p.Complete) { feedback = "Sudah selesai; hasilnya tersimpan di jurnal."; return false; }
            if (p.Step < task.Steps.Length)
            {
                var step = task.Steps[p.Step];
                if (option < 0 || option >= step.Options.Length) return false;
                feedback = step.Explanation;
                if (option != step.Answer) return false;
                p.Choices.Add(option); p.Step++;
                var note = task.Title + ": " + step.Explanation;
                if (!Evidence.Contains(note)) Evidence.Add(note);
            }
            if (p.Step == task.Steps.Length)
            {
                if (Money < task.Cost) { feedback = "Saldo tidak cukup. Pilih penyelesaian tanpa biaya tambahan bersama Bu Siti."; return false; }
                Money -= task.Cost;
                p.Complete = true;
                feedback = task.Outcome;
            }
            return true;
        }
        public TaskProgress PrepareBoard(AdventureTask task)
        {
            var p=Progress(task.Id);
            if(p==null){p=new TaskProgress {Id=task.Id, Placements=Enumerable.Repeat(-1,task.Board.Cards.Length).ToList()};Tasks.Add(p);}
            return p;
        }
        public bool Place(AdventureTask task,int card,int slot,out string feedback)
        {
            feedback="Selesaikan tugas sebelumnya.";
            if(task.Board==null||!CanStart(task)||card<0||card>=task.Board.Cards.Length||slot<0||slot>=(task.Board.Kind=="budget"?2:task.Board.Slots.Length))return false;
            var p=PrepareBoard(task);if(p.Complete)return false;
            if(task.Board.Kind!="budget" && p.Placements[card]==task.Board.Answers[card]){feedback="Bukti ini sudah tepat.";return false;}
            if(task.Board.Kind!="budget" && slot!=task.Board.Answers[card]){feedback=task.Board.Notes[card];return false;}
            p.Placements[card]=slot;feedback=task.Board.Kind=="budget"?"Alokasi diperbarui; periksa total sebelum menyetujui.":task.Board.Notes[card];
            if(task.Board.Kind!="budget"&&!Evidence.Contains(feedback))Evidence.Add(feedback);
            return true;
        }
        public bool CommitBoard(AdventureTask task,out string feedback)
        {
            feedback="Lengkapi papan terlebih dahulu.";
            var p=Progress(task.Id);
            if(!CanStart(task)||p==null||p.Complete||!task.Board.Solved(p.Placements))return false;
            if(Money+Bank<task.Cost){feedback="Dana belum cukup; bicarakan bantuan sebelum membayar.";return false;}
            int fromBank=Math.Max(0,task.Cost-Money);Bank-=fromBank;Money+=fromBank-task.Cost;
            p.Step=task.Steps.Length;p.Choices=task.Steps.Select(x=>x.Answer).ToList();p.Complete=true;
            foreach(string note in task.Board.Notes)if(!Evidence.Contains(note))Evidence.Add(note);
            feedback=task.Outcome;return true;
        }
        public bool CompleteEncounter(string taskId)
        {
            var task = AdventureContent.Get(Chapter).Tasks.FirstOrDefault(t => t.Id == taskId && t.Kind == "encounter");
            if (task == null || !CanStart(task) || Progress(taskId)?.Complete == true) return false;
            Tasks.Add(new TaskProgress { Id = taskId, Complete = true });
            return true;
        }
        public void Finish(AdventureChapter chapter)
        {
            if (chapter.Number != Chapter || chapter.Tasks.Any(t => Progress(t.Id)?.Complete != true)) return;
            Completed = true;
            if (!CompletedChapters.Contains(Chapter)) CompletedChapters.Add(Chapter);
            HighestUnlocked = Math.Max(HighestUnlocked, Math.Min(5, Chapter + 1));
        }
        public AdventureState StartChapter(int chapter)
        {
            if (chapter < 1 || chapter > HighestUnlocked) throw new ArgumentOutOfRangeException(nameof(chapter));
            return new AdventureState { Chapter = chapter, Area = AdventureContent.Get(chapter).StartArea, HighestUnlocked = HighestUnlocked,
                CompletedChapters = new List<int>(CompletedChapters), ReadOnlySave = ReadOnlySave,
                SideQuests = SideQuests.ConvertAll(q => new SideQuestProgress { Id = q.Id, Step = q.Step, Complete = q.Complete, Placements = new List<int>(q.Placements) }),
                Items = Items.ConvertAll(i => new ItemStack { Name = i.Name, Quantity = i.Quantity }),
                Bonds = Bonds.ConvertAll(b => new NpcBond { Npc = b.Npc, Hearts = b.Hearts }),
                PickedUp = new List<string>(PickedUp) };
        }
        public bool Valid()
        {
            if (Version != 3 || Chapter < 1 || Chapter > 5 || HighestUnlocked < Chapter || HighestUnlocked > 5 || TutorialStep < 0 || TutorialStep > 3 ||
                Area < 0 || Area >= AdventureContent.Get(Chapter).Areas.Length || Money < 0 || Money > 2000000 || Bank < 0 || Bank > 2000000 || !float.IsFinite(X) || !float.IsFinite(Y) ||
                Math.Abs(X) > 500 || Math.Abs(Y) > 500 || !float.IsFinite(PlaySeconds) || PlaySeconds < 0 ||
                Tasks == null || Evidence == null || Discoveries == null || CompletedChapters == null || !SideQuestRules.Valid(this)) return false;
            var chapter = AdventureContent.Get(Chapter);
            if (TutorialStep > 0 && (Chapter != 1 || Area != chapter.StartArea || IntroductionSeen || Completed || Tasks.Count > 0)) return false;
            if (Tasks.Any(t => t == null || t.Id == null) || Tasks.Select(t => t.Id).Distinct().Count() != Tasks.Count) return false;
            foreach (var p in Tasks)
            {
                var task = chapter.Tasks.FirstOrDefault(t => t.Id == p.Id);
                if (task == null || p.Step < 0 || p.Step > task.Steps.Length || p.Choices == null || p.Choices.Count != p.Step ||
                    (p.Complete && p.Step != task.Steps.Length) || !task.Prerequisites.All(id => Progress(id)?.Complete == true || chapter.Tasks.Any(t => t.Id == id && t.Kind == "encounter"))) return false;
                if (p.Placements == null) return false;
                if (task.Board != null && (p.Placements.Count != task.Board.Cards.Length || p.Placements.Any(v => v < -1 || v >= (task.Board.Kind == "budget" ? 2 : task.Board.Slots.Length)) || (p.Complete && !task.Board.Solved(p.Placements)))) return false;
                for (int i = 0; i < p.Step; i++) if (p.Choices[i] != task.Steps[i].Answer) return false;
            }
            return CompletedChapters.All(c => c >= 1 && c <= 5 && c <= HighestUnlocked) &&
                (!Completed || chapter.Tasks.All(t => Progress(t.Id)?.Complete == true));
        }
    }
    public static class AdventureSave
    {
        public const string Key = "Alif_Adventure_v3", BackupKey = "Alif_Adventure_v3_backup";
        public const string LegacyKey = "Alif_Adventure_v2", LegacyBackupKey = "Alif_Adventure_v2_backup";
        public const string RecoveryKey = "Alif_Adventure_v3_unreadable";
        public static AdventureState Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var s = JsonUtility.FromJson<AdventureState>(json);
                if (s == null || (s.Version != 2 && s.Version != 3)) return null;
                if (s.Version == 2)
                {
                    s.Version = 3;
                    // Completed saves keep earned completion. A new replay starts with no encounter result.
                    if (s.Completed && s.Chapter >= 2 && s.Chapter <= 5 && s.Tasks != null && s.Progress("c" + s.Chapter + ".encounter") == null)
                        s.Tasks.Add(new TaskProgress { Id = "c" + s.Chapter + ".encounter", Complete = true });
                }
                return s.Valid() ? s : null;
            }
            catch (Exception) { return null; }
        }
        static bool FutureVersion(string json)
        {
            try { return !string.IsNullOrEmpty(json) && JsonUtility.FromJson<AdventureState>(json)?.Version > 3; }
            catch (Exception) { return false; }
        }
        public static AdventureState Load(out string notice)
        {
            notice = "";
            string primary = PlayerPrefs.GetString(Key, "");
            bool future = FutureVersion(primary);
            var state = future ? null : Decode(primary);
            if (state != null) return state;
            if (!future)
            {
                state = Decode(PlayerPrefs.GetString(BackupKey, ""));
                if (state != null) { notice = "Progres dipulihkan dari cadangan; checkpoint lama tetap disimpan."; return state; }
                if (!PlayerPrefs.HasKey(Key))
                {
                    state = Decode(PlayerPrefs.GetString(LegacyKey, "")) ?? Decode(PlayerPrefs.GetString(LegacyBackupKey, ""));
                    if (state != null) { notice = "Checkpoint diperbarui; progres dan bab terbuka dipertahankan."; return state; }
                }
            }
            int highest = Mathf.Clamp(PlayerPrefs.GetInt("Alif_HighestChapterUnlocked", 1), 1, 5);
            state = new AdventureState { HighestUnlocked = highest, Chapter = Mathf.Clamp(PlayerPrefs.GetInt("Alif_SelectedChapter", 1), 1, highest) };
            for (int c = 1; c <= 5; c++) if (PlayerPrefs.GetInt($"Alif_Chapter_{c}_Completed", 0) == 1) state.CompletedChapters.Add(c);
            state.Area = AdventureContent.Get(state.Chapter).StartArea;
            state.ReadOnlySave = PlayerPrefs.HasKey(Key) || PlayerPrefs.HasKey(BackupKey) || PlayerPrefs.HasKey(LegacyKey) || PlayerPrefs.HasKey(LegacyBackupKey);
            if (state.ReadOnlySave) notice = "Checkpoint tidak didukung atau rusak. Simpan dinonaktifkan agar data lama tetap aman. Gunakan Main Baru untuk memulai ulang.";
            return state;
        }
        public static bool Store(AdventureState state)
        {
            if (state == null || state.ReadOnlySave || !state.Valid()) return false;
            try
            {
                string old = PlayerPrefs.GetString(Key, "");
                if (FutureVersion(old)) return false;
                if (Decode(old) != null) PlayerPrefs.SetString(BackupKey, old);
                else if (!string.IsNullOrEmpty(old))
                {
                    if (PlayerPrefs.HasKey(RecoveryKey) && PlayerPrefs.GetString(RecoveryKey) != old) return false;
                    PlayerPrefs.SetString(RecoveryKey, old);
                }
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(state));
                PlayerPrefs.SetInt("Alif_HasSave", 1);
                PlayerPrefs.SetInt("Alif_HighestChapterUnlocked", state.HighestUnlocked);
                PlayerPrefs.SetInt("Alif_SelectedChapter", state.Chapter);
                foreach (int c in state.CompletedChapters) PlayerPrefs.SetInt($"Alif_Chapter_{c}_Completed", 1);
                PlayerPrefs.Save(); return true;
            }
            catch (Exception e) { Debug.LogWarning("Checkpoint belum tersimpan: " + e.Message); return false; }
        }
    }
}
