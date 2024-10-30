// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using System.Reflection;

namespace LiarsAntiCheat.Helpers
{
    public static class ChatHelper
    {
        public static readonly string prefix = $"<b><color=#ffc300>[{LiarsAntiCheatMod.modName}]</color></b> ";
        public static MethodInfo rpcHandleMessageMethod = typeof(ChatNetwork).GetMethod("RpcHandleMessage", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Broadcast(string message, string color)
        {
            LiarsAntiCheatMod.mls.LogInfo($"(BROADCAST) {message}");

            // Find the ChatNetwork instance in the scene.
            ChatNetwork chatNetwork = UnityEngine.Object.FindObjectOfType<ChatNetwork>();

            if (chatNetwork == null)
            {
                LiarsAntiCheatMod.mls.LogError("Failed to broadcast message due to ChatNetwork instance not being found.");
                return;
            }
            if (rpcHandleMessageMethod == null)
            {
                LiarsAntiCheatMod.mls.LogError("Failed to broadcast message due to RpcHandleMessage method not found.");
                return;
            }

            // Invoke the RpcHandleMessage method with the message.
            string formattedMessage = $"{prefix}<color={color}>{message}</color>";
            rpcHandleMessageMethod.Invoke(chatNetwork, new object[] { formattedMessage });
        }

        public static string NormalizeNameInChat(string playerName)
        {
            // Copied from `ChatNetwork` class, `Send()` method
            string trimmedName = playerName.Trim().Replace(" ", string.Empty);
            return trimmedName.ToLower().Substring(0, UnityEngine.Mathf.Min(trimmedName.Length, 11));
        }
    }
}
