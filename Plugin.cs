// ----------------------------------------------------------------------
// Copyright (c) Tyzeron. All Rights Reserved.
// Licensed under the GNU Affero General Public License, Version 3
// ----------------------------------------------------------------------

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LiarsAntiCheat.Helpers;
using LiarsAntiCheat.Patches;

namespace LiarsAntiCheat
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class LiarsAntiCheatMod : BaseUnityPlugin
    {
        public const string modGUID = "LiarsAntiCheat";
        public const string modName = "Liar's Anti-Cheat Mod";
        public const string modVersion = "1.0.0";
        public const string modAuthor = "Tyzeron";
        public const string modRepository = "tyzeron/LiarsAntiCheat";

        private static LiarsAntiCheatMod Instance;
        public static ManualLogSource mls;
        private readonly Harmony harmony = new Harmony(modGUID);

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo($"{modName} {modVersion} loaded!");

            // Start the Task of getting the latest version
            _ = VersionChecker.GetLatestVersionAsync();

            // Patching stuff
            harmony.PatchAll(typeof(BlorfGamePlayPatch));
            harmony.PatchAll(typeof(BlorfGamePlayManagerPatch));
            harmony.PatchAll(typeof(SyncListPatch));
            harmony.PatchAll(typeof(ChatNetworkPatch));
            harmony.PatchAll(typeof(ManagerPatch));
            harmony.PatchAll(typeof(CustomNetworkManagerPatch));
        }
    }
}
