// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;
using LethalCompanyMinimap;
using LiarsAntiCheat.Helpers;
using Mirror;
using System;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(CustomNetworkManager))]
    internal class CustomNetworkManagerPatch
    {
        [HarmonyPatch(nameof(CustomNetworkManager.OnServerAddPlayer))]
        [HarmonyPostfix]
        static void OnPlayerJoin(CustomNetworkManager __instance, ref NetworkConnectionToClient conn)
        {
            if (conn.connectionId != 0)  // for some reason `__instance.Server` returns False even if you are the host
            {
                return;
            }

            // Check for updates
            if (VersionChecker.latestVersion == null)
            {
                ChatHelper.Broadcast("Failed to check for latest version", "red");
            }
            else if (new Version(LiarsAntiCheatMod.modVersion) < new Version(VersionChecker.latestVersion))
            {
                ChatHelper.Broadcast($"There is a new version available: <b>{VersionChecker.latestVersion}</b>", "green");
            }
            else
            {
                ChatHelper.Broadcast("GLHF and no cheating ;)", "white");
            }
        }
    }
}
