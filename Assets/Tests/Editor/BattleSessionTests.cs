using Alif.Battle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BattleSessionTests
{
    private BattleEncounterDefinition _definition;

    [SetUp]
    public void SetUp()
    {
        _definition = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BattleEncounterDefinition>("Assets/ScriptableObjects/Battle/DanaKilat.asset"));
        Assert.That(_definition, Is.Not.Null, "Install encounter before running tests.");
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_definition);

    private static int Correct(BattleSession session) => System.Array.FindIndex(session.Stage.Responses, r => r.Correct);
    private static int Wrong(BattleSession session) => System.Array.FindIndex(session.Stage.Responses, r => !r.Correct);

    [Test]
    public void ThreeCorrectResponsesWinWithoutMutatingDefinition()
    {
        string before = EditorJsonUtility.ToJson(_definition);
        var session = new BattleSession(_definition);
        for (int i = 0; i < 3; i++)
        {
            Assert.That(session.StageIndex, Is.EqualTo(i));
            Assert.That(session.Choose(Correct(session)), Is.True);
            Assert.That(session.EnemyHP, Is.EqualTo(Mathf.Max(0, 100 - 34 * (i + 1))));
            Assert.That(session.Choose(Correct(session)), Is.False, "Double click must not resolve twice.");
            Assert.That(session.Continue(), Is.True);
            Assert.That(session.Continue(), Is.False);
        }
        Assert.That(session.Phase, Is.EqualTo(BattlePhase.Won));
        Assert.That(session.PlayerHP, Is.EqualTo(100));
        Assert.That(session.Choose(0), Is.False);
        Assert.That(EditorJsonUtility.ToJson(_definition), Is.EqualTo(before));
    }

    [Test]
    public void ThreeMistakesLoseAndRetryResetsEverything()
    {
        var session = new BattleSession(_definition);
        int first = Wrong(session);
        session.Choose(first);
        session.Continue();
        Assert.That(session.PlayerHP, Is.EqualTo(66));
        Assert.That(session.StageIndex, Is.Zero);
        Assert.That(session.CanChoose(first), Is.False);
        int second = System.Array.FindIndex(session.Stage.Responses, r => !r.Correct && r != session.Stage.Responses[first]);
        session.Choose(second);
        session.Continue();
        session.Choose(Correct(session));
        session.Continue();
        session.Choose(Wrong(session));
        session.Continue();
        Assert.That(session.PlayerHP, Is.Zero);
        Assert.That(session.Phase, Is.EqualTo(BattlePhase.Lost));
        session.Reset();
        Assert.That(session.PlayerHP, Is.EqualTo(100));
        Assert.That(session.EnemyHP, Is.EqualTo(100));
        Assert.That(session.StageIndex, Is.Zero);
        Assert.That(session.LastResponse, Is.Null);
        Assert.That(session.CanChoose(first), Is.True);
    }

    [Test]
    public void InvalidChoicesCannotChangeHPOrAdvance()
    {
        var session = new BattleSession(_definition);
        Assert.That(session.Choose(-1), Is.False);
        Assert.That(session.Choose(3), Is.False);
        Assert.That(session.Continue(), Is.False);
        Assert.That(session.PlayerHP, Is.EqualTo(100));
        Assert.That(session.EnemyHP, Is.EqualTo(100));
    }

    [Test]
    public void ValidationRejectsUnplayableDataAndMissingReferences()
    {
        Assert.That(_definition.ValidationError(), Is.Null);
        _definition.EnemySprite = null;
        Assert.That(_definition.ValidationError(), Is.Not.Null);
        _definition.PlayerMaxHP = 0;
        Assert.That(_definition.ValidationError(false), Is.Not.Null);
        _definition.PlayerMaxHP = 100;
        _definition.Stages[0].Responses = new BattleResponse[3];
        Assert.That(_definition.ValidationError(false), Is.Not.Null);
        Assert.Throws<System.ArgumentException>(() => new BattleSession(_definition));
    }

    [Test]
    public void ValidationRequiresCorrectAnswerAndFinalStageVictory()
    {
        foreach (var response in _definition.Stages[0].Responses) response.Correct = false;
        Assert.That(_definition.ValidationError(false), Is.Not.Null);
        _definition.Stages[0].Responses[0].Correct = true;
        _definition.EnemyMaxHP = 500;
        Assert.That(_definition.ValidationError(false), Is.Not.Null);
        _definition.EnemyMaxHP = 34;
        Assert.That(_definition.ValidationError(false), Is.Not.Null);
    }
}
