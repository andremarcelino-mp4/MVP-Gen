using NUnit.Framework;

namespace GenJutsu.GenCore.Tests
{
    public class FurnitureCommandParserTests
    {
        [Test]
        public void AccentAndCase_MatchFurnitureAndTarget()
        {
            var intent = FurnitureCommandParser.Parse("GEN, TRAZ O SOFÁ PRA PERTO DA JANELA", "Acolhedor");
            Assert.IsTrue(intent.Matched);
            Assert.IsFalse(intent.Reset);
            Assert.AreEqual("almofada", intent.FurnitureId);
            Assert.AreEqual("janela", intent.TargetId);
            Assert.IsTrue(intent.Reply.IndexOf("janela", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }

        [Test]
        public void DemoPhrases_ResolvePerHouse()
        {
            for (var h = 0; h < FurnitureMoveCatalog.Houses.Length; h++)
            {
                var catalog = FurnitureMoveCatalog.Houses[h];
                for (var i = 0; i < catalog.Commands.Length; i++)
                {
                    var cmd = catalog.Commands[i];
                    var intent = FurnitureCommandParser.Parse(cmd.Phrase, catalog);
                    Assert.IsTrue(intent.Matched, catalog.HouseKey + " " + cmd.Phrase);
                    Assert.AreEqual(cmd.Reset, intent.Reset, cmd.Phrase);
                    if (!cmd.Reset)
                    {
                        Assert.AreEqual(cmd.FurnitureId, intent.FurnitureId, cmd.Phrase);
                        Assert.AreEqual(cmd.TargetId, intent.TargetId, cmd.Phrase);
                    }
                }
            }
        }

        [Test]
        public void Reset_IgnoresFurnitureWords()
        {
            var intent = FurnitureCommandParser.Parse("Gen, volta tudo pro lugar da mesa", "Zen");
            Assert.IsTrue(intent.Matched);
            Assert.IsTrue(intent.Reset);
        }

        [Test]
        public void Incomplete_DoesNotMatch()
        {
            Assert.IsFalse(FurnitureCommandParser.Parse("Gen, traz o sofá", "Acolhedor").Matched);
            Assert.IsFalse(FurnitureCommandParser.Parse("perto da janela", "Acolhedor").Matched);
            Assert.IsFalse(FurnitureCommandParser.Parse("boa tarde", "Zen").Matched);
        }
    }
}
