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

            ReplaceQualifier(qualifier, string.Empty);

            TreeNodeVisitor visitor = new FindUsingsVisitor(qualifier.QualifiedName);
            _provider.PsiFile.Accept(visitor);

            var usingDirective = factory.CreateUsingDirective(qualifier.QualifiedName);
            IUsingDirective anchor = null;
            _provider.PsiFile.AddImportAfter(usingDirective, anchor);

            return null;
        }

        private void ReplaceQualifier(ITreeNode treeNode, string alias) 
        {
            var textRange = treeNode.GetTreeTextRange();
            var fromOffset = textRange.StartOffset.Offset;
            var toOffset = textRange.EndOffset.Offset;

            if (string.IsNullOrWhiteSpace(alias))
                toOffset += 1; // to remove the '.' character that is no longer needed.

            var range = new TextRange(fromOffset, toOffset);

            this._provider.Document.ReplaceText(range, string.Empty);
        }

        public override string Text
        {
            get { return "Remove qualifier"; }
        }
    }

    public class FindUsingsVisitor : TreeNodeVisitor
    {
        private string qualifiedName;

        public FindUsingsVisitor(string qualifiedName)
        {
            this.qualifiedName = qualifiedName;
        }

        public override void VisitNode(ITreeNode node)
        {
            base.VisitNode(node);
        }
    }
}
