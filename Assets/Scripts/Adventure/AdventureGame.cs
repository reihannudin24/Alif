using System;
using System.Collections.Generic;
using System.Linq;
using Alif.Campaign;
using Alif.Core;
using Alif.Dialogue;
using Alif.Player;
using Alif.Systems;
using Alif.Characters;
using Alif.World;
using Alif.UI;
using Alif.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Alif.Adventure
{
    public enum CampaignActivity { World, Dialogue, Puzzle, Journal, Pause, Ending, Combat }
    public sealed class AdventureGame : MonoBehaviour
    {
        public int Chapter;
        public Vector2[] Centers, Spawns;
        public CharacterData[] Cast;
        public Sprite InteractionArrow, DocumentIcon, PromotionIcon;
        List<SceneDoor> _doors=new List<SceneDoor>();
        int _selectedCard=-1;
        public Action<string> ObjectiveCompleted;
        public static readonly Color Ink=new Color(.07f,.10f,.14f,.94f), Paper=new Color(.96f,.93f,.85f), Gold=new Color(.85f,.74f,.48f), Teal=new Color(.18f,.30f,.32f);
        public static AdventureGame Instance { get; private set; }
        public AdventureState State { get; private set; }
        public AdventureChapter Content { get; private set; }
        public CampaignActivity Activity { get; private set; } = CampaignActivity.World;
        public string ScreenName => Activity.ToString().ToLowerInvariant();
        public BattleEncounterDefinition DanaKilat;
        public ActionEncounterDefinition ActionEncounter;
        ICampaignEncounter _encounter;
        MonoBehaviour _encounterComponent;
        public ICampaignEncounter ActiveEncounter => _encounter;
        public string CurrentTaskId => CurrentTask?.Id ?? "";
        public int PuzzleStepIndex => _task == null ? -1 : State.Progress(_task.Id)?.Step ?? 0;
        AdventureTask CurrentTask => Content?.Tasks.FirstOrDefault(t => State.Progress(t.Id)?.Complete != true);
        RectTransform _canvas, _modal, _hud;
        TMP_Text _objective, _location, _toast, _prompt, _guide;
        PlayerController _player;
        List<AdventurePoint> _points = new List<AdventurePoint>();
        List<Button> _buttons = new List<Button>();
        AdventureTask _task;
        int _hint;
        float _toastUntil, _openedAt, _savedAt, _motionTime;
        float _textScale => PlayerPrefs.GetInt("Alif_LargeText",0)==1 ? 1.15f : 1f;
        Action _afterDialogue;
        DialogueData _dialogue;
        bool _transition, _paused;
        Vector2? _walkTarget;
        Vector2 _lastWalkPosition;
        float _stuckTime;
        CampaignActivity _resumeScreen;
        int _journalPage;
        const string MenuScene = "MainMenu";
        public static string SceneFor(int chapter) => "AdventureChapter"+chapter;
        void Start()
        {
            if (Chapter == 0) { SceneManager.LoadScene(MenuScene); return; }
            Instance=this;
            Application.targetFrameRate=60;
            Time.timeScale=1;
            if(GameManager.Instance==null) new GameObject("Adventure managers").AddComponent<GameManager>();
            if(DialogueManager.Instance==null) new GameObject("Adventure dialogue").AddComponent<DialogueManager>();
            if(EventSystem.current==null) {var e=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));e.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            _canvas=CampaignUI.Canvas("Alif • Adventure",50);
            State=ChapterProgress.LoadAdventure(out string notice);

            if(Chapter > State.HighestUnlocked) {SceneManager.LoadScene(MenuScene);return;}
            if(State.Chapter!=Chapter) State=State.StartChapter(Chapter);
            Content=AdventureContent.Get(Chapter);
            BindOriginalScene(); BuildHud();
            DialogueManager.Instance.OnLineDisplayed+=ShowDialogueLine;
            DialogueManager.Instance.OnDialogueEnded+=ExternalDialogueEnded;
            DialogueManager.Instance.OnDialogueCompleted+=DialogueCompleted;
            if(!string.IsNullOrEmpty(notice)) Toast(notice,10);
            if(State.Completed) {ShowEnding();return;}
            if(!State.IntroductionSeen)
                Say("Alif",Content.Introduction,()=>{State.IntroductionSeen=true;Save();},"Mulai menjelajah");
            else {SetPlaying();Toast(string.IsNullOrEmpty(notice)?"Checkpoint dimuat • "+Content.Areas[State.Area]:notice,8);}
        }
        void BindOriginalScene()
        {
            _player=FindFirstObjectByType<PlayerController>();
            if(!_player)throw new InvalidOperationException("The original scene needs its Player.");
            _points=FindObjectsByType<AdventurePoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).ToList();
            foreach(var point in _points)point.Game=this;
            _doors=FindObjectsByType<Alif.World.SceneDoor>(FindObjectsSortMode.None).ToList();
            foreach(var door in _doors) { if (!door.Destination) throw new InvalidOperationException("Door destination missing: " + door.name); door.Bind(this,AreaOf(door.transform.position),AreaOf(door.Destination.position)); }
            Physics2D.SyncTransforms();
            Vector2 position=State.HasPosition && AreaOf(new Vector2(State.X,State.Y))==State.Area && SafePosition(new Vector2(State.X,State.Y))
                ?new Vector2(State.X,State.Y):Spawns[State.Area];
            _player.transform.position=position;_player.GetComponent<Rigidbody2D>().position=position;
            Camera.main.GetComponent<CameraFollow>()?.SetTarget(_player.transform);
            Camera.main.GetComponent<CameraFollow>()?.SnapToTarget();
            CurrencySystem.Instance?.RestoreBalances(State.Money,State.Bank);
        }
        public int AreaOf(Vector2 position)
        {
            int result=0;float distance=float.MaxValue;
            for(int i=0;i<Centers.Length;i++){float d=Vector2.SqrMagnitude(position-Centers[i]);if(d<distance){distance=d;result=i;}}
            return result;
        }
        bool SafePosition(Vector2 position)
        {
            var feet=position+new Vector2(0,-.34f);
            return !Physics2D.OverlapCircleAll(feet,.1f).Any(c=>!c.isTrigger&&c.gameObject!=_player.gameObject&&c.gameObject.layer!=7);
        }
        public bool CanExplore => Activity==CampaignActivity.World&&!_transition&&!_paused && !(DialogueManager.Instance?.IsDialogueActive??false);
        void BuildHud()
        {
            if(_hud)Destroy(_hud.gameObject);
            _hud=CampaignUI.Rect(_canvas,"Directions",Vector2.zero,Vector2.one);
            var top=Panel(_hud,new Vector2(.25f,.88f),new Vector2(.75f,.98f));top.raycastTarget=false;
            _location=Text(_hud,"",new Vector2(.26f,.94f),new Vector2(.74f,.975f),14,Gold);
            _objective=Text(_hud,"",new Vector2(.26f,.886f),new Vector2(.74f,.94f),19);
            Button(_hud,"J  Jurnal",new Vector2(.78f,.9f),new Vector2(.89f,.965f),()=>Journal(0));
            Button(_hud,"Jeda",new Vector2(.9f,.9f),new Vector2(.99f,.965f),Pause);
            _prompt=Text(_hud,"",new Vector2(.25f,.17f),new Vector2(.75f,.22f),18);_prompt.alignment=TextAlignmentOptions.Center;
            _guide=Text(_hud,"",new Vector2(.25f,.835f),new Vector2(.75f,.877f),16,Gold);_guide.alignment=TextAlignmentOptions.Center;
            _toast=Text(_hud,"",new Vector2(.22f,.23f),new Vector2(.78f,.28f),17);_toast.alignment=TextAlignmentOptions.Center;
            RefreshHud();
        }
        void RefreshHud()
        {
            if(!_objective)return;
            var t=CurrentTask;int done=Content.Tasks.Count(x=>State.Progress(x.Id)?.Complete==true);
            _location.text=$"BAB {Chapter}  /  {Content.Title}  •  {Content.Areas[State.Area]}";
            _objective.text=t==null?"Perjalanan bab selesai":$"{done+1}/{Content.Tasks.Length}   {t.Title}";
            foreach(var p in _points)
            {
                bool active=t!=null && t.Target==p.Target && t.Area==p.Area;
                if(p.Label){p.Label.text=p.Target;p.Label.gameObject.SetActive(active&&p.Area==State.Area);}
            }
        }
        void Update()
        {
            var k=Keyboard.current;
            if(k!=null && k.tabKey.wasPressedThisFrame) CycleFocus((k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)?-1:1);
            if(Chapter==0 || State==null)return;
            if(_toast && Time.unscaledTime>_toastUntil)_toast.text="";
            if(k!=null && k.escapeKey.wasPressedThisFrame){if(_paused)Resume();else if(Activity==CampaignActivity.Journal)CloseModal();else Pause();return;}
            if(Activity!=CampaignActivity.World)return;
            State.PlaySeconds+=Time.unscaledDeltaTime;
            if(k!=null && k.jKey.wasPressedThisFrame){Journal(0);return;}
            var pos=(Vector2)_player.transform.position;
            State.Area=AreaOf(pos);
            var collider=_player.InteractionTarget;
            var nearest=collider?collider.GetComponentInParent<AdventurePoint>():null;
            bool near=nearest!=null;
            _prompt.text=near?"E • "+nearest.Target:"";
            var target=CurrentTask;
            Transform guide=null;
            if(target!=null)
            {
                guide=target.Area==State.Area?_points.Find(p=>p.Target==target.Target&&p.Area==target.Area)?.transform:NextDoor(target.Area)?.transform;
                if(guide){Vector2 delta=(Vector2)guide.position-pos;_guide.text=(Mathf.Abs(delta.x)>Mathf.Abs(delta.y)?delta.x>0?"Kanan → ":"← Kiri ":delta.y>0?"Atas ↑ ":"Bawah ↓ ")+(target.Area==State.Area?target.Target:"Pintu ke "+Content.Areas[NextDoor(target.Area).DestinationArea]);}
            }
            if(Time.unscaledTime-_savedAt>20)Save(false);
        }
        SceneDoor NextDoor(int targetArea)
        {
            var queue=new Queue<int>();var first=new Dictionary<int,SceneDoor>();queue.Enqueue(State.Area);first[State.Area]=null;
            while(queue.Count>0){int from=queue.Dequeue();foreach(var d in _doors.Where(d=>d.SourceArea==from))
            {if(first.ContainsKey(d.DestinationArea))continue;first[d.DestinationArea]=first[from]??d;if(d.DestinationArea==targetArea)return first[d.DestinationArea];queue.Enqueue(d.DestinationArea);}}
            return null;
        }
        void CycleFocus(int direction)
        {
            var visible=_buttons.Where(b=>b&&b.gameObject.activeInHierarchy&&b.interactable).ToList();if(visible.Count==0)return;
            int i=visible.FindIndex(b=>b.gameObject==EventSystem.current.currentSelectedGameObject);
            CampaignUI.Focus(visible[(i+direction+visible.Count)%visible.Count]);
        }
        public void Interact(string target)
        {
            if(!CanExplore)return;
            var t=CurrentTask;
            if(t==null){ShowEnding();return;}
            if(t.Target!=target||t.Area!=State.Area){Toast("Tugas berikutnya: "+t.Title,5);return;}
            _task=t;
            if (t.Kind == "encounter") { Say(t.Speaker,t.Introduction,BeginEncounter); return; }
            Say(t.Speaker,t.Introduction,()=>{if(t.Board!=null){_hint=0;_selectedCard=-1;State.PrepareBoard(t);Save();ShowPuzzle();}else CompleteTalk(t);});
        }
        void CompleteTalk(AdventureTask task)
        {
            if(State.Accept(task,0,out string feedback)) {Save();ObjectiveCompleted?.Invoke(task.Id);RefreshHud();}
            Say(task.Speaker,feedback,()=>{State.Finish(Content);Save();if(State.Completed)ShowEnding();else SetPlaying();});
        }
        public void Discover()
        {
            if(Activity!=CampaignActivity.World)return;
            var line=Content.Optional[Mathf.Min(State.Area,Content.Optional.Length-1)].Split('|');
            Say(line[0],line[1],()=>{string id=$"c{Chapter}.optional.{State.Area}";if(!State.Discoveries.Contains(id))State.Discoveries.Add(id);Save();});
        }
        public void Travel(SceneDoor door)
        {
            if(!CanExplore)return;
            _transition=true;
            SceneFadeController.Instance.TransitionTo(_player,_player.GetComponent<Rigidbody2D>(),door.Destination,Camera.main.GetComponent<CameraFollow>(),()=>
            {
                State.Area=door.DestinationArea;_transition=false;Save();RefreshHud();Toast("Memasuki"+" "+Content.Areas[State.Area],4);
            });
        }
        public void Travel(int area)
        {
            var door = NextDoor(area);
            if (door != null) Travel(door);
        }
        public bool Save(bool show=true)
        {
            if(_player){State.X=_player.transform.position.x;State.Y=_player.transform.position.y;State.Area=AreaOf(_player.transform.position);State.HasPosition=true;}
            if(CurrencySystem.Instance){State.Money=CurrencySystem.Instance.CurrentMoney;State.Bank=CurrencySystem.Instance.BankBalance;}
            bool ok=ChapterProgress.SaveAdventure(State);_savedAt=Time.unscaledTime;
            if(show)Toast(ok?"Checkpoint tersimpan":"Checkpoint belum tersimpan. Coba lagi lewat menu jeda.",ok?3:10);
            return ok;
        }
        void OnApplicationPause(bool paused){if(paused&&Chapter>0&&State!=null)Save(false);}
        void OnApplicationFocus(bool focused){if(!focused&&Chapter>0&&State!=null)Save(false);}
        void OnApplicationQuit(){if(Chapter>0&&State!=null)Save(false);}
        void SetPlaying(){Activity=CampaignActivity.World;_player?.SetMovementLocked(this, false);GameManager.Instance?.SetState(GameManager.GameState.Playing);RefreshHud();}
        void Toast(string message,float seconds=3){if(_toast){_toast.text=message;_toastUntil=Time.unscaledTime+seconds;}}
        void ClearModal(CampaignActivity screen)
        {
            if(_modal){_modal.gameObject.SetActive(false);Destroy(_modal.gameObject);}
            _buttons.Clear();_modal=CampaignUI.Rect(_canvas,screen.ToString(),Vector2.zero,Vector2.one);Activity=screen;_openedAt=Time.unscaledTime;
            _player?.SetMovementLocked(this, true);_player?.SetAdventureDirection(Vector2.zero);_walkTarget=null;
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
            var shade=Panel(_modal,Vector2.zero,Vector2.one);shade.color=new Color(.08f,.13f,.16f,.73f);
        }
        void CloseModal(){if(_modal){_modal.gameObject.SetActive(false);Destroy(_modal.gameObject);}_buttons.Clear();SetPlaying();}
        RectTransform Card(CampaignActivity screen,string eyebrow,string title)
        {
            ClearModal(screen);
            var p=Panel(_modal,new Vector2(.075f,.12f),new Vector2(.925f,.88f)).rectTransform;
            Panel(p,new Vector2(0,.99f),Vector2.one,Gold);
            Text(p,eyebrow,new Vector2(.03f,.88f),new Vector2(.97f,.97f),17,Gold);
            Text(p,title,new Vector2(.03f,.76f),new Vector2(.97f,.89f),31);
            return p;
        }
        Image Panel(Transform parent,Vector2 min,Vector2 max,Color? color=null) => CampaignUI.Panel(parent,"Panel",min,max,color??Ink,true);
        TMP_Text Text(Transform parent,string content,Vector2 min,Vector2 max,int size=22,Color? color=null)
        {
            var label=CampaignUI.Text(parent,content,min,max,Mathf.RoundToInt(size*_textScale));label.color=color??Paper;return label;
        }
        Button Button(Transform parent,string label,Vector2 min,Vector2 max,Action click)
        {
            var b=CampaignUI.Button(parent,label,min,max,()=>{if(Time.unscaledTime-_openedAt<.13f)return;click();});
            b.image.color=Teal;var colors=b.colors;colors.selectedColor=Gold;colors.highlightedColor=Gold;colors.pressedColor=Gold;b.colors=colors;
            var text=b.GetComponentInChildren<TMP_Text>();text.fontSize=Mathf.RoundToInt(19*_textScale);text.color=Paper;
            _buttons.Add(b);return b;
        }
        void FocusFirst(){if(_buttons.Count>0)CampaignUI.Focus(_buttons[0]);}
        void Icon(Transform parent,Sprite sprite,Vector2 min,Vector2 max)
        {var image=CampaignUI.Rect(parent,"Illustration",min,max).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;}
        void Say(string speaker,string text,Action after,string button="Lanjut")
        {
            _afterDialogue=after;
            if(_dialogue)Destroy(_dialogue);
            _dialogue=ScriptableObject.CreateInstance<DialogueData>();
            var character=Cast.FirstOrDefault(c=>c&&c.CharacterName.Equals(speaker,StringComparison.OrdinalIgnoreCase))??Cast.FirstOrDefault();
            _dialogue.Lines=new List<DialogueLine>{new DialogueLine{SpeakerName=speaker,Text=text,SpeakerData=character}};
            DialogueManager.Instance.StartDialogue(_dialogue,character);
        }
        void ShowDialogueLine(DialogueLine line)
        {
            if(_modal){Destroy(_modal.gameObject);_modal=null;}
            Activity=CampaignActivity.Dialogue;_hud.gameObject.SetActive(false);
        }
        void ExternalDialogueEnded(){_hud.gameObject.SetActive(true);if(_afterDialogue==null)SetPlaying();}
        void DialogueCompleted(DialogueData data)
        {
            if(data!=_dialogue)return;
            var after=_afterDialogue;_afterDialogue=null;CloseModal();after?.Invoke();
        }
        void ShowPuzzle(string feedback=null)
        {
            var board=_task.Board;var progress=State.PrepareBoard(_task);
            if(progress.Complete){Say(_task.Speaker,_task.Outcome,()=>{State.Finish(Content);Save();if(State.Completed)ShowEnding();});return;}
            string kind=board.Kind=="budget"?"ANGGARAN":board.Kind=="inspect"?"PERIKSA DETAIL":board.Kind=="flow"?"ARUS PEMBAYARAN":board.Kind=="match"?"COCOKKAN DOKUMEN":"KELOMPOKKAN BUKTI";
            var p=Card(CampaignActivity.Puzzle,kind,_task.Title.Split('—')[0]);
            Text(p,board.Instruction,new Vector2(.04f,.67f),new Vector2(.96f,.77f),18);
            if(board.Kind=="budget")
            {
                for(int i=0;i<board.Cards.Length;i++){int card=i;float y=.58f-i*.076f;bool chosen=progress.Placements[i]==1;
                    Button(p,(chosen?"✓  ":"+  ")+board.Cards[i]+" • Rp"+board.Costs[i].ToString("N0"),new Vector2(.04f,y),new Vector2(.58f,y+.068f),()=>Place(card,chosen?0:1));}
                int total=board.Total(progress.Placements);
                Text(p,"Dana tersedia\nRp"+board.Limit.ToString("N0")+"\n\nDialokasikan\nRp"+total.ToString("N0")+"\nSisa Rp"+(board.Limit-total).ToString("N0"),new Vector2(.63f,.23f),new Vector2(.95f,.66f),22,total>board.Limit?new Color(1,.6f,.5f):Gold);
            }
            else if(board.Kind=="inspect")
            {
                // The world artwork is unchanged. This diagram is a labelled inspection work surface.
                var face=Panel(p,new Vector2(.05f,.29f),new Vector2(.52f,.64f),new Color(.3f,.32f,.3f));
                if(_task.Id=="c3.rules")Icon(face.transform,PromotionIcon,Vector2.zero,Vector2.one);
                else {Text(face.transform,"RADIO  /  S-014",new Vector2(.02f,.62f),new Vector2(.98f,.96f),24);Text(face.transform,"▥   ───   ◉",new Vector2(.05f,.2f),new Vector2(.95f,.6f),40);}
                for(int i=0;i<board.Cards.Length;i++){int card=i;float y=.55f-i*.09f;
                    Button(p,(progress.Placements[i]==0?"✓ ":"Periksa: ")+board.Cards[i],new Vector2(.56f,y),new Vector2(.96f,y+.078f),()=>Place(card,0));}
            }
            else
            {
                for(int i=0;i<board.Cards.Length;i++){int card=i;float h=.4f/board.Cards.Length,y=.63f-(i+1)*h;
                    bool done=progress.Placements[i]==board.Answers[i];
                    var b=Button(p,(done?"✓ ":_selectedCard==i?"> ":"")+board.Cards[i],new Vector2(.04f,y),new Vector2(.51f,y+h-.008f),()=>{_selectedCard=card;ShowPuzzle();});b.interactable=!done;}
                for(int i=0;i<board.Slots.Length;i++){int slot=i;float h=.4f/board.Slots.Length,y=.63f-(i+1)*h;
                    Button(p,(board.Kind=="flow"?"→ ":"")+board.Slots[i],new Vector2(.57f,y),new Vector2(.96f,y+h-.008f),()=>{if(_selectedCard>=0)Place(_selectedCard,slot);else ShowPuzzle("Pilih kartu di kiri terlebih dahulu.");});}
            }
            Text(p,feedback??"Kemajuan disimpan setiap kali satu bukti ditempatkan dengan benar.",new Vector2(.035f,.13f),new Vector2(.96f,.23f),18,Gold);
            Button(p,_hint>=2?"Terapkan satu panduan":"Petunjuk",new Vector2(.04f,.035f),new Vector2(.28f,.11f),Hint);
            Button(p,"Kembali",new Vector2(.34f,.035f),new Vector2(.54f,.11f),()=>{Save();CloseModal();});
            Button(p,"Selesaikan aktivitas",new Vector2(.61f,.035f),new Vector2(.96f,.11f),Commit).interactable=board.Solved(progress.Placements);FocusFirst();
        }
        public void Place(int card,int slot)
        {
            if(Activity!=CampaignActivity.Puzzle)return;
            bool accepted=State.Place(_task,card,slot,out string feedback);
            if(accepted){Save();_selectedCard=-1;}ShowPuzzle(feedback);
        }
        void Commit()
        {
            if(State.CommitBoard(_task,out string feedback))
            {
                CurrencySystem.Instance?.RestoreBalances(State.Money,State.Bank);Save();ObjectiveCompleted?.Invoke(_task.Id);RefreshHud();ShowPuzzle();
            }else ShowPuzzle(feedback);
        }
        void Hint()
        {
            var b=_task.Board;var p=State.PrepareBoard(_task);
            if(_hint++<2){ShowPuzzle(_hint==1?b.Instruction:b.Notes[0]);return;}
            if(b.Kind=="budget")
            {
                // Enumerate the tiny authored board (at most six items) to find a feasible allocation.
                for(int mask=0;mask<(1<<b.Cards.Length);mask++)
                {
                    var values=Enumerable.Range(0,b.Cards.Length).Select(i=>(mask>>i)&1).ToList();if(!b.Solved(values))continue;
                    int next=Enumerable.Range(0,values.Count).FirstOrDefault(i=>p.Placements[i]!=values[i]);Place(next,values[next]);return;
                }
            }
            else for(int i=0;i<b.Cards.Length;i++)if(p.Placements[i]!=b.Answers[i]){Place(i,b.Answers[i]);return;}
        }
        void Journal(int page)
        {
            _journalPage=page;
            var entries=Content.Tasks.Select(t=>(State.Progress(t.Id)?.Complete==true?"✓  ":t==CurrentTask?">  ":"•  ")+t.Title).Concat(State.Evidence.Select(e=>"CATATAN  /  "+e)).Concat(State.Discoveries.Select(id=>"CERITA WARGA  /  "+Content.Optional[Mathf.Clamp(int.Parse(id.Substring(id.Length-1)),0,Content.Optional.Length-1)].Replace('|',':'))).ToArray();
            int pages=Mathf.Max(1,(entries.Length+3)/4);_journalPage=Mathf.Clamp(page,0,pages-1);
            var p=Card(CampaignActivity.Journal,$"BUKU PERJALANAN  /  HALAMAN {_journalPage+1} DARI {pages}",Content.Title);
            for(int i=0;i<4;i++){int n=_journalPage*4+i;if(n>=entries.Length)break;Text(p,entries[n],new Vector2(.04f,.61f-i*.14f),new Vector2(.96f,.75f-i*.14f),20);}
            Button(p,"< Sebelumnya",new Vector2(.04f,.03f),new Vector2(.26f,.12f),()=>Journal(_journalPage-1)).interactable=_journalPage>0;
            Button(p,"Berikutnya >",new Vector2(.29f,.03f),new Vector2(.51f,.12f),()=>Journal(_journalPage+1)).interactable=_journalPage<pages-1;
            Button(p,"Kembali",new Vector2(.74f,.03f),new Vector2(.96f,.12f),CloseModal);FocusFirst();
        }
        public void Pause()
        {
            if(_paused||_transition)return;
            _resumeScreen=Activity;_paused=true;_encounter?.SetPaused(true);Save(false);
            if(_modal)_modal.gameObject.SetActive(false);
            // Keep the current activity intact underneath the pause overlay.
            var previous=_modal;var previousButtons=new List<Button>(_buttons);_modal=null;
            var p=Card(CampaignActivity.Pause,"JEDA / PROGRES DISIMPAN","Ambil waktu sejenak");
            _pausePrevious=previous;_pauseButtons=previousButtons;
            Text(p,"Tidak ada batas waktu. Kamu bisa melanjutkan dari langkah ini.",new Vector2(.04f,.62f),new Vector2(.95f,.74f),24);
            Button(p,"Lanjutkan",new Vector2(.05f,.46f),new Vector2(.45f,.57f),Resume);
            Button(p,"Simpan checkpoint",new Vector2(.52f,.46f),new Vector2(.95f,.57f),()=>Save());
            Button(p,"Ukuran teks: "+(_textScale>1?"besar":"normal"),new Vector2(.05f,.29f),new Vector2(.45f,.4f),()=>ToggleSetting("Alif_LargeText"));
            Button(p,"Gerakan: "+(CampaignUI.ReducedMotion?"dikurangi":"normal"),new Vector2(.52f,.29f),new Vector2(.95f,.4f),()=>ToggleSetting("Alif_ReducedMotion"));
            Button(p,"Simpan & menu utama",new Vector2(.52f,.07f),new Vector2(.95f,.18f),()=>{if(Save())SceneManager.LoadScene(MenuScene);});
            GameManager.Instance.SetState(GameManager.GameState.Paused);FocusFirst();
        }
        RectTransform _pausePrevious;List<Button> _pauseButtons;
        public void Resume()
        {
            if(!_paused)return;Destroy(_modal.gameObject);_modal=_pausePrevious;_buttons=_pauseButtons;_pausePrevious=null;_paused=false;Activity=_resumeScreen;
            if(_modal)_modal.gameObject.SetActive(true);
            if(Activity==CampaignActivity.World)SetPlaying();else GameManager.Instance.SetState(Activity==CampaignActivity.Combat?GameManager.GameState.Combat:GameManager.GameState.Dialogue);_encounter?.SetPaused(false);FocusFirst();
        }
        void ToggleSetting(string key)
        {
            PlayerPrefs.SetInt(key,1-PlayerPrefs.GetInt(key,0));PlayerPrefs.Save();
            Resume();BuildHud();
            if(Activity==CampaignActivity.Dialogue)ShowDialogueLine(DialogueManager.Instance.CurrentLine);
            else if(Activity==CampaignActivity.Puzzle)ShowPuzzle();else if(Activity==CampaignActivity.Journal)Journal(_journalPage);
            Pause();
        }
        void ShowEnding()
        {
            var p=Card(CampaignActivity.Ending,Chapter==5?"EPILOG / LIMA BAB SELESAI":$"BAB {Chapter} SELESAI",Content.Title);
            Text(p,Content.Ending,new Vector2(.05f,.3f),new Vector2(.95f,.74f),25);
            Button(p,Chapter==5?"Kembali ke menu":"Lanjut ke bab "+(Chapter+1),new Vector2(.52f,.04f),new Vector2(.95f,.15f),()=>
            {
                if(Chapter==5){SceneManager.LoadScene(MenuScene);return;}
                if(Chapter==1){SceneManager.LoadScene("Chapter1Ending");return;}
                State=State.StartChapter(Chapter+1);if(ChapterProgress.SaveAdventure(State))SceneManager.LoadScene(SceneFor(Chapter+1));
            });
            Button(p,"Baca rujukan "+(Chapter==5?"OJK":"MUI"),new Vector2(.05f,.18f),new Vector2(.45f,.27f),()=>Application.OpenURL(Chapter==5?AdventureContent.OjkUrl:Chapter==2?AdventureContent.CreditUrl:AdventureContent.MuiUrl));
            Button(p,"Buka jurnal",new Vector2(.05f,.04f),new Vector2(.45f,.15f),()=>Journal(0));FocusFirst();
        }
        void BeginEncounter()
        {
            if (_encounter != null || _task == null || _task.Kind != "encounter" || !State.CanStart(_task)) return;
            if (!Save()) { CloseModal(); return; }
            _player.SetMovementLocked(this,true); Activity=CampaignActivity.Combat;
            GameManager.Instance.SetState(GameManager.GameState.Combat);
            _hud.gameObject.SetActive(false);
            if (Chapter == 2)
            {
                var controller = gameObject.AddComponent<BattleEncounterController>();controller.Definition=DanaKilat;
                _encounter=controller;_encounterComponent=controller;
                controller.Finished+=EncounterFinished;controller.Begin();
            }
            else
            {
                var controller=gameObject.AddComponent<ActionEncounterController>();controller.Definition=ActionEncounter;
                controller.PlayerSprite=_player.GetComponentInChildren<SpriteRenderer>().sprite;
                var backgrounds=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                controller.Backdrop=backgrounds.Where(r=>r.bounds.size.x>5).OrderBy(r=>Vector2.Distance(r.transform.position,_player.transform.position)).FirstOrDefault()?.sprite;
                _encounter=controller;_encounterComponent=controller;
                controller.Finished+=EncounterFinished;controller.Begin();
            }
        }
        void EncounterFinished(EncounterResult result)
        {
            _encounter.Finished-=EncounterFinished;_encounter=null;
            if(_encounterComponent)Destroy(_encounterComponent);_encounterComponent=null;
            _hud.gameObject.SetActive(true);
            if(result==EncounterResult.Won && State.CompleteEncounter(_task.Id))
            {
                Save();ObjectiveCompleted?.Invoke(_task.Id);
                Say(_task.Speaker,_task.Outcome,()=>{State.Finish(Content);Save();if(State.Completed)ShowEnding();});
            }
            else CloseModal();
        }
        void OnDestroy()
        {
            if (_encounter != null) { _encounter.Finished-=EncounterFinished; _encounter.Cancel(); }
            if (_player) _player.SetMovementLocked(this,false);
            if(DialogueManager.Instance){DialogueManager.Instance.OnLineDisplayed-=ShowDialogueLine;DialogueManager.Instance.OnDialogueCompleted-=DialogueCompleted;DialogueManager.Instance.OnDialogueEnded-=ExternalDialogueEnded;}
            if(_canvas)Destroy(_canvas.gameObject);if(_dialogue)Destroy(_dialogue);if(Instance==this)Instance=null;
        }
    }
}
