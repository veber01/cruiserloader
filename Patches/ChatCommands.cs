using HarmonyLib;
using GameNetcodeStuff;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace CruiserLoader.Patches
{
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextToChatOnServer))]
    public static class CaptureCommand
    {
        [HarmonyPrefix]
        public static bool Prefix(string chatMessage)
        {
            if (string.IsNullOrEmpty(chatMessage)) return true;
            if (!chatMessage.StartsWith(".cload")) return true;

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player == null) return false;

            VehicleController cruiser = Object.FindObjectOfType<VehicleController>();
            if (cruiser == null)
            {
                CruiserLoader.Log.LogInfo("No cruiser found!");
                HUDManager.Instance.AddTextToChatOnServer("[CL] Buy a cruiser first! BAKA!", -1);
                return false;
            }

            if (!NetworkManager.Singleton.IsHost)
            {
                if (NetworkManager.Singleton.CustomMessagingManager != null)
                {
                    using var writer = new FastBufferWriter(512, Allocator.Temp);
                    writer.WriteValueSafe(chatMessage);
                    CruiserLoader.Log.LogInfo($"Sending command to host: {chatMessage}");
                    NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("CLC", NetworkManager.ServerClientId, writer);
                }
                else
                {
                    HUDManager.Instance.AddTextToChatOnServer("[CL] Host does not have CL installed!", -1);
                }

                return false;
            }

            ExecuteCommand(chatMessage, player, cruiser);
            return false;
        }

        private static void ExecuteCommand(string chatMessage, PlayerControllerB player, VehicleController cruiser)
        {
            string[] parts = chatMessage.Trim().Split(' ');

            if (parts.Length == 1)
            {
                CruiserZoneManager.MoveItemsToCruiser(cruiser);
            }
            else if (parts.Length == 3)
            {
                string itemSearch = parts[1].ToLower();
                if (!int.TryParse(parts[2], out int count) || count <= 0)
                    return;
                
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
                    return;
                
                CruiserZoneManager.MoveItemsToCruiser(cruiser, matchedName, count);
            }
            else if (parts.Length == 4)
            {
                string itemSearch = parts[1].ToLower();
                if (!int.TryParse(parts[2], out int count) || count <= 0)
                    return;

                string overrideZone = parts[3].ToUpper();
                if (!CruiserPositions.Zones.ContainsKey(overrideZone))
                {
                    HUDManager.Instance.AddTextToChatOnServer($"[CL] Invalid zone: {overrideZone}", -1);
                    return;
                }

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
                    return;
                
                CruiserZoneManager.MoveItemsToCruiser(cruiser, matchedName, count, overrideZone);
            }
        }
    }
}


