using RimWorld;
using Verse;

namespace TalentTrade
{
    [StaticConstructorOnStartup]
    internal static class VersionNotifier
    {
        private const string ModVersion = "v1.5";

        static VersionNotifier()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                SendVersionLetter();
            }
        }

        private static void SendVersionLetter()
        {
            Find.LetterStack.ReceiveLetter(
                "TalentTrade_versionTitle".Translate(ModVersion),
                "TalentTrade_versionText".Translate(ModVersion),
                LetterDefOf.NeutralEvent,
                (LookTargets)null);
        }
    }
}
