using HarmonyLib;
using Verse;

namespace TalentTrade.Patches
{
    /// <summary>
    /// When the player exits to main menu, immediately delist all own listings
    /// to prevent ghost listings on the network.
    /// </summary>
    [HarmonyPatch(typeof(GenScene), nameof(GenScene.GoToMainMenu))]
    internal static class GenSceneGoToMainMenuPatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            TalentTradeManager.OnExitSave();
        }
    }
}
