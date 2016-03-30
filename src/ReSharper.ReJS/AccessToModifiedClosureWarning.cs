﻿using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharper.ReJS
{
    [ConfigurableSeverityHighlighting(HIGHLIGHTING_ID, JavaScriptLanguage.Name, OverlapResolve = OverlapResolveKind.WARNING)]
    public class AccessToModifiedClosureWarning : IHighlighting
    {
        public const string HIGHLIGHTING_ID = "JsAccessToModifiedClosure";

        private readonly IJavaScriptTreeNode _treeNode;

        public AccessToModifiedClosureWarning(IJavaScriptTreeNode treeNode)
        {
            _treeNode = treeNode;
        }

        public IJavaScriptTreeNode TreeNode
        {
            get { return _treeNode; }
        }

        public bool IsValid()
        {
            return TreeNode != null && TreeNode.IsValid();
        }

        public DocumentRange CalculateRange()
        {
            return TreeNode.GetDocumentRange();
        }

        public string ToolTip
        {
            get { return "Access to externally modified closure"; }
        }

        public string ErrorStripeToolTip
        {
            get { return ToolTip; }
        }

        public int NavigationOffsetPatch
        {
            get { return 0; }
        }
    }
}