using System;
using System.Collections.Generic;
using PhinixClient;
using RimWorld;
using UnityEngine;
using Verse;

namespace TalentTrade
{
    public static class TalentTradeManager
    {
        private static readonly Queue<Action> MainThreadQueue = new Queue<Action>();
        private static readonly object ProcessedLock = new object();
        private static readonly HashSet<string> ProcessedProtocolKeys = new HashSet<string>();

        private static bool initialized;
        private static int lastUpdateFrame = -1;

        // --- Market state ---
        private static readonly object MarketLock = new object();
        private static readonly Dictionary<string, MarketListing> MarketListings = new Dictionary<string, MarketListing>();
        // Local-only: serialized pawn data for listings we own (seller side)
        private static readonly Dictionary<string, string> LocalPawnData = new Dictionary<string, string>();

        // --- Rental state ---
        private static readonly object RentalLock = new object();
        private static readonly Dictionary<string, RentalContract> RentalContracts = new Dictionary<string, RentalContract>();

        // --- Direct trade state ---
        private static readonly object TradeLock = new object();
        private static readonly Dictionary<string, DirectTrade> ActiveTrades = new Dictionary<string, DirectTrade>();

        // --- Blob assembly ---
        private static readonly object BlobLock = new object();
        private static readonly Dictionary<string, string[]> PendingBlobs = new Dictionary<string, string[]>();

        // --- Def manifest exchange ---
        private static readonly object DefExchangeLock = new object();
        private static readonly Dictionary<string, TransferReport> PendingDefReports = new Dictionary<string, TransferReport>();
        private static readonly Dictionary<string, Action<TransferReport>> DefReportCallbacks = new Dictionary<string, Action<TransferReport>>();

        // --- Offline listing cache ---
        private static readonly Dictionary<string, MarketListing> OfflineListingCache = new Dictionary<string, MarketListing>();
        private static readonly Dictionary<string, string> OfflinePawnDataCache = new Dictionary<string, string>();

        // --- Purchase timeout tracking ---
        private static readonly Dictionary<string, int> PendingPurchases = new Dictionary<string, int>();
        private const int PURCHASE_TIMEOUT_TICKS = 600; // 10 seconds at 60 FPS

        public static void Initialize(Client client)
        {
            if (initialized || client == null) return;

            initialized = true;
            client.OnDisconnect += (s, e) =>
            {
                CacheMyListingsOnLogout();
                Clear();
            };

            // Restore cached listings on login
            RestoreMyListingsOnLogin();
        }

        public static void Update()
        {
            int frame = Time.frameCount;
            if (frame == lastUpdateFrame) return;
            lastUpdateFrame = frame;

            if (!initialized && Client.Instance != null)
            {
                Initialize(Client.Instance);
            }

            TalentTradeTransport.Update();
            PollRelayBuffer();

            // Only run game-dependent operations when a map is loaded
            if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
            {
                RunMainThreadQueue();
                CheckPurchaseTimeouts();
            }
        }

        public static void EnqueueMainThread(Action action)
        {
            if (action == null) return;
            lock (MainThreadQueue)
            {
                MainThreadQueue.Enqueue(action);
            }
        }

        public static string GetLocalUuid()
        {
            if (Client.Instance == null) return null;
            try
            {
                return Client.Instance.Uuid;
            }
            catch
            {
                return null;
            }
        }

        public static string GetLocalDisplayName()
        {
            string uuid = GetLocalUuid();
            if (string.IsNullOrEmpty(uuid) || Client.Instance == null) return "Unknown";
            try
            {
                string name;
                if (Client.Instance.TryGetDisplayName(uuid, out name))
                {
                    return name ?? "Unknown";
                }
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static void SendProtocol(string protocolMessage)
        {
            string uuid = GetLocalUuid();
            if (string.IsNullOrEmpty(uuid)) return;
            TalentTradeTransport.EnqueueProtocol(protocolMessage, uuid);
        }

        // --- Market accessors ---

        public static MarketListing[] GetMarketListingsSnapshot()
        {
            lock (MarketLock)
            {
                MarketListing[] result = new MarketListing[MarketListings.Count];
                int i = 0;
                foreach (var kvp in MarketListings)
                {
                    result[i++] = kvp.Value;
                }
                return result;
            }
        }

        // --- Market API ---

        /// <summary>
        /// Register a local market listing (seller side). Stores the held pawn and serialized data.
        /// </summary>
        public static void AddLocalMarketListing(string listingId, string sellerUuid, string sellerName, PawnSummary summary, int priceSilver, Pawn heldPawn, string b64PawnData)
        {
            MarketListing listing = new MarketListing
            {
                Id = listingId,
                SellerUuid = sellerUuid,
                SellerName = sellerName,
                Summary = summary,
                PriceSilver = priceSilver,
                State = MarketListingState.Active,
                CreatedAtUtc = DateTime.UtcNow,
                HeldPawn = heldPawn
            };

            lock (MarketLock)
            {
                MarketListings[listingId] = listing;
                LocalPawnData[listingId] = b64PawnData;
            }

            // Track this listing as belonging to the current save
            if (TalentTradeGameComponent.Current != null)
            {
                TalentTradeGameComponent.Current.TrackListing(listingId);
            }
        }

        /// <summary>
        /// Restore a delisted pawn back to the map (seller cancels listing).
        /// </summary>
        public static void RestoreDelistedPawn(string listingId)
        {
            string b64PawnData;
            lock (MarketLock)
            {
                if (!LocalPawnData.TryGetValue(listingId, out b64PawnData))
                {
                    Log.Warning($"【三角洲贸易】RestoreDelistedPawn: No pawn data for listing {listingId}");
                    return;
                }
                MarketListings.Remove(listingId);
                LocalPawnData.Remove(listingId);
            }

            // Untrack from current save
            if (TalentTradeGameComponent.Current != null)
            {
                TalentTradeGameComponent.Current.UntrackListing(listingId);
            }

            EnqueueMainThread(() =>
            {
                Pawn pawn = PawnDeserializer.DeserializeAndSpawn(b64PawnData);
                if (pawn != null)
                {
                    Messages.Message("TalentTrade_pawnRestored".Translate(pawn.LabelShortCap), new LookTargets(pawn), MessageTypeDefOf.NeutralEvent, false);
                }
            });
        }

        /// <summary>
        /// Get the serialized pawn data for a local listing (seller side).
        /// </summary>
        public static string GetLocalPawnData(string listingId)
        {
            lock (MarketLock)
            {
                string data;
                if (LocalPawnData.TryGetValue(listingId, out data))
                    return data;
                return null;
            }
        }

        /// <summary>
        /// Remove a listing and its pawn data from local state (used by GameComponent on save exit).
        /// Does NOT broadcast delist — caller handles that.
        /// </summary>
        public static void RemoveListingLocally(string listingId)
        {
            lock (MarketLock)
            {
                MarketListings.Remove(listingId);
                LocalPawnData.Remove(listingId);
            }
        }

        // --- Rental accessors ---

        public static RentalContract[] GetRentalContractsSnapshot()
        {
            lock (RentalLock)
            {
                RentalContract[] result = new RentalContract[RentalContracts.Count];
                int i = 0;
                foreach (var kvp in RentalContracts)
                {
                    result[i++] = kvp.Value;
                }
                return result;
            }
        }

        // --- Direct trade accessors ---

        public static DirectTrade[] GetActiveTradesSnapshot()
        {
            lock (TradeLock)
            {
                DirectTrade[] result = new DirectTrade[ActiveTrades.Count];
                int i = 0;
                foreach (var kvp in ActiveTrades)
                {
                    result[i++] = kvp.Value;
                }
                return result;
            }
        }

        // --- Def exchange API ---

        /// <summary>
        /// Initiate a def compatibility check with a target player before sending a pawn.
        /// Collects the pawn's referenced defs, sends them to the target, and invokes the callback
        /// when the target responds with their missing defs report.
        /// </summary>
        public static string RequestDefCheck(Pawn pawn, string targetUuid, Action<TransferReport> onResult)
        {
            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid) || string.IsNullOrEmpty(targetUuid)) return null;

            string sessionId = Guid.NewGuid().ToString("N").Substring(0, 12);
            DefManifest manifest = DefManifestHelper.CollectFromPawn(pawn);
            string defListStr = DefManifestHelper.SerializeManifest(manifest);
            string b64DefList = TalentTradeTransport.Compress(defListStr);

            if (onResult != null)
            {
                lock (DefExchangeLock)
                {
                    DefReportCallbacks[sessionId] = onResult;
                }
            }

            string msg = TalentTradeProtocol.BuildDefManifest(sessionId, localUuid, targetUuid, b64DefList);
            SendProtocol(msg);
            return sessionId;
        }

        /// <summary>
        /// Get a previously received def report by session ID. Returns null if not yet received.
        /// </summary>
        public static TransferReport GetDefReport(string sessionId)
        {
            lock (DefExchangeLock)
            {
                TransferReport report;
                if (PendingDefReports.TryGetValue(sessionId, out report))
                    return report;
                return null;
            }
        }

        // --- Internal ---

        private static void Clear()
        {
            lock (MarketLock) { MarketListings.Clear(); LocalPawnData.Clear(); }
            lock (RentalLock) { RentalContracts.Clear(); }
            lock (TradeLock) { ActiveTrades.Clear(); }
            lock (BlobLock) { PendingBlobs.Clear(); }
            lock (ProcessedLock) { ProcessedProtocolKeys.Clear(); }
            lock (DefExchangeLock) { PendingDefReports.Clear(); DefReportCallbacks.Clear(); }
            lock (MainThreadQueue) { MainThreadQueue.Clear(); }
            TalentTradeTransport.Clear();
        }

        private static void PollRelayBuffer()
        {
            int maxPerFrame = 20;
            int count = 0;
            string message;
            while (count < maxPerFrame && TalentTradeTransport.TryDequeueIncoming(out message))
            {
                count++;
                ProcessProtocolMessage(message);
            }
        }

        private static void ProcessProtocolMessage(string message)
        {
            TalentTradeMessageType msgType;
            string[] parts;
            if (!TalentTradeProtocol.TryParse(message, out msgType, out parts)) return;

            // Dedup
            string dedupKey = BuildDedupKey(msgType, parts);
            if (!string.IsNullOrEmpty(dedupKey))
            {
                lock (ProcessedLock)
                {
                    if (!ProcessedProtocolKeys.Add(dedupKey)) return;
                }
            }

            switch (msgType)
            {
                case TalentTradeMessageType.MarketList:
                    HandleMarketList(parts);
                    break;
                case TalentTradeMessageType.MarketDelist:
                    HandleMarketDelist(parts);
                    break;
                case TalentTradeMessageType.MarketBuy:
                    HandleMarketBuy(parts);
                    break;
                case TalentTradeMessageType.MarketSell:
                    HandleMarketSell(parts);
                    break;
                case TalentTradeMessageType.MarketPaid:
                    HandleMarketPaid(parts);
                    break;
                case TalentTradeMessageType.MarketSync:
                    HandleMarketSync(parts);
                    break;
                case TalentTradeMessageType.TradeRequest:
                    HandleTradeRequest(parts);
                    break;
                case TalentTradeMessageType.TradeAccept:
                    HandleTradeAccept(parts);
                    break;
                case TalentTradeMessageType.TradeReject:
                    HandleTradeReject(parts);
                    break;
                case TalentTradeMessageType.TradeOffer:
                    HandleTradeOffer(parts);
                    break;
                case TalentTradeMessageType.TradeLock:
                    HandleTradeLock(parts);
                    break;
                case TalentTradeMessageType.TradeExecute:
                    HandleTradeExecute(parts);
                    break;
                case TalentTradeMessageType.TradeCancel:
                    HandleTradeCancel(parts);
                    break;
                case TalentTradeMessageType.RentalList:
                    HandleRentalList(parts);
                    break;
                case TalentTradeMessageType.RentalDelist:
                    HandleRentalDelist(parts);
                    break;
                case TalentTradeMessageType.RentalRent:
                    HandleRentalRent(parts);
                    break;
                case TalentTradeMessageType.RentalConfirm:
                    HandleRentalConfirm(parts);
                    break;
                case TalentTradeMessageType.RentalReturn:
                    HandleRentalReturn(parts);
                    break;
                case TalentTradeMessageType.RentalExpiry:
                    HandleRentalExpiry(parts);
                    break;
                case TalentTradeMessageType.RentalDead:
                    HandleRentalDead(parts);
                    break;
                case TalentTradeMessageType.RentalRevive:
                    HandleRentalRevive(parts);
                    break;
                case TalentTradeMessageType.DefManifest:
                    HandleDefManifest(parts);
                    break;
                case TalentTradeMessageType.DefAck:
                    HandleDefAck(parts);
                    break;
                case TalentTradeMessageType.BlobPart:
                    HandleBlobPart(parts);
                    break;
            }
        }

        private static string BuildDedupKey(TalentTradeMessageType msgType, string[] parts)
        {
            // Use type + first ID field for dedup
            if (parts.Length > 3)
            {
                return msgType.ToString() + "|" + parts[3];
            }
            return null;
        }

        private static void RunMainThreadQueue()
        {
            Action[] actions;
            lock (MainThreadQueue)
            {
                if (MainThreadQueue.Count == 0) return;
                actions = MainThreadQueue.ToArray();
                MainThreadQueue.Clear();
            }

            for (int i = 0; i < actions.Length; i++)
            {
                try
                {
                    actions[i]();
                }
                catch (Exception ex)
                {
                    Log.Error("【三角洲贸易】MainThreadQueue action error: " + ex);
                }
            }
        }

        // --- Stub handlers (to be implemented in later phases) ---

        private static void HandleMarketList(string[] parts)
        {
            // PHXTT|v1|mlist|listingId|sellerUuid|b64PawnSummary|priceSilver|b64SellerName
            if (parts.Length < 8) return;
            string listingId = parts[3];
            string sellerUuid = parts[4];
            string b64Summary = parts[5];
            int priceSilver;
            if (!int.TryParse(parts[6], out priceSilver)) return;
            string sellerName = TalentTradeProtocol.DecodeField(parts[7]);

            PawnSummary summary = PawnSummary.FromBase64(b64Summary);

            MarketListing listing = new MarketListing
            {
                Id = listingId,
                SellerUuid = sellerUuid,
                SellerName = sellerName,
                Summary = summary,
                PriceSilver = priceSilver,
                State = MarketListingState.Active,
                CreatedAtUtc = DateTime.UtcNow
            };

            lock (MarketLock)
            {
                MarketListings[listingId] = listing;
            }
        }

        private static void HandleMarketDelist(string[] parts)
        {
            if (parts.Length < 5) return;
            string listingId = parts[3];
            lock (MarketLock)
            {
                MarketListings.Remove(listingId);
            }
        }

        private static void HandleMarketBuy(string[] parts)
        {
            // PHXTT|v1|mbuy|listingId|buyerUuid|b64BuyerName
            // Received by the SELLER — someone wants to buy our listing
            Log.Message($"【三角洲贸易】HandleMarketBuy called, parts.Length={parts.Length}");
            if (parts.Length < 6) return;
            string listingId = parts[3];
            string buyerUuid = parts[4];
            string buyerName = TalentTradeProtocol.DecodeField(parts[5]);
            Log.Message($"【三角洲贸易】HandleMarketBuy: listingId={listingId}, buyerUuid={buyerUuid}");

            string localUuid = GetLocalUuid();
            Log.Message($"【三角洲贸易】HandleMarketBuy: localUuid={localUuid}");
            if (string.IsNullOrEmpty(localUuid)) return;

            // Check if we own this listing
            MarketListing listing;
            lock (MarketLock)
            {
                if (!MarketListings.TryGetValue(listingId, out listing))
                {
                    Log.Warning($"【三角洲贸易】HandleMarketBuy: Listing {listingId} not found");
                    return;
                }
                if (listing.SellerUuid != localUuid)
                {
                    Log.Message($"【三角洲贸易】HandleMarketBuy: Not our listing (seller={listing.SellerUuid})");
                    return;
                }
                if (listing.State != MarketListingState.Active)
                {
                    Log.Warning($"【三角洲贸易】HandleMarketBuy: Listing state is {listing.State}, not Active");
                    return;
                }
            }

            Log.Message($"【三角洲贸易】Completing sale directly");
            CompleteSale(listingId, buyerUuid);
        }

        private static void CompleteSale(string listingId, string buyerUuid)
        {
            string localUuid = GetLocalUuid();
            string b64PawnData;
            MarketListing listing;

            lock (MarketLock)
            {
                if (!MarketListings.TryGetValue(listingId, out listing)) return;
                if (listing.SellerUuid != localUuid) return;
                if (listing.State != MarketListingState.Active) return;

                listing.State = MarketListingState.Sold;

                if (!LocalPawnData.TryGetValue(listingId, out b64PawnData))
                {
                    b64PawnData = null;
                }
                LocalPawnData.Remove(listingId);
            }

            if (string.IsNullOrEmpty(b64PawnData))
            {
                Log.Error("【三角洲贸易】CompleteSale: No pawn data for listing " + listingId);
                return;
            }

            string msg = TalentTradeProtocol.BuildMarketSell(listingId, localUuid, buyerUuid, b64PawnData);
            SendProtocol(msg);

            // Untrack from current save
            if (TalentTradeGameComponent.Current != null)
            {
                TalentTradeGameComponent.Current.UntrackListing(listingId);
            }

            lock (MarketLock)
            {
                MarketListings.Remove(listingId);
            }
        }

        private static void HandleMarketSell(string[] parts)
        {
            // PHXTT|v1|msell|listingId|sellerUuid|buyerUuid|b64CompressedPawnData
            // Received by the BUYER — seller is sending us the pawn
            if (parts.Length < 7) return;
            string listingId = parts[3];
            string buyerUuid = parts[5];
            string b64PawnData = parts[6];

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;
            if (buyerUuid != localUuid) return;

            // Remove from pending purchases
            PendingPurchases.Remove(listingId);

            // Remove listing from local view
            lock (MarketLock)
            {
                MarketListings.Remove(listingId);
            }

            // Deserialize and spawn pawn on main thread
            EnqueueMainThread(() =>
            {
                Pawn pawn = PawnDeserializer.DeserializeAndSpawn(b64PawnData);
                if (pawn != null)
                {
                    Messages.Message(
                        "TalentTrade_pawnReceivedMessage".Translate(pawn.LabelShortCap),
                        new LookTargets(pawn),
                        MessageTypeDefOf.PositiveEvent,
                        false);
                }
                else
                {
                    Log.Error("【三角洲贸易】HandleMarketSell: Failed to deserialize pawn for listing " + listingId);
                }
            });
        }

        private static void HandleMarketPaid(string[] parts)
        {
            // PHXTT|v1|mpaid|listingId|buyerUuid|b64CompressedSilverItems
            // Future: handle silver transfer confirmation. For now, market is trust-based.
            if (parts.Length < 6) return;
        }

        private static void HandleMarketSync(string[] parts)
        {
            // PHXTT|v1|msync|requestorUuid
            // Re-broadcast all our active listings so the requestor can see them
            if (parts.Length < 4) return;

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;
            string localName = GetLocalDisplayName();

            MarketListing[] snapshot;
            lock (MarketLock)
            {
                snapshot = new MarketListing[MarketListings.Count];
                int idx = 0;
                foreach (var kvp in MarketListings)
                {
                    snapshot[idx++] = kvp.Value;
                }
            }

            for (int i = 0; i < snapshot.Length; i++)
            {
                MarketListing l = snapshot[i];
                if (l.SellerUuid != localUuid) continue;
                if (l.State != MarketListingState.Active) continue;
                if (l.Summary == null) continue;

                string msg = TalentTradeProtocol.BuildMarketList(l.Id, localUuid, l.Summary.ToBase64(), l.PriceSilver, localName);
                SendProtocol(msg);
            }
        }

        private static void HandleTradeRequest(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeAccept(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeReject(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeOffer(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeLock(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeExecute(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleTradeCancel(string[] parts)
        {
            // TODO: Phase 4
        }

        private static void HandleRentalList(string[] parts)
        {
            // PHXTT|v1|rlist|rentalId|ownerUuid|b64PawnSummary|pricePerDay|maxDays|deposit|b64OwnerName
            if (parts.Length < 10) return;
            string rentalId = parts[3];
            string ownerUuid = parts[4];
            string b64Summary = parts[5];
            int pricePerDay;
            if (!int.TryParse(parts[6], out pricePerDay)) return;
            int maxDays;
            if (!int.TryParse(parts[7], out maxDays)) return;
            int deposit;
            if (!int.TryParse(parts[8], out deposit)) return;
            string ownerName = TalentTradeProtocol.DecodeField(parts[9]);

            PawnSummary summary = PawnSummary.FromBase64(b64Summary);

            RentalContract contract = new RentalContract
            {
                Id = rentalId,
                OwnerUuid = ownerUuid,
                OwnerName = ownerName,
                Summary = summary,
                PricePerDay = pricePerDay,
                MaxDays = maxDays,
                Deposit = deposit,
                State = RentalContractState.Listed
            };

            lock (RentalLock)
            {
                RentalContracts[rentalId] = contract;
            }
        }

        private static void HandleRentalDelist(string[] parts)
        {
            if (parts.Length < 5) return;
            string rentalId = parts[3];
            lock (RentalLock)
            {
                RentalContracts.Remove(rentalId);
            }
        }

        private static void HandleRentalRent(string[] parts)
        {
            // PHXTT|v1|rrent|rentalId|renterUuid|days|b64RenterName
            // Received by OWNER — someone wants to rent our pawn
            if (parts.Length < 7) return;
            string rentalId = parts[3];
            string renterUuid = parts[4];
            int days;
            if (!int.TryParse(parts[5], out days)) return;
            string renterName = TalentTradeProtocol.DecodeField(parts[6]);

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            RentalContract contract;
            string cachedXml;
            lock (RentalLock)
            {
                if (!RentalContracts.TryGetValue(rentalId, out contract)) return;
                if (contract.OwnerUuid != localUuid) return;
                if (contract.State != RentalContractState.Listed) return;

                // Cache original pawn XML before sending
                cachedXml = contract.OriginalPawnData;
            }

            if (string.IsNullOrEmpty(cachedXml))
            {
                Log.Error("【三角洲贸易】HandleRentalRent: No cached pawn data for rental " + rentalId);
                return;
            }

            // Update contract state
            lock (RentalLock)
            {
                contract.State = RentalContractState.Active;
                contract.RenterUuid = renterUuid;
                contract.RenterName = renterName;
                contract.RentedDays = days;
                contract.StartTick = Find.TickManager.TicksGame;
                contract.ExpiryTick = contract.StartTick + days * 60000;
            }

            // Send cached pawn data to renter
            string msg = TalentTradeProtocol.BuildRentalConfirm(rentalId, localUuid, renterUuid, cachedXml);
            SendProtocol(msg);
        }

        private static void HandleRentalConfirm(string[] parts)
        {
            // PHXTT|v1|rconf|rentalId|ownerUuid|renterUuid|b64CompressedPawnData
            // Received by RENTER — owner is sending us the pawn
            if (parts.Length < 7) return;
            string rentalId = parts[3];
            string renterUuid = parts[5];
            string b64PawnData = parts[6];

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;
            if (renterUuid != localUuid) return;

            // Spawn pawn
            EnqueueMainThread(() =>
            {
                Pawn pawn = PawnDeserializer.DeserializeAndSpawn(b64PawnData);
                if (pawn != null)
                {
                    Messages.Message(
                        "TalentTrade_pawnReceivedMessage".Translate(pawn.LabelShortCap),
                        new LookTargets(pawn),
                        MessageTypeDefOf.PositiveEvent,
                        false);
                }
            });
        }

        private static void HandleRentalReturn(string[] parts)
        {
            // PHXTT|v1|rret|rentalId|renterUuid|b64CompressedPawnData
            // Received by OWNER — renter is returning the pawn (IGNORE their data, use cache)
            if (parts.Length < 6) return;
            string rentalId = parts[3];
            string renterUuid = parts[4];

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            RentalContract contract;
            string cachedXml;
            lock (RentalLock)
            {
                if (!RentalContracts.TryGetValue(rentalId, out contract)) return;
                if (contract.OwnerUuid != localUuid) return;
                if (contract.RenterUuid != renterUuid) return;

                cachedXml = contract.OriginalPawnData;
                contract.State = RentalContractState.Returned;
                RentalContracts.Remove(rentalId);
            }

            // Restore from cache (ignore renter's data)
            if (!string.IsNullOrEmpty(cachedXml))
            {
                EnqueueMainThread(() =>
                {
                    Pawn pawn = PawnDeserializer.DeserializeAndSpawn(cachedXml);
                    if (pawn != null)
                    {
                        Messages.Message(
                            "TalentTrade_pawnReceivedMessage".Translate(pawn.LabelShortCap),
                            new LookTargets(pawn),
                            MessageTypeDefOf.PositiveEvent,
                            false);
                    }
                });
            }
        }

        private static void HandleRentalExpiry(string[] parts)
        {
            // PHXTT|v1|rexp|rentalId
            // Broadcast: rental expired, auto-return
            if (parts.Length < 4) return;
            string rentalId = parts[3];

            lock (RentalLock)
            {
                RentalContracts.Remove(rentalId);
            }
        }

        private static void HandleRentalDead(string[] parts)
        {
            // PHXTT|v1|rdead|rentalId|renterUuid
            // Received by OWNER — renter reports pawn died, we revive from cache
            if (parts.Length < 5) return;
            string rentalId = parts[3];
            string renterUuid = parts[4];

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            RentalContract contract;
            string cachedXml;
            lock (RentalLock)
            {
                if (!RentalContracts.TryGetValue(rentalId, out contract)) return;
                if (contract.OwnerUuid != localUuid) return;
                if (contract.RenterUuid != renterUuid) return;

                cachedXml = contract.OriginalPawnData;
                contract.State = RentalContractState.PawnLost;
            }

            // Revive from cache using ResurrectionUtility
            if (!string.IsNullOrEmpty(cachedXml))
            {
                EnqueueMainThread(() =>
                {
                    Pawn pawn = PawnDeserializer.Deserialize(cachedXml);
                    if (pawn != null)
                    {
                        // Resurrect the pawn
                        ResurrectionUtility.TryResurrect(pawn, new ResurrectionParams());
                        PawnDeserializer.SpawnViaDropPod(pawn);

                        // Send revived pawn back to renter
                        string revivedXml = PawnSerializer.Serialize(pawn);
                        if (!string.IsNullOrEmpty(revivedXml))
                        {
                            string msg = TalentTradeProtocol.BuildRentalRevive(rentalId, localUuid, revivedXml);
                            SendProtocol(msg);
                        }
                    }
                });
            }
        }

        private static void HandleRentalRevive(string[] parts)
        {
            // PHXTT|v1|rrev|rentalId|ownerUuid|b64CompressedPawnData
            // Received by RENTER — owner revived and returned the pawn
            if (parts.Length < 6) return;
            string rentalId = parts[3];
            string b64PawnData = parts[5];

            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            // Spawn revived pawn
            EnqueueMainThread(() =>
            {
                Pawn pawn = PawnDeserializer.DeserializeAndSpawn(b64PawnData);
                if (pawn != null)
                {
                    Messages.Message(
                        "TalentTrade_rentalPawnDied".Translate(pawn.LabelShortCap),
                        new LookTargets(pawn),
                        MessageTypeDefOf.NeutralEvent,
                        false);
                }
            });

            lock (RentalLock)
            {
                RentalContracts.Remove(rentalId);
            }
        }

        private static void HandleDefManifest(string[] parts)
        {
            // PHXTT|v1|defs|sessionId|senderUuid|targetUuid|b64CompressedDefList
            if (parts.Length < 7) return;
            string sessionId = parts[3];
            string senderUuid = parts[4];
            string targetUuid = parts[5];
            string b64DefList = parts[6];

            // Only process if we are the target
            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid) || targetUuid != localUuid) return;

            // Decompress and deserialize the manifest
            string defListStr;
            try
            {
                defListStr = TalentTradeTransport.Decompress(b64DefList);
            }
            catch
            {
                defListStr = b64DefList; // fallback: maybe it wasn't compressed
            }

            DefManifest manifest = DefManifestHelper.DeserializeManifest(defListStr);
            TransferReport report = DefManifestHelper.CheckCompatibility(manifest);

            // Collect our missing defs and send back
            string missingStr = string.Empty;
            if (report.HasMissing)
            {
                missingStr = string.Join("\n", report.Missing.ToArray());
            }

            string b64Missing = TalentTradeTransport.Compress(missingStr);
            string ackMsg = TalentTradeProtocol.BuildDefAck(sessionId, localUuid, b64Missing);
            SendProtocol(ackMsg);
        }

        private static void HandleDefAck(string[] parts)
        {
            // PHXTT|v1|dack|sessionId|senderUuid|b64MissingDefs
            if (parts.Length < 6) return;
            string sessionId = parts[3];
            string b64Missing = parts[5];

            string missingStr;
            try
            {
                missingStr = TalentTradeTransport.Decompress(b64Missing);
            }
            catch
            {
                missingStr = b64Missing;
            }

            // Build a report from the response
            var report = new TransferReport();
            if (!string.IsNullOrEmpty(missingStr))
            {
                string[] lines = missingStr.Split('\n');
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                        report.Missing.Add(line);
                }
            }

            // Store the report and invoke callback if registered
            Action<TransferReport> callback = null;
            lock (DefExchangeLock)
            {
                PendingDefReports[sessionId] = report;
                if (DefReportCallbacks.TryGetValue(sessionId, out callback))
                {
                    DefReportCallbacks.Remove(sessionId);
                }
            }

            if (callback != null)
            {
                EnqueueMainThread(() => callback(report));
            }
        }

        private static void HandleBlobPart(string[] parts)
        {
            // PHXTT|v1|blob|blobId|partIndex|totalParts|b64PartData
            if (parts.Length < 7) return;
            string blobId = parts[3];
            int partIndex;
            if (!int.TryParse(parts[4], out partIndex)) return;
            int totalParts;
            if (!int.TryParse(parts[5], out totalParts)) return;
            if (totalParts <= 0 || partIndex < 0 || partIndex >= totalParts) return;

            string partData = parts[6];

            lock (BlobLock)
            {
                string[] blobParts;
                if (!PendingBlobs.TryGetValue(blobId, out blobParts))
                {
                    blobParts = new string[totalParts];
                    PendingBlobs[blobId] = blobParts;
                }

                blobParts[partIndex] = partData;

                // Check if complete
                bool complete = true;
                for (int i = 0; i < blobParts.Length; i++)
                {
                    if (blobParts[i] == null) { complete = false; break; }
                }

                if (complete)
                {
                    PendingBlobs.Remove(blobId);
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < blobParts.Length; i++)
                    {
                        sb.Append(blobParts[i]);
                    }
                    // Reassembled blob — process as a protocol message
                    string assembled = sb.ToString();
                    ProcessProtocolMessage(assembled);
                }
            }
        }

        // --- Offline handling ---

        public static void TrackPurchase(string listingId)
        {
            PendingPurchases[listingId] = Find.TickManager.TicksGame;
        }

        private static void CheckPurchaseTimeouts()
        {
            if (PendingPurchases.Count == 0) return;

            int currentTick = Find.TickManager.TicksGame;
            List<string> timedOut = new List<string>();

            foreach (var kvp in PendingPurchases)
            {
                if (currentTick - kvp.Value > PURCHASE_TIMEOUT_TICKS)
                {
                    timedOut.Add(kvp.Key);
                }
            }

            foreach (string listingId in timedOut)
            {
                PendingPurchases.Remove(listingId);
                lock (MarketLock)
                {
                    MarketListings.Remove(listingId);
                }
                Log.Warning($"【三角洲贸易】Purchase timeout for listing {listingId}, seller offline");
                Messages.Message("TalentTrade_sellerOffline".Translate(), MessageTypeDefOf.RejectInput, false);
            }
        }

        public static void RestoreMyListingsOnLogin()
        {
            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            lock (MarketLock)
            {
                foreach (var kvp in OfflineListingCache)
                {
                    MarketListings[kvp.Key] = kvp.Value;
                }
                foreach (var kvp in OfflinePawnDataCache)
                {
                    LocalPawnData[kvp.Key] = kvp.Value;
                }
            }

            if (OfflineListingCache.Count > 0)
            {
                Log.Message($"【三角洲贸易】Restored {OfflineListingCache.Count} cached listings");
                string localName = GetLocalDisplayName();
                foreach (var kvp in OfflineListingCache)
                {
                    MarketListing l = kvp.Value;
                    if (l.Summary != null)
                    {
                        string msg = TalentTradeProtocol.BuildMarketList(l.Id, localUuid, l.Summary.ToBase64(), l.PriceSilver, localName);
                        SendProtocol(msg);
                    }
                }
            }
        }

        public static void CacheMyListingsOnLogout()
        {
            string localUuid = GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            OfflineListingCache.Clear();
            OfflinePawnDataCache.Clear();

            lock (MarketLock)
            {
                foreach (var kvp in MarketListings)
                {
                    if (kvp.Value.SellerUuid == localUuid)
                    {
                        OfflineListingCache[kvp.Key] = kvp.Value;
                    }
                }
                foreach (var kvp in LocalPawnData)
                {
                    OfflinePawnDataCache[kvp.Key] = kvp.Value;
                }
            }

            if (OfflineListingCache.Count > 0)
            {
                Log.Message($"【三角洲贸易】Cached {OfflineListingCache.Count} listings for next login");
            }
        }
    }
}
