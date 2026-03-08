using System;
using System.Collections.Generic;
using PhinixClient;
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

        // --- Rental state ---
        private static readonly object RentalLock = new object();
        private static readonly Dictionary<string, RentalContract> RentalContracts = new Dictionary<string, RentalContract>();

        // --- Direct trade state ---
        private static readonly object TradeLock = new object();
        private static readonly Dictionary<string, DirectTrade> ActiveTrades = new Dictionary<string, DirectTrade>();

        // --- Blob assembly ---
        private static readonly object BlobLock = new object();
        private static readonly Dictionary<string, string[]> PendingBlobs = new Dictionary<string, string[]>();

        public static void Initialize(Client client)
        {
            if (initialized || client == null) return;

            initialized = true;
            client.OnDisconnect += (s, e) => Clear();
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
            RunMainThreadQueue();
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

        // --- Internal ---

        private static void Clear()
        {
            lock (MarketLock) { MarketListings.Clear(); }
            lock (RentalLock) { RentalContracts.Clear(); }
            lock (TradeLock) { ActiveTrades.Clear(); }
            lock (BlobLock) { PendingBlobs.Clear(); }
            lock (ProcessedLock) { ProcessedProtocolKeys.Clear(); }
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
                    Log.Error("[TalentTrade] MainThreadQueue action error: " + ex);
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
            if (parts.Length < 6) return;
            // TODO: Phase 3 — seller responds with msell
        }

        private static void HandleMarketSell(string[] parts)
        {
            // PHXTT|v1|msell|listingId|sellerUuid|buyerUuid|b64CompressedPawnData
            if (parts.Length < 7) return;
            // TODO: Phase 3 — buyer receives pawn data
        }

        private static void HandleMarketPaid(string[] parts)
        {
            // TODO: Phase 3
        }

        private static void HandleMarketSync(string[] parts)
        {
            // TODO: Phase 3 — respond with current listings
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
            // TODO: Phase 5
        }

        private static void HandleRentalConfirm(string[] parts)
        {
            // TODO: Phase 5
        }

        private static void HandleRentalReturn(string[] parts)
        {
            // TODO: Phase 5
        }

        private static void HandleRentalExpiry(string[] parts)
        {
            // TODO: Phase 5
        }

        private static void HandleRentalDead(string[] parts)
        {
            // TODO: Phase 5
        }

        private static void HandleRentalRevive(string[] parts)
        {
            // TODO: Phase 5
        }

        private static void HandleDefManifest(string[] parts)
        {
            // TODO: Phase 2
        }

        private static void HandleDefAck(string[] parts)
        {
            // TODO: Phase 2
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
    }
}
