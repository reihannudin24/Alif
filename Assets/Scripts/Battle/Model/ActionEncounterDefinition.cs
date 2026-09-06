using UnityEngine;

namespace Alif.Battle
{
    [CreateAssetMenu(menuName = "Alif/Action Encounter")]
    public sealed class ActionEncounterDefinition : ScriptableObject
    {
        [Range(2, 5)] public int Chapter = 2;
        public string BossName;
        public Sprite BossSprite;
        public Sprite[] CombatPoses;
        public Color Accent = new Color(.3f, .9f, .75f);
        [Min(1)] public int BossHP = 180;
        [Range(1, 100)] public int PlayerDamage = 18, BossDamage = 16;
        [Min(.1f)] public float AttackDuration = .45f, DodgeDuration = .25f, DodgeCooldown = .8f, DodgeImmunity = .15f;
        [Min(.3f)] public float Anticipation = .9f, ActiveDuration = .65f, Recovery = 1.3f;
        public AudioClip SwingSound, HitSound, DodgeSound, VictorySound;
        // Opsional: jika kosong, controller memakai fallback (Hit/Dodge) — aset lama tetap valid.
        public AudioClip EnrageSound, PerfectDodgeSound;
        public string ReferenceTitle;
        public string ReferenceUrl;
        [TextArea(3, 7)] public string Lesson;
        public string ValidationError()
        {
            if (Chapter < 2 || Chapter > 5 || string.IsNullOrWhiteSpace(BossName) || BossSprite == null) return "Identitas boss belum lengkap.";
            if (BossHP <= 0 || PlayerDamage <= 0 || BossDamage <= 0) return "HP dan damage harus positif.";
            if (!(Anticipation >= .3f) || !(ActiveDuration >= .1f) || !(Recovery >= .1f)) return "Timing boss tidak valid.";
            try { new ActionCombatSession(BossHP, AttackDuration, DodgeDuration, DodgeCooldown, DodgeImmunity); }
            catch (System.ArgumentOutOfRangeException) { return "Timing Alif tidak valid."; }
            if (!System.Uri.TryCreate(ReferenceUrl, System.UriKind.Absolute, out var url) || url.Scheme != "https") return "Rujukan HTTPS diperlukan.";
            return null;
        }
    }
}
