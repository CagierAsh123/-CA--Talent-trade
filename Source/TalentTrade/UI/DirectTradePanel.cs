using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TalentTrade
{
    /// <summary>
    /// Direct trade sub-tab: online users list + active trades list.
    /// </summary>
    public class DirectTradePanel
    {
        private const float ROW_HEIGHT = 36f;
        private const float BUTTON_WIDTH = 80f;
        private const float BUTTON_HEIGHT = 28f;
        private const float TOOLBAR_HEIGHT = 36f;
        private const float SPACING = 6f;

        private Vector2 usersScrollPos;
        private Vector2 tradesScrollPos;

        // Currently open trade window
        private DirectTradeWindow openTradeWindow;

        public void Draw(Rect rect)
        {
            // Left: online users (40%)
            float leftWidth = Mathf.Floor(rect.width * 0.4f) - SPACING;
            Rect leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            DrawOnlineUsers(leftRect);

            // Right: active trades (60%)
            Rect rightRect = new Rect(rect.x + leftWidth + SPACING, rect.y, rect.width - leftWidth - SPACING, rect.height);
            DrawActiveTrades(rightRect);
        }

        private void DrawOnlineUsers(Rect rect)
        {
            // Header
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, TOOLBAR_HEIGHT);
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, "TalentTrade_tradeOnlineUsers".Translate());

            Rect listRect = new Rect(rect.x, rect.y + TOOLBAR_HEIGHT, rect.width, rect.height - TOOLBAR_HEIGHT);
            Widgets.DrawMenuSection(listRect);

            string localUuid = TalentTradeManager.GetLocalUuid();
            string[] userUuids = TalentTradeManager.GetOnlineUserUuids();

            // Filter out self
            List<string> others = new List<string>();
            for (int i = 0; i < userUuids.Length; i++)
            {
                if (userUuids[i] != localUuid)
                    others.Add(userUuids[i]);
            }

            if (others.Count == 0)
            {
                Widgets.NoneLabelCenteredVertically(listRect, "---");
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, others.Count * (ROW_HEIGHT + SPACING));
            Widgets.BeginScrollView(listRect, ref usersScrollPos, viewRect);

            float y = 0f;
            for (int i = 0; i < others.Count; i++)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, ROW_HEIGHT);
                DrawUserRow(rowRect, others[i]);
                y += ROW_HEIGHT + SPACING;
            }

            Widgets.EndScrollView();
        }

        private void DrawUserRow(Rect rect, string uuid)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect inner = rect.ContractedBy(4f);

            // Display name
            string displayName = uuid;
            try
            {
                string name;
                if (PhinixClient.Client.Instance != null && PhinixClient.Client.Instance.TryGetDisplayName(uuid, out name))
                {
                    displayName = name;
                }
            }
            catch { }

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(inner.x, inner.y, inner.width - BUTTON_WIDTH - SPACING, inner.height), displayName);

            // Trade button
            Rect btnRect = new Rect(inner.xMax - BUTTON_WIDTH, inner.y, BUTTON_WIDTH, BUTTON_HEIGHT);
            if (Widgets.ButtonText(btnRect, "TalentTrade_tradeWith".Translate()))
            {
                string tradeId = TalentTradeManager.InitiateDirectTrade(uuid);
                if (tradeId != null)
                {
                    OpenTradeWindow(tradeId);
                }
            }
        }

        private void DrawActiveTrades(Rect rect)
        {
            // Header
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, TOOLBAR_HEIGHT);
            Text.Font = GameFont.Small;
            Widgets.Label(headerRect, "TalentTrade_tradeActiveTrades".Translate());

            Rect listRect = new Rect(rect.x, rect.y + TOOLBAR_HEIGHT, rect.width, rect.height - TOOLBAR_HEIGHT);
            Widgets.DrawMenuSection(listRect);

            DirectTrade[] trades = TalentTradeManager.GetActiveTradesSnapshot();
            if (trades.Length == 0)
            {
                Widgets.NoneLabelCenteredVertically(listRect, "TalentTrade_tradeNoActive".Translate());
                return;
            }

            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, trades.Length * (ROW_HEIGHT + SPACING));
            Widgets.BeginScrollView(listRect, ref tradesScrollPos, viewRect);

            float y = 0f;
            string localUuid = TalentTradeManager.GetLocalUuid();
            for (int i = 0; i < trades.Length; i++)
            {
                if (trades[i] == null) continue;
                Rect rowRect = new Rect(0f, y, viewRect.width, ROW_HEIGHT);
                DrawTradeRow(rowRect, trades[i], localUuid);
                y += ROW_HEIGHT + SPACING;
            }

            Widgets.EndScrollView();
        }

        private void DrawTradeRow(Rect rect, DirectTrade trade, string localUuid)
        {
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
            }

            Rect inner = rect.ContractedBy(4f);

            // Other party name
            bool isInitiator = trade.InitiatorUuid == localUuid;
            string otherName = isInitiator ? trade.TargetName : trade.InitiatorName;
            if (string.IsNullOrEmpty(otherName)) otherName = isInitiator ? trade.TargetUuid : trade.InitiatorUuid;

            Text.Font = GameFont.Small;
            float labelWidth = inner.width - BUTTON_WIDTH * 2 - SPACING * 2;
            Widgets.Label(new Rect(inner.x, inner.y, labelWidth, inner.height),
                otherName + " | " + "TalentTrade_tradeStatus".Translate(trade.State.ToString()));

            float btnX = inner.xMax - BUTTON_WIDTH * 2 - SPACING;

            // Pending trades from others: Accept/Reject
            if (trade.State == DirectTradeState.Pending && trade.TargetUuid == localUuid)
            {
                Rect acceptBtn = new Rect(btnX, inner.y, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (Widgets.ButtonText(acceptBtn, "TalentTrade_tradeAcceptRequest".Translate()))
                {
                    TalentTradeManager.AcceptTrade(trade.Id);
                    OpenTradeWindow(trade.Id);
                }
                btnX += BUTTON_WIDTH + SPACING;

                Rect rejectBtn = new Rect(btnX, inner.y, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (Widgets.ButtonText(rejectBtn, "TalentTrade_tradeRejectRequest".Translate()))
                {
                    TalentTradeManager.RejectTrade(trade.Id);
                }
            }
            else
            {
                // Open button
                Rect openBtn = new Rect(btnX, inner.y, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (Widgets.ButtonText(openBtn, "TalentTrade_tradeOpen".Translate()))
                {
                    OpenTradeWindow(trade.Id);
                }
                btnX += BUTTON_WIDTH + SPACING;

                // Cancel button
                Rect cancelBtn = new Rect(btnX, inner.y, BUTTON_WIDTH, BUTTON_HEIGHT);
                if (Widgets.ButtonText(cancelBtn, "TalentTrade_cancel".Translate()))
                {
                    TalentTradeManager.CancelTrade(trade.Id);
                }
            }
        }

        private void OpenTradeWindow(string tradeId)
        {
            DirectTrade trade = TalentTradeManager.GetTrade(tradeId);
            if (trade == null) return;

            if (openTradeWindow != null)
            {
                openTradeWindow.Close();
            }

            openTradeWindow = new DirectTradeWindow(trade);
            Find.WindowStack.Add(openTradeWindow);
        }
    }
}
