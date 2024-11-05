// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using HarmonyLib;
using LiarsAntiCheat.Helpers;
using LiarsAntiCheat.Models;
using Mirror;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace LiarsAntiCheat.Patches
{
    [HarmonyPatch(typeof(BlorfGamePlay))]
    internal class BlorfGamePlayPatch
    {
        private static readonly FieldInfo currentRevolverField = AccessTools.Field(typeof(BlorfGamePlay), "currentrevoler");
        private static readonly FieldInfo revolverBulletField = AccessTools.Field(typeof(BlorfGamePlay), "revolverbulllet");
        private static readonly MethodBase updateCallMethod = AccessTools.Method(typeof(BlorfGamePlay), "UpdateCall");

        private const int fakeCard = 4;  // Joker
        private const int fakeBullet = 5;  // 6th (last) slot

        private static readonly Dictionary<BlorfGamePlay, BlorfGamePlaySyncVars> syncVarCache = new Dictionary<BlorfGamePlay, BlorfGamePlaySyncVars>();
        public static readonly Dictionary<NetworkConnectionToClient, int> realBullets = new Dictionary<NetworkConnectionToClient, int>();  // storing the real bullet position

        [HarmonyPatch(nameof(BlorfGamePlay.RandomCards))]
        [HarmonyPrefix]
        static bool ObfuscatePlayerCards(BlorfGamePlay __instance, int card1, int card2, int card3, int card4, int card5)
        {
            // Loop through all connected clients and send appropriate RPCs
            foreach (NetworkConnectionToClient cn in NetworkServer.connections.Values)
            {
                if (cn == __instance.connectionToClient || cn == NetworkServer.localConnection)
                {
                    // Send actual card data to the owner and host
                    SendStartingCardsRpc(__instance, cn, card1, card2, card3, card4, card5);
                }
                else
                {
                    // Send obfuscated card data to other clients
                    SendStartingCardsRpc(__instance, cn, fakeCard, fakeCard, fakeCard, fakeCard, fakeCard);
                }
            }
            // Skip original method
            return false;
        }

        private static void SendStartingCardsRpc(NetworkBehaviour nb, NetworkConnectionToClient conn, int card1, int card2, int card3, int card4, int card5)
        {
            NetworkWriterPooled writer = NetworkWriterPool.Get();
            writer.WriteInt(card1);
            writer.WriteInt(card2);
            writer.WriteInt(card3);
            writer.WriteInt(card4);
            writer.WriteInt(card5);

            NetworkHelper.SendTargetCustomRPCInternal(
                nb, conn,
                "System.Void BlorfGamePlay::RandomCards(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)",
                1239976969,
                writer,
                0
            );
            NetworkWriterPool.Return(writer);
        }

        [HarmonyPatch(typeof(BlorfGamePlay), "UserCode_ThrowCardsCmd__List`1")]
        [HarmonyPrefix]
        static bool ValidateThrownCard(BlorfGamePlay __instance, ref List<int> types)
        {
            // Get the cards that the player should have in their hands
            List<int> playerCards = __instance.Cards
                .Where(card => card.activeSelf)
                .Select(card => card.GetComponent<Card>().Devil ? -1 : card.GetComponent<Card>().cardtype)
                .ToList();

            // Validate that the card they throw is in their hand
            foreach (int cardType in types)
            {
                if (!playerCards.Contains(cardType))
                {
                    // The player is trying to play a card they don't have
                    PlayerHelper.KickPlayer(
                        __instance.connectionToClient,
                        $"Player {__instance.connectionToClient.connectionId} tried to play card(s) that they did not have."
                    );
                    return false; // Cancel the execution of the original method
                }
                else
                {
                    // Remove the card from the player's hand for further validation
                    playerCards.Remove(cardType);
                }
            }

            // Store the real thrown cards elsewhere, since we are going to obfuscate these thrown cards
            BlorfGamePlayManagerPatch.RealLastRound = new List<int>(types);

            // Allow the original method to execute since validation passed
            return true;
        }

        [HarmonyPatch(nameof(BlorfGamePlay.StartRevolverProcesses))]
        [HarmonyPrefix]
        static void SendRealBulletToPlayer(BlorfGamePlay __instance, ref NetworkConnectionToClient cn, ref bool playses)
        {
            // Determine if the player will actually die or not
            int currentRevolver = (int)currentRevolverField.GetValue(__instance);
            int realBullet = realBullets[__instance.connectionToClient];
            if (currentRevolver != realBullet)
            {
                return;
            }

            // Yup, the player is about to die, need to tell them about the real bullet - TODO: it does not work, FIX THIS
            __instance.Networkrevolverbulllet = realBullet;
        }

        [HarmonyPatch(nameof(BlorfGamePlay.Networkrevolverbulllet), MethodType.Setter)]
        [HarmonyPrefix]
        static void SendOurFakeBulletToPlayers(BlorfGamePlay __instance, ref int value)
        {
            // We are probably the client and not the host (server)
            if (__instance.connectionToClient == null)
            {
                return;
            }

            // Determine if we setting bullet from BlorfGamePlay.UpdateCall() method
            StackTrace stackTrace = new StackTrace();
            MethodBase method = stackTrace.GetFrame(2)?.GetMethod();
            if (method != updateCallMethod)
            {
                return;
            }

            // Store our real bullet locally and send fake bullet to everyone else
            realBullets[__instance.connectionToClient] = value;
            value = fakeBullet;
        }

        [HarmonyPatch(nameof(BlorfGamePlay.DeserializeSyncVars))]
        [HarmonyPrefix]
        static void CacheSyncVarFields(BlorfGamePlay __instance, ref NetworkReader reader, ref bool initialState)
        {
            // Get private SyncVar fields using reflection
            int currentRevolver = (int)currentRevolverField.GetValue(__instance);
            int revolverBullet = (int)revolverBulletField.GetValue(__instance);

            // We cache the current state of SyncVar fields of BlorfGamePlay for later validation
            syncVarCache[__instance] = new BlorfGamePlaySyncVars
            {
                Looking = __instance.Looking,
                CurrentRevoler = currentRevolver,
                RevolverBulllet = revolverBullet,
                HaveCards = __instance.HaveCards
            };
        }

        [HarmonyPatch(nameof(BlorfGamePlay.DeserializeSyncVars))]
        [HarmonyPostfix]
        static void ValidateReceivedSyncVars(BlorfGamePlay __instance, ref NetworkReader reader, ref bool initialState)
        {
            // We are probably the client and not the host (server)
            if (__instance.connectionToClient == null)
            {
                return;
            }

            BlorfGamePlaySyncVars oldSyncVars = syncVarCache[__instance];
            int currentRevolver = (int)currentRevolverField.GetValue(__instance);
            int revolverBullet = (int)revolverBulletField.GetValue(__instance);
            bool isFirstCall = !realBullets.ContainsKey(__instance.connectionToClient);

            // LiarsAntiCheatMod.mls.LogInfo($"----- Player {__instance.connectionToClient.connectionId} -----");
            // LiarsAntiCheatMod.mls.LogInfo($"looking: {oldSyncVars.Looking} -> {__instance.Looking}");
            // LiarsAntiCheatMod.mls.LogInfo($"CurrentRevoler: {oldSyncVars.CurrentRevoler} -> {currentRevolver}");
            // LiarsAntiCheatMod.mls.LogInfo($"RevolverBulllet: {oldSyncVars.RevolverBulllet} -> {revolverBullet}");
            // LiarsAntiCheatMod.mls.LogInfo($"HaveCards: {oldSyncVars.HaveCards} -> {__instance.HaveCards}");
            // LiarsAntiCheatMod.mls.LogInfo($"--------------------");

            // Validate revolver slot value
            if (currentRevolver < 0 || currentRevolver > 5)
            {
                // The player tried to set their current revolver slot to an invalid one
                PlayerHelper.KickPlayer(
                    __instance.connectionToClient,
                    $"Player {__instance.connectionToClient.connectionId} tried to set their current revolver slot to {currentRevolver}"
                );
                return;
            }

            // Validate revolver bullet value
            if (revolverBullet < 0 || revolverBullet > 5)
            {
                // The player tried to load thier bullet to an invalid slot
                PlayerHelper.KickPlayer(
                    __instance.connectionToClient,
                    $"Player {__instance.connectionToClient.connectionId} tried to load their bullet to slot {revolverBullet}"
                );
                return;
            }

            // Validate revolver slot can either not change or go up by one
            if (currentRevolver != oldSyncVars.CurrentRevoler && currentRevolver != oldSyncVars.CurrentRevoler + 1)
            {
                // The player tried to illegally modify their current revolver slot
                PlayerHelper.KickPlayer(
                    __instance.connectionToClient,
                    $"Player {__instance.connectionToClient.connectionId} tried to change " +
                    $"their current revolver slot from {oldSyncVars.CurrentRevoler} to {currentRevolver}"
                );
                return;
            }
            
            // Validate the first call of the game
            if (isFirstCall)
            {
                if (currentRevolver != 0)
                {
                    // The player tried to start on a non-zero revolver slot
                    PlayerHelper.KickPlayer(
                        __instance.connectionToClient,
                        $"Player {__instance.connectionToClient.connectionId} tried to start on the {currentRevolver} slot"
                    );
                    return;
                }

                // If the client is on a potato PC, there is a chance of false positive, since the first call would be late
                /*
                if (__instance.Looking)
                {
                    // The player should not be able to look around at the start of the game
                    PlayerHelper.KickPlayer(
                        __instance.connectionToClient,
                        $"Player {__instance.connectionToClient.connectionId} tried to look around while looking should be locked"
                    );
                    return;
                }
                if (__instance.HaveCards)
                {
                    // The player should start with no cards
                    PlayerHelper.KickPlayer(
                        __instance.connectionToClient,
                        $"Player {__instance.connectionToClient.connectionId} tried to have cards before they are being distributed"
                    );
                    return;
                }
                */

                // We randomly determine bullet position for the player on their first call
                realBullets[__instance.connectionToClient] = UnityEngine.Random.Range(0, 6);

                // Send fake bullet position to everyone  - TODO: it does not work, FIX THIS
                __instance.Networkrevolverbulllet = fakeBullet;
            }
            

            // If revolver bullet position changed and this is not the first call
            if (oldSyncVars.RevolverBulllet != revolverBullet && !isFirstCall)
            {
                // The player tried to change the revolver bullet position mid-game
                PlayerHelper.KickPlayer(
                     __instance.connectionToClient,
                     $"Player {__instance.connectionToClient.connectionId} tried to change their bullet position " +
                     $"from {oldSyncVars.RevolverBulllet} to {revolverBullet}"
                 );
                return;
            }
        }
    }
}
