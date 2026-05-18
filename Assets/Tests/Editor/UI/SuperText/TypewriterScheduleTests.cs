using GameMain.RunTime;
using NUnit.Framework;

namespace Test.Editor.UI.SuperText
{
    public class TypewriterScheduleTests
    {
        [Test]
        public void ComputeRevealedCount_ZeroElapsed_ReturnsZero()
        {
            int revealed = EffectTextRenderer.ComputeRevealedCount(
                elapsed: 0f,
                charsPerSecond: 30f,
                total: 100
            );

            Assert.AreEqual(0, revealed);
        }

        [Test]
        public void ComputeRevealedCount_MidReveal_ReturnsIntermediateCount()
        {
            int revealed = EffectTextRenderer.ComputeRevealedCount(
                elapsed: 0.5f,
                charsPerSecond: 30f,
                total: 100
            );

            Assert.AreEqual(15, revealed);
        }

        [Test]
        public void ComputeRevealedCount_ElapsedPastDuration_SaturatesAtTotal()
        {
            int revealed = EffectTextRenderer.ComputeRevealedCount(
                elapsed: 60f,
                charsPerSecond: 30f,
                total: 10
            );

            Assert.AreEqual(10, revealed);
        }

        [Test]
        public void ComputeRevealedCount_NegativeChars_ReturnsZero()
        {
            int revealed = EffectTextRenderer.ComputeRevealedCount(
                elapsed: 1f,
                charsPerSecond: -5f,
                total: 50
            );

            Assert.AreEqual(0, revealed);
        }

        [Test]
        public void ComputeRevealedCount_ZeroTotal_ReturnsZero()
        {
            int revealed = EffectTextRenderer.ComputeRevealedCount(
                elapsed: 10f,
                charsPerSecond: 30f,
                total: 0
            );

            Assert.AreEqual(0, revealed);
        }
    }
}
