using NUnit.Framework;
using UnityEngine;

namespace Alif.Adventure.Tests
{
    /// <summary>Warna cahaya siang–malam (DayLight): terang di siang, redup-biru di malam, tanpa lompatan.</summary>
    public class DayLightTests
    {
        static float Brightness(Color c) => (c.r + c.g + c.b) / 3f;

        [Test]
        public void NoonIsFullyLitAndNightIsDimButNeverBlack()
        {
            Assert.That(Brightness(DayLight.Outdoor(12f)), Is.EqualTo(1f).Within(.001f));
            foreach (float hour in new[] { 0f, 3f, 22f, 23.9f })
            {
                float b = Brightness(DayLight.Outdoor(hour));
                Assert.That(b, Is.LessThan(.6f), "jam " + hour);
                Assert.That(b, Is.GreaterThan(.35f), "jam " + hour + " harus tetap terbaca");
            }
        }

        [Test]
        public void NightIsBlueDawnAndDuskAreWarm()
        {
            var night = DayLight.Outdoor(2f); Assert.That(night.b, Is.GreaterThan(night.r));
            var dawn = DayLight.Outdoor(6f); Assert.That(dawn.r, Is.GreaterThan(dawn.b));
            var dusk = DayLight.Outdoor(17.5f); Assert.That(dusk.r, Is.GreaterThan(dusk.b));
        }

        [Test]
        public void LightChangesGraduallyAndWrapsAtMidnight()
        {
            for (float hour = 0f; hour < 24f; hour += .05f)
            {
                Color a = DayLight.Outdoor(hour), b = DayLight.Outdoor(hour + .05f);
                Assert.That(Mathf.Abs(Brightness(a) - Brightness(b)), Is.LessThan(.03f), "lompatan di jam " + hour);
            }
            Assert.That(DayLight.Outdoor(24f), Is.EqualTo(DayLight.Outdoor(0f)));
        }

        [Test]
        public void IndoorsStayBrighterThanOutdoorsAtNight()
        {
            Assert.That(Brightness(DayLight.Indoor(23f)), Is.GreaterThan(Brightness(DayLight.Outdoor(23f)) + .25f));
            Assert.That(Brightness(DayLight.Indoor(12f)), Is.GreaterThan(.9f));
        }
    }
}
