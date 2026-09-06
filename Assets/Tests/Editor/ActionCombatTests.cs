using Alif.Battle;
using NUnit.Framework;

public class ActionCombatTests
{
    [Test]
    public void AttackHasAnticipationAndHitsOnlyOncePerSwing()
    {
        var s = new ActionCombatSession(100);
        Assert.IsTrue(s.TryAttack()); Assert.IsFalse(s.TryAttack()); Assert.IsFalse(s.HitBoss(1, 18));
        s.Tick(.13f); Assert.IsTrue(s.HitBoss(1, 18)); Assert.IsFalse(s.HitBoss(1, 18)); Assert.AreEqual(82,s.BossHP);
        s.Tick(.4f); Assert.IsFalse(s.HitBoss(1,18)); Assert.IsTrue(s.TryAttack()); s.Tick(.13f); Assert.IsTrue(s.HitBoss(1,18));
    }
    [Test]
    public void DodgeImmunityExpiresBeforeCooldownAndDamageIsBounded()
    {
        var s=new ActionCombatSession(100); Assert.IsTrue(s.TryDodge()); Assert.IsFalse(s.HurtPlayer(20));
        s.Tick(.16f); Assert.IsTrue(s.HurtPlayer(20)); Assert.IsFalse(s.HurtPlayer(20)); Assert.IsFalse(s.TryDodge());
        s.Tick(.7f); Assert.IsTrue(s.TryDodge()); s.Tick(.2f); Assert.IsTrue(s.HurtPlayer(int.MaxValue)); Assert.AreEqual(0,s.PlayerHP);
        Assert.IsFalse(s.TryAttack()); Assert.IsFalse(s.TryDodge());
    }
    [Test]
    public void RetryClearsAllTransientCombatState()
    {
        var s=new ActionCombatSession(36); s.TryAttack(); s.Tick(.13f); s.HitBoss(1,36);
        Assert.IsTrue(s.Finished); s.Reset(); Assert.AreEqual(100,s.PlayerHP); Assert.AreEqual(36,s.BossHP);
        Assert.IsFalse(s.Attacking); Assert.IsFalse(s.Dodging); Assert.AreEqual(0,s.Immunity); Assert.AreEqual(0,s.DodgeCooldown);
        Assert.IsTrue(s.TryAttack()); s.Tick(.13f); Assert.IsTrue(s.HitBoss(1,18));
    }
    [Test]
    public void InvalidDamageAndClocksCannotCorruptSession()
    {
        var s=new ActionCombatSession(100); Assert.IsFalse(s.HurtPlayer(-10)); Assert.IsFalse(s.LastBlockedByDodge);
        s.Tick(float.NaN); s.Tick(-1);
        Assert.AreEqual(100,s.PlayerHP); Assert.Throws<System.ArgumentOutOfRangeException>(()=>new ActionCombatSession(1,float.NaN));
    }
    [Test]
    public void ComboBuildsOnHitsThenDecaysAndResetsOnHurt()
    {
        var s=new ActionCombatSession(500);
        for(int i=1;i<=6;i++)
        {
            Assert.IsTrue(s.TryAttack()); s.Tick(.13f); Assert.IsTrue(s.HitBoss(1,18)); Assert.AreEqual(i,s.Combo);
            s.Tick(.4f); // biarkan ayunan selesai sebelum TryAttack berikutnya
        }
        Assert.AreEqual(6,s.BestCombo);
        s.Tick(1.9f); Assert.AreEqual(6,s.Combo);   // masih dalam jendela combo setelah hit terakhir (2.5 - 0.4)
        s.Tick(.3f);   Assert.AreEqual(0,s.Combo);   // jendela lewat -> combo hangus
        Assert.IsTrue(s.TryAttack()); s.Tick(.13f); Assert.IsTrue(s.HitBoss(1,18)); Assert.AreEqual(1,s.Combo);
        Assert.IsTrue(s.HurtPlayer(20)); Assert.AreEqual(0,s.Combo); // kena hit -> combo putus
        Assert.AreEqual(6,s.BestCombo);
        s.Reset(); Assert.AreEqual(0,s.Combo); Assert.AreEqual(0,s.BestCombo);
    }
    [Test]
    public void PerfectDodgeFlagAndRefundSupportTheRewardFlow()
    {
        var s=new ActionCombatSession(100);
        Assert.IsTrue(s.TryDodge());
        Assert.IsFalse(s.HurtPlayer(20)); Assert.IsTrue(s.LastBlockedByDodge);
        s.Tick(.16f); Assert.IsTrue(s.HurtPlayer(20)); Assert.IsFalse(s.LastBlockedByDodge);
        Assert.AreEqual(80,s.PlayerHP);
        Assert.IsFalse(s.TryDodge());          // dodge masih berjalan (durasi .25)
        s.Tick(.1f);                            // dodge selesai -> yang menahan tinggal cooldown
        Assert.IsFalse(s.TryDodge());
        s.RefundDodge();
        Assert.IsTrue(s.TryDodge());
        Assert.IsFalse(s.HurtPlayer(20)); Assert.IsTrue(s.LastBlockedByDodge);
        Assert.AreEqual(80,s.PlayerHP);         // tetap tidak ada damage
    }
}
