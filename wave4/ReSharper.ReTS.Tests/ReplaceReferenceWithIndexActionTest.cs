using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
    public class ReplaceReferenceWithIndexActionTest : JavaScriptContextActionExecuteTestBase<ReplaceReferenceWithIndexAction>
    {
        [TestCase("execute01")]
        public void Test(string file)
        {
            DoOneTest(file);
        }

        protected override string ExtraPath
        {
            get { return "ReplaceReferenceWithIndexActionTest"; }
        }

        protected override string RelativeTestDataPath
        {
            get { return ExtraPath; }
        }
    }
}