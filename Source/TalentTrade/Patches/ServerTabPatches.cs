using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PhinixClient;
using RimWorld;
using UnityEngine;
using Verse;

namespace TalentTrade.Patches
{
    internal static class ServerTabAccess
    {
        private static readonly FieldInfo ActiveTabField = AccessTools.Field(typeof(ServerTab), "activeTab");
        private static readonly FieldInfo TabListField = AccessTools.Field(typeof(ServerTab), "tabList");

        public static int GetActiveTab(ServerTab instance)
        {
            return (int)ActiveTabField.GetValue(instance);
        }

        public static void SetActiveTab(ServerTab instance, int value)
        {
            ActiveTabField.SetValue(instance, value);
        }

        public static List<TabRecord> GetTabList(ServerTab instance)
        {
            return TabListField.GetValue(instance) as List<TabRecord>;
        }
    }

    [HarmonyPatch(typeof(ServerTab))]
    [HarmonyPatch(MethodType.Constructor)]
    internal static class ServerTab_Ctor_Patch
    {
        private static void Postfix(ServerTab __instance)
        {
            List<TabRecord> tabs = ServerTabAccess.GetTabList(__instance);
            if (tabs == null) return;

            int tabIndex = tabs.Count;
            tabs.Add(new TabRecord(
                label: "TalentTrade_tab".Translate(),
                clickedAction: () => ServerTabAccess.SetActiveTab(__instance, tabIndex),
                selected: () => ServerTabAccess.GetActiveTab(__instance) == tabIndex
            ));

            TalentTradeTab.Instance.TabIndex = tabIndex;
        }
    }

    [HarmonyPatch(typeof(ServerTab), nameof(ServerTab.DoWindowContents))]
    internal static class ServerTab_DoWindowContents_Patch
    {
        private static void Postfix(ServerTab __instance, Rect inRect)
        {
            if (ServerTabAccess.GetActiveTab(__instance) != TalentTradeTab.Instance.TabIndex) return;

            Rect usableRect = inRect.BottomPartPixels(inRect.height - TabDrawer.TabHeight);
            TalentTradeTab.Instance.Draw(usableRect);
        }
    }

    [HarmonyPatch(typeof(ServerTab), nameof(ServerTab.OnAcceptKeyPressed))]
    internal static class ServerTab_OnAcceptKeyPressed_Patch
    {
        private static bool Prefix(ServerTab __instance)
        {
            return ServerTabAccess.GetActiveTab(__instance) == 0;
        }
    }
}
