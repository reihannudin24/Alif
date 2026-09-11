using System;
using Alif.Campaign;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.Battle
{
    public class BattleEncounterController : MonoBehaviour, ICampaignEncounter
    {
        public BattleEncounterDefinition Definition;
        public BattleSession Session { get; private set; }
        public event Action<EncounterResult> Finished;
        RectTransform _canvas;
        bool _paused, _finished;
        float _opened;
        public void Begin()
        {
            if (Definition == null) throw new InvalidOperationException("Dana Kilat definition is missing.");
            string error = Definition.ValidationError();
            if (error != null) throw new InvalidOperationException(error);
            Session = new BattleSession(Definition);
            _finished = false; _paused = false;
            Render();
        }
        public void Choose(int index)
        {
            if (_paused || _finished || Session == null || !Session.Choose(index)) return;
            Render();
        }
        public void Continue()
        {
            if (_paused || _finished || Session == null || !Session.Continue()) return;
            if (Session.Phase == BattlePhase.Won) Complete(EncounterResult.Won);
            else Render();
        }
        public void Retry()
        {
            if (_paused || Session?.Phase != BattlePhase.Lost) return;
            Session.Reset(); Render();
        }
        public void SetPaused(bool paused)
        {
            _paused = paused;
            if (_canvas) _canvas.gameObject.SetActive(!paused);
        }
        public void Cancel() => Complete(EncounterResult.Cancelled);
        void Complete(EncounterResult result)
        {
            if (_finished) return;
            _finished = true;
            if (_canvas) { Destroy(_canvas.gameObject); _canvas = null; }
            Finished?.Invoke(result);
        }
        Button Button(Transform parent, string label, float y, Action action)
        {
            return CampaignUI.Button(parent, label, new Vector2(.05f,y), new Vector2(.95f,y+.09f), () =>
            { if (!_paused && Time.unscaledTime - _opened > .15f) action(); });
        }
        void Render()
        {
            if (_canvas) { _canvas.gameObject.SetActive(false); Destroy(_canvas.gameObject); }
            _opened = Time.unscaledTime;
            _canvas = CampaignUI.Canvas("Dana Kilat", 70);
            var panel = CampaignUI.Panel(_canvas, "Encounter", new Vector2(.06f,.04f), new Vector2(.94f,.96f), CampaignUI.Ink, true).transform;
            CampaignUI.Text(panel, "BAB 2 / GODAAN DANA KILAT", new Vector2(.04f,.91f), new Vector2(.88f,.99f), 23);
            CampaignUI.Button(panel, "Jeda", new Vector2(.86f,.91f), new Vector2(.97f,.99f), () => Alif.Adventure.AdventureGame.Instance?.Pause());
            var picture = CampaignUI.Rect(panel,"Dana Kilat",new Vector2(.7f,.59f),new Vector2(.94f,.89f)).gameObject.AddComponent<Image>();
            picture.sprite = Definition.EnemySprite; picture.preserveAspect = true; picture.raycastTarget = false;
            CampaignUI.Text(panel, $"Keteguhan {Session.PlayerHP}/{Definition.PlayerMaxHP}  •  Godaan {Session.EnemyHP}/{Definition.EnemyMaxHP}", new Vector2(.04f,.81f), new Vector2(.7f,.91f), 23);
            CampaignUI.Text(panel, Session.Stage.Skill + "\n" + Session.Stage.Offer, new Vector2(.04f,.57f), new Vector2(.69f,.81f), 22);
            Button focus;
            if (Session.Phase == BattlePhase.Choosing)
            {
                focus = null;
                for (int i=0;i<Session.Stage.Responses.Length;i++)
                {
                    int choice=i;
                    var b=Button(panel,Session.Stage.Responses[i].Text,.43f-i*.105f,()=>Choose(choice));
                    b.interactable=Session.CanChoose(i); if (focus==null && b.interactable) focus=b;
                }
            }
            else
            {
                CampaignUI.Text(panel, Session.LastResponse.Explanation, new Vector2(.04f,.30f), new Vector2(.96f,.56f), 23);
                focus=Button(panel,Session.Phase==BattlePhase.Lost?"Coba lagi • tanpa biaya":"Lanjutkan",.20f,()=>{if(Session.Phase==BattlePhase.Lost)Retry();else Continue();});
            }
            CampaignUI.Button(panel,"Kembali ke checkpoint",new Vector2(.05f,.05f),new Vector2(.49f,.14f),Cancel);
            CampaignUI.Button(panel,"Baca rujukan MUI",new Vector2(.52f,.05f),new Vector2(.95f,.14f),()=>Application.OpenURL(Definition.ReferenceUrl));
            if(focus)CampaignUI.Focus(focus);
        }
        void OnDestroy() { if (_canvas) Destroy(_canvas.gameObject); }
    }
}
