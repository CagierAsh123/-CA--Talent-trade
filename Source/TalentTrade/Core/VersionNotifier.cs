using RimWorld;
using Verse;

namespace TalentTrade
{
    /// <summary>
    /// Version constant. The actual notification is triggered from TalentTradeGameComponent.FinalizeInit().
    /// </summary>
    internal static class VersionNotifier
    {
        public const string ModVersion = "v2.0";

        public static void TryNotify(string lastSeenVersion)
        {
            if (lastSeenVersion == ModVersion)
                return;

            Find.LetterStack.ReceiveLetter(
                "TalentTrade_versionTitle".Translate(ModVersion),
                "TalentTrade_versionText".Translate(ModVersion),
                LetterDefOf.NeutralEvent,
                (LookTargets)null);
        }
    }
}
