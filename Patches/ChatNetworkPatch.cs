// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;
using LiarsAntiCheat.Helpers;
using Mirror;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(ChatNetwork))]
    internal class ChatNetworkPatch
    {
        [HarmonyPatch(typeof(ChatNetwork), "InvokeUserCode_CmdSendMessage__String")]
        [HarmonyPrefix]
        static bool ValidateChatMessage(ChatNetwork __instance, ref NetworkBehaviour obj, ref NetworkReader reader, ref NetworkConnectionToClient senderConnection)
        {
            if (senderConnection == NetworkServer.localConnection)
            {
                return true;
            }
            PlayerObjectController player = PlayerHelper.GetPlayerByConnection(senderConnection);
            if (player == null)
            {
                LiarsAntiCheatMod.mls.LogError("Failed to validate chat message due to the error above!");
                return true;
            }

            string expectedPlayerName = ChatHelper.NormalizeNameInChat(player.PlayerName);
            string expectedPrefix = $"<color=#FDE2AA>[{expectedPlayerName}]</color>:";

            int originalPosition = reader.Position;
            string chatMessage = reader.ReadString();
            reader.Position = originalPosition;

            if (!chatMessage.StartsWith(expectedPrefix))
            {
                LiarsAntiCheatMod.mls.LogWarning(
                    $"Player {player.PlayerName} (SteamID: {player.PlayerSteamID}) attempted " +
                    $"to send the following illegal chat message: {chatMessage}"
                    );
                PlayerHelper.KickPlayer(senderConnection, $"Player {senderConnection.connectionId} sent an illegal chat message.");
                return false;
            }

            return true;
        }
    }
}
