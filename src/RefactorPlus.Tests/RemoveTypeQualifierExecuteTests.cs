using System.Linq;

namespace RefactorPlus.Tests
{
    using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
    using JetBrains.ReSharper.Intentions.CSharp.Test;
    using JetBrains.ReSharper.Intentions.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class RemoveTypeQualifierExecuteTests : CSharpContextActionExecuteTestBase
    {
        private string actionName = typeof(RemoveTypeQualifierAction).Name;

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

        [TestCase("execute01")]
        [TestCase("execute02")]
        [TestCase("execute03")]
        [TestCase("execute04")]
        [TestCase("execute05")]
        [Test]
        public void TestCases(string testSrc)
        {
            DoOneTest(testSrc);
        }
    }
}