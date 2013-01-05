using System.Linq;

namespace RefactorPlus.Tests
{
    using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
    using JetBrains.ReSharper.Intentions.CSharp.Test;
    using JetBrains.ReSharper.Intentions.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class RemoveQualifierExecuteTests : CSharpContextActionExecuteTestBase
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
            return new RemoveTypeQualifierContextAction(dataProvider);
        }

        [Test]
        public void ExecuteTest()
        {
            DoTestFiles("execute01.cs");
        }
    }
}