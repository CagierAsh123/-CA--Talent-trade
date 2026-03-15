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
        // Security limits
        private const int MAX_XML_LENGTH = 2 * 1024 * 1024; // 2MB max decompressed XML
        private const int MAX_B64_LENGTH = 4 * 1024 * 1024; // 4MB max compressed data

        /// <summary>
        /// Full pipeline: Base64 → GZip decompress → XML → Pawn object.
        /// Returns null on failure. Does NOT spawn the pawn.
        /// </summary>
        public static Pawn Deserialize(string b64Compressed)
        {
            if (string.IsNullOrEmpty(b64Compressed)) return null;

            // Security: reject oversized payloads
            if (b64Compressed.Length > MAX_B64_LENGTH)
            {
                Log.Error("【三角洲贸易】Pawn data rejected: payload too large (" + b64Compressed.Length + " bytes)");
                return null;
            }

            string xml;
            try
            {
                xml = TalentTradeTransport.Decompress(b64Compressed);
            }
            catch (Exception ex)
            {
                Log.Error("【三角洲贸易】PawnDeserializer.Deserialize decompress failed: " + ex);
                return null;
            }

            // Security: reject oversized XML
            if (xml != null && xml.Length > MAX_XML_LENGTH)
            {
                Log.Error("【三角洲贸易】Pawn XML rejected: decompressed size too large (" + xml.Length + " chars)");
                return null;
            }

            // Security: basic XML sanity check
            if (!ValidateXmlSafety(xml))
            {
                Log.Error("【三角洲贸易】Pawn XML rejected: failed safety validation");
                return null;
            }

            // Security: structural validation of pawn XML
            if (!ValidatePawnStructure(xml))
            {
                Log.Error("【三角洲贸易】Pawn XML rejected: failed structural validation");
                return null;
            }

            Pawn pawn = XmlToPawn(xml);
            if (pawn != null)
            {
                // Critical check: reject pawn if def is null (missing race mod)
                if (pawn.def == null)
                {
                    Log.Error("【三角洲贸易】Pawn deserialization failed: pawn.def is null (missing race mod). Rejecting pawn.");
                    return null;
                }
                PostProcessPawn(pawn);
                Log.Message("【三角洲贸易】Pawn deserialized successfully.");
            }
            return pawn;
        }

        /// <summary>
        /// Basic XML safety validation — reject obviously malicious or malformed data.
        /// </summary>
        private static bool ValidateXmlSafety(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return false;

            // Must start with XML-like content
            string trimmed = xml.TrimStart();
            if (!trimmed.StartsWith("<")) return false;

            // Reject XML with external entity declarations (XXE prevention)
            if (xml.IndexOf("<!ENTITY", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (xml.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (xml.IndexOf("SYSTEM", StringComparison.OrdinalIgnoreCase) >= 0 &&
                xml.IndexOf("<!", StringComparison.Ordinal) >= 0) return false;

            return true;
        }

        /// <summary>
        /// Structural validation: ensure XML looks like a valid RimWorld Pawn, not arbitrary data.
        /// </summary>
        private static bool ValidatePawnStructure(string xml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null; // Prevent XXE attacks
                doc.LoadXml(xml);

                XmlNode root = doc.DocumentElement;
                if (root == null) return false;

                // Root must be "saveable" (from DebugOutputFor) or contain one
                XmlNode pawnNode = root;
                if (root.Name != "saveable" && root["saveable"] != null)
                    pawnNode = root["saveable"];

                // Must have Class attribute pointing to a Pawn type
                XmlAttribute classAttr = pawnNode.Attributes?["Class"];
                if (classAttr == null) return false;
                string className = classAttr.Value;
                if (className.IndexOf("Pawn", StringComparison.Ordinal) < 0) return false;

                // Must contain a <def> element (race definition)
                if (pawnNode["def"] == null) return false;

                // Must contain <kindDef> (pawn kind)
                if (pawnNode["kindDef"] == null) return false;

                // Reject excessive XML depth (> 50 levels) to prevent stack overflow attacks
                if (GetMaxDepth(pawnNode, 0) > 50) return false;

                // Reject excessive child node count (> 10000) to prevent memory bombs
                if (CountNodes(pawnNode, 0) > 10000) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static int GetMaxDepth(XmlNode node, int current)
        {
            if (current > 50) return current;
            int max = current;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    int d = GetMaxDepth(child, current + 1);
                    if (d > max) max = d;
                    if (max > 50) return max;
                }
            }
            return max;
        }

        private static int CountNodes(XmlNode node, int current)
        {
            if (current > 10000) return current;
            int count = current + 1;
            foreach (XmlNode child in node.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    count = CountNodes(child, count);
                    if (count > 10000) return count;
                }
            }
            return count;
        }

        /// <summary>
        /// Parse XML string into a Pawn object using Scribe loader.
        /// </summary>
        public static Pawn XmlToPawn(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return null;
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                Log.Warning("【三角洲贸易】XmlToPawn called while Scribe is busy (mode=" + Scribe.mode + "). Aborting.");
                return null;
            }

            Pawn pawn = null;
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null; // Prevent XXE attacks
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

                // Critical check BEFORE cross-ref resolution
                if (pawn != null && pawn.def == null)
                {
                    Log.Error("【三角洲贸易】Pawn.def is null after SaveableFromNode, aborting");
                    pawn = null;
                }

                if (pawn != null)
                {
                    // Resolve cross-references
                    Scribe.loader.crossRefs.ResolveAllCrossReferences();

                    // Clean up null hediffs BEFORE post-load init
                    if (pawn.health != null && pawn.health.hediffSet != null)
                    {
                        pawn.health.hediffSet.hediffs.RemoveAll(h => h == null || h.def == null);
                    }

                    // Run post-load init
                    Scribe.loader.initer.DoAllPostLoadInits();
                }
            }
            catch (Exception ex)
            {
                Log.Error("【三角洲贸易】XmlToPawn failed: " + ex);
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
        /// Post-process a deserialized pawn to fix cross-save incompatibilities.
        /// </summary>
        private static void PostProcessPawn(Pawn pawn)
        {
            if (pawn == null) return;

            // Security: validate race is a humanlike pawn (reject animals, mechanoids, etc.)
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                Log.Error("【三角洲贸易】PostProcess rejected: pawn is not humanlike (race=" + pawn.def?.defName + ")");
                return;
            }

            // Security: cap skill levels to valid range
            if (pawn.skills != null)
            {
                foreach (var sr in pawn.skills.skills)
                {
                    if (sr != null && sr.Level > 20) sr.Level = 20;
                }
            }

            // Regenerate Thing IDs to avoid conflicts
            pawn.SetForbidden(false, false);
            pawn.thingIDNumber = -1;
            pawn.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();

            // Regenerate IDs for apparel
            if (pawn.apparel != null && pawn.apparel.WornApparel != null)
            {
                foreach (var ap in pawn.apparel.WornApparel)
                {
                    if (ap != null)
                    {
                        ap.thingIDNumber = -1;
                        ap.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();
                    }
                }
            }

            // Regenerate IDs for equipment
            if (pawn.equipment != null && pawn.equipment.AllEquipmentListForReading != null)
            {
                foreach (var eq in pawn.equipment.AllEquipmentListForReading)
                {
                    if (eq != null)
                    {
                        eq.thingIDNumber = -1;
                        eq.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();
                    }
                }
            }

            // Regenerate IDs for inventory
            if (pawn.inventory != null && pawn.inventory.innerContainer != null)
            {
                foreach (var item in pawn.inventory.innerContainer)
                {
                    if (item != null)
                    {
                        item.thingIDNumber = -1;
                        item.thingIDNumber = Find.UniqueIDsManager.GetNextThingID();
                    }
                }
            }

            // Clean up null hediffs
            if (pawn.health != null && pawn.health.hediffSet != null)
            {
                pawn.health.hediffSet.hediffs.RemoveAll(h => h == null || h.def == null);

                // Regenerate hediff loadIDs
                foreach (var hediff in pawn.health.hediffSet.hediffs)
                {
                    if (hediff != null)
                    {
                        hediff.loadID = Find.UniqueIDsManager.GetNextHediffID();
                    }
                }
            }

            // Regenerate Gene loadIDs
            if (pawn.genes != null)
            {
                if (pawn.genes.Endogenes != null)
                {
                    foreach (var gene in pawn.genes.Endogenes)
                    {
                        if (gene != null)
                        {
                            gene.loadID = Find.UniqueIDsManager.GetNextGeneID();
                        }
                    }
                }
                if (pawn.genes.Xenogenes != null)
                {
                    foreach (var gene in pawn.genes.Xenogenes)
                    {
                        if (gene != null)
                        {
                            gene.loadID = Find.UniqueIDsManager.GetNextGeneID();
                        }
                    }
                }
            }

            // Fix Ideo
            if (ModsConfig.IdeologyActive && pawn.ideo != null)
            {
                Ideo receiverIdeo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
                if (receiverIdeo != null)
                {
                    pawn.ideo.SetIdeo(receiverIdeo);
                }
            }

            // Clear invalid Thing references
            if (pawn.mindState != null)
            {
                pawn.mindState.lastAttackedTarget = LocalTargetInfo.Invalid;
                pawn.mindState.enemyTarget = null;
                pawn.mindState.meleeThreat = null;
            }

            // Reset jobs to avoid stale references
            if (pawn.jobs != null)
            {
                pawn.jobs.ClearQueuedJobs();
                if (pawn.jobs.curJob != null)
                {
                    pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
                }
            }

            // Reset stances
            if (pawn.stances != null)
            {
                pawn.stances.SetStance(new Stance_Mobile());
            }

            // Reset verb tracker to rebuild from current equipment
            if (pawn.verbTracker != null)
            {
                pawn.verbTracker = new VerbTracker(pawn);
            }
            if (pawn.meleeVerbs != null)
            {
                pawn.meleeVerbs.Notify_PawnDespawned();
            }
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
                    Log.Error("【三角洲贸易】SpawnViaDropPod: No map available.");
                    return false;
                }

                IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);

                // Set pawn faction to player colony
                if (pawn.Faction == null || pawn.Faction != Faction.OfPlayer)
                {
                    pawn.SetFaction(Faction.OfPlayer);
                }

                TradeUtility.SpawnDropPod(dropSpot, map, pawn);

                // Force refresh pawn graphics cache
                pawn.Drawer.renderer.SetAllGraphicsDirty();
                PortraitsCache.SetDirty(pawn);

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("【三角洲贸易】SpawnViaDropPod failed: " + ex);
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
