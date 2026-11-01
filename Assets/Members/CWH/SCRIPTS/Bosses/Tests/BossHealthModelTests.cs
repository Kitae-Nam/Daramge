using System;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime;
using Daramge.Bosses.Members.CWH.SCRIPTS.Bosses.Runtime.Common.Health;
using NUnit.Framework;

namespace Members.CWH.SCRIPTS.Bosses.Tests
{
    public sealed class BossHealthModelTests
    {
        [Test]
        public void IndependentModifiersMultiplyAndCanBeRemovedSeparately()
        {
            var health = new BossHealthModel(1000f);
            object protection = new object();
            object groggy = new object();
            health.SetDamageMultiplier(protection, 0.5f);
            health.SetDamageMultiplier(groggy, 1.5f);

            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(75f));
            health.RemoveDamageMultiplier(protection);
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(150f));
            Assert.That(health.HealthRatio, Is.EqualTo(0.775f).Within(0.0001f));
        }

        [Test]
        public void UpdatingSameSourceDoesNotStackItsMultiplier()
        {
            var health = new BossHealthModel(1000f);
            object source = new object();
            health.SetDamageMultiplier(source, 1.5f);
            health.SetDamageMultiplier(source, 1.5f);
            Assert.That(health.ReceiveDamage(100f), Is.EqualTo(150f));
        }

        [Test]
        public void LethalDamageClampsAndNotifiesDeathOnce()
        {
            var health = new BossHealthModel(100f);
            int deaths = 0;
            int changes = 0;
            health.Died += () => deaths++;
            health.HealthChanged += (_, __) => changes++;
            Assert.That(health.ReceiveDamage(float.MaxValue), Is.EqualTo(100f));
            Assert.That(health.ReceiveDamage(1f), Is.Zero);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void ZeroMultiplierBlocksDamageWithoutHealthEvent()
        {
            var health = new BossHealthModel(100f);
            int changes = 0;
            health.HealthChanged += (_, __) => changes++;
            health.SetDamageMultiplier(this, 0f);
            Assert.That(health.ReceiveDamage(50f), Is.Zero);
            Assert.That(changes, Is.Zero);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidNumbersAreRejected(float value)
        {
            var health = new BossHealthModel(100f);
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossHealthModel(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.ReceiveDamage(value));
            Assert.Throws<ArgumentOutOfRangeException>(() => health.SetDamageMultiplier(this, value));
        }

        [Test]
        public void ZeroMaximumHealthIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BossHealthModel(0f));
        }
    }
}
