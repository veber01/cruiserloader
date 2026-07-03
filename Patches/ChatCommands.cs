using HarmonyLib;
using GameNetcodeStuff;
using Unity.Collections;
using Unity.Netcode;

namespace CruiserLoader.Patches
{
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.AddTextToChatOnServer))]
    public static class CaptureCommand
    {
        private const float timeout = 2f;

        [HarmonyPrefix]
        public static bool Prefix(string chatMessage)
        {
            if (!string.IsNullOrEmpty(chatMessage) && chatMessage.StartsWith(".cload"))
            {
                CruiserLoader.Log.LogInfo("[CL] Prefix intercepted .cload");
            }
            if (string.IsNullOrEmpty(chatMessage)) return true;
            if (!chatMessage.StartsWith(".cload")) return true;

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player == null) return false;

            VehicleController cruiser = UnityEngine.Object.FindObjectOfType<VehicleController>();
            if (cruiser == null)
            {
                CruiserLoader.DisplayTip("CruiserLoader", "Buy a cruiser first!\nBAKA!");
                return false;
            }

            if (!NetworkManager.Singleton.IsHost)
            {
                if (NetworkManager.Singleton.CustomMessagingManager == null)
                {
                    CruiserLoader.DisplayTip("CruiserLoader", "Network issue: CustomMessagingManager unavailable");
                    return false;
                }

                MSGHandlers.RegisterHandlers();

                MSGHandlers.pendingVerification = true;
                MSGHandlers.verificationReceived = false;
                

                using var writer = new FastBufferWriter(512, Allocator.Temp);
                writer.WriteValueSafe(chatMessage);
                CruiserLoader.Log.LogInfo($"Sending command to host: {chatMessage}");
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("CLC", NetworkManager.ServerClientId, writer);

                VeriRunner.Instance.WaitThen(timeout, () =>
                {
                    if (MSGHandlers.pendingVerification && !MSGHandlers.verificationReceived)
                    {
                        CruiserLoader.DisplayTip("CruiserLoader", "Host does not have\nCruiser Loader installed!");
                        MSGHandlers.pendingVerification = false;

                        try
                        {
                            if (CruiserLoader.ExperimentalClientUse != null && CruiserLoader.ExperimentalClientUse.Value)
                            {
                                string localResult = ExecuteCommand(chatMessage, player, cruiser);
                                if (!string.IsNullOrEmpty(localResult))
                                {
                                    CruiserLoader.DisplayTip("CruiserLoader", localResult);
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            CruiserLoader.Log.LogError($"[CL] BRUH1: {ex}");
                        }
                    }
                });

                return false;
            }

            string resultText = ExecuteCommand(chatMessage, player, cruiser);
            if (!string.IsNullOrEmpty(resultText))
            {
                CruiserLoader.DisplayTip("CruiserLoader", resultText);
                MSGHandlers.SendMSGToClients(resultText);
            }
            return false;
        }

        private static string ExecuteCommand(string chatMessage, PlayerControllerB player, VehicleController cruiser)
        {
            string[] parts = chatMessage.Trim().Split(' ');

            if (parts.Length == 1)
            {
                return CruiserZoneManager.MoveItemsToCruiser(cruiser);
            }
            else if (parts.Length == 2 && parts[1].ToLower() == "restock")
            {
                bool enabled = !CruiserLoader.AutoRestockEnabled.Value;
                CruiserLoader.AutoRestockEnabled.Value = enabled;
                CruiserLoader.ItemZoneConfig.Save();
                CruiserLoader.DisplayTip("CruiserLoader", $"AutoRestock {CruiserLoader.AutoRestockEnabled.Value}");
                return enabled ? "Auto restock enabled." : "Auto restock disabled.";
            }
            else if (parts.Length == 3)
            {
                string itemSearch = parts[1].ToLower();
                if (!int.TryParse(parts[2], out int count) || count <= 0)
                    return string.Empty;

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
                    return string.Empty;

                return CruiserZoneManager.MoveItemsToCruiser(cruiser, matchedName, count);
            }
            else if (parts.Length == 4)
            {
                string itemSearch = parts[1].ToLower();
                if (!int.TryParse(parts[2], out int count) || count <= 0)
                    return string.Empty;

                string overrideZone = parts[3].ToUpper();
                if (!CruiserPositions.Zones.ContainsKey(overrideZone))
                {
                    return $"Invalid zone: {overrideZone}";
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
                    return string.Empty;

                return CruiserZoneManager.MoveItemsToCruiser(cruiser, matchedName, count, overrideZone);
            }

            return string.Empty;
        }

}
}


