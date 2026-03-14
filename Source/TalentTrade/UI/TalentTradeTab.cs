using PhinixClient;
using UnityEngine;
using Verse;

namespace TalentTrade
{
    public class TalentTradeTab
    {
        public static readonly TalentTradeTab Instance = new TalentTradeTab();

        public int TabIndex = -1;

        private const float SUB_TAB_HEIGHT = 30f;
        private const float SPACING = 10f;

        private enum SubTab
        {
            DirectTrade,
            Market,
            Rental
        }

        private SubTab activeSubTab = SubTab.Market;
        private readonly MarketPanel marketPanel = new MarketPanel();

        public void Draw(Rect inRect)
        {
            if (Client.Instance == null || !Client.Instance.Online)
            {
                Widgets.DrawMenuSection(inRect);
                Widgets.NoneLabelCenteredVertically(inRect, "TalentTrade_pleaseLogIn".Translate());
                return;
            }

            // Sub-tab bar
            Rect tabBarRect = new Rect(inRect.x, inRect.y, inRect.width, SUB_TAB_HEIGHT);
            DrawSubTabs(tabBarRect);

            // Content area
            Rect contentRect = new Rect(inRect.x, inRect.y + SUB_TAB_HEIGHT + SPACING, inRect.width, inRect.height - SUB_TAB_HEIGHT - SPACING);

            switch (activeSubTab)
            {
                case SubTab.DirectTrade:
                    DrawDirectTradePanel(contentRect);
                    break;
                case SubTab.Market:
                    DrawMarketPanel(contentRect);
                    break;
                case SubTab.Rental:
                    DrawRentalPanel(contentRect);
                    break;
            }
        }

        private void DrawSubTabs(Rect rect)
        {
            float tabWidth = rect.width / 3f;

            Rect tab1 = new Rect(rect.x, rect.y, tabWidth, rect.height);
            Rect tab2 = new Rect(rect.x + tabWidth, rect.y, tabWidth, rect.height);
            Rect tab3 = new Rect(rect.x + tabWidth * 2f, rect.y, tabWidth, rect.height);

            if (DrawSubTabButton(tab1, "TalentTrade_subTabDirectTrade".Translate(), activeSubTab == SubTab.DirectTrade))
            {
                activeSubTab = SubTab.DirectTrade;
            }
            if (DrawSubTabButton(tab2, "TalentTrade_subTabMarket".Translate(), activeSubTab == SubTab.Market))
            {
                activeSubTab = SubTab.Market;
            }
            if (DrawSubTabButton(tab3, "TalentTrade_subTabRental".Translate(), activeSubTab == SubTab.Rental))
            {
                activeSubTab = SubTab.Rental;
            }
        }

        private bool DrawSubTabButton(Rect rect, string label, bool active)
        {
            if (active)
            {
                Widgets.DrawHighlight(rect);
            }

            bool clicked = Widgets.ButtonText(rect, label, drawBackground: !active, doMouseoverSound: true, active: !active);
            return clicked;
        }

        private void DrawDirectTradePanel(Rect rect)
        {
            // TODO: Phase 4 — DirectTradePanel
            Widgets.DrawMenuSection(rect);
            Widgets.NoneLabelCenteredVertically(rect, "TalentTrade_comingSoon".Translate());
        }

        private void DrawMarketPanel(Rect rect)
        {
            marketPanel.Draw(rect);
        }

        private void DrawRentalPanel(Rect rect)
        {
            // TODO: Phase 5 — RentalPanel
            Widgets.DrawMenuSection(rect);
            Widgets.NoneLabelCenteredVertically(rect, "TalentTrade_comingSoon".Translate());
        }
    }
}
