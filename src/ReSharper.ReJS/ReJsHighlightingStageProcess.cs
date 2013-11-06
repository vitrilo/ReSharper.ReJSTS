using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.JavaScript.Impl;
using JetBrains.ReSharper.Daemon.Stages;
using JetBrains.ReSharper.Feature.Services.LinqTools;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharper.ReJS
{
    public class ReJsHighlightingStageProcess : JavaScriptDaemonStageProcessBase
    {
        public ReJsHighlightingStageProcess(IDaemonProcess process, IContextBoundSettingsStore settingsStore, IJavaScriptFile file)
            : base(process, settingsStore, file)
        {
        }

        public override void Execute(Action<DaemonStageResult> commiter)
        {
            HighlightInFile((file, consumer) => file.ProcessDescendants(this, consumer), commiter);
        }

        public override void VisitInvocationExpression(IInvocationExpression invocation, IHighlightingConsumer consumer)
        {
            if (IsCallWithTheSameContextAsFunctionOwner(invocation))
                consumer.AddHighlighting(new CallWithSameContextWarning(invocation), invocation.GetHighlightingRange(), File);

            base.VisitInvocationExpression(invocation, consumer);
        }

        public override void VisitAccessorBody(IAccessorBody accessorBody, IHighlightingConsumer consumer)
        {
            VisitFunctionLike(accessorBody, consumer);
        }

        public override void VisitFunctionExpression(IFunctionExpression function, IHighlightingConsumer consumer)
        {
            VisitFunctionLike(function, consumer);
        }

        private void VisitFunctionLike(ITreeNode function, IHighlightingConsumer consumer)
        {
            var accessAnalizer = new ReferenceExpressionCollector();
            function.ProcessThisAndDescendants(accessAnalizer);
            var accessToExternalModifiedClosure = accessAnalizer.References
                .GroupBy(r => r.Reference.Resolve().DeclaredElement)
                .ToDictionary(g => g.Key, g => g.Select(r => new ReferenceInfo(r)).ToArray())
                .Where(l => HasExternallyModifiedClosure(function, l))
                .SelectMany(l => l.Value)
                .Where(r => r.FunctionLike != function)
                .Select(r => r.ReferenceExpression);

            foreach (var referenceExpression in accessToExternalModifiedClosure)
            {
                consumer.AddHighlighting(new AccessToModifiedClosureWarning(referenceExpression), referenceExpression.GetHighlightingRange(), File);
            }
        }

        private bool HasExternallyModifiedClosure(ITreeNode function, KeyValuePair<IDeclaredElement, ReferenceInfo[]> kvp)
        {
            return kvp.Value.Any(r => r.FunctionLike != function && r.FunctionLike.GetContainingNode<IForStatement>() != null) &&
                   (kvp.Value.Any(r => r.FunctionLike == function && r.IsWriteUsage) ||
                    kvp.Key.GetDeclarationsIn(File.GetSourceFile()).Any(d => d.GetContainingNode<IForStatement>() != null));
        }


        private static bool IsCallWithTheSameContextAsFunctionOwner(IInvocationExpression invocation)
        {
            var invokedReference = invocation.InvokedExpression as IReferenceExpression;
            if (invokedReference == null || invokedReference.Name != "call")
                return false;

            var function = invokedReference.Qualifier as IReferenceExpression;
            if (function == null)
                return false;

            return AreSame(function.Qualifier, invocation.Arguments.FirstOrDefault());
        }

        private static bool AreSame(ITreeNode x, ITreeNode y)
        {
            var referenceX = x as IReferenceExpression;
            var referenceY = y as IReferenceExpression;
            if (referenceX != null && referenceY != null)
            {
                var resolvedReferenceX = referenceX.Reference.Resolve().DeclaredElement;
                var resolvedReferenceY = referenceY.Reference.Resolve().DeclaredElement;

                return Equals(resolvedReferenceY, resolvedReferenceX);
            }

            var thisX = x as IThisExpression;
            var thisY = y as IThisExpression;
            if (thisX != null && thisY != null)
            {
                //TODO: add proper comparision
                return true;
            }

            return false;
        }
    }
}
