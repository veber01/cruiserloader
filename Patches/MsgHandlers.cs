using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;

namespace CruiserLoader.Patches
{
    public static class MSGHandlers
    {
        public static bool pendingVerification = false;
        public static bool verificationReceived = false;
        public static bool messageHandlersOK = false;

        public static void SendMSGToClients(string result)
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
            if (string.IsNullOrEmpty(result)) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (client == NetworkManager.ServerClientId) continue;

                using var writer = new FastBufferWriter(512, Allocator.Temp);
                writer.WriteValueSafe(result);
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("CLTR", client, writer);
            }
        }

        public static void OnVerificationReceived()
        {
            verificationReceived = true;
            CruiserLoader.Log.LogInfo("[CL] Host verification OK.");
            pendingVerification = false;
        }

        public static void ResetVerificationState()
        {
            MSGHandlers.pendingVerification = false;
            MSGHandlers.verificationReceived = false;
            if (MSGHandlers.messageHandlersOK && NetworkManager.Singleton?.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("CLVR");
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("CLTR");
                MSGHandlers.messageHandlersOK = false;
                CruiserLoader.Log.LogInfo("[CL] Reset things on disconnect.");
            }
        }

        public static void RegisterHandlers()
        {
            if (MSGHandlers.messageHandlersOK) return;
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;

            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("CLVR", OnVerificationResponse);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("CLTR", OnMSGReceived);
            MSGHandlers.messageHandlersOK = true;
            CruiserLoader.Log.LogInfo("[CL] Verification handlers OK.");
        }

        public static void OnMSGReceived(ulong serverId, FastBufferReader reader)
        {
            FastBufferReader readerCopy = reader;
            readerCopy.ReadValueSafe(out string message);
            if (!string.IsNullOrEmpty(message))
            {
                CruiserLoader.DisplayTip("CruiserLoader", message);
            }
        }

        public static void OnVerificationResponse(ulong serverId, FastBufferReader reader)
        {
            FastBufferReader readerCopy = reader;
            readerCopy.ReadValueSafe(out string message);
            if (message == "CLVR_OK")
            {
                MSGHandlers.OnVerificationReceived();
            }
        }
    }

    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    public static class RegisterVerificationHandlersOnStart
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsHost) return;
            MSGHandlers.RegisterHandlers();
        }
    }

    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Disconnect))]
    public static class ResetVerificationOnDisconnect
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            MSGHandlers.ResetVerificationState();
        }
    }
}
