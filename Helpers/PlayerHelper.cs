// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using Mirror;

namespace LiarsAntiCheat.Helpers
{
    public static class PlayerHelper
    {
        public static PlayerObjectController GetPlayerByConnection(NetworkConnectionToClient cn)
        {
            // Find the CustomNetworkManager instance in the scene.
            CustomNetworkManager networkManager = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>();
            if (networkManager == null)
            {
                LiarsAntiCheatMod.mls.LogError("CustomNetworkManager instance not being found.");
                return null;
            }

            // Find the player with the provided connection
            foreach (PlayerObjectController player in networkManager.GamePlayers)
            {
                if (player.connectionToClient == cn)
                {
                    return player;
                }
            }
            LiarsAntiCheatMod.mls.LogError($"Player with connection ID {cn.connectionId} is not found.");
            return null;
        }

        public static void KickPlayer(NetworkConnectionToClient cn, string reason)
        {
            ChatHelper.Broadcast(reason, "red");
            PlayerObjectController player = GetPlayerByConnection(cn);

            LiarsAntiCheatMod.mls.LogInfo($"Kicking Player {cn.connectionId}...");
            cn.Disconnect();

            if (player == null)
            {
                ChatHelper.Broadcast($"Kicked Player {cn.connectionId}!", "red");
                return;
            }

            string shortName = ChatHelper.NormalizeNameInChat(player.PlayerName);
            ChatHelper.Broadcast($"Kicked {shortName} (SteamID: {player.PlayerSteamID})", "red");
        }
    }
}
