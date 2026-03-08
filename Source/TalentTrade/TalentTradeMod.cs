using HarmonyLib;
using PhinixClient;
using UnityEngine;
using Verse;

namespace TalentTrade
{
    public class TalentTradeMod : Mod
    {
        public const string HarmonyId = "iniad.talenttrade";
        public static TalentTradeMod Instance;
        public static TalentTradeSettings Settings;

        public TalentTradeMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<TalentTradeSettings>();
            Harmony harmony = new Harmony(HarmonyId);
            harmony.PatchAll();
            if (Client.Instance != null)
            {
                TalentTradeManager.Initialize(Client.Instance);
            }
        }

        public override string SettingsCategory()
        {
            return "TalentTrade_settingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            bool notifications = Settings != null && Settings.EnableNotifications;
            listing.CheckboxLabeled(
                "TalentTrade_settingNotifications".Translate(),
                ref notifications,
                "TalentTrade_settingNotificationsDesc".Translate()
            );
            if (Settings != null)
            {
                Settings.EnableNotifications = notifications;
            }

            listing.End();
        }
    }
}
