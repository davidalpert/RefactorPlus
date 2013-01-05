using System;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace RefactorPlus
{
    using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

    [ContextAction(Description = "Remove Type Qualifier", Group = "C#", Name = "RemoveTypeQualifier", Priority = 1)]
    public class RemoveTypeQualifierContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider provider;
        private string ACTION_NAME = "Remove Type Qualifier";

        public RemoveTypeQualifierContextAction(ICSharpContextActionDataProvider provider)
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

            return typeUsage != null;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            RemoveTypeQualifier();

            return null;
        }

        private void RemoveTypeQualifier()
        {
            var typeUsage = this.provider.GetSelectedElement<IUserDeclaredTypeUsage>(true, true);
            if (typeUsage == null)
                return;

            var referenceName = typeUsage.FirstChild as IReferenceName;
            if (referenceName == null)
                return;

            var typeQualifier = referenceName.Qualifier;
            if (typeQualifier == null)
                return;

            string qualifyingNamespace = typeQualifier.QualifiedName;

            // handles the case of RegularExpressions in the following source:
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

            var file = this.provider.GetSelectedElement<ICSharpFile>(true, true);
            if (file == null)
                return;

            var usingDirective = EnsureUsingExists(qualifyingNamespace, file);

            if (usingDirective == null)
                return;

            ReQualify(typeQualifier, usingDirective);

            IReferenceName newQualifier = null;
            if (usingDirective is IUsingAliasDirective)
            {
                var usingAliasDirective = usingDirective as IUsingAliasDirective;

                var aliasName = usingAliasDirective.AliasName;

                newQualifier =
                    this.provider.ElementFactory.CreateReferenceName(aliasName);
            }
            var visitor = new ReplaceAllTypeQualifiersVisitor(usingDirective, newQualifier);
            file.Accept(visitor);

            // TODO: generate local variale for control.Selection.SetRange(textRange);
        }

        private INamespace AttemptToResolve(IReferenceName typeQualifier)
        {
            var resolveResult = typeQualifier.Reference.Resolve();
            var resolvedNamespace = resolveResult.DeclaredElement as INamespace;
            return resolvedNamespace;
        }

        private void ReQualify(IReferenceName typeQualifier, IUsingDirective usingDirective)
        {
            if (usingDirective is IUsingAliasDirective)
            {
                var usingAliasDirective = usingDirective as IUsingAliasDirective;

                var aliasName = usingAliasDirective.AliasName;

                IReferenceName newQualifier =
                    this.provider.ElementFactory.CreateReferenceName(aliasName);

                typeQualifier.ReplaceBy(newQualifier);

                //IReferenceExpression x = ModificationUtil.ReplaceChild(typeQualifier, newQualifier);
            }
            else
            {
                ModificationUtil.DeleteChildRange(typeQualifier, typeQualifier.NextSibling);
            }
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

            IUsingDirective anchor = usingList.LastChild as IUsingDirective;
            if (anchor == null)
                return null;

            var newUsing = this.provider.ElementFactory.CreateUsingDirective(qualifyingNamespace);

            var import = ModificationUtil.AddChildAfter(anchor, newUsing);

            //var import = toFile.AddImportAfter(newUsing, anchor);

            return import;
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

        public override void VisitReferenceName(IReferenceName existingReference)
        {
            if (IsMatch(existingReference, usingDirective))
                Replace(existingReference, replacingReference);
        }

        /*
        public bool InteriorShouldBeProcessed(ITreeNode element)
        {
            if (element is IUsingList
                || element is IUsingDirective)
                return false;

            var existingReference = element as IReferenceName;
            if (existingReference == null)
                return true;


            return false;
        }

         */
        private void Replace(IReferenceName existingReference, IReferenceName referenceName)
        {
            if (referenceName != null)
            {
                existingReference.ReplaceBy(this.replacingReference);
            }
            else
            {
                ModificationUtil.DeleteChildRange(existingReference, existingReference.NextSibling);
            }
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
