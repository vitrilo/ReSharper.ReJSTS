using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.JavaScript.Bulbs;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.JavaScript.Services;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.TextControl;
using JetBrains.Util;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Resources.Shell;


namespace ReSharper.ReJS
{
    [ContextAction(Name = "ReplaceReferenceWithIndex", Description = "Replaces reference expression with index expression", Group = "JavaScript")]
    public class ReplaceReferenceWithIndexAction : ContextActionBase
    {
        private IReferenceExpression _referenceExpression;
        private string _replacement;
        private readonly IJavaScriptContextActionDataProvider _provider;

        public ReplaceReferenceWithIndexAction(IJavaScriptContextActionDataProvider provider)
        {
            _provider = provider;
        }

        public override string Text
        {
            get { return "Replace with " + _replacement; }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var reference = _provider.GetSelectedElement<IReferenceExpression>(true, true);
            if (reference != null && reference.IsValid())
            {
                var qualifier = reference.Qualifier;
                var nameIdentifier = reference.NameIdentifier;
                if (qualifier != null && nameIdentifier != null)
                {
                    _referenceExpression = reference;
                    _replacement = string.Format("{0}['{1}']", qualifier.GetText(), nameIdentifier.GetText());
                    return true;
                }
            }
            return false;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            var factory = JavaScriptElementFactory.GetInstance(_referenceExpression);
            using (WriteLockCookie.Create())
                ModificationUtil.ReplaceChild(_referenceExpression, factory.CreateExpression(_replacement));
            return null;
        }
    }
}