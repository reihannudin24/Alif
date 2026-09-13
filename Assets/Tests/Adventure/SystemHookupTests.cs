using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Alif.Campaign;
using Alif.Systems;

namespace Alif.Adventure.Tests
{
    /// <summary>
    /// Perilaku hookup polish: seleksi slot inventory dan broadcast event campaign
    /// dari DialogueData.OnCompleteEventId.
    /// </summary>
    public class SystemHookupTests
    {
        private GameObject _owner;

        [SetUp]
        public void SetUp()
        {
            _owner = new GameObject("InventorySystem under test");
            var system = _owner.AddComponent<InventorySystem>();
            system.InitializeSlots();
        }

        [TearDown]
        public void TearDown()
        {
            if (_owner != null) Object.DestroyImmediate(_owner);
            CampaignEvents.ResetForTests();
        }

        private InventorySystem System => _owner.GetComponent<InventorySystem>();

        [Test]
        public void SelectingOccupiedSlotTogglesSelectionAndSkipsEmptySlots()
        {
            var system = System;
            Assert.That(system.AddItem("Voucher kereta", null, 2), Is.True);

            Assert.That(system.Select(0), Is.True);
            Assert.That(system.SelectedIndex, Is.EqualTo(0));

            // Klik kedua pada slot terpilih membatalkan seleksi.
            Assert.That(system.Select(0), Is.True);
            Assert.That(system.SelectedIndex, Is.EqualTo(-1));

            // Slot kosong dan index di luar jangkauan tidak mengubah seleksi.
            Assert.That(system.Select(0), Is.True);
            Assert.That(system.Select(1), Is.False);
            Assert.That(system.Select(4), Is.False);
            Assert.That(system.Select(-1), Is.False);
            Assert.That(system.Select(9), Is.False);
            Assert.That(system.SelectedIndex, Is.EqualTo(0));
        }

        [Test]
        public void SelectionChangesNotifySubscribersWithTheNewIndex()
        {
            var system = System;
            Assert.That(system.AddItem("Voucher kereta", null, 1), Is.True);

            var notified = new List<int>();
            system.OnSelectionChanged += notified.Add;

            system.Select(0);
            system.Select(0);

            Assert.That(notified, Is.EqualTo(new[] { 0, -1 }));
        }

        [Test]
        public void RemovingLastItemClearsSelection()
        {
            var system = System;
            Assert.That(system.AddItem("Karcis", null, 1), Is.True);
            Assert.That(system.Select(0), Is.True);

            var notified = new List<int>();
            system.OnSelectionChanged += notified.Add;

            system.RemoveItemAt(0);

            Assert.That(system.SelectedIndex, Is.EqualTo(-1));
            Assert.That(notified, Is.EqualTo(new[] { -1 }));
            Assert.That(system.Select(0), Is.False, "Slot yang baru dikosongkan tidak bisa dipilih.");
        }

        [Test]
        public void RemovingPartialStackKeepsSelection()
        {
            var system = System;
            Assert.That(system.AddItem("Koin", null, 3), Is.True);
            Assert.That(system.Select(0), Is.True);

            system.RemoveItemAt(0, 1);

            Assert.That(system.SelectedIndex, Is.EqualTo(0), "Sisa item di slot yang sama tetap terpilih.");
        }

        [Test]
        public void CompletedCampaignEventsReachSubscribersAndEmptyIdsAreIgnored()
        {
            var received = new List<string>();
            CampaignEvents.Completed += received.Add;

            CampaignEvents.NotifyCompleted("quest.c1.papan");
            CampaignEvents.NotifyCompleted(null);
            CampaignEvents.NotifyCompleted("");

            Assert.That(received, Is.EqualTo(new[] { "quest.c1.papan" }));
        }

        [Test]
        public void FailingCampaignEventSubscriberDoesNotBreakTheCaller()
        {
            CampaignEvents.Completed += _ => throw new System.Exception("subscriber bug");
            var received = new List<string>();
            CampaignEvents.Completed += received.Add;

            Assert.DoesNotThrow(() => CampaignEvents.NotifyCompleted("quest.safe"));
            Assert.That(received, Is.EqualTo(new[] { "quest.safe" }), "Subscriber lain tetap menerima event.");
        }
    }
}
