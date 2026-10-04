using MemosIsland.Story;
using MemosIsland.World;
using NUnit.Framework;

namespace MemosIsland.Tests
{
    public class TextTagTests
    {
        static PlayerProfile Profile(Pronoun p) => new() { name = "Joa", pronoun = p, refugeName = "Refugio Lunita" };

        [Test]
        public void ReplacesNameAndRefuge()
        {
            Assert.AreEqual("¡Joa! Bienvenida al Refugio Lunita.",
                TextTags.Apply("¡{nombre}! Bienvenid{o/a/e} al {refugio}.", Profile(Pronoun.Ella)));
        }

        [Test]
        public void PicksTheOptionByPronoun()
        {
            const string text = "Querid{o/a/e} {nieto/nieta/niete}";
            Assert.AreEqual("Querido nieto", TextTags.Apply(text, Profile(Pronoun.El)));
            Assert.AreEqual("Querida nieta", TextTags.Apply(text, Profile(Pronoun.Ella)));
            Assert.AreEqual("Queride niete", TextTags.Apply(text, Profile(Pronoun.Elle)));
        }

        [Test]
        public void TwoOptionsUseTheSecondForElle()
        {
            Assert.AreEqual("él", TextTags.Apply("{él/elle}", Profile(Pronoun.El)));
            Assert.AreEqual("elle", TextTags.Apply("{él/elle}", Profile(Pronoun.Elle)));
        }

        [Test]
        public void LeavesOtherBracesAlone()
        {
            Assert.AreEqual("Hola, {memo}.", TextTags.Apply("Hola, {memo}.", Profile(Pronoun.El)));
        }

        [Test]
        public void EmptyRefugeNameMeansGrandpas()
        {
            var p = Profile(Pronoun.El);
            p.refugeName = " ";
            Assert.AreEqual("Refugio del abuelo", p.RefugeDisplayName);
        }
    }

    public class CalmMinigameTests
    {
        [Test]
        public void RunningOrRushingScaresIt()
        {
            Assert.AreEqual(-CalmMinigame.RushLoss, CalmMinigame.StepDelta(90f, true, 5f, false));
            Assert.AreEqual(-CalmMinigame.RushLoss, CalmMinigame.StepDelta(90f, false, 0.3f, false));
        }

        [Test]
        public void SlowApproachIsFineOnceItIsCalm()
        {
            Assert.AreEqual(0f, CalmMinigame.StepDelta(60f, false, 2f, true));
            Assert.Less(CalmMinigame.StepDelta(10f, false, 2f, true), 0f, "con mucho miedo, ni despacio te podés acercar");
            Assert.AreEqual(0f, CalmMinigame.StepDelta(10f, false, 2f, false), "alejarse despacio no lo asusta");
        }
    }
}
