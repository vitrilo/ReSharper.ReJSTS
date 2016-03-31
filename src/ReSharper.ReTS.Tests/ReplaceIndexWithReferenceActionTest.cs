using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
    public class ReplaceIndexWithReferenceActionTest : JavaScriptContextActionExecuteTestBase<ReplaceIndexWithReferenceAction>
    {
        protected override string ExtraPath
        {
            get { return "ReplaceIndexWithReferenceActionTest"; }
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