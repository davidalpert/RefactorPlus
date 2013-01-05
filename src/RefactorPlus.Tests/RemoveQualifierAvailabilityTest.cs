using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.CSharp.Test;
using JetBrains.ReSharper.Intentions.Extensibility;
using NUnit.Framework;

namespace RefactorPlus.Tests
{
    [TestFixture]
    public class RemoveQualifierAvailabilityTest : CSharpContextActionAvailabilityTestBase
    {
        protected override string ExtraPath
        {
            get { return "RemoveQualifierAction"; }
        }

        protected override string RelativeTestDataPath
        {
            get { return "RemoveQualifierAction"; }
        }

        protected override IContextAction CreateContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            return new RemoveQualifierAction(dataProvider);
        }

        [TestCase("availability01")]
        [Test]
        public void TestCases(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}
