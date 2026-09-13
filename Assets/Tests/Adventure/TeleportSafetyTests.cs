using Alif.Player;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    /// <summary>
    /// Teleport antar area (SceneDoor/SceneFadeController) memakai
    /// PlayerController.ResolveSafeLandingPosition supaya Player tidak pernah
    /// ditaruh di dalam collider solid walau tujuan pintu tiba-tiba tertutup
    /// objek di runtime. Di sini geometri kaki memakai default field (pusat badan,
    /// half-extents 0.16x0.12) karena Awake tidak jalan di EditMode — cukup untuk
    /// menguji pola geser keluar dari blocker.
    /// </summary>
    public sealed class TeleportSafetyTests
    {
        PlayerController _player;

        [SetUp]
        public void CreatePlayerFarFromProbeZone()
        {
            var go = new GameObject("TeleportTestPlayer");
            _player = go.AddComponent<PlayerController>();
            go.transform.position = new Vector3(50f, 50f, 0f);
        }

        [TearDown]
        public void DestroySceneObjects()
        {
            Object.DestroyImmediate(_player != null ? _player.gameObject : null);
            var blockers = Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < blockers.Length; i++)
            {
                if (blockers[i].name == "TeleportTestBlocker" && blockers[i].transform.parent == null)
                {
                    Object.DestroyImmediate(blockers[i].gameObject);
                }
            }
        }

        [Test]
        public void FreeDestinationIsReturnedUnchanged()
        {
            Physics2D.SyncTransforms();
            Vector2 desired = new Vector2(10f, -4f);
            Vector2 landing = _player.ResolveSafeLandingPosition(desired);
            Assert.That(landing, Is.EqualTo(desired));
        }

        [Test]
        public void BlockedDestinationIsNudgedToStandableSpot()
        {
            var blocker = new GameObject("TeleportTestBlocker");
            blocker.transform.position = Vector3.zero;
            var box = blocker.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 0.8f);
            Physics2D.SyncTransforms();

            Assert.That(_player.CanStandAt(Vector2.zero), Is.False, "sanity: desired spot overlaps blocker");

            Vector2 landing = _player.ResolveSafeLandingPosition(Vector2.zero);
            Assert.That(landing, Is.Not.EqualTo(Vector2.zero));
            Assert.That(_player.CanStandAt(landing), Is.True, "landing spot must be free of solid colliders");
            Assert.That(Vector2.Distance(landing, Vector2.zero), Is.LessThanOrEqualTo(1.5f), "nudge must stay near the door");
        }

        [Test]
        public void FullyBuriedDestinationFallsBackToDesired()
        {
            var blocker = new GameObject("TeleportTestBlocker");
            blocker.transform.position = Vector3.zero;
            var box = blocker.AddComponent<BoxCollider2D>();
            box.size = new Vector2(6f, 6f);
            Physics2D.SyncTransforms();

            Vector2 landing = _player.ResolveSafeLandingPosition(Vector2.zero);
            Assert.That(landing, Is.EqualTo(Vector2.zero), "when every candidate is blocked, keep the authored destination");
        }
    }
}
