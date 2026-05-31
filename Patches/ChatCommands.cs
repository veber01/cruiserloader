using HarmonyLib;
using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace CruiserLoader.Patches
{
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextToChatOnServer))]
    public static class CaptureCommand
    {
        [HarmonyPrefix]
        public static bool Prefix(string chatMessage)
        {
            if (string.IsNullOrEmpty(chatMessage)) return true;
            if (!chatMessage.Equals("/cload")) return true;

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player == null) return false;
            if (!NetworkManager.Singleton.IsHost) return false;

            VehicleController cruiser = Object.FindObjectOfType<VehicleController>();
            if (cruiser == null)
            {
                CruiserLoader.Log.LogInfo("No cruiser found!");
                return false;
            }

            CruiserZoneManager.MoveItemsToCruiser(cruiser);

            return false;
        }
    }
}


