// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;
using Mirror;
using System.Diagnostics;
using System.Reflection;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(SyncList<int>))]
    internal class SyncListPatch
    {
        [HarmonyPatch(nameof(SyncList<int>.Add))]
        [HarmonyPrefix]
        static void ObfuscateThrownCards(SyncList<int> __instance, ref int item)
        {
            // Check the call stack to see if the caller is UserCode_ThrowCardsCmd__List
            StackTrace stackTrace = new StackTrace();
            foreach (StackFrame frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method.Name.Contains("UserCode_ThrowCardsCmd__List") && method.DeclaringType.Name == "BlorfGamePlay")
                {
                    // Obfuscate thrown cards
                    item = 4;
                    break;
                }
            }
        }
    }
}
