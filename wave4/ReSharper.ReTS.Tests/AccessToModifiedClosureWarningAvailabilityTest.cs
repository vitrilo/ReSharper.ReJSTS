using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Psi;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
    public class AccessToModifiedClosureWarningAvailabilityTest : QuickFixAvailabilityTestBase
    {
        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile psiSourceFile)
        {
            return highlighting is AccessToModifiedClosureWarning;
        }

        [TestCase("test01.js")]
        [TestCase("test02.js")]
        [TestCase("test03.js")]
        [TestCase("test04.js")]
        [TestCase("test05.js")]
        [TestCase("test06.js")]
        [TestCase("test07.js")]
        [TestCase("test08.js")]
        [TestCase("test09.js")]
        public void Test(string file)
        {
            DoTestFiles(file);
        }
    }
}