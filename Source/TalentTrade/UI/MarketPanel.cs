using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TalentTrade
{
    /// <summary>
    /// Market sub-tab panel: browse listings, buy pawns, list pawns for sale.
    /// </summary>
    public class MarketPanel
    {
        private const float ROW_HEIGHT = 80f;
        private const float BUTTON_WIDTH = 90f;
        private const float BUTTON_HEIGHT = 30f;
        private const float TOOLBAR_HEIGHT = 36f;
        private const float SPACING = 6f;

        private Vector2 listScrollPos;
        private bool showMyListings;

        // Sell flow state
        private bool sellMode;
        private Pawn selectedPawn;
        private string priceBuffer = "100";
        private int priceValue = 100;

        public void Draw(Rect rect)
        {
            // Toolbar
            Rect toolbarRect = new Rect(rect.x, rect.y, rect.width, TOOLBAR_HEIGHT);
            DrawToolbar(toolbarRect);

            // Content
            Rect contentRect = new Rect(rect.x, rect.y + TOOLBAR_HEIGHT + SPACING, rect.width, rect.height - TOOLBAR_HEIGHT - SPACING);

            if (sellMode)
            {
                DrawSellPanel(contentRect);
            }
            else
            {
                DrawListings(contentRect);
            }
        }

        private void DrawToolbar(Rect rect)
        {
            float x = rect.x;

            // List for Sale button
            Rect sellBtnRect = new Rect(x, rect.y, 120f, rect.height);
            if (Widgets.ButtonText(sellBtnRect, sellMode ? "TalentTrade_cancel".Translate() : "TalentTrade_marketSell".Translate()))
            {
                sellMode = !sellMode;
                if (!sellMode) ResetSellState();
            }
            x += 120f + SPACING + 20f;

            // My Listings toggle
            if (!sellMode)
            {
                Rect myBtnRect = new Rect(x, rect.y, 120f, rect.height);
                if (Widgets.ButtonText(myBtnRect, "TalentTrade_marketMyListings".Translate()))
                {
                    showMyListings = !showMyListings;
                }
                x += 120f + SPACING;
            }

            // Refresh
            if (!sellMode)
            {
                // Force cleanup button (left of refresh)
                Rect cleanupRect = new Rect(rect.xMax - 80f - 20f - 80f - SPACING, rect.y, 80f, rect.height);
                if (Widgets.ButtonText(cleanupRect, "TalentTrade_forceCleanup".Translate()))
                {
                    ForceCleanupAllMyListings();
                }

                Rect refreshRect = new Rect(rect.xMax - 80f - 20f, rect.y, 80f, rect.height);
                if (Widgets.ButtonText(refreshRect, "TalentTrade_refresh".Translate()))
                {
                    string uuid = TalentTradeManager.GetLocalUuid();
                    if (!string.IsNullOrEmpty(uuid))
                    {
                        TalentTradeManager.SendProtocol(TalentTradeProtocol.BuildMarketSync(uuid));
                    }
                }
            }
        }

        // --- Listings ---

        private void DrawListings(Rect rect)
        {
            MarketListing[] listings = TalentTradeManager.GetMarketListingsSnapshot();
            string localUuid = TalentTradeManager.GetLocalUuid();

            // Filter - create a copy to avoid modification during iteration
            List<MarketListing> filtered = new List<MarketListing>();
            for (int i = 0; i < listings.Length; i++)
            {
                if (listings[i] == null) continue;
                if (listings[i].State != MarketListingState.Active) continue;
                if (showMyListings)
                {
                    if (listings[i].SellerUuid == localUuid)
                        filtered.Add(listings[i]);
                }
                else
                {
                    filtered.Add(listings[i]);
                }
            }

            if (filtered.Count == 0)
            {
                Widgets.DrawMenuSection(rect);
                Widgets.NoneLabelCenteredVertically(rect, "TalentTrade_marketNoListings".Translate());
                return;
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, filtered.Count * (ROW_HEIGHT + SPACING));
            Widgets.BeginScrollView(rect, ref listScrollPos, viewRect);

            float y = 0f;
            for (int i = 0; i < filtered.Count; i++)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, ROW_HEIGHT);
                DrawListingRow(rowRect, filtered[i], localUuid);
                y += ROW_HEIGHT + SPACING;
            }

            Widgets.EndScrollView();
        }

        private void DrawListingRow(Rect rect, MarketListing listing, string localUuid)
        {
            // Background
            Widgets.DrawMenuSection(rect);
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect inner = rect.ContractedBy(6f);

            // Left: pawn info
            float infoWidth = inner.width - BUTTON_WIDTH - SPACING;
            Rect infoRect = new Rect(inner.x, inner.y, infoWidth, inner.height);

            // Name line
            string displayLabel = listing.Summary != null ? listing.Summary.GetDisplayLabel() : "???";
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(infoRect.x, infoRect.y, infoRect.width, 22f), displayLabel);

            // Race + Age line
            Text.Font = GameFont.Tiny;
            string raceAge = "";
            if (listing.Summary != null)
            {
                string raceName = listing.Summary.RaceDefName ?? "Human";
                bool hasRace = DefDatabase<ThingDef>.GetNamedSilentFail(raceName) != null;
                string raceStatus = hasRace ? "✓" : "✗";
                raceAge = $"{raceStatus} {raceName} | {listing.Summary.BiologicalAge} {"TalentTrade_ageUnit".Translate()}";
            }
            Widgets.Label(new Rect(infoRect.x, infoRect.y + 22f, infoRect.width, 18f), raceAge);

            // Seller line
            string sellerText = "TalentTrade_marketSeller".Translate(listing.SellerName ?? "???");
            Widgets.Label(new Rect(infoRect.x, infoRect.y + 40f, infoRect.width, 18f), sellerText);

            // Price line
            string priceText = "TalentTrade_marketPriceFormat".Translate(listing.PriceSilver.ToString());
            Widgets.Label(new Rect(infoRect.x, infoRect.y + 58f, infoRect.width, 18f), priceText);

            Text.Font = GameFont.Small;

            // Right: action button
            Rect btnRect = new Rect(inner.xMax - BUTTON_WIDTH, inner.y + (inner.height - BUTTON_HEIGHT) / 2f, BUTTON_WIDTH, BUTTON_HEIGHT);

            bool isMine = listing.SellerUuid == localUuid;
            if (isMine)
            {
                if (Widgets.ButtonText(btnRect, "TalentTrade_marketDelist".Translate()))
                {
                    DelistListing(listing);
                }
            }
            else
            {
                if (Widgets.ButtonText(btnRect, "TalentTrade_marketBuy".Translate()))
                {
                    ConfirmBuy(listing);
                }
            }

            // Skills tooltip
            if (listing.Summary != null && Mouse.IsOver(infoRect))
            {
                string tip = listing.Summary.SkillsSummary;
                if (!string.IsNullOrEmpty(listing.Summary.TraitsSummary))
                    tip += "\n" + listing.Summary.TraitsSummary;
                if (!string.IsNullOrEmpty(listing.Summary.HealthSummary))
                    tip += "\n" + listing.Summary.HealthSummary;
                TooltipHandler.TipRegion(infoRect, tip);
            }
        }

        // --- Sell flow ---

        private void DrawSellPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect inner = rect.ContractedBy(12f);

            float y = inner.y;

            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inner.x, y, inner.width, 30f), "TalentTrade_marketSell".Translate());
            Text.Font = GameFont.Small;
            y += 36f;

            // Pawn selector
            Widgets.Label(new Rect(inner.x, y, 120f, 28f), "TalentTrade_selectPawn".Translate());
            Rect pawnBtnRect = new Rect(inner.x + 130f, y, 200f, 28f);
            string pawnLabel = selectedPawn != null ? selectedPawn.LabelShortCap : (string)"TalentTrade_select".Translate();
            if (Widgets.ButtonText(pawnBtnRect, pawnLabel))
            {
                ShowPawnPicker();
            }
            y += 36f;

            // Pawn summary preview
            if (selectedPawn != null)
            {
                PawnSummary preview = PawnSummary.FromPawn(selectedPawn);
                Text.Font = GameFont.Tiny;
                string previewText = preview.GetDisplayLabel() + "\n" + preview.SkillsSummary + "\n" + preview.TraitsSummary;
                float previewHeight = Text.CalcHeight(previewText, inner.width);
                Widgets.Label(new Rect(inner.x, y, inner.width, previewHeight), previewText);
                y += previewHeight + SPACING;
                Text.Font = GameFont.Small;
            }

            // Price input
            Widgets.Label(new Rect(inner.x, y, 120f, 28f), "TalentTrade_marketPrice".Translate());
            Rect priceFieldRect = new Rect(inner.x + 130f, y, 120f, 28f);
            priceBuffer = Widgets.TextField(priceFieldRect, priceBuffer);
            int.TryParse(priceBuffer, out priceValue);
            if (priceValue < 0) priceValue = 0;
            Widgets.Label(new Rect(inner.x + 260f, y, 60f, 28f), "TalentTrade_silver".Translate());
            y += 36f;

            // Confirm button
            Rect confirmRect = new Rect(inner.x, y, 160f, 36f);
            bool canConfirm = selectedPawn != null && priceValue > 0;
            if (canConfirm && Widgets.ButtonText(confirmRect, "TalentTrade_confirm".Translate()))
            {
                DoListForSale();
            }
        }

        private void ShowPawnPicker()
        {
            List<Pawn> colonists = new List<Pawn>();
            if (Find.CurrentMap != null)
            {
                foreach (Pawn p in Find.CurrentMap.mapPawns.FreeColonistsSpawned)
                {
                    colonists.Add(p);
                }
            }

            if (colonists.Count == 0)
            {
                Messages.Message("TalentTrade_noPawnsAvailable".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (Pawn p in colonists)
            {
                Pawn captured = p;
                options.Add(new FloatMenuOption(captured.LabelShortCap, delegate
                {
                    selectedPawn = captured;
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DoListForSale()
        {
            if (selectedPawn == null || priceValue <= 0) return;

            // Prevent listing a pawn that is not on the map
            // (e.g. inside a drop pod, despawned, dead)
            if (!selectedPawn.Spawned || selectedPawn.Dead)
            {
                Messages.Message("TalentTrade_noPawnsAvailable".Translate(), MessageTypeDefOf.RejectInput, false);
                selectedPawn = null;
                return;
            }

            string localUuid = TalentTradeManager.GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            string listingId = Guid.NewGuid().ToString("N").Substring(0, 12);
            string localName = TalentTradeManager.GetLocalDisplayName();
            PawnSummary summary = PawnSummary.FromPawn(selectedPawn);

            // Serialize pawn data and hold it
            string b64Pawn = PawnSerializer.Serialize(selectedPawn);
            if (string.IsNullOrEmpty(b64Pawn))
            {
                Log.Error("【三角洲贸易】Failed to serialize pawn for market listing.");
                return;
            }

            PawnSerializer.DespawnAndHold(selectedPawn);

            // Register locally
            TalentTradeManager.AddLocalMarketListing(listingId, localUuid, localName, summary, priceValue, selectedPawn, b64Pawn);

            // Broadcast listing
            string msg = TalentTradeProtocol.BuildMarketList(listingId, localUuid, summary.ToBase64(), priceValue, localName);
            TalentTradeManager.SendProtocol(msg);

            // Reset sell state
            ResetSellState();
            sellMode = false;
        }

        // --- Buy flow ---

        private void ConfirmBuy(MarketListing listing)
        {
            Log.Message($"【三角洲贸易】ConfirmBuy called for listing {listing.Id}");

            // Check race compatibility on buyer side
            if (listing.Summary != null && !string.IsNullOrEmpty(listing.Summary.RaceDefName))
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(listing.Summary.RaceDefName) == null)
                {
                    // Buyer doesn't have the race mod
                    string pawnName = listing.Summary.GetDisplayLabel();
                    string message = "TalentTrade_cannotBuyNoRace".Translate(pawnName, listing.Summary.RaceDefName);
                    Find.WindowStack.Add(new Dialog_MessageBox(message));
                    return;
                }
            }

            string pawnName2 = listing.Summary != null ? listing.Summary.GetDisplayLabel() : "???";
            string confirmText = "TalentTrade_marketBuyConfirm".Translate(pawnName2, listing.PriceSilver.ToString());

            Dialog_MessageBox dialog = Dialog_MessageBox.CreateConfirmation(confirmText, delegate
            {
                Log.Message("【三角洲贸易】Buy confirmation accepted");
                DoBuy(listing);
            }, destructive: false);
            Find.WindowStack.Add(dialog);
        }

        private void DoBuy(MarketListing listing)
        {
            string localUuid = TalentTradeManager.GetLocalUuid();
            Log.Message($"【三角洲贸易】DoBuy: localUuid={localUuid}, listingId={listing.Id}");
            if (string.IsNullOrEmpty(localUuid))
            {
                Log.Warning("【三角洲贸易】DoBuy: localUuid is null or empty, aborting");
                return;
            }

            string localName = TalentTradeManager.GetLocalDisplayName();
            string msg = TalentTradeProtocol.BuildMarketBuy(listing.Id, localUuid, localName);
            Log.Message($"【三角洲贸易】Sending buy request: {msg}");
            TalentTradeManager.SendProtocol(msg);
            TalentTradeManager.TrackPurchase(listing.Id);
        }

        // --- Delist ---

        private void DelistListing(MarketListing listing)
        {
            string localUuid = TalentTradeManager.GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            // Prevent cross-save delist — only allow if this save owns the listing
            if (TalentTradeGameComponent.Current != null && !TalentTradeGameComponent.Current.OwnsListing(listing.Id))
            {
                Messages.Message("TalentTrade_cannotDelistWrongSave".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            // Restore held pawn
            TalentTradeManager.RestoreDelistedPawn(listing.Id);

            string msg = TalentTradeProtocol.BuildMarketDelist(listing.Id, localUuid);
            TalentTradeManager.SendProtocol(msg);
        }

        private void ResetSellState()
        {
            selectedPawn = null;
            priceBuffer = "100";
            priceValue = 100;
        }

        private void ForceCleanupAllMyListings()
        {
            string localUuid = TalentTradeManager.GetLocalUuid();
            if (string.IsNullOrEmpty(localUuid)) return;

            MarketListing[] all = TalentTradeManager.GetMarketListingsSnapshot();
            List<string> toRemove = new List<string>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].SellerUuid == localUuid && all[i].State == MarketListingState.Active)
                {
                    toRemove.Add(all[i].Id);
                }
            }

            if (toRemove.Count == 0)
            {
                Messages.Message("TalentTrade_noListingsToClean".Translate(), MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            foreach (string id in toRemove)
            {
                // Broadcast delist
                string msg = TalentTradeProtocol.BuildMarketDelist(id, localUuid);
                TalentTradeManager.SendProtocol(msg);

                // Remove locally (pawn data is gone — sent to the warp)
                TalentTradeManager.RemoveListingLocally(id);

                if (TalentTradeGameComponent.Current != null)
                {
                    TalentTradeGameComponent.Current.UntrackListing(id);
                }
            }

            Messages.Message("TalentTrade_cleanupDone".Translate(toRemove.Count.ToString()), MessageTypeDefOf.NeutralEvent, false);
        }
    }
}
