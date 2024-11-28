// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;
using LiarsAntiCheat.Helpers;
using System.Collections.Generic;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(BlorfGamePlayManager))]
    internal class BlorfGamePlayManagerPatch
    {
        public static List<int> RealLastRound = new List<int>();  // storing the real cards that were thrown on to the table

        [HarmonyPatch(nameof(BlorfGamePlayManager.CallLiar))]
        [HarmonyPrefix]
        static void SendRealCardsOnTable(BlorfGamePlayManager __instance)
        {
            // We need to send to everyone the real cards that were played on the table before revealing them :D
            __instance.LastRound.Clear();
            for (int i = 0; i < RealLastRound.Count; i++)
            {
                __instance.LastRound.Add(RealLastRound[i]);
            }
        }

        [HarmonyPatch(typeof(BlorfGamePlayManager), "ResetRound")]
        [HarmonyPrefix]
        static void OnNewGame(BlorfGamePlayManager __instance, ref bool first)
        {
            if (!first)
            {
                return;
            }
            ChatHelper.Broadcast("This game is protected by an Anti-Cheat mod", "white");
        }
    }
}
