using JetBrains.Application.Settings;
using JetBrains.ReSharper.Daemon.JavaScript;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.Tree;


namespace ReSharper.ReTs
{
    [DaemonStage(StagesBefore = new[] { typeof (LanguageSpecificDaemonStage) })]
    public class ReJsHighlightingStage : JavaScriptDaemonStageBase
    {
        protected override IDaemonStageProcess CreateProcess(IDaemonProcess process, IContextBoundSettingsStore settings, DaemonProcessKind processKind, IJavaScriptFile file)
        {
            return new ReJsHighlightingStageProcess(process, settings, file);
        }

        public override ErrorStripeRequest NeedsErrorStripe(IPsiSourceFile sourceFile, IContextBoundSettingsStore settings)
        {
            return !IsSupported(sourceFile) ? ErrorStripeRequest.NONE : ErrorStripeRequest.STRIPE_AND_ERRORS;
        }
    }
}
