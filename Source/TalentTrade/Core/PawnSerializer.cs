using System;
using System.IO;
using System.Xml;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TalentTrade
{
    /// <summary>
    /// Serializes a Pawn to a compressed Base64 string using RimWorld's Scribe system.
    /// Must be called on the main thread (Scribe is not thread-safe).
    /// </summary>
    public static class PawnSerializer
    {
        /// <summary>
        /// Serialize a Pawn to XML string via ScribeSaver.DebugOutputFor.
        /// Returns null on failure.
        /// </summary>
        public static string PawnToXml(Pawn pawn)
        {
            if (pawn == null) return null;
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Log.Warning("[TalentTrade] PawnToXml called while Scribe is busy (mode=" + Scribe.mode + "). Aborting.");
                return null;
            }

            try
            {
                string xml = Scribe.saver.DebugOutputFor(pawn);
                if (string.IsNullOrEmpty(xml))
                {
                    Log.Error("[TalentTrade] PawnToXml: DebugOutputFor returned empty for " + pawn.LabelShort);
                    return null;
                }
                return xml;
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] PawnToXml failed: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Full pipeline: Pawn → XML → GZip → Base64.
        /// Returns null on failure.
        /// </summary>
        public static string Serialize(Pawn pawn)
        {
            string xml = PawnToXml(pawn);
            if (xml == null) return null;

            try
            {
                return TalentTradeTransport.Compress(xml);
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] PawnSerializer.Serialize compress failed: " + ex);
                return null;
            }
        }

        /// <summary>
        /// Despawn a pawn from the map and hold it for trading.
        /// Returns true if the pawn was successfully despawned or was already despawned.
        /// </summary>
        public static bool DespawnAndHold(Pawn pawn)
        {
            if (pawn == null) return false;

            try
            {
                if (pawn.Spawned)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                }

                // Prevent garbage collection by registering with WorldPawns
                if (!pawn.Destroyed && Find.WorldPawns != null)
                {
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] DespawnAndHold failed: " + ex);
                return false;
            }
        }
    }
}
