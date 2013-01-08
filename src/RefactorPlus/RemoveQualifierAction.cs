using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace RefactorPlus
{
    [ContextAction(Description = "Remove type qualifier", Group = "C#", Name = "RemoveTypeQualifier", Priority = 1)]
    public class RemoveTypeQualifierAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider provider;
        private string ACTION_NAME = "Remove type qualifier";

        public RemoveTypeQualifierAction(ICSharpContextActionDataProvider provider)
        {
            this.provider = provider;
        }

        public override string Text
        {
            get { return ACTION_NAME; }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var typeUsage = this.provider.GetSelectedElement<IUserDeclaredTypeUsage>(true, true);

            return typeUsage != null
                && typeUsage.TypeName != null
                && typeUsage.TypeName.Qualifier != null;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            return RemoveTypeQualifier();
        }

        private Action<ITextControl> RemoveTypeQualifier()
        {
            var typeUsage = this.provider.GetSelectedElement<IUserDeclaredTypeUsage>(true, true);
            if (typeUsage == null)
                return null;

            var referenceName = typeUsage.FirstChild as IReferenceName;
            if (referenceName == null)
                return null;

            var typeQualifier = referenceName.Qualifier;
            if (typeQualifier == null)
                return null;

            string qualifyingNamespace = typeQualifier.QualifiedName;

            // Resolving the qualifier is required to handle the 
            // case of RegularExpressions in the following source:
            //
            // namespace System.Text { 
            //   public class Class1 { 
            //     public void Method1 { 
            //       var x = new RegularExpressions.Regex("a"); 
            //     } 
            //   } 
            // }
            var resolvedNamespace = AttemptToResolve(typeQualifier);
            if (resolvedNamespace != null)
                qualifyingNamespace = resolvedNamespace.QualifiedName;

            var file = this.provider.PsiFile;
            var usingDirective = EnsureUsingExists(qualifyingNamespace, file);

            if (usingDirective == null)
                return null;

            var newQualifier = ReQualify(typeQualifier, usingDirective);

            var visitor = new ReplaceAllTypeQualifiersVisitor(usingDirective, newQualifier);
            file.Accept(visitor);

            var caretTarget = typeUsage.GetDocumentStartOffset();

            if (newQualifier != null)
            {
                caretTarget = newQualifier.GetDocumentStartOffset();
            }

            DocOffsetAndVirtual cursorOffset = new DocOffsetAndVirtual(caretTarget.TextRange.StartOffset);
            return tc => tc.Caret.MoveTo(cursorOffset, CaretVisualPlacement.Generic);
        }

        private INamespace AttemptToResolve(IReferenceName typeQualifier)
        {
            var resolveResult = typeQualifier.Reference.Resolve();
            var resolvedNamespace = resolveResult.DeclaredElement as INamespace;
            return resolvedNamespace;
        }

        private IReferenceName ReQualify(IReferenceName typeQualifier, IUsingDirective usingDirective)
        {
            if (usingDirective is IUsingAliasDirective)
            {
                var usingAliasDirective = usingDirective as IUsingAliasDirective;

                var aliasName = usingAliasDirective.AliasName;

                IReferenceName newQualifier =
                    this.provider.ElementFactory.CreateReferenceName(aliasName);

                return typeQualifier.ReplaceBy(newQualifier);
            }

            if (typeQualifier != null) 
                ModificationUtil.DeleteChildRange(typeQualifier, typeQualifier.NextSibling);

            return null;
        }

        private IUsingDirective EnsureUsingExists(string qualifyingNamespace, ICSharpFile file)
        {
            var existingUsing =
                file.ImportsEnumerable.FirstOrDefault(i =>
                    i.ImportedSymbolName.QualifiedName.Equals(qualifyingNamespace, StringComparison.InvariantCultureIgnoreCase));

            return existingUsing ?? AddUsing(qualifyingNamespace, file);
        }

        private IUsingDirective AddUsing(string qualifyingNamespace, ICSharpFile toFile)
        {
            FindUsingsListVisitor findUsingsListVisitor = new FindUsingsListVisitor();
            toFile.ProcessDescendantsForResolve(findUsingsListVisitor);

            var usingList = findUsingsListVisitor.UsingList;
            if (usingList == null)
                return null;

            var newUsing = this.provider.ElementFactory.CreateUsingDirective(qualifyingNamespace);

            IUsingDirective anchor = usingList.LastChild as IUsingDirective;
            if (anchor == null)
                return ModificationUtil.AddChild(usingList, newUsing);

            return ModificationUtil.AddChildAfter(anchor, newUsing);
        }
    }

    public class ReplaceAllTypeQualifiersVisitor : TreeNodeVisitor
    {
        private readonly IUsingDirective usingDirective;
        private readonly IReferenceName replacingReference;

        public ReplaceAllTypeQualifiersVisitor(IUsingDirective usingDirective, IReferenceName replacingReference)
        {
            this.usingDirective = usingDirective;
            this.replacingReference = replacingReference;
        }

        public override void VisitUsingList(IUsingList usingListParam)
        {
            // ignore
        }

        public override void VisitUserDeclaredTypeUsage(IUserDeclaredTypeUsage userDeclaredTypeUsageParam)
        {
            return;
            var typeQualifier = userDeclaredTypeUsageParam.TypeName.Qualifier;
            if (IsMatch(typeQualifier, usingDirective))
                Replace(typeQualifier, replacingReference);
        }

        public override void VisitReferenceName(IReferenceName existingReference)
        {
            if (IsMatch(existingReference, usingDirective))
                Replace(existingReference, replacingReference);
        }

        private bool IsMatch(IReferenceName existingReference, IUsingDirective usingDirective)
        {
            var result = existingReference.Reference.Resolve();
            if (result.IsValid())
            {
                var ns = result.DeclaredElement as INamespace;
                if (ns == null)
                    return false;

                if (ns.QualifiedName.Equals(usingDirective.ImportedSymbolName.QualifiedName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void Replace(IReferenceName existingReference, IReferenceName referenceName)
        {
            // TODO: confirm that cursor fix works here too.
            if (referenceName != null)
            {
                existingReference.ReplaceBy(this.replacingReference);
            }
            else
            {
                ModificationUtil.DeleteChildRange(existingReference, existingReference.NextSibling);
            }
        }
    }

    public class FindUsingsListVisitor : IRecursiveElementProcessor
    {
        public bool ProcessingIsFinished { get; private set; }

        public IUsingList UsingList { get; private set; }

        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            if (element is IUsingList)
            {
                UsingList = element as IUsingList;
                ProcessingIsFinished = true;
            }

            if (element is IClassDeclaration)
            {
                ProcessingIsFinished = true;
            }

            return !ProcessingIsFinished;
        }

        public void ProcessBeforeInterior(ITreeNode element)
        {
        }

        public void ProcessAfterInterior(ITreeNode element)
        {
        }
    }
}
