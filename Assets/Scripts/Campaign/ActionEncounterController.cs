using System;
using Alif.Battle;
using Alif.Core;
using Alif.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Alif.Campaign
{
    // A bounded mental arena; movement is simulated in arena units, separate from world physics.
    public sealed class ActionEncounterController : MonoBehaviour, ICampaignEncounter
    {
        public ActionEncounterDefinition Definition;
        public Sprite PlayerSprite, Backdrop;
        [SerializeField] Vector2 _arenaHalfSize = new Vector2(4,2);
        [SerializeField] float _moveSpeed = 3, _dodgeSpeed = 8, _attackRange = 1.4f, _dangerRadius = 1.05f;
        public ActionCombatSession Session { get; private set; }
        public event Action<EncounterResult> Finished;
        public enum BossPhase { Anticipation, Active, Recovery }
        public BossPhase Phase { get; private set; }
        public Vector2 PlayerPosition { get; private set; }
        public Vector2 BossPosition => new Vector2(2,0);
        public Vector2 DangerPosition { get; private set; }
        RectTransform _canvas, _arena, _player, _boss, _danger;
        Image _bossImage;
        TMP_Text _status, _telegraph;
        VirtualJoystick _joystick;
        Vector2 _direction, _facing = Vector2.right, _dodgeDirection;
        float _phaseAge;
        bool _paused, _finished, _lossShown;
        AudioSource _audio;
        public void Begin()
        {
            if (!Definition) throw new InvalidOperationException("Action encounter definition is missing.");
            string error=Definition.ValidationError();if(error!=null)throw new InvalidOperationException(error);
            Session=new ActionCombatSession(Definition.BossHP,Definition.AttackDuration,Definition.DodgeDuration,Definition.DodgeCooldown,Definition.DodgeImmunity);
            _audio=GetComponent<AudioSource>();if(!_audio)_audio=gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend=0;
            _finished=false;_paused=false;ResetArena();BuildView();
        }
        void ResetArena()
        {
            Session.Reset();PlayerPosition=new Vector2(-2,0);DangerPosition=PlayerPosition;
            Phase=BossPhase.Anticipation;_phaseAge=0;_lossShown=false;_direction=Vector2.zero;_facing=Vector2.right;
        }
        public void Attack()
        {
            if(!_paused&&!_finished&&Session!=null&&Session.TryAttack())Play(Definition.SwingSound);
        }
        public void Dodge()
        {
            if(!_paused&&!_finished&&Session!=null&&Session.TryDodge())
            { _dodgeDirection=_direction.sqrMagnitude>.01f?_direction.normalized:_facing;Play(Definition.DodgeSound); }
        }
        void Update()
        {
            if(_paused||_finished||Session==null||Session.Finished)return;
            var k=Keyboard.current;
            Vector2 keys=k==null?Vector2.zero:new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),
                (k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
            var stick=_joystick?_joystick.Direction:Vector2.zero;
            _direction=Vector2.ClampMagnitude(keys.sqrMagnitude>=stick.sqrMagnitude?keys:stick,1);
            if(k!=null&&(k.leftShiftKey.wasPressedThisFrame||k.rightShiftKey.wasPressedThisFrame))Dodge();
            if(k!=null&&k.spaceKey.wasPressedThisFrame)Attack();
            if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame && !(EventSystem.current&&EventSystem.current.IsPointerOverGameObject()))Attack();
        }
        void FixedUpdate() => Step(Time.fixedDeltaTime,_direction);
        public void Step(float dt, Vector2 direction)
        {
            if(_paused||_finished||Session==null||Session.Finished||!float.IsFinite(dt)||dt<=0)return;
            direction=Vector2.ClampMagnitude(direction,1);
            if(direction.sqrMagnitude>.01f&&!Session.Dodging)_facing=direction.normalized;
            PlayerPosition+= (Session.Dodging?_dodgeDirection*_dodgeSpeed:direction*_moveSpeed)*dt;
            PlayerPosition=new Vector2(Mathf.Clamp(PlayerPosition.x,-_arenaHalfSize.x+.3f,_arenaHalfSize.x-.3f),Mathf.Clamp(PlayerPosition.y,-_arenaHalfSize.y+.3f,_arenaHalfSize.y-.3f));
            Session.Tick(dt);
            _phaseAge+=dt;
            float duration=Phase==BossPhase.Anticipation?Definition.Anticipation:Phase==BossPhase.Active?Definition.ActiveDuration:Definition.Recovery;
            if(_phaseAge>=duration)
            {
                _phaseAge-=duration;
                Phase=Phase==BossPhase.Anticipation?BossPhase.Active:Phase==BossPhase.Active?BossPhase.Recovery:BossPhase.Anticipation;
                if(Phase==BossPhase.Anticipation)DangerPosition=PlayerPosition;
            }
            Vector2 toBoss=BossPosition-PlayerPosition;
            if(toBoss.magnitude<=_attackRange && Vector2.Dot(_facing,toBoss.normalized)>=.2f && Session.HitBoss(1,Definition.PlayerDamage))Play(Definition.HitSound);
            if(Phase==BossPhase.Active&&Mathf.Abs(PlayerPosition.x-DangerPosition.x)<=_dangerRadius&&Mathf.Abs(PlayerPosition.y-DangerPosition.y)<=_dangerRadius)
            {
                if(Session.HurtPlayer(Definition.BossDamage))Play(Definition.HitSound);
            }
            RefreshView();
            if(Session.Finished)
            {
                if(Session.BossHP==0){Play(Definition.VictorySound);Complete(EncounterResult.Won);}
                else if(!_lossShown){_lossShown=true;ShowLoss();}
            }
        }
        void Position(RectTransform rect,Vector2 position)
        {
            var anchor=new Vector2(.5f+position.x/(_arenaHalfSize.x*2),.5f+position.y/(_arenaHalfSize.y*2));
            rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=Vector2.zero;
        }
        RectTransform Actor(string name,Sprite sprite,Vector2 position)
        {
            var rect=CampaignUI.Rect(_arena,name,Vector2.one*.5f,Vector2.one*.5f);rect.sizeDelta=new Vector2(105,125);
            var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;Position(rect,position);return rect;
        }
        void BuildView()
        {
            if(_canvas){_canvas.gameObject.SetActive(false);Destroy(_canvas.gameObject);}
            _canvas=CampaignUI.Canvas("ActionEncounter",70);
            CampaignUI.Panel(_canvas,"Arena shade",Vector2.zero,Vector2.one,CampaignUI.Ink,true);
            _arena=CampaignUI.Rect(_canvas,"Mental arena",new Vector2(.06f,.27f),new Vector2(.94f,.82f));
            var background=CampaignUI.Panel(_arena,"Remembered world",Vector2.zero,Vector2.one,new Color(.55f,.55f,.55f,1));background.sprite=Backdrop;
            _danger=CampaignUI.Panel(_arena,"Danger",Vector2.zero,Vector2.zero,new Color(1,.65f,.2f,.35f)).rectTransform;
            _danger.sizeDelta=new Vector2(150,100);
            CampaignUI.Text(_danger,"!\nHINDARI",Vector2.zero,Vector2.one,18).alignment=TextAlignmentOptions.Center;
            _player=Actor("Alif",PlayerSprite,PlayerPosition);_boss=Actor(Definition.BossName,Definition.BossSprite,BossPosition);_bossImage=_boss.GetComponent<Image>();
            _status=CampaignUI.Text(_canvas,"",new Vector2(.05f,.88f),new Vector2(.84f,.98f),24);
            _telegraph=CampaignUI.Text(_canvas,"",new Vector2(.05f,.81f),new Vector2(.85f,.88f),22);
            CampaignUI.Button(_canvas,"Jeda",new Vector2(.86f,.89f),new Vector2(.96f,.98f),()=>Alif.Adventure.AdventureGame.Instance?.Pause());
            var pad=CampaignUI.Panel(_canvas,"Gerak",new Vector2(.05f,.025f),new Vector2(.22f,.24f),new Color(.18f,.3f,.32f),true).rectTransform;
            var knob=CampaignUI.Panel(pad,"Knob",Vector2.one*.5f,Vector2.one*.5f,CampaignUI.Paper).rectTransform;knob.sizeDelta=new Vector2(38,38);
            _joystick=pad.gameObject.AddComponent<VirtualJoystick>();_joystick.Configure(pad,knob);
            CampaignUI.Text(_canvas,"WASD / panah: gerak\nHadapi bayangan untuk menyerang.\nKeluar dari tanda ! sebelum serangan.",new Vector2(.24f,.02f),new Vector2(.64f,.24f),20);
            var attack=CampaignUI.Button(_canvas,"Serang\nSpasi",new Vector2(.65f,.04f),new Vector2(.8f,.23f),Attack);
            var dodge=CampaignUI.Button(_canvas,"Hindar\nShift",new Vector2(.82f,.04f),new Vector2(.97f,.23f),Dodge);
            var navigation=new Navigation{mode=Navigation.Mode.None};attack.navigation=navigation;dodge.navigation=navigation;
            EventSystem.current?.SetSelectedGameObject(null);RefreshView();
        }
        void RefreshView()
        {
            if(!_canvas)return;
            Position(_player,PlayerPosition);Position(_danger,DangerPosition);
            var radius=new Vector2(_dangerRadius/(_arenaHalfSize.x*2),_dangerRadius/(_arenaHalfSize.y*2));
            _danger.anchorMin-=radius;_danger.anchorMax+=radius;_danger.offsetMin=_danger.offsetMax=Vector2.zero;
            _danger.gameObject.SetActive(Phase!=BossPhase.Recovery);
            _danger.GetComponent<Image>().color=Phase==BossPhase.Active?new Color(1,.3f,.2f,.5f):new Color(1,.75f,.3f,.3f);
            if(Definition.CombatPoses!=null&&Definition.CombatPoses.Length>0)
                _bossImage.sprite=Definition.CombatPoses[Mathf.Min((int)Phase,Definition.CombatPoses.Length-1)]??Definition.BossSprite;
            _status.text=$"BAB {Definition.Chapter} / {Definition.BossName}    Keteguhan {Session.PlayerHP}    Godaan {Session.BossHP}";
            _telegraph.text=Phase==BossPhase.Anticipation?"! Bersiap — tinggalkan area bertanda":Phase==BossPhase.Active?"! SERANGAN AKTIF — hindari tanda":"Celah terbuka — dekati dan serang";
        }
        void ShowLoss()
        {
            var panel=CampaignUI.Panel(_canvas,"Retry",new Vector2(.24f,.3f),new Vector2(.76f,.72f),CampaignUI.Ink,true).transform;
            CampaignUI.Text(panel,"Tarik napas. Bukti dan uang tetap aman.",new Vector2(.04f,.59f),new Vector2(.96f,.95f),25);
            CampaignUI.Focus(CampaignUI.Button(panel,"Coba lagi • tanpa biaya",new Vector2(.05f,.31f),new Vector2(.95f,.55f),Retry));
            CampaignUI.Button(panel,"Kembali ke checkpoint",new Vector2(.05f,.04f),new Vector2(.95f,.27f),Cancel);
        }
        public void Retry(){if(_paused||Session==null||Session.PlayerHP>0)return;ResetArena();BuildView();}
        public void SetPaused(bool paused){_paused=paused;_direction=Vector2.zero;if(_canvas)_canvas.gameObject.SetActive(!paused);}
        public void Cancel()=>Complete(EncounterResult.Cancelled);
        void Complete(EncounterResult result)
        {
            if(_finished)return;_finished=true;
            if(_canvas){Destroy(_canvas.gameObject);_canvas=null;}
            Finished?.Invoke(result);
        }
        void Play(AudioClip clip){if(clip&&_audio)_audio.PlayOneShot(clip,.65f);}
        void OnDestroy(){if(_canvas)Destroy(_canvas.gameObject);}
    }
}
