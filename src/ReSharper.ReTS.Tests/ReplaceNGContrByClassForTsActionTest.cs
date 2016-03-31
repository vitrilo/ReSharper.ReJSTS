using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
	public class ReplaceNGContrByClassForTsActionTest : TypeScriptContextActionAvailabilityTestBase<ReplaceNgContrByClassForTsAction>
    {
        protected override string ExtraPath
        {
			get { return "ReplaceNGContrByClassForTsAction"; }
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

		[TestCase("execute02")]
		public void Test2(string file)
		{
			DoOneTest(file);
		}
    }
}