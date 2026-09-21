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
    public enum CampaignActivity { World, Dialogue, Puzzle, Journal, Pause, Ending, Combat, Bag, Item, Phone }
    public sealed partial class AdventureGame : MonoBehaviour
    {
        public int Chapter;
        public Vector2[] Centers, Spawns;
        public CharacterData[] Cast;
        public Sprite InteractionArrow, DocumentIcon, PromotionIcon;
        [Header("Handcrafted Solo UI (optional approved assets)")]
        [Header("Chapter 1 ambience (approved assets only)")]
        public Sprite[] SignFamilySprites, LocationPropSprites, ReactionPoseSprites, EmoteSprites;
        List<SceneDoor> _doors=new List<SceneDoor>();
        int _selectedCard=-1;
        public Action<string> ObjectiveCompleted;
        public static readonly Color Ink=new Color(.07f,.10f,.14f,.94f), Paper=new Color(.96f,.93f,.85f), Teal=new Color(.18f,.30f,.32f);
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
        public string CurrentTaskId => TutorialActive ? "" : CurrentTask?.Id ?? "";
        public int PuzzleStepIndex => _task == null ? -1 : State.Progress(_task.Id)?.Step ?? 0;
        // Loop eksplisit (bukan LINQ) — getter ini dibaca tiap frame dari Update.
        AdventureTask CurrentTask
        {
            get
            {
                if(Content==null)return null;
                var tasks=Content.Tasks;
                for(int i=0;i<tasks.Length;i++)
                    if(State.Progress(tasks[i].Id)?.Complete!=true)return tasks[i];
                return null;
            }
        }
        RectTransform _canvas, _modal, _hud, _safeRoot;
        TMP_Text _objective, _location, _toast, _prompt, _guide;
        Image _guideArrow;
        GameObject _touchControls;
        VirtualJoystick _joystick;
        Button _skipTutorial;
        // Elemen HUD yang disorot tutorial pembuka (papan status, catatan tujuan, 4 tombol ikon).
        RectTransform _statusBoard,_objectivePanel,_coachCard,_coachGlow,_coachDim,_dpadRect,_interactRect;
        CanvasGroup _statusFade;
        readonly Button[] _hudIcons=new Button[4];
        TMP_Text _coachText;Button _coachNext;
        RectTransform _hotbar;Rigidbody2D _playerBody;float _hotbarIdleFor;
        readonly Queue<(string name,Sprite icon)> _receivedItems=new Queue<(string name,Sprite icon)>();
        const float HotbarShownY=14,HotbarShowDelay=.3f;
        Image[] _hotbarSlots, _hotbarIcons;
        TMP_Text[] _hotbarCounts;
        TMP_Text _dayText, _clockText, _moneyText, _energyText;
        RectTransform _dayTab, _energyFill, _logicFill, _shariaFill;
        TMP_Text _logicText, _shariaText;
        static readonly Dictionary<string,string> DayNamesId=new Dictionary<string,string>{{"Monday","Senin"},{"Tuesday","Selasa"},{"Wednesday","Rabu"},{"Thursday","Kamis"},{"Friday","Jumat"},{"Saturday","Sabtu"},{"Sunday","Minggu"}};
        Rect _lastSafeArea;
        int _lastScreenWidth, _lastScreenHeight;
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
        Vector2 _tutorialLastPosition;
        float _tutorialDistance;
        CampaignActivity _resumeScreen;
        int _journalPage;
        const string MenuScene = "MainMenu";
        const string TutorialTarget = "Papan arah";
        public static string SceneFor(int chapter) => "AdventureChapter"+chapter;
        public bool TutorialActive => Chapter==1 && State?.TutorialStep>0;
        void Start()
        {
            if (Chapter == 0) { SceneTransition.Load(MenuScene); return; }
            Instance=this;
            Application.targetFrameRate=60;
            Time.timeScale=1;
            if(GameManager.Instance==null) new GameObject("Adventure managers").AddComponent<GameManager>();
            if(DialogueManager.Instance==null) new GameObject("Adventure dialogue").AddComponent<DialogueManager>();
            if(EventSystem.current==null) {var e=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));e.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            _canvas=CampaignUI.Canvas("Alif • Adventure",50);
            State=ChapterProgress.LoadAdventure(out string notice);

            if(Chapter > State.HighestUnlocked) {SceneTransition.Load(MenuScene);return;}
            if(State.Chapter!=Chapter) State=State.StartChapter(Chapter);
            Content=AdventureContent.Get(Chapter);
            BuildCity(); BindOriginalScene(); BuildHud(); StartSideQuests();
            DialogueManager.Instance.OnLineDisplayed+=ShowDialogueLine;
            DialogueManager.Instance.OnDialogueEnded+=ExternalDialogueEnded;
            DialogueManager.Instance.OnDialogueCompleted+=DialogueCompleted;
            if(!string.IsNullOrEmpty(notice)) Toast(notice,10);
            if(State.Completed) {ShowEnding();return;}
            BeginChapterIntroduction(notice);
        }
        // Adegan masuk Main Baru Bab 1: Alif muncul di sisi kanan ruang tunggu lalu berjalan
        // sendiri ke titik spawn di tengah (tombol gerak pemain membatalkannya). 5.1: kaki
        // Alif (lebar .32) masih bebas dari BoundaryWall_Right (x 5.55) — di 5.4 posisinya
        // dianggap tidak aman dan adegan dilewati.
        static readonly Vector2 OpeningWalkOffset=new Vector2(5.1f,0f);
        void BeginTutorial(bool openingWalk)
        {
            SetPlaying();
            Vector2 walkFrom=Spawns[0]+OpeningWalkOffset;
            if(openingWalk)
            {
                if(!SafePosition(walkFrom)){Debug.LogWarning($"[Alif] Adegan masuk dilewati: titik awal {walkFrom} tertutup collider.");StartTutorialCounting();return;}
                _player.transform.position=walkFrom;_player.GetComponent<Rigidbody2D>().position=walkFrom;
                Camera.main.GetComponent<CameraFollow>()?.SnapToTarget();
                // Jarak jalan otomatis tidak dihitung sebagai langkah tutorial pertama pemain.
                _player.AutoWalkTo(Spawns[0],StartTutorialCounting);
                return;
            }
            StartTutorialCounting();
        }
        void StartTutorialCounting()
        {
            _tutorialLastPosition=_player.transform.position;_tutorialDistance=0;
            Toast(State.TutorialStep==1?"Tutorial dimulai • bergerak sejauh satu langkah":"Tutorial dilanjutkan dari checkpoint",6);
        }
        /// <summary>Pembuka bab selalu diputar lebih dulu; di Main Baru Bab 1 tutorial menyusul
        /// sesudahnya — "Alif baru tiba di Cempaka" tidak masuk akal setelah pemain berkeliling stasiun.</summary>
        void BeginChapterIntroduction(string notice="")
        {
            // Save() mengisi posisi, jadi adegan masuk (jalan otomatis) diputuskan sebelum menyimpan.
            bool openingWalk=Chapter==1&&State.TutorialStep==1&&!State.HasPosition;
            if(!State.IntroductionSeen)
                PlayStory(StoryContent.ChapterIntro(Content),()=>
                {
                    State.IntroductionSeen=true;Save(!TutorialActive);
                    if(TutorialActive)BeginTutorial(openingWalk);else SetPlaying();
                });
            else if(TutorialActive)BeginTutorial(openingWalk);
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
            return _player.CanStandAt(position);
        }
        public bool CanExplore => Activity==CampaignActivity.World&&!_transition&&!_paused && !(DialogueManager.Instance?.IsDialogueActive??false);
        void BuildHud()
        {
            if(_safeRoot)Destroy(_safeRoot.gameObject);
            _safeRoot=CampaignUI.Rect(_canvas,"Safe Area",Vector2.zero,Vector2.one);
            ApplySafeArea(true);
            _hud=CampaignUI.Rect(_safeRoot,"Adventure HUD",Vector2.zero,Vector2.one);

            BuildStatusBoard();

            var top=Panel(_hud,new Vector2(.228f,.88f),new Vector2(.772f,.975f),Color.white);
            top.raycastTarget=false;ApplySprite(top,PixelSkin.Panel());
            _objective=Text(top.transform,"",Vector2.zero,Vector2.one,18,PixelSkin.TextDark);
            _objective.margin=new Vector4(26,0,26,0);_objective.textWrappingMode=TextWrappingModes.NoWrap;_objective.overflowMode=TextOverflowModes.Ellipsis;
            _objectivePanel=(RectTransform)top.transform;
            // Banner tugas sekaligus tombol: diklik = tujuannya ditandai panah oranye di dunia.
            top.raycastTarget=true;
            var objectiveButton=top.gameObject.AddComponent<Button>();
            objectiveButton.targetGraphic=top;objectiveButton.transition=Selectable.Transition.None;
            objectiveButton.navigation=new Navigation{mode=Navigation.Mode.None};
            objectiveButton.onClick.AddListener(PingObjective);
            _hudIcons[0]=IconButton("HP",PixelSkin.PhoneIcon(),3,OpenPhone);
            _hudIcons[1]=IconButton("Tas",PixelSkin.BagIcon(),2,Bag);
            _hudIcons[2]=IconButton("Jurnal",PixelSkin.NotebookIcon(),1,()=>Journal(0));
            _hudIcons[3]=IconButton("Jeda",PixelSkin.MenuIcon(),0,Pause);
            BuildCoach();
            _skipTutorial=Button(_hud,"Lewati tutorial",Vector2.one,Vector2.one,SkipTutorial);
            var skipRect=(RectTransform)_skipTutorial.transform;skipRect.pivot=Vector2.one;skipRect.anchoredPosition=new Vector2(-14,-84);skipRect.sizeDelta=new Vector2(196,40);
            _skipTutorial.GetComponentInChildren<TMP_Text>().fontSize=Mathf.RoundToInt(16*_textScale);

            _guideArrow=CampaignUI.Rect(_hud,"Route arrow",new Vector2(.31f,.82f),new Vector2(.345f,.87f)).gameObject.AddComponent<Image>();
            _guideArrow.sprite=InteractionArrow;_guideArrow.preserveAspect=true;_guideArrow.raycastTarget=false;
            _guideArrow.color=PixelSkin.Orange;_guideArrow.gameObject.SetActive(false);
            _guide=WorldText(new Vector2(.345f,.82f),new Vector2(.69f,.87f),16);_guide.color=PixelSkin.OrangeLight;
            _prompt=WorldText(new Vector2(.27f,.19f),new Vector2(.73f,.245f),18);
            _toast=WorldText(new Vector2(.22f,.255f),new Vector2(.78f,.31f),17);
            BuildTouchControls();
            BuildHotbar();
            RefreshHud();SetTouchControlsVisible();
        }

        /// <summary>Tombol ikon persegi di kanan-atas (Tas / Jurnal / Jeda). Nama GameObject
        /// tetap nama tombolnya (dipakai smoke test & navigasi keyboard); label teks disembunyikan.</summary>
        /// <summary>Tutorial pembuka 7 langkah: gerak → tombol E → papan status → catatan tujuan →
        /// HP → Tas → Jurnal. Tiap langkah menyorot elemen HUD-nya dan menjelaskan gunanya; langkah
        /// yang perlu dicoba sendiri (gerak, E, tiga tombol) menunggu pemain melakukannya, sisanya
        /// lanjut lewat tombol. "Lewati tutorial" tetap memotong semuanya.</summary>
        static readonly (string title,string body)[] TutorialSteps =
        {
            ("Bergerak","WASD, tombol panah, atau D-pad di kiri layar untuk melangkah."),
            ("Berinteraksi","Dekati Papan arah sampai muncul tulisan E, lalu tekan E atau tombol lingkaran."),
            ("Papan status","Hari, jam, dan uangmu ada di sini. Tiga bar di bawahnya: energi, Logika Finansial, dan Kepatuhan Syariah — dua bar terakhir naik saat kamu mengambil keputusan yang benar."),
            ("Catatan tujuan","Baris ini selalu menunjukkan tugasmu berikutnya. Ikuti panah petunjuk di layar kalau tujuannya di area lain."),
            ("HP","Coba buka HP. Isinya Peta untuk bepergian, Kontak warga, Kalender, dan kartu Investasi yang terbuka satu per satu."),
            ("Tas","Coba buka Tas. Struk, brosur, dan barang titipan warga tersimpan di sini."),
            ("Buku Perjalanan","Coba buka Jurnal — semua tujuanmu tercatat di sana. Tutup lagi untuk mengakhiri tutorial."),
        };

        void BuildCoach()
        {
            // Layar diredupkan seperti modal lain (Jurnal/Tas/HP) supaya mata tertuju ke elemen
            // yang sedang dijelaskan; elemen itu sendiri diangkat ke atas lapisan gelapnya.
            // Raycast dimatikan supaya tombol HUD di baliknya tetap bisa diklik saat langkah
            // "coba buka HP / Tas / Jurnal".
            _coachDim=CampaignUI.Rect(_hud,"Tutorial dim",Vector2.zero,Vector2.one);
            var dim=_coachDim.gameObject.AddComponent<Image>();
            dim.color=new Color(.10f,.06f,.04f,.62f);dim.raycastTarget=false;
            _coachDim.gameObject.SetActive(false);

            _coachGlow=CampaignUI.Rect(_hud,"Tutorial glow",Vector2.one*.5f,Vector2.one*.5f);
            var glow=_coachGlow.gameObject.AddComponent<Image>();glow.raycastTarget=false;
            ApplySprite(glow,PixelSkin.Panel());glow.color=new Color(1f,.62f,.24f,.34f);
            _coachGlow.gameObject.SetActive(false);

            _coachCard=(RectTransform)Panel(_hud,new Vector2(.26f,.40f),new Vector2(.74f,.60f),Color.white).transform;
            ApplySprite(_coachCard.GetComponent<Image>(),PixelSkin.Panel());
            _coachText=Text(_coachCard,"",new Vector2(.05f,.30f),new Vector2(.95f,.92f),17,PixelSkin.TextDark);
            _coachText.alignment=TextAlignmentOptions.Top;_coachText.textWrappingMode=TextWrappingModes.Normal;
            _coachNext=Button(_coachCard,"Lanjut",new Vector2(.34f,.07f),new Vector2(.66f,.26f),AdvanceCoach);
            _coachCard.gameObject.SetActive(false);
        }

        /// <summary>Sorot elemen HUD langkah ini dan tampilkan penjelasannya.</summary>
        void RefreshCoach()
        {
            if(_coachCard==null)return;
            int step=TutorialActive?State.TutorialStep:0;
            RevealHudForTutorial(step);
            bool show=step>=1&&step<=TutorialSteps.Length;
            _coachCard.gameObject.SetActive(show);
            _coachGlow.gameObject.SetActive(show);
            _coachDim.gameObject.SetActive(show);
            if(!show)return;
            var info=TutorialSteps[step-1];
            _coachText.text=$"<b>{info.title}</b>\n{info.body}";
            // Langkah 1-2 dilatih sambil berjalan, jadi layarnya hanya diredupkan tipis dan
            // kartunya digeser ke bawah supaya jalur pemain tetap terlihat.
            bool walking=step<=2;
            _coachDim.GetComponent<Image>().color=new Color(.10f,.06f,.04f,walking?.32f:.62f);
            _coachCard.anchorMin=new Vector2(.26f,walking?.62f:.40f);
            _coachCard.anchorMax=new Vector2(.74f,walking?.80f:.60f);
            _coachNext.gameObject.SetActive(step==3||step==4);              // sisanya menunggu aksi aslinya
            var target=step==1?_dpadRect:step==2?_interactRect
                :step==3?_statusBoard:step==4?_objectivePanel
                :_hudIcons[step-5]?(RectTransform)_hudIcons[step-5].transform:null;
            // Urutan lapisan: gelap → sorotan → elemen yang dijelaskan → kartu penjelasan,
            // jadi hanya elemen itu yang tetap terang di atas layar yang diredupkan.
            _coachDim.SetAsLastSibling();
            if(_skipTutorial)_skipTutorial.transform.SetAsLastSibling();
            if(!target){_coachGlow.gameObject.SetActive(false);_coachCard.SetAsLastSibling();return;}
            _coachGlow.position=target.TransformPoint(target.rect.center);
            _coachGlow.sizeDelta=target.rect.size+new Vector2(18f,18f);
            _coachGlow.SetAsLastSibling();
            target.SetAsLastSibling();
            _coachCard.SetAsLastSibling();
        }

        /// <summary>HUD tumbuh bertahap selama tutorial: tiap elemen baru muncul di langkah yang
        /// menjelaskannya, jadi layar pemain baru tidak penuh sekaligus. Di luar tutorial semuanya
        /// tampil seperti biasa.</summary>
        void RevealHudForTutorial(int step)
        {
            bool done=step<=0;                                   // 0 = tutorial selesai/dilewati
            Show(_interactRect,done||step>=2);                   // tombol E — langkah 2
            Show(_statusBoard,done||step>=3);                    // papan status — langkah 3
            Show(_objectivePanel,done||step>=4);                 // catatan tujuan — langkah 4
            for(int i=0;i<_hudIcons.Length;i++)                  // HP, Tas, Jurnal, lalu Jeda
                if(_hudIcons[i])Show((RectTransform)_hudIcons[i].transform,done||step>=(i==3?7:5+i));
        }

        static void Show(RectTransform rect,bool visible)
        {
            if(rect&&rect.gameObject.activeSelf!=visible)rect.gameObject.SetActive(visible);
        }

        void AdvanceCoach()
        {
            if(!TutorialActive)return;
            if(State.TutorialStep>=7){CompleteTutorial();return;}
            State.TutorialStep++;Save(false);RefreshHud();
        }

        /// <summary>Langkah tutorial yang selesai dengan membuka fitur HUD-nya sendiri.</summary>
        void CoachOpened(int step)
        {
            if(TutorialActive&&State.TutorialStep==step)AdvanceCoach();
        }

        Button IconButton(string name,Sprite icon,int slotFromRight,Action click)
        {
            const float size=60,gap=8;
            var b=Button(_hud,name,Vector2.one,Vector2.one,click);
            var rect=(RectTransform)b.transform;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-14-slotFromRight*(size+gap),-16);rect.sizeDelta=Vector2.one*size;
            b.GetComponentInChildren<TMP_Text>().gameObject.SetActive(false);
            var image=CampaignUI.Rect(rect,"Icon",Vector2.one*.5f,Vector2.one*.5f).gameObject.AddComponent<Image>();
            image.sprite=icon;image.raycastTarget=false;image.rectTransform.sizeDelta=icon.rect.size*PixelSkin.PixelScale;
            return b;
        }

        /// <summary>Papan status kiri-atas bergaya game manajemen retro: tab hari (krem) di tepi
        /// atas papan oranye, baris lokasi / jam / uang dalam kotak krem berikon, dan bar energi
        /// hijau di bawah papan. Ukuran dalam unit kanvas (referensi 1280x720) supaya piksel
        /// bingkai tetap tajam.</summary>
        void BuildStatusBoard()
        {
            const float width=262,rowHeight=28,iconSize=26;
            var board=CampaignUI.Rect(_hud,"Status board",new Vector2(0,1),new Vector2(0,1));
            _statusBoard=board;
            // Toko di tepi map (Toko Kelontong di ujung kiri Jalan Pasar) jatuh persis di bawah
            // papan ini karena kamera mentok di tepi — papannya memudar kalau pemain ada di
            // baliknya, jadi etalase & penanda pintunya tetap terbaca.
            _statusFade=board.gameObject.AddComponent<CanvasGroup>();
            board.pivot=new Vector2(0,1);board.anchoredPosition=new Vector2(14,-22);board.sizeDelta=new Vector2(width,130);
            var boardImage=board.gameObject.AddComponent<Image>();boardImage.raycastTarget=false;ApplySprite(boardImage,PixelSkin.Button());

            _dayTab=CampaignUI.Rect(board,"Day tab",new Vector2(0,1),new Vector2(0,1));
            _dayTab.pivot=new Vector2(0,.5f);_dayTab.anchoredPosition=new Vector2(14,2);_dayTab.sizeDelta=new Vector2(110,30);
            var tabImage=_dayTab.gameObject.AddComponent<Image>();tabImage.raycastTarget=false;ApplySprite(tabImage,PixelSkin.Tab());
            _dayText=StatusText(_dayTab,15,TextAlignmentOptions.Center);

            // Baris 1 selebar papan (lokasi), baris 2-3 dengan ikon di kiri kotak.
            _location=StatusText(StatusRow(board,"Location row",-22,14,rowHeight),13,TextAlignmentOptions.Center);
            _clockText=StatusText(StatusRow(board,"Clock row",-56,14+iconSize+8,rowHeight),15,TextAlignmentOptions.Left);
            StatusIcon(board,PixelSkin.ClockIcon(),-56-rowHeight*.5f,iconSize);
            _moneyText=StatusText(StatusRow(board,"Money row",-90,14+iconSize+8,rowHeight),15,TextAlignmentOptions.Left);
            StatusIcon(board,PixelSkin.CoinIcon(),-90-rowHeight*.5f,iconSize);

            // Bar di bawah papan: kotak ikon oranye + bar krem berisi warna, teks di tengah.
            (_energyFill,_energyText)=StatusBar("Energy",PixelSkin.BoltIcon(),-158,width,new Color(.55f,.88f,.36f));
            (_logicFill,_logicText)=StatusBar("Financial logic",PixelSkin.ChartIcon(),-194,width,new Color(.45f,.7f,1f));
            (_shariaFill,_shariaText)=StatusBar("Sharia compliance",PixelSkin.CrescentIcon(),-230,width,new Color(1f,.8f,.3f));

            var time=TimeSystem.Instance;var energy=EnergySystem.Instance;var currency=CurrencySystem.Instance;
            if(time){time.OnMinuteChanged-=RefreshStatusBoard;time.OnMinuteChanged+=RefreshStatusBoard;time.OnDayChanged-=RefreshStatusBoard;time.OnDayChanged+=RefreshStatusBoard;}
            if(energy){energy.OnEnergyChanged-=RefreshStatusEnergy;energy.OnEnergyChanged+=RefreshStatusEnergy;}
            if(currency){currency.OnMoneyChanged-=RefreshStatusMoney;currency.OnMoneyChanged+=RefreshStatusMoney;}
            var score=ScoreSystem.Instance;
            if(score){score.OnFinancialLogicChanged-=RefreshStatusLogic;score.OnFinancialLogicChanged+=RefreshStatusLogic;score.OnShariaComplianceChanged-=RefreshStatusSharia;score.OnShariaComplianceChanged+=RefreshStatusSharia;}
            RefreshStatusBoard();
        }
        (RectTransform fill,TMP_Text text) StatusBar(string name,Sprite icon,float top,float width,Color color)
        {
            var iconBox=CampaignUI.Rect(_hud,name+" icon",new Vector2(0,1),new Vector2(0,1));
            iconBox.pivot=new Vector2(0,1);iconBox.anchoredPosition=new Vector2(14,top);iconBox.sizeDelta=new Vector2(34,30);
            var iconBoxImage=iconBox.gameObject.AddComponent<Image>();iconBoxImage.raycastTarget=false;ApplySprite(iconBoxImage,PixelSkin.Button());
            var iconImage=CampaignUI.Rect(iconBox,"Icon",Vector2.one*.5f,Vector2.one*.5f).gameObject.AddComponent<Image>();
            iconImage.sprite=icon;iconImage.rectTransform.sizeDelta=icon.rect.size*2;iconImage.raycastTarget=false;
            var bar=CampaignUI.Rect(_hud,name+" bar",new Vector2(0,1),new Vector2(0,1));
            bar.pivot=new Vector2(0,1);bar.anchoredPosition=new Vector2(52,top);bar.sizeDelta=new Vector2(width-38,30);
            var barImage=bar.gameObject.AddComponent<Image>();barImage.raycastTarget=false;ApplySprite(barImage,PixelSkin.Slot());
            var fillArea=CampaignUI.Rect(bar,"Fill area",Vector2.zero,Vector2.one);fillArea.offsetMin=Vector2.one*4;fillArea.offsetMax=-Vector2.one*4;
            var fill=CampaignUI.Rect(fillArea,"Fill",Vector2.zero,Vector2.one);
            var fillImage=fill.gameObject.AddComponent<Image>();fillImage.raycastTarget=false;ApplySprite(fillImage,PixelSkin.BarFill());fillImage.color=color;
            return (fill,StatusText(bar,13,TextAlignmentOptions.Center));
        }
        static void SetBar(RectTransform fill,TMP_Text text,string label,float percent01)
        {
            if(!fill)return;
            percent01=Mathf.Clamp01(percent01);
            fill.anchorMax=new Vector2(percent01,1);fill.offsetMax=Vector2.zero;
            text.text=(label.Length>0?label+"  ":"")+Mathf.RoundToInt(percent01*100)+"%";
        }
        RectTransform StatusRow(RectTransform board,string name,float top,float left,float height)
        {
            var row=CampaignUI.Rect(board,name,new Vector2(0,1),new Vector2(1,1));
            row.pivot=new Vector2(.5f,1);row.offsetMin=new Vector2(left,top-height);row.offsetMax=new Vector2(-14,top);
            var image=row.gameObject.AddComponent<Image>();image.raycastTarget=false;ApplySprite(image,PixelSkin.Slot());
            return row;
        }
        void StatusIcon(RectTransform board,Sprite sprite,float centerY,float size)
        {
            var icon=CampaignUI.Rect(board,"Icon",new Vector2(0,1),new Vector2(0,1)).gameObject.AddComponent<Image>();
            icon.rectTransform.pivot=new Vector2(0,.5f);icon.rectTransform.anchoredPosition=new Vector2(14,centerY);icon.rectTransform.sizeDelta=Vector2.one*size;
            icon.sprite=sprite;icon.raycastTarget=false;
        }
        TMP_Text StatusText(RectTransform parent,int size,TextAlignmentOptions alignment)
        {
            var text=Text(parent,"",Vector2.zero,Vector2.one,size,PixelSkin.TextDark);
            text.alignment=alignment;text.margin=new Vector4(10,0,10,0);text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;
            return text;
        }
        void RefreshStatusBoard()
        {
            if(!_dayText)return; // HUD lama sudah dihancurkan (BuildHud ulang)
            var time=TimeSystem.Instance;
            string day=time?(DayNamesId.TryGetValue(time.CurrentDayName,out var id)?id:time.CurrentDayName):"—";
            if(State!=null)day=$"Hari {State.Day}  •  {day}";
            _dayText.text=day;
            _dayTab.sizeDelta=new Vector2(_dayText.GetPreferredValues(day).x+44,_dayTab.sizeDelta.y);
            _clockText.text=time?$"{time.GetFormattedTime()}   •   {NpcSchedule.Label(NpcSchedule.PartOf(time.CurrentHour))}":"--:--";
            RefreshStatusMoney(CurrencySystem.Instance?CurrencySystem.Instance.CurrentMoney:State?.Money??0);
            RefreshStatusEnergy(EnergySystem.Instance?EnergySystem.Instance.EnergyPercent01:1f);
            var score=ScoreSystem.Instance;
            RefreshStatusLogic(score?score.FinancialLogicPercent01:.5f);
            RefreshStatusSharia(score?score.ShariaCompliancePercent01:.5f);
        }
        void RefreshStatusMoney(int amount){if(_moneyText)_moneyText.text=CurrencySystem.FormatRupiah(amount);}
        void RefreshStatusEnergy(float percent01)=>SetBar(_energyFill,_energyText,"",percent01);
        void RefreshStatusLogic(float percent01)=>SetBar(_logicFill,_logicText,"Logika Finansial",percent01);
        void RefreshStatusSharia(float percent01)=>SetBar(_shariaFill,_shariaText,"Kepatuhan Syariah",percent01);

        /// <summary>Hotbar 5 slot di bawah-tengah: ikon & jumlah barang dari InventorySystem;
        /// klik slot memilih barang (klik lagi membatalkan). Panel inventory lama dari scene
        /// dihapus di BuildTouchControls, jadi ini satu-satunya tampilan tas di HUD.</summary>
        void BuildHotbar()
        {
            var inventory=InventorySystem.Instance;
            if(!inventory){_hotbarSlots=null;return;}
            int count=inventory.Slots.Count;const float slot=64,gap=8,pad=14;
            var bar=_hotbar=CampaignUI.Rect(_hud,"Hotbar",new Vector2(.5f,0),new Vector2(.5f,0));
            bar.pivot=new Vector2(.5f,0);bar.anchoredPosition=new Vector2(0,HotbarShownY);
            bar.sizeDelta=new Vector2(count*slot+(count-1)*gap+pad*2,slot+pad*2);
            var barImage=bar.gameObject.AddComponent<Image>();barImage.raycastTarget=false;ApplySprite(barImage,PixelSkin.Tab());
            _hotbarSlots=new Image[count];_hotbarIcons=new Image[count];_hotbarCounts=new TMP_Text[count];
            for(int i=0;i<count;i++)
            {
                int index=i;
                var rt=CampaignUI.Rect(bar,"Slot "+(i+1),new Vector2(0,.5f),new Vector2(0,.5f));
                rt.pivot=new Vector2(0,.5f);rt.anchoredPosition=new Vector2(pad+i*(slot+gap),0);rt.sizeDelta=Vector2.one*slot;
                var slotImage=rt.gameObject.AddComponent<Image>();ApplySprite(slotImage,PixelSkin.Slot());
                var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=slotImage;button.navigation=new Navigation{mode=Navigation.Mode.None};
                button.onClick.AddListener(()=>{if(Activity==CampaignActivity.World)ShowItemDetail(index,false);});
                var icon=CampaignUI.Rect(rt,"Icon",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
                icon.rectTransform.offsetMin=Vector2.one*10;icon.rectTransform.offsetMax=-Vector2.one*10;icon.preserveAspect=true;icon.raycastTarget=false;
                rt.gameObject.AddComponent<InventorySlotDrag>().Setup(index,icon);
                var quantity=CampaignUI.Text(rt,"",Vector2.zero,Vector2.one,14);quantity.alignment=TextAlignmentOptions.BottomRight;quantity.margin=new Vector4(0,0,6,3);quantity.color=PixelSkin.TextDark;
                _hotbarSlots[i]=slotImage;_hotbarIcons[i]=icon;_hotbarCounts[i]=quantity;
            }
            inventory.OnInventoryChanged-=InventoryChanged;inventory.OnInventoryChanged+=InventoryChanged;
            inventory.OnSelectionChanged-=RefreshHotbarSelection;inventory.OnSelectionChanged+=RefreshHotbarSelection;
            inventory.OnItemAdded-=QueueReceivedItem;inventory.OnItemAdded+=QueueReceivedItem;
            RefreshHotbar();
        }
        void RefreshHotbarSelection(int _)=>RefreshHotbar();
        /// <summary>Hotbar turun keluar layar selama pemain berjalan dan naik lagi setelah
        /// berhenti sebentar, supaya tidak menutupi lantai saat menjelajah.</summary>
        void AnimateHotbar()
        {
            if(!_hotbar)return;
            if(!_playerBody&&_player)_playerBody=_player.GetComponent<Rigidbody2D>();
            bool moving=Activity==CampaignActivity.World&&_playerBody&&_playerBody.linearVelocity.sqrMagnitude>.01f;
            _hotbarIdleFor=moving?0:_hotbarIdleFor+Time.unscaledDeltaTime;
            float target=_hotbarIdleFor>=HotbarShowDelay?HotbarShownY:-_hotbar.sizeDelta.y-12;
            var position=_hotbar.anchoredPosition;
            position.y=CampaignUI.ReducedMotion?target:Mathf.Lerp(position.y,target,1-Mathf.Exp(-14*Time.unscaledDeltaTime));
            _hotbar.anchoredPosition=position;
        }
        void InventoryChanged(){RefreshHotbar();RefreshQuestMarkers();if(Activity==CampaignActivity.Bag&&!_paused)Bag();}
        // Popup ditunda sampai pemain kembali menjelajah (mis. setelah dialog pengambilan barang).
        void QueueReceivedItem(string name,Sprite icon,int quantity){if(!_restoringInventory)_receivedItems.Enqueue((name,icon));}
        void RefreshHotbar()
        {
            var inventory=InventorySystem.Instance;
            if(!inventory||_hotbarSlots==null)return;
            for(int i=0;i<_hotbarSlots.Length&&i<inventory.Slots.Count;i++)
            {
                if(!_hotbarSlots[i])return; // HUD lama sudah dihancurkan (BuildHud ulang)
                var item=inventory.Slots[i];
                _hotbarIcons[i].sprite=item.Icon;_hotbarIcons[i].enabled=!item.IsEmpty&&item.Icon;
                _hotbarCounts[i].text=!item.IsEmpty&&item.Quantity>1?"x"+item.Quantity:"";
                _hotbarSlots[i].color=i==inventory.SelectedIndex?PixelSkin.OrangeLight:Color.white;
            }
        }

        void ApplySafeArea(bool force=false)
        {
            if(!_safeRoot || Screen.width<=0 || Screen.height<=0)return;
            Rect safe=Screen.safeArea;
            if(!force && safe==_lastSafeArea && Screen.width==_lastScreenWidth && Screen.height==_lastScreenHeight)return;
            _lastSafeArea=safe;_lastScreenWidth=Screen.width;_lastScreenHeight=Screen.height;
            _safeRoot.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            _safeRoot.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            _safeRoot.offsetMin=_safeRoot.offsetMax=Vector2.zero;
        }

        void BuildTouchControls()
        {
            foreach(var legacy in FindObjectsByType<VirtualJoystick>(FindObjectsInactive.Include))
            {
                var legacyCanvas=legacy.GetComponentInParent<Canvas>();
                if(legacyCanvas)
                {
                    foreach(var name in new[]{"HUD_Panel","Inventory_Panel","VirtualJoystick","InteractButton","PauseButton","PauseMenuPanel","QuitConfirmPopup"})
                    {
                        var old=legacyCanvas.transform.Find(name);if(old)Destroy(old.gameObject);
                    }
                }
                else legacy.gameObject.SetActive(false);
            }
            _touchControls=CampaignUI.Rect(_hud,"Touch Controls",Vector2.zero,Vector2.one).gameObject;
            // Kontrol gerak = D-pad empat arah (bukan stik analog): arah tegas, cocok untuk map
            // berbasis petak dan lebih terbaca di layar kecil. VirtualJoystick tetap dipakai sebagai
            // sumber baca PlayerController — arahnya disetel tombol-tombol ini lewat SetDirection.
            var touchArea=CampaignUI.Rect(_touchControls.transform,"Dpad area",new Vector2(0,.02f),new Vector2(.46f,.58f));
            var pad=CampaignUI.Rect(touchArea,"Dpad",Vector2.zero,Vector2.zero);
            pad.anchorMin=pad.anchorMax=Vector2.zero;pad.pivot=new Vector2(.5f,.5f);
            pad.anchoredPosition=new Vector2(124,124);pad.sizeDelta=new Vector2(212,212);
            _dpadRect=pad;
            DpadButton(pad,"Atas",0,new Vector2(0,68),180f);
            DpadButton(pad,"Bawah",1,new Vector2(0,-68),0f);
            DpadButton(pad,"Kiri",2,new Vector2(-68,0),-90f);
            DpadButton(pad,"Kanan",3,new Vector2(68,0),90f);
            _joystick=touchArea.gameObject.AddComponent<VirtualJoystick>();
            _joystick.Configure(touchArea,pad,null,VirtualJoystick.SavedMode);_player.BindJoystick(_joystick);

            var interact=CampaignUI.Rect(_touchControls.transform,"Interact",Vector2.one,Vector2.one);
            _interactRect=interact;
            interact.anchorMin=interact.anchorMax=interact.pivot=new Vector2(1,0);
            interact.anchoredPosition=new Vector2(-26,28);interact.sizeDelta=new Vector2(104,104);
            var image=interact.gameObject.AddComponent<Image>();image.sprite=PixelSkin.Disc();image.preserveAspect=true;
            var button=interact.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(_player.Interact);button.navigation=new Navigation{mode=Navigation.Mode.None};
            var label=CampaignUI.Text(interact,"E",Vector2.zero,Vector2.one,34);label.alignment=TextAlignmentOptions.Center;label.color=PixelSkin.TextLight;label.margin=Vector4.zero;
        }

        bool _padUp,_padDown,_padLeft,_padRight;

        /// <summary>Satu tombol arah D-pad: panah yang sama dengan penanda pintu, diputar sesuai
        /// arahnya. Ditekan-tahan (PointerDown/Up); PointerExit ikut melepas supaya arah tidak
        /// "nyangkut" kalau jari digeser keluar tombol.</summary>
        void DpadButton(RectTransform parent,string name,int direction,Vector2 offset,float rotation)
        {
            var rect=CampaignUI.Rect(parent,name,Vector2.one*.5f,Vector2.one*.5f);
            rect.anchoredPosition=offset;rect.sizeDelta=new Vector2(74,74);
            var image=rect.gameObject.AddComponent<Image>();ApplySprite(image,PixelSkin.Button());
            var arrow=CampaignUI.Rect(rect,"Arrow",Vector2.one*.5f,Vector2.one*.5f);
            arrow.sizeDelta=new Vector2(38,38);arrow.localRotation=Quaternion.Euler(0,0,rotation);
            var arrowImage=arrow.gameObject.AddComponent<Image>();
            arrowImage.sprite=PixelSkin.EntranceArrow();arrowImage.preserveAspect=true;arrowImage.raycastTarget=false;
            var trigger=rect.gameObject.AddComponent<EventTrigger>();
            AddPadTrigger(trigger,EventTriggerType.PointerDown,direction,true);
            AddPadTrigger(trigger,EventTriggerType.PointerUp,direction,false);
            AddPadTrigger(trigger,EventTriggerType.PointerExit,direction,false);
        }

        void AddPadTrigger(EventTrigger trigger,EventTriggerType type,int direction,bool pressed)
        {
            var entry=new EventTrigger.Entry{eventID=type};
            entry.callback.AddListener(_=>PressDpad(direction,pressed));
            trigger.triggers.Add(entry);
        }

        void PressDpad(int direction,bool pressed)
        {
            switch(direction)
            {
                case 0:_padUp=pressed;break;
                case 1:_padDown=pressed;break;
                case 2:_padLeft=pressed;break;
                default:_padRight=pressed;break;
            }
            var move=new Vector2((_padRight?1f:0f)-(_padLeft?1f:0f),(_padUp?1f:0f)-(_padDown?1f:0f));
            _joystick?.SetDirection(move.sqrMagnitude>1f?move.normalized:move);
        }

        SpriteRenderer _objectivePing;FloatingPrompt _pingFloat;Transform _pingDoor;float _pingUntil;

        /// <summary>Klik banner tugas = toast judul tugasnya. Sasaran di area yang sama sudah selalu
        /// bertanda panah (lihat <see cref="MarkObjective"/>); kalau sasarannya di area lain, pintu
        /// menuju ke sana ditandai beberapa detik — pintu punya penanda masuknya sendiri.</summary>
        public void PingObjective()
        {
            if(!CanExplore)return;
            var task=CurrentTask;
            if(task==null){Toast(TutorialActive?"Selesaikan tutorial dulu":"Semua tugas bab ini sudah selesai",4);return;}
            if(task.Area!=State.Area){_pingDoor=NextDoor(task.Area)?.transform;_pingUntil=Time.unscaledTime+6f;}
            Toast("Tujuan: "+task.Title,5);
        }

        /// <summary>Panah oranye melayang di atas sasaran tugas (papan menu, tokoh, loket…) selama
        /// sasaran itu ada di area pemain; null = sembunyikan. Posisi dasarnya diserahkan ke
        /// FloatingPrompt — ia menimpa localPosition tiap frame — dan diperbarui terus karena
        /// tokoh bisa berpindah mengikuti jadwalnya.</summary>
        void MarkObjective(Transform target)
        {
            if(!target){if(_objectivePing)_objectivePing.gameObject.SetActive(false);return;}
            if(!_objectivePing)
            {
                var go=new GameObject("Penanda tujuan");
                _objectivePing=go.AddComponent<SpriteRenderer>();
                _objectivePing.sprite=PixelSkin.EntranceArrow();
                _objectivePing.color=PixelSkin.Orange;
                _objectivePing.sortingOrder=YSortOrder.PromptOrderBase+30;
                go.transform.localScale=Vector3.one*1.8f;
                _pingFloat=go.AddComponent<FloatingPrompt>();
            }
            Vector3 position=(Vector2)target.position+new Vector2(0f,1.15f);
            _pingFloat.SetBasePosition(position);
            if(_objectivePing.gameObject.activeSelf)return;
            _objectivePing.transform.position=position;_objectivePing.gameObject.SetActive(true);
        }

        void SetTouchControlsVisible()
        {
            if(!_touchControls)return;
            // Mode efektif bisa berbeda dari preferensi tersimpan: desktop tanpa layar sentuh
            // memulai dengan Hidden otomatis (lihat VirtualJoystick.Configure).
            var mode=_joystick!=null?_joystick.Mode:VirtualJoystick.SavedMode;
            bool visible=CanExplore && mode!=JoystickMode.Hidden;
            _touchControls.SetActive(visible);
            if(visible)_joystick?.SetMode(mode);
        }

        static void ApplySprite(Image image,Sprite sprite)
        {
            if(!image||!sprite)return;image.sprite=sprite;image.type=sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;
        }
        void RefreshHud()
        {
            if(!_objective)return;
            RefreshCoach();
            RefreshEntranceMarkers();
            RefreshQuestMarkers();
            if(_skipTutorial)_skipTutorial.gameObject.SetActive(TutorialActive);
            if(TutorialActive)
            {
                _location.text=$"BAB {Chapter}  •  {Content.Areas[State.Area]}";
                int step=Mathf.Clamp(State.TutorialStep,1,TutorialSteps.Length);
                _objective.text=Objective($"TUTORIAL {step}/{TutorialSteps.Length}",TutorialSteps[step-1].title);
                foreach(var p in _points)if(p.Label)p.Label.gameObject.SetActive(State.TutorialStep==2&&p.Target==TutorialTarget&&p.Area==0);
                return;
            }
            var t=CurrentTask;int done=0;
            var tasks=Content.Tasks;
            for(int i=0;i<tasks.Length;i++)if(State.Progress(tasks[i].Id)?.Complete==true)done++;
            _location.text=$"BAB {Chapter}  •  HARI {State.Day}  •  {Content.Areas[State.Area]}";
            bool waiting=WaitingForCases(t);
            _objective.text=t==null?"Perjalanan bab selesai":waiting?CaseObjective():Objective($"TUGAS {done+1}/{Content.Tasks.Length}",t.Title);
            foreach(var p in _points)
            {
                bool active=t!=null && !waiting && t.Target==p.Target && t.Area==p.Area;
                if(p.Label){p.Label.text=p.Target;p.Label.gameObject.SetActive(active&&p.Area==State.Area);}
            }
        }
        void Update()
        {
            ApplySafeArea();
            var k=Keyboard.current;
            if(k!=null && k.tabKey.wasPressedThisFrame) CycleFocus((k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)?-1:1);
            if(Chapter==0 || State==null)return;
            AnimateHotbar();
            if(_story)return; // cutscene cerita memegang input sendiri
            if(_toast && Time.unscaledTime>_toastUntil)_toast.text="";
            if(k!=null && k.escapeKey.wasPressedThisFrame){if(_paused)Resume();else if(Activity==CampaignActivity.Journal||Activity==CampaignActivity.Bag||Activity==CampaignActivity.Item||Activity==CampaignActivity.Phone)CloseModal();else Pause();return;}
            if(Activity!=CampaignActivity.World)return;
            if(_receivedItems.Count>0&&CanExplore){ShowItemReceived(_receivedItems.Dequeue());return;}
            State.PlaySeconds+=Time.unscaledDeltaTime;
            if(k!=null && k.jKey.wasPressedThisFrame){Journal(0);return;}
            if(k!=null && k.iKey.wasPressedThisFrame){Bag();return;}
            if(k!=null && k.pKey.wasPressedThisFrame){OpenPhone();return;}
            var pos=(Vector2)_player.transform.position;
            State.Area=AreaOf(pos);
            FadeStatusBoard(pos);
            var collider=_player.InteractionTarget;
            var nearest=collider?collider.GetComponentInParent<AdventurePoint>():null;
            bool near=nearest!=null;
            _prompt.text=near&&(!TutorialActive||(State.TutorialStep==2&&nearest.Target==TutorialTarget))?"E • "+PointName(nearest.Target):"";
            if(TutorialActive)
            {
                UpdateTutorial(pos);
                var board=State.TutorialStep==2?FindPoint(TutorialTarget,0)?.transform:null;
                if(board)ShowGuide(board,pos,TutorialTarget);
                else {_guide.text="";_guideArrow.gameObject.SetActive(false);}
                MarkObjective(board);
                if(Time.unscaledTime-_savedAt>20)Save(false);
                return;
            }
            var target=CurrentTask;
            Transform guide=null;bool here=false;
            if(WaitingForCases(target))
            {
                // Bab menunggu Kasus Warga: tunjuk tokoh langkah kasus yang terbuka, atau ranjang kamar kos.
                var (caseTarget,caseArea)=CaseGuideTarget();
                if(caseArea>=0)
                {
                    here=caseArea==State.Area;
                    var door=caseArea==State.Area?null:NextDoor(caseArea);
                    guide=caseArea==State.Area?(_questNpcRoots.TryGetValue(caseTarget,out var npcRoot)&&npcRoot&&npcRoot.activeSelf?npcRoot.transform:null):door?.transform;
                    if(guide)ShowGuide(guide,pos,caseArea==State.Area?PointName(caseTarget):"Pintu ke "+Content.Areas[door.DestinationArea]);
                }
            }
            else if(target!=null)
            {
                here=target.Area==State.Area;
                var door=target.Area==State.Area?null:NextDoor(target.Area);
                guide=target.Area==State.Area?FindPoint(target.Target,target.Area)?.transform:door?.transform;
                if(guide)ShowGuide(guide,pos,target.Area==State.Area?target.Target:"Pintu ke "+Content.Areas[door.DestinationArea]);
            }
            if(!guide){_guide.text="";_guideArrow.gameObject.SetActive(false);}
            MarkObjective(here?guide:Time.unscaledTime<_pingUntil?_pingDoor:null);
            if(Time.unscaledTime-_savedAt>20)Save(false);
        }
        void UpdateTutorial(Vector2 position)
        {
            if(State.TutorialStep!=1)return;
            if(_player.IsAutoWalking){_tutorialLastPosition=position;return;}
            _tutorialDistance+=Vector2.Distance(position,_tutorialLastPosition);_tutorialLastPosition=position;
            if(_tutorialDistance<1f)return;
            State.TutorialStep=2;Save(false);RefreshHud();Toast("Bagus • dekati Papan arah dan tekan E atau tombol interaksi",6);
        }
        /// <summary>Papan status memudar saat pemain berjalan di belakangnya (pojok kiri-atas layar),
        /// supaya toko di tepi map tidak tertutup HUD.</summary>
        void FadeStatusBoard(Vector2 position)
        {
            if(!_statusFade||!Camera.main)return;
            Vector3 view=Camera.main.WorldToViewportPoint(position);
            bool behind=view.z>0f&&view.x<.34f&&view.y>.58f;
            float target=behind?.25f:1f;
            _statusFade.alpha=Mathf.MoveTowards(_statusFade.alpha,target,Time.unscaledDeltaTime*4f);
        }

        void ShowGuide(Transform guide,Vector2 position,string target)
        {
            if(!guide){_guide.text="";_guideArrow.gameObject.SetActive(false);return;}
            Vector2 delta=(Vector2)guide.position-position;
            bool horizontal=Mathf.Abs(delta.x)>Mathf.Abs(delta.y);
            // InteractionArrow.png menunjuk ke ATAS; rotasi Z positif = berlawanan arah jarum jam.
            float angle=horizontal?(delta.x>0?-90:90):(delta.y>0?0:180);
            _guideArrow.rectTransform.localEulerAngles=new Vector3(0,0,angle);_guideArrow.gameObject.SetActive(true);
            string direction=horizontal?(delta.x>0?"Kanan":"Kiri"):(delta.y>0?"Atas":"Bawah");
            _guide.text=direction+"  •  "+target;
        }
        AdventurePoint FindPoint(string target,int area)
        {
            for(int i=0;i<_points.Count;i++)
            {
                var p=_points[i];
                if(p.Target==target&&p.Area==area)return p;
            }
            // Sasaran tugas ditulis dengan nama tokoh ("Bu Tini"), sedangkan warga kota dipasang
            // dengan id "npc:tini" — cocokkan lewat namanya supaya panah petunjuk tetap menemukannya.
            for(int i=0;i<_points.Count;i++)
            {
                var p=_points[i];
                if(p.Area==area&&p.Target!=null&&p.Target.StartsWith("npc:")&&SideQuestContent.Npc(p.Target)?.Name==target)return p;
            }
            return null;
        }
        int _doorFrom=-1,_doorTo=-1;SceneDoor _doorCache;
        SceneDoor NextDoor(int targetArea)
        {
            // Peta pintu tidak berubah saat runtime; BFS cukup sekali per pasangan area
            // (dihitung ulang hanya setelah Travel/Warp mengubah State.Area).
            if(_doorFrom==State.Area&&_doorTo==targetArea)return _doorCache;
            var queue=new Queue<int>();var first=new Dictionary<int,SceneDoor>();queue.Enqueue(State.Area);first[State.Area]=null;
            while(queue.Count>0){int from=queue.Dequeue();foreach(var d in _doors.Where(d=>d.SourceArea==from))
            {if(first.ContainsKey(d.DestinationArea))continue;first[d.DestinationArea]=first[from]??d;if(d.DestinationArea==targetArea)return CacheDoor(targetArea,first[d.DestinationArea]);queue.Enqueue(d.DestinationArea);}}
            return CacheDoor(targetArea,null);
        }
        SceneDoor CacheDoor(int targetArea,SceneDoor door)
        {
            _doorFrom=State.Area;_doorTo=targetArea;_doorCache=door;return door;
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
            if(target.StartsWith("npc:")){InteractQuestNpc(target);return;}
            if(TutorialActive)
            {
                if(State.TutorialStep!=2||target!=TutorialTarget||State.Area!=0){Toast("Ikuti langkah tutorial yang tampil di atas",4);return;}
                Say("Alif","Papan ini menunjukkan jalan keluar stasiun dan arah Warung Bu Siti. Interaksi berhasil—sekarang coba buka Jurnal.",()=>
                {State.TutorialStep=3;Save(false);RefreshHud();});
                return;
            }
            var t=CurrentTask;
            if(t==null){ShowEnding();return;}
            if(WaitingForCases(t))
            {
                // Tugas penutup bab tertahan sampai semua Kasus Warga selesai.
                var (openCase,openStep)=OpenCaseStep();
                string hint=openStep!=null?$"Kasus warga: {openCase.Title} — {openStep.Objective}":"Belum ada kabar baru hari ini • tidur di kamar kos";
                if(t.Target==target&&t.Area==State.Area)Say(t.Speaker,"Masih ada warga yang menunggu uluran tanganmu, Nak. Tuntaskan dulu, nanti ceritakan semuanya padaku.",()=>{SetPlaying();Toast(hint,6);});
                else Toast(hint,5);
                return;
            }
            if(t.Target!=target||t.Area!=State.Area){Toast("Tugas berikutnya: "+t.Title,5);return;}
            _task=t;
            // Pertama bertemu tokoh main quest: adegan cerita dulu, lalu tugasnya.
            // Adegan tokoh dulu (kalau baru kenal), lalu adegan khusus tugas itu (kalau ada),
            // baru dialog tugasnya sendiri.
            PlayStoryOnce(StoryContent.MainIntro(t.Speaker),()=>
                PlayStoryOnce(StoryContent.TaskScene(t.Id),()=>
                {
                    if (t.Kind == "encounter") { Say(t.Speaker,t.Introduction,BeginEncounter); return; }
                    Action begin=()=>{if(t.Board!=null){State.PrepareBoard(t);Save();OpenBoard(MainBoard(t));}else CompleteTalk(t);};
                    // Introduction kosong = pembukanya sudah dibawakan adegan tugasnya (mis. Bu Siti mengantar pesanan).
                    if(string.IsNullOrEmpty(t.Introduction))begin();else Say(t.Speaker,t.Introduction,begin);
                }));
        }
        void CompleteTalk(AdventureTask task)
        {
            if(task.SkipMinutes>0)TimeSystem.Instance?.SkipMinutes(task.SkipMinutes);
            if(State.Accept(task,0,out string feedback)) {Save();ObjectiveCompleted?.Invoke(task.Id);RefreshHud();}
            // Adegan sesudah tugas (mis. Raka masuk setelah Alif selesai makan), baru kalimat penutupnya.
            PlayStoryOnce(StoryContent.TaskOutro(task.Id),()=>
                Say(task.Speaker,feedback,()=>{State.Finish(Content);Save();if(State.Completed)ShowEnding();else SetPlaying();}));
        }
        public void Discover()
        {
            if(Activity!=CampaignActivity.World)return;
            if(TutorialActive){Toast("Selesaikan atau lewati tutorial sebelum menjelajah",4);return;}
            var line=Content.Optional[Mathf.Min(State.Area,Content.Optional.Length-1)].Split('|');
            Say(line[0],line[1],()=>{string id=$"c{Chapter}.optional.{State.Area}";if(!State.Discoveries.Contains(id))State.Discoveries.Add(id);Save();});
        }
        /// <summary>Gedung yang belum ada urusannya dikunci sampai rantai tugas di Buku Perjalanan
        /// selesai — pemain baru tidak tersesat masuk sepuluh toko sebelum alur harinya jalan.
        /// Yang tetap terbuka: gedung tempat tugas berikutnya.</summary>
        /// <remarks>Sebagian gedung punya kuncinya sendiri: terbuka begitu tugas penanda selesai,
        /// tanpa menunggu seluruh rantai. Kamar kos baru bisa dimasuki setelah Bu Siti memberi
        /// alamatnya — sebelum itu Bu Tini menunggu di trotoar depan kos.</remarks>
        static readonly (string area,string task)[] UnlockedByTask = { ("Kamar Alif","c1.rent") };

        bool BuildingLocked(SceneDoor door) => door.EntersBuilding && BuildingLocked(door.DestinationArea);

        bool BuildingLocked(int destinationArea)
        {
            var task = CurrentTask;
            if (task == null) return false;                                  // rantai tugas selesai
            if (destinationArea == task.Area) return false;                  // gedung tugas berikutnya
            if (destinationArea < 0 || destinationArea >= Content.Areas.Length) return false;
            foreach (var (area,unlock) in UnlockedByTask)
                if (Content.Areas[destinationArea] == area) return State.Progress(unlock)?.Complete != true;
            return true;
        }

        /// <summary>Pintu selalu bertanya dulu — pemain sering menyenggol pintu saat menyusuri
        /// tepi ruangan, dan pindah area tanpa diminta membuat dia kehilangan arah.</summary>
        /// <summary>Versi demo berhenti di titik yang diatur <see cref="DemoStage"/>: keluar dari
        /// area penutup pada hari batas memutar adegan penutup lalu layar kredit, bukan berpindah.</summary>
        bool DemoEndsHere(SceneDoor door)
        {
            if(!DemoStage.Enabled||State.Day<DemoStage.EndDay)return false;
            if(State.Area<0||State.Area>=Content.Areas.Length)return false;
            return Content.Areas[State.Area]==DemoStage.EndArea&&door.DestinationArea!=State.Area;
        }

        public void Travel(SceneDoor door)
        {
            if(!CanExplore)return;
            if(DemoEndsHere(door)){PlayStoryOnce(StoryContent.DemoOutroFor(State.BoardTier("c1.offer"),State.Debt),ShowDemoCredits);return;}
            if(TutorialActive){Toast("Selesaikan atau lewati tutorial sebelum meninggalkan stasiun",5);return;}
            if(BuildingLocked(door))
            {
                Toast($"Belum ada urusan di sini. Selesaikan dulu: {CurrentTask.Title}",5);
                return;
            }
            ClearModal(CampaignActivity.Item);
            var panel=Panel(_modal,new Vector2(.32f,.36f),new Vector2(.68f,.64f),Color.white);ApplySprite(panel,PixelSkin.Panel());
            var p=panel.rectTransform;
            Text(p,string.IsNullOrEmpty(door.ConfirmMessage)?"Pindah ke area lain?":door.ConfirmMessage,
                new Vector2(.08f,.46f),new Vector2(.92f,.86f),22).alignment=TextAlignmentOptions.Center;
            Button(p,"Ya",new Vector2(.1f,.14f),new Vector2(.48f,.38f),()=>{CloseModal();BeginTravel(door);});
            Button(p,"Batal",new Vector2(.52f,.14f),new Vector2(.9f,.38f),CloseModal);
            FocusFirst();
        }

        /// <summary>Layar kredit demo: daftar kredit dari DemoStage.Credits, lalu kembali ke menu.</summary>
        void ShowDemoCredits()
        {
            ClearModal(CampaignActivity.Ending);
            var panel=Panel(_modal,new Vector2(.14f,.1f),new Vector2(.86f,.92f),Color.white);ApplySprite(panel,PixelSkin.Panel());
            var p=panel.rectTransform;
            var lines=DemoStage.Credits;
            float top=.86f,height=.62f/Mathf.Max(1,lines.Length);
            for(int i=0;i<lines.Length;i++)
            {
                if(string.IsNullOrEmpty(lines[i]))continue;
                var text=Text(p,lines[i],new Vector2(.06f,top-(i+1)*height),new Vector2(.94f,top-i*height),i==0?26:18,
                    i==0?PixelSkin.Accent:PixelSkin.TextDark);
                text.alignment=TextAlignmentOptions.Center;
                if(i==0)text.fontStyle=FontStyles.Bold;
            }
            Button(p,"Kembali ke menu",new Vector2(.3f,.06f),new Vector2(.7f,.16f),()=>SceneTransition.Load(MenuScene));
            FocusFirst();
        }

        void BeginTravel(SceneDoor door)
        {
            _transition=true;
            SceneFadeController.Instance.TransitionTo(_player,_player.GetComponent<Rigidbody2D>(),door.Destination,Camera.main.GetComponent<CameraFollow>(),()=>
            {
                State.Area=door.DestinationArea;_transition=false;Save();RefreshHud();Toast("Memasuki"+" "+Content.Areas[State.Area],4);
                PlayStoryOnce(StoryContent.AreaIntro(Content.Areas[State.Area]),null);
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
            SnapshotInventory();
            if(CurrencySystem.Instance){State.Money=CurrencySystem.Instance.CurrentMoney;State.Bank=CurrencySystem.Instance.BankBalance;}
            bool ok=ChapterProgress.SaveAdventure(State);_savedAt=Time.unscaledTime;
            if(show)Toast(ok?"Checkpoint tersimpan":"Checkpoint belum tersimpan. Coba lagi lewat menu jeda.",ok?3:10);
            return ok;
        }
        void OnApplicationPause(bool paused){if(paused&&Chapter>0&&State!=null)Save(false);}
        void OnApplicationFocus(bool focused){if(!focused&&Chapter>0&&State!=null)Save(false);}
        void OnApplicationQuit(){if(Chapter>0&&State!=null)Save(false);}
        void SetPlaying(){Activity=CampaignActivity.World;_player?.SetMovementLocked(this, false);GameManager.Instance?.SetState(GameManager.GameState.Playing);RefreshHud();SetTouchControlsVisible();}
        void Toast(string message,float seconds=3){if(_toast){_toast.text=message;_toastUntil=Time.unscaledTime+seconds;}}
        void ClearModal(CampaignActivity screen)
        {
            if(_modal){_modal.gameObject.SetActive(false);Destroy(_modal.gameObject);}
            _buttons.Clear();_modal=CampaignUI.Rect(_canvas,screen.ToString(),Vector2.zero,Vector2.one);Activity=screen;_openedAt=Time.unscaledTime;
            SetTouchControlsVisible();
            _player?.SetMovementLocked(this, true);_player?.SetAdventureDirection(Vector2.zero);_walkTarget=null;
            GameManager.Instance?.SetState(GameManager.GameState.Dialogue);
            Panel(_modal,Vector2.zero,Vector2.one,new Color(.10f,.06f,.04f,.62f));
        }
        void CloseModal()
        {
            bool completedTutorialJournal=Activity==CampaignActivity.Journal&&TutorialActive&&State.TutorialStep==7;
            if(_modal){_modal.gameObject.SetActive(false);Destroy(_modal.gameObject);}_buttons.Clear();SetPlaying();
            if(completedTutorialJournal)CompleteTutorial();
        }
        public void SkipTutorial()
        {
            if(!TutorialActive)return;
            State.TutorialStep=0;Save(false);RefreshHud();Toast("Tutorial dilewati",3);
        }
        void CompleteTutorial()
        {
            State.TutorialStep=0;Save(false);RefreshHud();Toast("Tutorial selesai",3);
        }
        RectTransform Card(CampaignActivity screen,string eyebrow,string title)
        {
            ClearModal(screen);
            var card=Panel(_modal,new Vector2(.075f,.1f),new Vector2(.925f,.86f),Color.white);ApplySprite(card,PixelSkin.Panel());
            var p=card.rectTransform;
            var tab=CampaignUI.Rect(p,"Eyebrow tab",new Vector2(.025f,1),new Vector2(.025f,1));tab.pivot=new Vector2(0,.5f);
            var tabImage=tab.gameObject.AddComponent<Image>();tabImage.raycastTarget=false;ApplySprite(tabImage,PixelSkin.Tab());
            var tabText=Text(tab,eyebrow,Vector2.zero,Vector2.one,16);tabText.alignment=TextAlignmentOptions.Center;tabText.margin=Vector4.zero;tabText.textWrappingMode=TextWrappingModes.NoWrap;
            tab.sizeDelta=new Vector2(tabText.GetPreferredValues(eyebrow).x+40,Mathf.Round(40*_textScale));
            Text(p,title,new Vector2(.03f,.76f),new Vector2(.97f,.9f),31);
            return p;
        }
        Image Panel(Transform parent,Vector2 min,Vector2 max,Color? color=null) => CampaignUI.Panel(parent,"Panel",min,max,color??Ink,true);
        /// <summary>Default warna mengikuti latar: gelap di dalam kartu krem, terang di HUD.</summary>
        TMP_Text Text(Transform parent,string content,Vector2 min,Vector2 max,int size=22,Color? color=null)
        {
            bool onCard=_modal&&parent.IsChildOf(_modal);
            var label=CampaignUI.Text(parent,content,min,max,Mathf.RoundToInt(size*_textScale));label.color=color??(onCard?PixelSkin.TextDark:Paper);return label;
        }
        /// <summary>Teks HUD yang melayang di atas lantai: terang bergaris coklat tua.</summary>
        TMP_Text WorldText(Vector2 min,Vector2 max,int size)
        {
            var label=Text(_hud,"",min,max,size,PixelSkin.TextLight);label.alignment=TextAlignmentOptions.Center;
            label.outlineColor=PixelSkin.Outline;label.outlineWidth=.28f;return label;
        }
        static string Objective(string tag,string title) => $"<color=#{ColorUtility.ToHtmlStringRGB(PixelSkin.Accent)}>{tag}</color>   {title}";
        Button Button(Transform parent,string label,Vector2 min,Vector2 max,Action click)
        {
            var b=CampaignUI.Button(parent,label,min,max,()=>{if(Time.unscaledTime-_openedAt<.13f)return;click();});
            b.GetComponentInChildren<TMP_Text>().fontSize=Mathf.RoundToInt(19*_textScale);
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
            Activity=CampaignActivity.Dialogue;_hud.gameObject.SetActive(false);SetTouchControlsVisible();
        }
        void ExternalDialogueEnded(){_hud.gameObject.SetActive(true);if(_afterDialogue==null)SetPlaying();else SetTouchControlsVisible();}
        void DialogueCompleted(DialogueData data)
        {
            if(data!=_dialogue)return;
            var after=_afterDialogue;_afterDialogue=null;CloseModal();after?.Invoke();
        }
        /// <summary>Papan puzzle yang sedang dibuka — tugas bab utama maupun langkah side quest
        /// memakai UI yang sama; sesi menentukan penyimpanan penempatan dan akibat saat selesai.</summary>
        sealed class BoardSession
        {
            public string Id,Title;
            public ActivityBoard Board;
            public Func<List<int>> Placements;
            public Func<int,int,(bool ok,string feedback)> Place;
            public Func<(bool ok,string feedback)> Commit;
        }
        BoardSession _board;
        void OpenBoard(BoardSession session){_board=session;_hint=0;_selectedCard=-1;_judging=-1;if(session.Board.Document!=null)ShowDocument();else ShowPuzzle();}

        /// <summary>Tampilan dokumen sebelum papan (mis. struk warung): kertas dokumen di kiri,
        /// pembandingnya di kanan, lalu lanjut ke papan. Bisa dibuka lagi dari papannya.</summary>
        void ShowDocument()
        {
            var doc=_board.Board.Document;
            var p=Card(CampaignActivity.Puzzle,doc.Label.ToUpperInvariant(),_board.Title.Split('—')[0]);
            Text(p,doc.Prompt,new Vector2(.04f,.67f),new Vector2(.96f,.77f),18);
            DocumentSheet(p,new Vector2(.04f,.15f),new Vector2(.53f,.65f),doc.Header,doc.Rows,doc.Total,PixelSkin.Slot());
            DocumentSheet(p,new Vector2(.57f,.25f),new Vector2(.96f,.65f),doc.CompareHeader,doc.CompareRows,doc.CompareTotal,PixelSkin.Panel());
            Button(p,"Periksa "+doc.Label+" >",new Vector2(.61f,.035f),new Vector2(.96f,.11f),()=>ShowPuzzle());
            Button(p,"Kembali",new Vector2(.34f,.035f),new Vector2(.54f,.11f),()=>{Save();CloseModal();});
            FocusFirst();
        }
        void DocumentSheet(Transform parent,Vector2 min,Vector2 max,string header,string[] rows,string total,Sprite skin)
        {
            var sheet=Panel(parent,min,max,Color.white);ApplySprite(sheet,skin);sheet.raycastTarget=false;
            var s=sheet.transform;
            var title=Text(s,header,new Vector2(.05f,.82f),new Vector2(.95f,.97f),18,PixelSkin.Accent);
            title.alignment=TextAlignmentOptions.Center;title.fontStyle=FontStyles.Bold;
            float h=Mathf.Min(.16f,.56f/Mathf.Max(1,rows.Length));
            for(int i=0;i<rows.Length;i++)DocumentRow(s,rows[i],.8f-(i+1)*h,h,false);
            DocumentRow(s,total,.05f,.17f,true);
        }
        void DocumentRow(Transform sheet,string row,float y,float height,bool bold)
        {
            var parts=row.Split(new[]{'|'},2);
            var label=Text(sheet,parts[0],new Vector2(.07f,y),new Vector2(.7f,y+height),bold?18:16);
            var amount=Text(sheet,parts.Length>1?parts[1]:"",new Vector2(.7f,y),new Vector2(.93f,y+height),bold?18:16);
            label.alignment=TextAlignmentOptions.MidlineLeft;amount.alignment=TextAlignmentOptions.MidlineRight;
            label.textWrappingMode=amount.textWrappingMode=TextWrappingModes.NoWrap;
            if(bold)label.fontStyle=amount.fontStyle=FontStyles.Bold;
        }
        BoardSession MainBoard(AdventureTask t)=>new BoardSession
        {
            Id=t.Id,Title=t.Title,Board=t.Board,
            Placements=()=>State.PrepareBoard(t).Placements,
            Place=(card,slot)=>(State.Place(t,card,slot,out string feedback),feedback),
            Commit=()=>
            {
                if(!State.CommitBoard(t,out string feedback))return (false,feedback);
                CurrencySystem.Instance?.RestoreBalances(State.Money,State.Bank);ObjectiveCompleted?.Invoke(t.Id);RefreshHud();
                Say(t.Speaker,feedback,()=>{State.Finish(Content);Save();if(State.Completed)ShowEnding();});
                return (true,feedback);
            },
        };
        /// <summary>Ikon kartu anggaran dipilih dari namanya — menu warung punya gambarnya sendiri,
        /// papan anggaran lain (biaya kuliah, acara warga) memakai ikon koin.</summary>
        static Sprite MenuIconFor(string card)
        {
            string name=card.ToLowerInvariant();
            if(name.Contains("geprek"))return PixelSkin.ChickenIcon();
            if(name.Contains("bakso")||name.Contains("soto"))return PixelSkin.BowlIcon();
            if(name.Contains("nasi")||name.Contains("makan"))return PixelSkin.RiceIcon();
            if(name.Contains("air"))return PixelSkin.BottleIcon();
            if(name.Contains("teh")||name.Contains("kopi"))return PixelSkin.TeaIcon();
            if(name.Contains("jus")||name.Contains("es ")||name.Contains("minum"))return PixelSkin.JuiceIcon();
            return PixelSkin.CoinIcon();
        }

        /// <summary>Nama kebutuhan untuk judul langkah ("Makanan", "Minuman").</summary>
        static string GroupLabel(ActivityBoard board,int group) =>
            board.GroupNames!=null&&group<board.GroupNames.Length?board.GroupNames[group]:"Kebutuhan "+(group+1);

        /// <summary>Kartu yang sedang dinilai di papan bebas; -1 = kartu pertama yang belum dinilai,
        /// atau layar ringkasan kalau semuanya sudah.</summary>
        int _judging=-1;

        /// <summary>Papan penilaian bebas (ActivityBoard.Free): satu syarat per layar dengan pilihan
        /// besar, lalu ringkasan yang tiap barisnya bisa dibuka lagi sebelum disampaikan. Tidak ada
        /// benar/salah di sini — akibatnya datang lewat cerita.</summary>
        void ShowJudgement(string feedback=null)
        {
            var board=_board.Board;var placements=_board.Placements();
            int count=board.Cards.Length,current=_judging>=0?_judging:placements.FindIndex(v=>v<0);
            var p=Card(CampaignActivity.Puzzle,"NILAI TAWARAN",_board.Title.Split('—')[0]);
            if(current>=0)
            {
                var step=Text(p,$"SYARAT {current+1} DARI {count}",new Vector2(.04f,.69f),new Vector2(.5f,.76f),17,PixelSkin.Accent);step.fontStyle=FontStyles.Bold;
                // Titik kemajuan: terisi = sudah dinilai, berbingkai = yang sedang dibuka.
                for(int i=0;i<count;i++)
                {
                    var pip=Panel(p,new Vector2(.96f-(count-i)*.035f,.705f),new Vector2(.96f-(count-i)*.035f+.025f,.745f),i==current?PixelSkin.Orange:placements[i]>=0?PixelSkin.Accent:new Color(.78f,.72f,.64f));
                    pip.raycastTarget=false;
                }
                Text(p,board.Instruction,new Vector2(.04f,.59f),new Vector2(.96f,.69f),17);
                var sheet=Panel(p,new Vector2(.1f,.31f),new Vector2(.9f,.58f),Color.white);ApplySprite(sheet,PixelSkin.Slot());sheet.raycastTarget=false;
                var clause=Text(sheet.transform,"\""+board.Cards[current]+"\"",new Vector2(.05f,.08f),new Vector2(.95f,.92f),26);
                clause.alignment=TextAlignmentOptions.Center;
                float width=.84f/board.Slots.Length;
                for(int s=0;s<board.Slots.Length;s++)
                {
                    int slot=s;float x=.08f+s*width;
                    var choice=Button(p,(placements[current]==s?"> ":"")+board.Slots[s],new Vector2(x,.14f),new Vector2(x+width-.03f,.28f),()=>Judge(current,slot));
                    choice.GetComponentInChildren<TMP_Text>().fontSize=Mathf.RoundToInt(23*_textScale);
                }
                Button(p,"Kembali",new Vector2(.04f,.035f),new Vector2(.24f,.11f),()=>{Save();CloseModal();});
                if(placements.All(v=>v>=0))Button(p,"Lihat ringkasan",new Vector2(.66f,.035f),new Vector2(.96f,.11f),()=>{_judging=-1;ShowJudgement();});
            }
            else
            {
                Text(p,"Ini penilaianmu. Ketuk satu baris untuk mengubahnya, lalu sampaikan — Bu Siti akan mengikutinya.",new Vector2(.04f,.66f),new Vector2(.96f,.77f),17);
                float h=.42f/count;
                for(int i=0;i<count;i++)
                {
                    int card=i;float y=.65f-(i+1)*h;
                    var row=Panel(p,new Vector2(.04f,y),new Vector2(.62f,y+h-.012f),Color.white);ApplySprite(row,PixelSkin.Slot());row.raycastTarget=false;
                    var text=Text(row.transform,board.Cards[i],new Vector2(.04f,0f),new Vector2(.96f,1f),16);text.alignment=TextAlignmentOptions.MidlineLeft;
                    Button(p,board.Slots[placements[i]],new Vector2(.64f,y),new Vector2(.96f,y+h-.012f),()=>{_judging=card;ShowJudgement();});
                }
                Button(p,"Sampaikan penilaian",new Vector2(.61f,.035f),new Vector2(.96f,.11f),Commit);
                Button(p,"Kembali",new Vector2(.04f,.035f),new Vector2(.24f,.11f),()=>{Save();CloseModal();});
            }
            if(feedback!=null)Text(p,feedback,new Vector2(.27f,.035f),new Vector2(.6f,.11f),15,PixelSkin.Accent).alignment=TextAlignmentOptions.Center;
            FocusFirst();
        }
        void Judge(int card,int slot)
        {
            var (accepted,feedback)=_board.Place(card,slot);
            if(accepted)Save();
            _judging=-1;ShowJudgement(accepted?null:feedback);
        }

        void ShowPuzzle(string feedback=null)
        {
            if(_board.Board.Free){ShowJudgement(feedback);return;}
            var board=_board.Board;var placements=_board.Placements();
            string kind=board.Kind=="budget"?"ANGGARAN":board.Kind=="inspect"?"PERIKSA DETAIL":board.Kind=="flow"?"ARUS PEMBAYARAN":board.Kind=="match"?"COCOKKAN DOKUMEN":"KELOMPOKKAN BUKTI";
            var p=Card(CampaignActivity.Puzzle,kind,_board.Title.Split('—')[0]);
            Text(p,board.Instruction,new Vector2(.04f,.67f),new Vector2(.96f,.77f),18);
            if(board.Kind=="budget")
            {
                // Satu kebutuhan per langkah: pilih makanan dulu, baru minuman. Kartunya besar
                // dengan gambar, jadi pemain memilih dari gambar bukan dari daftar harga.
                var groups=board.Groups.Where(g=>g>=0).Distinct().OrderBy(g=>g).ToArray();
                int step=Array.FindIndex(groups,g=>!Enumerable.Range(0,board.Cards.Length).Any(i=>board.Groups[i]==g&&placements[i]==1));
                int shown=step>=0?groups[step]:-1;
                string title=step>=0
                    ?$"LANGKAH {step+1} DARI {groups.Length}  —  PILIH {GroupLabel(board,groups[step]).ToUpperInvariant()}"
                    :"PESANANMU  —  KLIK KARTU UNTUK MENGGANTI";
                Text(p,title,new Vector2(.045f,.60f),new Vector2(.55f,.66f),16,PixelSkin.Accent);

                var cards=shown>=0
                    ?Enumerable.Range(0,board.Cards.Length).Where(i=>board.Groups[i]==shown).ToArray()
                    :Enumerable.Range(0,board.Cards.Length).Where(i=>placements[i]==1).ToArray();
                float width=Mathf.Min(.185f,.92f/Mathf.Max(1,cards.Length)),gap=.012f;
                float left=.5f-(cards.Length*(width+gap)-gap)/2f;
                for(int m=0;m<cards.Length;m++)
                {
                    int card=cards[m];bool chosen=placements[card]==1;
                    float x=left+m*(width+gap);
                    var b=Button(p,"",new Vector2(x,.245f),new Vector2(x+width,.585f),()=>Place(card,chosen?0:1));
                    if(b.targetGraphic is Image face)ApplySprite(face,chosen?PixelSkin.Button():PixelSkin.Panel());
                    var caption=b.GetComponentInChildren<TMP_Text>();
                    caption.text=board.Cards[card]+"\nRp"+board.Costs[card].ToString("N0");
                    caption.fontSize=Mathf.RoundToInt(14*_textScale);caption.alignment=TextAlignmentOptions.Center;
                    caption.color=chosen?PixelSkin.TextLight:PixelSkin.TextDark;caption.margin=new Vector4(4,0,4,0);
                    var captionRect=caption.rectTransform;
                    captionRect.anchorMin=new Vector2(.04f,.06f);captionRect.anchorMax=new Vector2(.96f,.40f);
                    captionRect.offsetMin=captionRect.offsetMax=Vector2.zero;
                    Icon(b.transform,MenuIconFor(board.Cards[card]),new Vector2(.24f,.44f),new Vector2(.76f,.94f));
                    if(chosen)Icon(b.transform,PixelSkin.CheckIcon(),new Vector2(.74f,.78f),new Vector2(.96f,.96f));
                }

                int total=board.Total(placements);
                // Sebaris dengan judul langkah (rata kanan) — di bawah kartu sudah ada baris umpan balik.
                Text(p,$"Dana Rp{board.Limit:N0}  •  Dipesan Rp{total:N0}  •  Sisa Rp{board.Limit-total:N0}",
                    new Vector2(.55f,.60f),new Vector2(.955f,.66f),16,total>board.Limit?PixelSkin.Warning:PixelSkin.Accent)
                    .alignment=TextAlignmentOptions.Right;
            }
            else if(board.Kind=="inspect")
            {
                // The world artwork is unchanged. This diagram is a labelled inspection work surface.
                var face=Panel(p,new Vector2(.05f,.29f),new Vector2(.52f,.64f),new Color(.3f,.32f,.3f));
                if(_board.Id=="c3.rules")Icon(face.transform,PromotionIcon,Vector2.zero,Vector2.one);
                else {Text(face.transform,"RADIO  /  S-014",new Vector2(.02f,.62f),new Vector2(.98f,.96f),24,Paper);Text(face.transform,"▥   ───   ◉",new Vector2(.05f,.2f),new Vector2(.95f,.6f),40,Paper);}
                for(int i=0;i<board.Cards.Length;i++){int card=i;float y=.55f-i*.09f;
                    Button(p,(placements[i]==0?"Sudah • ":"Periksa: ")+board.Cards[i],new Vector2(.56f,y),new Vector2(.96f,y+.078f),()=>Place(card,0));}
            }
            else
            {
                for(int i=0;i<board.Cards.Length;i++){int card=i;float h=.4f/board.Cards.Length,y=.63f-(i+1)*h;
                    bool done=placements[i]==board.Answers[i];
                    var b=Button(p,(done?"Sudah • ":_selectedCard==i?"> ":"")+board.Cards[i],new Vector2(.04f,y),new Vector2(.51f,y+h-.008f),()=>{_selectedCard=card;ShowPuzzle();});b.interactable=!done;}
                for(int i=0;i<board.Slots.Length;i++){int slot=i;float h=.4f/board.Slots.Length,y=.63f-(i+1)*h;
                    Button(p,(board.Kind=="flow"?"Urutan: ":"")+board.Slots[i],new Vector2(.57f,y),new Vector2(.96f,y+h-.008f),()=>{if(_selectedCard>=0)Place(_selectedCard,slot);else ShowPuzzle("Pilih kartu di kiri terlebih dahulu.");});}
            }
            Text(p,feedback??"Kemajuan disimpan setiap kali satu bukti ditempatkan dengan benar.",new Vector2(.035f,.13f),new Vector2(.96f,.23f),18,PixelSkin.Accent);
            var document=board.Document;
            if(document==null)
            {
                Button(p,_hint>=2?"Terapkan satu panduan":"Petunjuk",new Vector2(.04f,.035f),new Vector2(.28f,.11f),Hint);
                Button(p,"Kembali",new Vector2(.34f,.035f),new Vector2(.54f,.11f),()=>{Save();CloseModal();});
            }
            else
            {
                // Papan berdokumen: baris tombol dibagi empat supaya dokumennya bisa dilihat lagi.
                Button(p,_hint>=2?"Terapkan satu panduan":"Petunjuk",new Vector2(.04f,.035f),new Vector2(.23f,.11f),Hint);
                Button(p,"Lihat "+document.Label,new Vector2(.25f,.035f),new Vector2(.42f,.11f),ShowDocument);
                Button(p,"Kembali",new Vector2(.44f,.035f),new Vector2(.59f,.11f),()=>{Save();CloseModal();});
            }
            Button(p,"Selesaikan aktivitas",new Vector2(.61f,.035f),new Vector2(.96f,.11f),Commit).interactable=board.Solved(placements);FocusFirst();
        }
        public void Place(int card,int slot)
        {
            if(Activity!=CampaignActivity.Puzzle)return;
            var (accepted,feedback)=_board.Place(card,slot);
            if(accepted){Save();_selectedCard=-1;}ShowPuzzle(feedback);
        }
        void Commit()
        {
            var (ok,feedback)=_board.Commit();
            Save();if(!ok)ShowPuzzle(feedback);
        }
        void Hint()
        {
            var b=_board.Board;var placements=_board.Placements();
            if(_hint++<2){ShowPuzzle(_hint==1?b.Instruction:b.Notes[0]);return;}
            if(b.Kind=="budget")
            {
                // Enumerate the tiny authored board (at most six items) to find a feasible allocation.
                for(int mask=0;mask<(1<<b.Cards.Length);mask++)
                {
                    var values=Enumerable.Range(0,b.Cards.Length).Select(i=>(mask>>i)&1).ToList();if(!b.Solved(values))continue;
                    int next=Enumerable.Range(0,values.Count).FirstOrDefault(i=>placements[i]!=values[i]);Place(next,values[next]);return;
                }
            }
            else for(int i=0;i<b.Cards.Length;i++)if(placements[i]!=b.Answers[i]){Place(i,b.Answers[i]);return;}
        }
        /// <summary>Satu baris Buku Perjalanan: 0 = belum dikerjakan, 1 = sedang dikerjakan,
        /// 2 = selesai, 3 = catatan (bukan tugas, jadi tanpa kotak centang).</summary>
        const int RowsPerJournalPage=5;

        void Journal(int page)
        {
            _journalPage=page;
            var entries=Content.Tasks.Select(t=>(t.Title,State.Progress(t.Id)?.Complete==true?2:t==CurrentTask?1:0))
                .Concat(State.Evidence.Select(e=>("Catatan  /  "+e,3)))
                .Concat(State.Discoveries.Select(id=>("Cerita warga  /  "+Content.Optional[Mathf.Clamp(int.Parse(id.Substring(id.Length-1)),0,Content.Optional.Length-1)].Replace('|',':'),3)))
                .Concat(SideQuestJournalEntries().Select(e=>(e.TrimStart('>','•','✓',' '),e.StartsWith("✓")?2:e.StartsWith(">")?1:0)))
                .ToArray();
            int pages=Mathf.Max(1,(entries.Length+RowsPerJournalPage-1)/RowsPerJournalPage);_journalPage=Mathf.Clamp(page,0,pages-1);
            var p=Card(CampaignActivity.Journal,$"BUKU PERJALANAN  /  HALAMAN {_journalPage+1} DARI {pages}",Content.Title);
            for(int i=0;i<RowsPerJournalPage;i++)
            {
                int n=_journalPage*RowsPerJournalPage+i;if(n>=entries.Length)break;
                JournalRow(p,entries[n].Item1,entries[n].Item2,.70f-i*.115f);
            }
            Button(p,"< Sebelumnya",new Vector2(.04f,.03f),new Vector2(.26f,.12f),()=>Journal(_journalPage-1)).interactable=_journalPage>0;
            Button(p,"Berikutnya >",new Vector2(.29f,.03f),new Vector2(.51f,.12f),()=>Journal(_journalPage+1)).interactable=_journalPage<pages-1;
            Button(p,"Kembali",new Vector2(.74f,.03f),new Vector2(.96f,.12f),CloseModal);FocusFirst();
        }

        /// <summary>Satu baris daftar tugas: kotak centang + judulnya. Yang sedang dikerjakan
        /// memakai kotak oranye, yang selesai dicentang dan teksnya diredupkan, catatan warga
        /// tanpa kotak (bukan tugas).</summary>
        void JournalRow(RectTransform parent,string label,int state,float y)
        {
            var row=CampaignUI.Rect(parent,"Row",new Vector2(.05f,y),new Vector2(.95f,y+.105f));
            if(state<3)
            {
                var box=CampaignUI.Rect(row,"Box",new Vector2(0,.5f),new Vector2(0,.5f));
                box.pivot=new Vector2(0,.5f);box.sizeDelta=new Vector2(30,30);
                var boxImage=box.gameObject.AddComponent<Image>();
                ApplySprite(boxImage,state==1?PixelSkin.Button():PixelSkin.Panel());
                if(state==2)Icon(box,PixelSkin.CheckIcon(),new Vector2(.12f,.16f),new Vector2(.88f,.84f));
            }
            var text=Text(row,label,new Vector2(.045f,0f),new Vector2(1f,1f),19,
                state==2?new Color(PixelSkin.TextDark.r,PixelSkin.TextDark.g,PixelSkin.TextDark.b,.5f):PixelSkin.TextDark);
            text.alignment=TextAlignmentOptions.Left;text.fontStyle=state==1?FontStyles.Bold:FontStyles.Normal;
            text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;
        }
        /// <summary>Popup "Aku mendapatkan …" bergaya kartu hadiah: nama barang, ikon di kotak
        /// slot, tombol OK.</summary>
        void ShowItemReceived((string name,Sprite icon) item)
        {
            ClearModal(CampaignActivity.Item);
            var panel=Panel(_modal,new Vector2(.3f,.2f),new Vector2(.7f,.8f),Color.white);ApplySprite(panel,PixelSkin.Panel());
            var p=panel.rectTransform;
            Text(p,"Aku mendapatkan "+item.name,new Vector2(.06f,.68f),new Vector2(.94f,.9f),24).alignment=TextAlignmentOptions.Center;
            ItemTile(p,item.icon,new Vector2(.5f,.47f),112);
            Button(p,"OK",new Vector2(.3f,.08f),new Vector2(.7f,.24f),CloseModal);FocusFirst();
        }
        RectTransform ItemTile(RectTransform parent,Sprite icon,Vector2 anchor,float size)
        {
            var tile=CampaignUI.Rect(parent,"Item tile",anchor,anchor);tile.sizeDelta=Vector2.one*size;
            var tileImage=tile.gameObject.AddComponent<Image>();tileImage.raycastTarget=false;ApplySprite(tileImage,PixelSkin.Slot());
            if(icon){var image=CampaignUI.Rect(tile,"Icon",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();image.sprite=icon;image.preserveAspect=true;image.raycastTarget=false;image.rectTransform.offsetMin=Vector2.one*size*.18f;image.rectTransform.offsetMax=-Vector2.one*size*.18f;}
            return tile;
        }
        /// <summary>Kartu detail barang: ikon, jumlah, kegunaan & fun fact (ItemCatalog), serta
        /// tombol Pakai/Lepas — barang yang dipakai disorot di hotbar.</summary>
        void ShowItemDetail(int index,bool fromBag)
        {
            var inventory=InventorySystem.Instance;
            if(!inventory||index>=inventory.Slots.Count||inventory.Slots[index].IsEmpty)return;
            var item=inventory.Slots[index];var info=ItemCatalog.Get(item.ItemName);
            bool equipped=inventory.SelectedIndex==index;
            var p=Card(CampaignActivity.Item,"DETAIL BARANG",item.ItemName);
            ItemTile(p,item.Icon,new Vector2(.17f,.5f),170);
            Text(p,(item.Quantity>1?"Jumlah: "+item.Quantity+"   ":"")+(equipped?"Sedang dipakai":""),new Vector2(.03f,.2f),new Vector2(.31f,.3f),16,PixelSkin.Accent).alignment=TextAlignmentOptions.Center;
            Text(p,"KEGUNAAN",new Vector2(.34f,.64f),new Vector2(.96f,.72f),17,PixelSkin.Accent);
            Text(p,info.Usage,new Vector2(.34f,.46f),new Vector2(.96f,.64f),18).alignment=TextAlignmentOptions.TopLeft;
            Text(p,"FUN FACT",new Vector2(.34f,.37f),new Vector2(.96f,.45f),17,PixelSkin.Accent);
            Text(p,info.FunFact,new Vector2(.34f,.15f),new Vector2(.96f,.37f),17).alignment=TextAlignmentOptions.TopLeft;
            Button(p,equipped?"Lepas":"Pakai",new Vector2(.04f,.035f),new Vector2(.28f,.13f),()=>{inventory.Select(index);ShowItemDetail(index,fromBag);});
            Button(p,fromBag?"Kembali ke Tas":"Kembali",new Vector2(.7f,.035f),new Vector2(.96f,.13f),()=>{if(fromBag)Bag();else CloseModal();});
            FocusFirst();
        }

        /// <summary>Kartu Tas: semua slot InventorySystem dengan ikon, nama, dan jumlah.</summary>
        void Bag()
        {
            CoachOpened(6);                       // langkah tutorial "Tas" selesai saat dibuka
            var p=Card(CampaignActivity.Bag,"TAS  /  BARANG BAWAAN","Isi tas Alif");
            var slots=InventorySystem.Instance?InventorySystem.Instance.Slots:null;
            bool any=false;
            for(int i=0;slots!=null&&i<slots.Count;i++)
            {
                var item=slots[i];float x=.04f+i*.184f;
                var tile=Panel(p,new Vector2(x,.3f),new Vector2(x+.17f,.66f),Color.white);ApplySprite(tile,PixelSkin.Slot());
                Image tileIcon=null;
                if(item.IsEmpty){tile.gameObject.AddComponent<InventorySlotDrag>().Setup(i,null);Text(p,"Kosong",new Vector2(x,.19f),new Vector2(x+.17f,.29f),15,PixelSkin.CreamShade*.8f).alignment=TextAlignmentOptions.Center;continue;}
                any=true;
                if(item.Icon){tileIcon=CampaignUI.Rect(tile.transform,"Icon",Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();tileIcon.sprite=item.Icon;tileIcon.preserveAspect=true;tileIcon.raycastTarget=false;tileIcon.rectTransform.offsetMin=Vector2.one*22;tileIcon.rectTransform.offsetMax=-Vector2.one*22;}
                tile.gameObject.AddComponent<InventorySlotDrag>().Setup(i,tileIcon);
                int index=i;var tileButton=tile.gameObject.AddComponent<Button>();tileButton.targetGraphic=tile;tileButton.onClick.AddListener(()=>ShowItemDetail(index,true));
                if(item.Quantity>1)Text(tile.transform,"x"+item.Quantity,Vector2.zero,Vector2.one,17).alignment=TextAlignmentOptions.BottomRight;
                Text(p,item.ItemName,new Vector2(x,.16f),new Vector2(x+.17f,.29f),15).alignment=TextAlignmentOptions.Top;
            }
            Text(p,any?"Klik barang untuk melihat detail • seret ke kotak lain untuk menata.":"Tas masih kosong. Barang yang kamu ambil akan muncul di sini.",new Vector2(.04f,.67f),new Vector2(.96f,.75f),18,PixelSkin.Accent);
            Button(p,"Kembali",new Vector2(.74f,.04f),new Vector2(.96f,.14f),CloseModal);FocusFirst();
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
            Button(p,"Joystick: "+JoystickModeLabel(_joystick!=null?_joystick.Mode:VirtualJoystick.SavedMode),new Vector2(.05f,.12f),new Vector2(.45f,.23f),CycleJoystickMode);
            Button(p,"Simpan & menu utama",new Vector2(.52f,.07f),new Vector2(.95f,.18f),()=>{if(Save())SceneTransition.Load(MenuScene);});
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
            else if(Activity==CampaignActivity.Puzzle)ShowPuzzle();else if(Activity==CampaignActivity.Journal)Journal(_journalPage);else if(Activity==CampaignActivity.Bag)Bag();
            Pause();
        }
        static string JoystickModeLabel(JoystickMode mode) => mode==JoystickMode.Fixed?"tetap":mode==JoystickMode.Floating?"mengambang":"tersembunyi";
        void CycleJoystickMode()
        {
            var current=_joystick!=null?_joystick.Mode:VirtualJoystick.SavedMode;
            var next=(JoystickMode)(((int)current+1)%3);
            VirtualJoystick.SaveMode(next);
            Resume();BuildHud();Pause();
        }
        void ShowEnding()
        {
            var p=Card(CampaignActivity.Ending,Chapter==5?"EPILOG / LIMA BAB SELESAI":$"BAB {Chapter} SELESAI",Content.Title);
            Text(p,Content.Ending,new Vector2(.05f,.3f),new Vector2(.95f,.74f),25);
            Button(p,Chapter==5?"Kembali ke menu":"Lanjut ke bab "+(Chapter+1),new Vector2(.52f,.04f),new Vector2(.95f,.15f),()=>
            {
                if(Chapter==5){SceneTransition.Load(MenuScene);return;}
                if(Chapter==1){SceneTransition.Load("Chapter1Ending");return;}
                State=State.StartChapter(Chapter+1);if(ChapterProgress.SaveAdventure(State))SceneTransition.Load(SceneFor(Chapter+1));
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
            StopSideQuests();
            if (_player) _player.SetMovementLocked(this,false);
            if(InventorySystem.Instance){InventorySystem.Instance.OnInventoryChanged-=InventoryChanged;InventorySystem.Instance.OnSelectionChanged-=RefreshHotbarSelection;InventorySystem.Instance.OnItemAdded-=QueueReceivedItem;}
            UnwatchClock();
            if(TimeSystem.Instance){TimeSystem.Instance.OnMinuteChanged-=RefreshStatusBoard;TimeSystem.Instance.OnDayChanged-=RefreshStatusBoard;}
            if(EnergySystem.Instance)EnergySystem.Instance.OnEnergyChanged-=RefreshStatusEnergy;
            if(CurrencySystem.Instance)CurrencySystem.Instance.OnMoneyChanged-=RefreshStatusMoney;
            if(ScoreSystem.Instance){ScoreSystem.Instance.OnFinancialLogicChanged-=RefreshStatusLogic;ScoreSystem.Instance.OnShariaComplianceChanged-=RefreshStatusSharia;}
            if(DialogueManager.Instance){DialogueManager.Instance.OnLineDisplayed-=ShowDialogueLine;DialogueManager.Instance.OnDialogueCompleted-=DialogueCompleted;DialogueManager.Instance.OnDialogueEnded-=ExternalDialogueEnded;}
            if(_canvas)Destroy(_canvas.gameObject);if(_dialogue)Destroy(_dialogue);if(Instance==this)Instance=null;
        }
    }
}
