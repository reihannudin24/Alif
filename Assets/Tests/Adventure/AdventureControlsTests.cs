using System.Reflection;
using Alif.Characters;
using Alif.Player;
using Alif.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Alif.Adventure.Tests
{
    public sealed class AdventureControlsTests
    {
        [TestCase(-1, JoystickMode.Fixed)]
        [TestCase(0, JoystickMode.Fixed)]
        [TestCase(1, JoystickMode.Floating)]
        [TestCase(2, JoystickMode.Hidden)]
        [TestCase(3, JoystickMode.Fixed)]
        public void JoystickModeParsingUsesSafeFallback(int saved, JoystickMode expected)
        {
            Assert.That(VirtualJoystick.ParseMode(saved), Is.EqualTo(expected));
        }

        [Test]
        public void SavedJoystickModeRoundTrips()
        {
            try
            {
                VirtualJoystick.SaveMode(JoystickMode.Floating);
                Assert.That(VirtualJoystick.SavedMode, Is.EqualTo(JoystickMode.Floating));
            }
            finally { PlayerPrefs.DeleteKey(VirtualJoystick.ModePreferenceKey); }
        }

        [Test]
        public void FloatingCenterClampsInsideControlZone()
        {
            var area = new Rect(0, 0, 200, 160);
            Assert.That(VirtualJoystick.ClampFloatingCenter(area, new Vector2(-50, 300), 40), Is.EqualTo(new Vector2(40, 120)));
            Assert.That(VirtualJoystick.ClampFloatingCenter(new Rect(0, 0, 40, 40), Vector2.zero, 40), Is.EqualTo(new Vector2(20, 20)));
        }

        [Test]
        public void HiddenModeDisablesTouchRaycasts()
        {
            var go = new GameObject("joystick", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
            try
            {
                var rect = go.GetComponent<RectTransform>();
                var joystick = go.GetComponent<VirtualJoystick>();
                joystick.Configure(rect, rect, rect, JoystickMode.Hidden);
                Assert.That(go.GetComponent<Image>().raycastTarget, Is.False);
                Assert.That(joystick.Mode, Is.EqualTo(JoystickMode.Hidden));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DisabledInteractableDoesNotMaskEnabledSibling()
        {
            var go = new GameObject("interaction", typeof(BoxCollider2D));
            try
            {
                go.AddComponent<TestInteractable>().enabled = false;
                var enabled = go.AddComponent<TestInteractable>();
                var method = typeof(PlayerController).GetMethod("GetInteractable", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(method, Is.Not.Null);
                Assert.That(method.Invoke(null, new object[] { go.GetComponent<Collider2D>() }), Is.SameAs(enabled));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SavedPositionUsesThePlayersActualFeetAgainstSolidInteractables()
        {
            var player=new GameObject("player",typeof(Rigidbody2D),typeof(CapsuleCollider2D),typeof(PlayerController));
            player.transform.localScale=Vector3.one*2;
            var capsule=player.GetComponent<CapsuleCollider2D>();capsule.offset=new Vector2(0,-.34f);capsule.size=new Vector2(.32f,.22f);
            var blocker=new GameObject("solid interactable",typeof(BoxCollider2D));blocker.layer=7;blocker.transform.position=new Vector2(0,-.68f);
            try
            {
                var bodyField=typeof(PlayerController).GetField("_rigidbody",BindingFlags.NonPublic|BindingFlags.Instance);
                if(bodyField.GetValue(player.GetComponent<PlayerController>())==null)
                    typeof(PlayerController).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player.GetComponent<PlayerController>(),null);
                Physics2D.SyncTransforms();
                Assert.That(player.GetComponent<PlayerController>().CanStandAt(Vector2.zero),Is.False);
                blocker.transform.position=Vector2.right*2;Physics2D.SyncTransforms();
                Assert.That(player.GetComponent<PlayerController>().CanStandAt(Vector2.zero),Is.True);
            }
            finally { Object.DestroyImmediate(player);Object.DestroyImmediate(blocker); }
        }
    }

    public sealed class TestInteractable : MonoBehaviour, IInteractable
    {
        public void Interact() { }
    }
}
