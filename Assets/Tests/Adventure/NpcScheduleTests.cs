using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    /// <summary>Jadwal hadir warga: acak per hari & waktu, deterministik, dan tidak pernah mengosongkan map.</summary>
    public class NpcScheduleTests
    {
        static readonly DayPart[] Parts = (DayPart[])Enum.GetValues(typeof(DayPart));

        [TestCase(0, DayPart.LarutMalam)][TestCase(5, DayPart.LarutMalam)][TestCase(6, DayPart.Pagi)][TestCase(11, DayPart.Pagi)]
        [TestCase(12, DayPart.Siang)][TestCase(17, DayPart.Siang)][TestCase(18, DayPart.Malam)][TestCase(23, DayPart.Malam)]
        public void HoursMapToTheFourDayParts(int hour, DayPart expected) => Assert.That(NpcSchedule.PartOf(hour), Is.EqualTo(expected));

        [Test]
        public void ScheduleIsDeterministicButVariesAcrossDaysAndParts()
        {
            var people = SideQuestContent.Npcs.Where(n => !n.IsObject).Select(n => n.Id).ToList();
            foreach (string npc in people)
            {
                var week = Enumerable.Range(1, 14).SelectMany(d => Parts.Select(p => NpcSchedule.Present(npc, d, p))).ToList();
                var again = Enumerable.Range(1, 14).SelectMany(d => Parts.Select(p => NpcSchedule.Present(npc, d, p))).ToList();
                CollectionAssert.AreEqual(week, again, npc + ": hasil harus sama setelah reload.");
                Assert.That(week.Distinct().Count(), Is.EqualTo(2), npc + " harus kadang hadir dan kadang tidak.");
            }
            int late = Enumerable.Range(1, 30).Sum(d => people.Count(p => NpcSchedule.Present(p, d, DayPart.LarutMalam)));
            int noon = Enumerable.Range(1, 30).Sum(d => people.Count(p => NpcSchedule.Present(p, d, DayPart.Siang)));
            Assert.That(late * 2, Is.LessThan(noon), "Larut malam harus jauh lebih sepi daripada siang.");
        }

        [Test]
        public void EveryoneCanBeMetDuringDaytimeEveryDay()
        {
            foreach (var npc in SideQuestContent.Npcs.Where(n => !n.IsObject))
                for (int day = 1; day <= AdventureState.MaxDay; day++)
                    Assert.That(NpcSchedule.Present(npc.Id, day, DayPart.Pagi) || NpcSchedule.Present(npc.Id, day, DayPart.Siang), Is.True, $"{npc.Id} hari {day}");
        }

        [Test]
        public void NoCityMapIsEverEmpty()
        {
            var spec = JsonUtility.FromJson<CityWorld.Spec>(Resources.Load<TextAsset>("Kota/city").text);
            for (int day = 1; day <= AdventureState.MaxDay; day++)
            {
                var state = new AdventureState { Day = day };
                foreach (var map in spec.maps)
                {
                    var residents = map.npcs.Select(n => SideQuestContent.Npc(n.id))
                        .Where(n => n != null && !n.IsObject && SideQuestRules.NpcVisible(state, n, SideQuestContent.All)).Select(n => n.Id).ToList();
                    foreach (var part in Parts)
                        Assert.That(NpcSchedule.PresentIn(residents, day, part), Is.Not.Empty, $"{map.area} kosong di hari {day} {part}");
                }
            }
        }

        [Test]
        public void DawnWithoutSleepAdvancesTheDayExceptDuringTutorialOrAtTheCap()
        {
            var state = new AdventureState();
            Assert.That(state.PassNight(), Is.True);
            Assert.That(state.Day, Is.EqualTo(2));
            Assert.That(new AdventureState { TutorialStep = 2 }.PassNight(), Is.False);
            Assert.That(new AdventureState { Day = AdventureState.MaxDay }.PassNight(), Is.False);
        }
    }
}
