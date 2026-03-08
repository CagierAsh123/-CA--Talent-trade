using System;
using System.Collections.Generic;
using System.Xml;
using RimWorld;
using Verse;

namespace TalentTrade
{
    /// <summary>
    /// Deserializes a Pawn from compressed Base64 data and spawns it on the map.
    /// Must be called on the main thread (Scribe is not thread-safe).
    /// </summary>
    public static class PawnDeserializer
    {
        /// <summary>
        /// Full pipeline: Base64 → GZip decompress → XML → Pawn object.
        /// Returns null on failure. Does NOT spawn the pawn.
        /// </summary>
        public static Pawn Deserialize(string b64Compressed)
        {
            if (string.IsNullOrEmpty(b64Compressed)) return null;

            string xml;
            try
            {
                xml = TalentTradeTransport.Decompress(b64Compressed);
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] PawnDeserializer.Deserialize decompress failed: " + ex);
                return null;
            }

            return XmlToPawn(xml);
        }

        /// <summary>
        /// Parse XML string into a Pawn object using Scribe loader.
        /// </summary>
        public static Pawn XmlToPawn(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Log.Warning("[TalentTrade] XmlToPawn called while Scribe is busy (mode=" + Scribe.mode + "). Aborting.");
                return null;
            }

            Pawn pawn = null;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                XmlNode pawnNode = doc.DocumentElement;
                if (pawnNode.Name == "saveable")
                {
                    // DebugOutputFor wraps in <saveable>, use it directly
                }
                else if (doc.DocumentElement["saveable"] != null)
                {
                    pawnNode = doc.DocumentElement["saveable"];
                }

                // Set up Scribe for loading
                Scribe.mode = LoadSaveMode.LoadingVars;
                Scribe.loader.curXmlParent = pawnNode;
                Scribe.loader.curParent = null;
                Scribe.loader.curPathRelToParent = null;

                pawn = ScribeExtractor.SaveableFromNode<Pawn>(pawnNode, new object[0]);

                // Resolve cross-references
                Scribe.loader.crossRefs.ResolveAllCrossReferences();

                // Run post-load init
                Scribe.loader.initer.DoAllPostLoadInits();
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] XmlToPawn failed: " + ex);
                pawn = null;
            }
            finally
            {
                // Always reset Scribe state
                Scribe.mode = LoadSaveMode.Inactive;
                Scribe.loader.FinalizeLoading();
            }

            return pawn;
        }

        /// <summary>
        /// Spawn a deserialized pawn on the map via drop pod at the trade drop spot.
        /// </summary>
        public static bool SpawnViaDropPod(Pawn pawn, Map map = null)
        {
            if (pawn == null) return false;

            try
            {
                if (map == null)
                {
                    map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
                }
                if (map == null)
                {
                    Log.Error("[TalentTrade] SpawnViaDropPod: No map available.");
                    return false;
                }

                IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);

                // Set pawn faction to player colony
                if (pawn.Faction == null || pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(Faction.OfPlayer);
                }

                TradeUtility.SpawnDropPod(dropSpot, map, pawn);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("[TalentTrade] SpawnViaDropPod failed: " + ex);
                return false;
            }
        }

        /// <summary>
        /// Full pipeline: deserialize + spawn via drop pod.
        /// </summary>
        public static Pawn DeserializeAndSpawn(string b64Compressed, Map map = null)
        {
            Pawn pawn = Deserialize(b64Compressed);
            if (pawn == null) return null;

            if (SpawnViaDropPod(pawn, map))
            {
                return pawn;
            }

            return null;
        }
    }
}
