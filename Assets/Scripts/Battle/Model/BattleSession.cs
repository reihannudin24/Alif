using System;
using System.Collections.Generic;

namespace Alif.Battle
{
    public enum BattlePhase { Choosing, Feedback, Won, Lost }

    public sealed class BattleSession
    {
        private readonly BattleEncounterDefinition _definition;
        private readonly HashSet<int> _rejected = new HashSet<int>();
        private bool _lastCorrect;
        public int PlayerHP { get; private set; }
        public int EnemyHP { get; private set; }
        public int StageIndex { get; private set; }
        public BattlePhase Phase { get; private set; }
        public BattleResponse LastResponse { get; private set; }
        public BattleStage Stage => _definition.Stages[StageIndex];

        public BattleSession(BattleEncounterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            string error = definition.ValidationError(false);
            if (error != null) throw new ArgumentException(error, nameof(definition));
            _definition = definition;
            Reset();
        }

        public void Reset()
        {
            PlayerHP = _definition.PlayerMaxHP;
            EnemyHP = _definition.EnemyMaxHP;
            StageIndex = 0;
            LastResponse = null;
            _rejected.Clear();
            Phase = BattlePhase.Choosing;
        }

        public bool CanChoose(int index) => Phase == BattlePhase.Choosing && index >= 0 && index < Stage.Responses.Length && !_rejected.Contains(index);

        public bool Choose(int index)
        {
            if (!CanChoose(index)) return false;
            LastResponse = Stage.Responses[index];
            _lastCorrect = LastResponse.Correct;
            if (_lastCorrect) EnemyHP = Math.Max(0, EnemyHP - _definition.Damage);
            else
            {
                PlayerHP = Math.Max(0, PlayerHP - _definition.Damage);
                _rejected.Add(index);
            }
            Phase = BattlePhase.Feedback;
            return true;
        }

        public bool Continue()
        {
            if (Phase != BattlePhase.Feedback) return false;
            if (PlayerHP == 0) Phase = BattlePhase.Lost;
            else if (EnemyHP == 0) Phase = BattlePhase.Won;
            else
            {
                if (_lastCorrect)
                {
                    StageIndex++;
                    _rejected.Clear();
                }
                Phase = BattlePhase.Choosing;
            }
            return true;
        }
    }
}
