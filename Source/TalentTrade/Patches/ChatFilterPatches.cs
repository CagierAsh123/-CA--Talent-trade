using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using PhinixClient;
using PhinixClient.GUI;

namespace TalentTrade.Patches
{
    [HarmonyPatch(typeof(ChatMessageList), "ChatMessageReceivedEventHandler")]
    internal static class ChatMessageList_MessageReceived_Patch
    {
        private static bool Prefix(object sender, UIChatMessageEventArgs args)
        {
            string message = args?.Message?.Message;
            return !TalentTradeProtocol.IsProtocolMessage(message);
        }
    }

    [HarmonyPatch(typeof(ChatMessageList), "recalculateMessageRects")]
    internal static class ChatMessageList_RecalculateMessageRects_Patch
    {
        private static readonly FieldInfo FilteredMessagesField = AccessTools.Field(typeof(ChatMessageList), "filteredMessages");
        private static readonly FieldInfo MessagesField = AccessTools.Field(typeof(ChatMessageList), "messages");
        private static readonly FieldInfo MessagesLockField = AccessTools.Field(typeof(ChatMessageList), "messagesLock");

        private static void Prefix(ChatMessageList __instance)
        {
            List<UIChatMessage> filteredMessages = FilteredMessagesField.GetValue(__instance) as List<UIChatMessage>;
            List<UIChatMessage> messages = MessagesField.GetValue(__instance) as List<UIChatMessage>;
            object messagesLock = MessagesLockField.GetValue(__instance);

            if (messagesLock != null && messages != null)
            {
                lock (messagesLock)
                {
                    messages.RemoveAll(message => TalentTradeProtocol.IsProtocolMessage(message?.Message));
                }
            }

            if (filteredMessages != null)
            {
                filteredMessages.RemoveAll(message => TalentTradeProtocol.IsProtocolMessage(message?.Message));
            }
        }
    }

    [HarmonyPatch(typeof(ChatMessageList), "ReplaceWithBuffer")]
    internal static class ChatMessageList_ReplaceWithBuffer_Patch
    {
        private static readonly FieldInfo MessagesField = AccessTools.Field(typeof(ChatMessageList), "messages");
        private static readonly FieldInfo MessagesLockField = AccessTools.Field(typeof(ChatMessageList), "messagesLock");
        private static readonly FieldInfo MessagesChangedField = AccessTools.Field(typeof(ChatMessageList), "messagesChanged");

        private static bool Prefix(ChatMessageList __instance)
        {
            if (Client.Instance == null) return true;

            List<UIChatMessage> messages = MessagesField.GetValue(__instance) as List<UIChatMessage>;
            object messagesLock = MessagesLockField.GetValue(__instance);
            if (messages == null || messagesLock == null) return true;

            lock (messagesLock)
            {
                __instance.Clear();

                List<UIChatMessage> buffer = Client.Instance.GetChatMessages()
                    .Where(message => message != null)
                    .Where(message => !Client.Instance.Settings.BlockedUsers.Contains(message.SenderUuid))
                    .Where(message => !TalentTradeProtocol.IsProtocolMessage(message.Message))
                    .ToList();

                int limit = Client.Instance.Settings.ChatMessageLimit;
                int skip = Math.Max(0, buffer.Count - limit);
                messages.AddRange(buffer.Skip(skip));

                MessagesChangedField.SetValue(__instance, true);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ChatMessageList), "drawChatMessage")]
    internal static class ChatMessageList_DrawChatMessage_Patch
    {
        private static bool Prefix(UIChatMessage chatMessage)
        {
            return !TalentTradeProtocol.IsProtocolMessage(chatMessage?.Message);
        }
    }
}
