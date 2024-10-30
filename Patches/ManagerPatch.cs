// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(Manager))]
    internal class ManagerPatch
    {
        [HarmonyPatch(typeof(Manager), "StartGame")]
        [HarmonyPostfix]
        static void OnStartGame(Manager __instance)
        {
            if (__instance.mode == CustomNetworkManager.GameMode.LiarsDeck)
            { 
                BlorfGamePlayPatch.realBullets.Clear();
            }
        }
    }
}
