using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TalentTrade
{
    internal static class TradeablePawnUtility
    {
        public static List<Pawn> GetTradeablePawns(Map map)
        {
            List<Pawn> result = new List<Pawn>();
            if (map == null || map.mapPawns == null)
                return result;

            AddRange(result, map.mapPawns.FreeColonistsSpawned);
            AddRange(result, map.mapPawns.SpawnedColonyAnimals);
            AddRange(result, map.mapPawns.SpawnedColonyMechs);
            AddRange(result, map.mapPawns.PrisonersOfColonySpawned);
            return result;
        }

        public static bool CanTrade(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || !pawn.Spawned || pawn.Destroyed)
                return false;
            if (pawn.Faction != Faction.OfPlayer)
                return false;
            if (pawn.IsSlaveOfColony)
                return false;
            if (pawn.RaceProps == null)
                return false;

            if (pawn.IsPrisonerOfColony)
                return true;
            if (pawn.IsColonyMech)
                return true;
            if (pawn.RaceProps.Humanlike && pawn.IsColonistPlayerControlled)
                return true;
            if (pawn.RaceProps.Animal)
                return true;
            return false;
        }

        public static string GetLabel(Pawn pawn)
        {
            if (pawn == null)
                return "???";
            PawnSummary summary = PawnSummary.FromPawn(pawn);
            return summary.GetDisplayLabel();
        }

        private static void AddRange(List<Pawn> result, IEnumerable<Pawn> pawns)
        {
            if (pawns == null)
                return;
            foreach (Pawn pawn in pawns)
            {
                if (CanTrade(pawn) && !result.Contains(pawn))
                    result.Add(pawn);
            }
        }
    }
}
