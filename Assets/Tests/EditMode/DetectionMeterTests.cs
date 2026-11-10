using StealthRaider.AI;
using NUnit.Framework;

namespace StealthRaider.Tests
{
    public class DetectionMeterTests
    {
        const float Near = 0.5f;
        const float Far = 4f;
        const float Decay = 0.2f;

        static DetectionMeter NewMeter() => new DetectionMeter(Near, Far, Decay);

        static float SecondsToFill(DetectionMeter meter, float distance01, float multiplier = 1f)
        {
            const float step = 0.01f;
            float time = 0f;
            while (!meter.IsFull && time < 100f)
            {
                meter.Tick(true, distance01, step, multiplier);
                time += step;
            }
            return time;
        }

        [Test]
        public void FillsFasterUpCloseThanFarAway()
        {
            float near = SecondsToFill(NewMeter(), 0.1f);
            float far = SecondsToFill(NewMeter(), 0.9f);
            Assert.Less(near, far);
        }

        [Test]
        public void FillTimeMatchesConfigAtBothEnds()
        {
            Assert.AreEqual(Near, SecondsToFill(NewMeter(), 0f), 0.02f);
            Assert.AreEqual(Far, SecondsToFill(NewMeter(), 1f), 0.02f);
        }

        [Test]
        public void DistanceBeyondViewIsTreatedAsTheEdge()
        {
            var meter = NewMeter();
            Assert.AreEqual(meter.FillTimeAt(1f), meter.FillTimeAt(3f));
        }

        [Test]
        public void AlertMultiplierShortensFillTime()
        {
            float calm = SecondsToFill(NewMeter(), 0.5f);
            float alert = SecondsToFill(NewMeter(), 0.5f, 2f);
            Assert.AreEqual(calm / 2f, alert, 0.02f);
        }

        [Test]
        public void DrainsWhenUnseenAndNeverGoesBelowZero()
        {
            var meter = NewMeter();
            meter.Tick(true, 0f, Near * 0.5f);
            Assert.AreEqual(0.5f, meter.Value, 0.001f);

            meter.Tick(false, 0f, 1f);
            Assert.AreEqual(0.5f - Decay, meter.Value, 0.001f);

            meter.Tick(false, 0f, 100f);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void NeverGoesAboveOne()
        {
            var meter = NewMeter();
            meter.Tick(true, 0f, 100f);
            Assert.AreEqual(1f, meter.Value);
            Assert.IsTrue(meter.IsFull);
        }

        [Test]
        public void FillAndResetJumpToTheEnds()
        {
            var meter = NewMeter();
            meter.Fill();
            Assert.IsTrue(meter.IsFull);
            meter.Reset();
            Assert.AreEqual(0f, meter.Value);
        }
    }
}
