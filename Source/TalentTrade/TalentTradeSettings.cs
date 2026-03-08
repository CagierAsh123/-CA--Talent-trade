using Verse;

namespace TalentTrade
{
    public class TalentTradeSettings : ModSettings
    {
        public bool EnableNotifications = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableNotifications, "enableNotifications", true);
        }
    }
}
