using Verse;

namespace TalentTrade
{
    public class TalentTradeSettings : ModSettings
    {
        public bool EnableNotifications = true;
        public bool EnableDebugLog = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableNotifications, "enableNotifications", true);
            Scribe_Values.Look(ref EnableDebugLog, "enableDebugLog", false);
        }
    }
}
