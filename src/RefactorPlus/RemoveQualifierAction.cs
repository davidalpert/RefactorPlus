using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.Extensibility.Menu;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace RefactorPlus
{
    [ContextAction(Name = "RemoveQualifierAction", Description = "Removes a namespace qualifier, adding a using if required.", Group = "C#")]
    public class RemoveQualifierAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _provider;
        private ILiteralExpression _stringLiteral;

        public RemoveQualifierAction(ICSharpContextActionDataProvider provider)
        {
            _provider = provider;
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var typeDeclaration = _provider.GetSelectedElement<IUserDeclaredTypeUsage>(true, true);

            return (typeDeclaration != null
                    && typeDeclaration.TypeName != null
                    && typeDeclaration.TypeName.Qualifier != null);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            CSharpElementFactory factory = CSharpElementFactory.GetInstance(_provider.PsiModule);

            var typeDeclaration = _provider.GetSelectedElement<IUserDeclaredTypeUsage>(true, true);
            if (typeDeclaration == null
             || typeDeclaration.TypeName == null
             || typeDeclaration.TypeName.Qualifier == null)
                return null;

            var qualifier = typeDeclaration.TypeName.Qualifier;

            var textRange = qualifier.GetTreeTextRange();
            TextRange range = new TextRange(textRange.StartOffset.Offset, textRange.EndOffset.Offset+1);
            _provider.Document.ReplaceText(range, string.Empty);

            return null;
        }

        public override string Text
        {
            get { return "Remove qualifier"; }
        }
    }
}
