using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using NUnit.Framework;
using ReSharper.ReTs;

namespace ReSharper.ReTS.Tests
{
    [TestFixture]
    public class CallWithSameContextWarningFixTest : JavaScriptQuickFixTestBase<CallWithSameContextWarningFix>
    {
        [TestCase("execute01.js")]
        [TestCase("execute02.js")]
        public void Test(string file)
        {
            DoTestFiles(file);
        }
    }
}