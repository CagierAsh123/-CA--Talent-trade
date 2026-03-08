using HarmonyLib;
using Verse;

namespace TalentTrade.Patches
{
    [HarmonyPatch(typeof(Root), nameof(Root.Update))]
    internal static class RootUpdatePatches
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TalentTradeManager.Update();
        }
    }
}
