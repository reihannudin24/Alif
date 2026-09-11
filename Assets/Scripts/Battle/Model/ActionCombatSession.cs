using System;
using System.Collections.Generic;

namespace Alif.Battle
{
    // Deterministic combat clock. Presentation and physics consume this state.
    public sealed class ActionCombatSession
    {
        public int PlayerHP { get; private set; }
        public int BossHP { get; private set; }
        public float AttackAge { get; private set; } = -1;
        public float DodgeAge { get; private set; } = -1;
        public float DodgeCooldown { get; private set; }
        public float Immunity { get; private set; }
        public int Combo { get; private set; }
        public int BestCombo { get; private set; }
        // Hanya true bila pemanggilan HurtPlayer TERAKHIR diblokir oleh i-frame dodge aktif
        // ("hindar sempurna"). Dibaca langsung setelah HurtPlayer; tidak pernah bertahan
        // melewati pemanggilan berikutnya.
        public bool LastBlockedByDodge { get; private set; }
        public const float ComboWindow = 2.5f;
        public bool Finished => PlayerHP == 0 || BossHP == 0;
        public bool Attacking => AttackAge >= 0;
        public bool Dodging => DodgeAge >= 0;
        public bool AttackActive => AttackAge >= AttackDuration * .27f && AttackAge < AttackDuration * .58f;
        public readonly float AttackDuration, DodgeDuration, DodgeRecovery, DodgeInvulnerability;
        private readonly int _bossMaxHP;
        private readonly HashSet<int> _hitTargets = new HashSet<int>();
        private float _comboTimer;

        public ActionCombatSession(int bossHP, float attack = .45f, float dodge = .25f, float recovery = .8f, float invulnerability = .15f)
        {
            if (bossHP <= 0 || !Positive(attack) || !Positive(dodge) || !Positive(recovery) ||
                !Positive(invulnerability) || invulnerability > dodge) throw new ArgumentOutOfRangeException();
            _bossMaxHP = bossHP; AttackDuration = attack; DodgeDuration = dodge;
            DodgeRecovery = recovery; DodgeInvulnerability = invulnerability;
            Reset();
        }
        private static bool Positive(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
        public void Reset()
        {
            PlayerHP = 100; BossHP = _bossMaxHP; AttackAge = DodgeAge = -1;
            DodgeCooldown = Immunity = 0; _hitTargets.Clear();
            Combo = BestCombo = 0; _comboTimer = 0; LastBlockedByDodge = false;
        }
        public void Tick(float dt)
        {
            if (Finished || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (Attacking) { AttackAge += dt; if (AttackAge >= AttackDuration) AttackAge = -1; }
            if (Dodging) { DodgeAge += dt; if (DodgeAge >= DodgeDuration) DodgeAge = -1; }
            DodgeCooldown = Math.Max(0, DodgeCooldown - dt); Immunity = Math.Max(0, Immunity - dt);
            if (Combo > 0) { _comboTimer -= dt; if (_comboTimer <= 0) Combo = 0; }
        }
        public bool TryAttack()
        {
            if (Finished || Attacking || Dodging) return false;
            AttackAge = 0; _hitTargets.Clear(); return true;
        }
        public bool TryDodge()
        {
            if (Finished || Dodging || DodgeCooldown > 0) return false;
            AttackAge = -1; DodgeAge = 0; DodgeCooldown = DodgeRecovery; return true;
        }
        // Hadiah untuk hindar sempurna: cooldown dodge dicairkan supaya pemain bisa langsung
        // bergerak lagi setelah momen slow-mo.
        public void RefundDodge() { DodgeCooldown = 0; }
        public bool HitBoss(int target, int damage)
        {
            if (Finished || !AttackActive || damage <= 0 || !_hitTargets.Add(target)) return false;
            BossHP = Math.Max(0, BossHP - Math.Min(damage, _bossMaxHP));
            Combo++; BestCombo = Math.Max(BestCombo, Combo); _comboTimer = ComboWindow;
            return true;
        }
        public bool HurtPlayer(int damage)
        {
            if (Finished || damage <= 0) { LastBlockedByDodge = false; return false; }
            if (Dodging && DodgeAge < DodgeInvulnerability) { LastBlockedByDodge = true; return false; }
            LastBlockedByDodge = false;
            if (Immunity > 0) return false;
            PlayerHP = Math.Max(0, PlayerHP - Math.Min(damage, 100)); Immunity = .65f;
            AttackAge = -1; Combo = 0; _comboTimer = 0;
            return true;
        }
    }
}
