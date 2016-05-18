using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
	public class ReplaceByClassForTsActionTest : TypeScriptContextActionAvailabilityTestBase<ReplaceByClassForTsAction>
    {
        protected override string ExtraPath
        {
			get { return "ReplaceByClassForTsActionTest"; }
        }

        protected override string RelativeTestDataPath
        {
            get { return ExtraPath; }
        }

        [TestCase("execute01")]
        public void Test(string file)
        {
            DoOneTest(file);
        }
    }
}