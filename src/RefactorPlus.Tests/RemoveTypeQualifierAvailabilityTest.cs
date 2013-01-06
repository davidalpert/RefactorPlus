using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.CSharp.Test;
using JetBrains.ReSharper.Intentions.Extensibility;
using NUnit.Framework;

namespace RefactorPlus.Tests
{
    [TestFixture]
    public class RemoveTypeQualifierAvailabilityTest : CSharpContextActionAvailabilityTestBase
    {
        private string actionName = typeof (RemoveTypeQualifierAction).Name;

        protected override string ExtraPath
        {
            get { return actionName; }
        }

        protected override string RelativeTestDataPath
        {
            get { return actionName; }
        }

        protected override IContextAction CreateContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            return new RemoveTypeQualifierAction(dataProvider);
        }

        [TestCase("availability01")]
        [Test]
        public void TestCases(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}
