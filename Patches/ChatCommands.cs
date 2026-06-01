using HarmonyLib;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace CruiserLoader.Patches
{
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextToChatOnServer))]
    public static class CaptureCommand
    {
        [HarmonyPrefix]
        public static bool Prefix(string chatMessage)
        {
            if (string.IsNullOrEmpty(chatMessage)) return true;
            if (!chatMessage.StartsWith("/cload")) return true;

            if (!NetworkManager.Singleton.IsHost)
            {
                HUDManager.Instance.AddTextToChatOnServer("Only the host can use /cload.", -1);
                return false;
            }

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player == null) return false;

            VehicleController cruiser = Object.FindObjectOfType<VehicleController>();
            if (cruiser == null)
            {
                CruiserLoader.Log.LogInfo("No cruiser found!");
                return false;
            }

            string[] parts = chatMessage.Trim().Split(' ');

            if (parts.Length == 1)
            {
                CruiserZoneManager.MoveItemsToCruiser(cruiser);
            }
            else if (parts.Length == 3)
            {
                string itemSearch = parts[1].ToLower();
                if (!int.TryParse(parts[2], out int count) || count <= 0)
                    return false;
                

                string? matchedName = null;
                foreach (var key in CruiserLoadPatch.ItemZoneConfig.Keys)
                {
                    if (key.ToLower().Contains(itemSearch))
                    {
                        matchedName = key;
                        break;
                    }
                }

                if (matchedName == null)
                    return false;
                
                CruiserZoneManager.MoveItemsToCruiser(cruiser, matchedName, count);
            }

            return false;
        }
    }
}


