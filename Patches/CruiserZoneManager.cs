using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CruiserLoader.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class CruiserLoadPatch
    {
        internal static Dictionary<string, ConfigEntry<string>> ItemZoneConfig =
            new Dictionary<string, ConfigEntry<string>>();

        internal static Dictionary<string, ConfigEntry<int>> ItemCountConfig =
            new Dictionary<string, ConfigEntry<int>>();

        [HarmonyPatch("LoadShipGrabbableItems")]
        [HarmonyPrefix]
        internal static void PatchLogic()
        {
            CruiserPositions.PositionsDictionary.Clear();
            CruiserPositions.ZoneAllocated.Clear();
            CruiserPositions.Zones.Clear();
            CruiserPositions.CreateZones();
            PopulateItemConfig();
            TranslateDictionaries();
            NetworkHelper();
        }

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        internal static void Prefix()
        {
            NetworkHelper();
        }

        internal static void NetworkHelper()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsHost) return;
            if (CruiserLoaderNetworkHandler.Instance != null) return;

            CruiserLoader.Log.LogInfo("Creating network handler on host...");
            GameObject handlerObj = new GameObject("CruiserLoaderNetworkHandler");
            handlerObj.AddComponent<CruiserLoaderNetworkHandler>();
        }

        internal static void PopulateItemConfig()
        {
            List<Item> allItemsList = StartOfRound.Instance.allItemsList.itemsList;

            Dictionary<string, string> defaultZones = new Dictionary<string, string>
    {
        { "Radar-booster",   "D2" },
        { "Kitchen knife",   "F2" },
        { "Shovel",          "A2" },
        { "Shotgun",         "A3" },
        { "Ammo",            "B3" },
        { "Weed killer",     "D1" },
        { "Key",             "E2" },
        { "Extension ladder","D3" },
        { "Lockpicker",      "G2" },
        { "TZP-Inhalant",    "F3" },
        { "Boombox",         "C2" },
    };

            Dictionary<string, int> defaultCounts = new Dictionary<string, int>
    {
        { "Radar-booster",   1 },
        { "Kitchen knife",   3 },
        { "Shotgun",         2 },
        { "Weed killer",     15 },
        { "TZP-Inhalant",    15 },
    };

            foreach (Item item in allItemsList)
            {
                if (item.isScrap && !item.itemName.Equals("Kitchen knife") && !item.itemName.Equals("Shotgun")) continue;
                if (item.itemName == "Binoculars" || item.itemName == "box" || item.itemName == "Mapper") continue;

                string defaultZone = defaultZones.GetValueOrDefault(item.itemName, "");
                int defaultCount = defaultCounts.GetValueOrDefault(item.itemName, 5);

                ItemZoneConfig[item.itemName] = CruiserLoader.ItemZoneConfig.Bind(
                    "Items",
                    item.itemName,
                    defaultZone,
                    $"Set cruiser zone for {item.itemName}. Leave empty to not move. (e.g. A1, B2, C3)"
                );

                ItemCountConfig[item.itemName] = CruiserLoader.ItemZoneConfig.Bind(
                    "ItemCounts",
                    item.itemName,
                    defaultCount,
                    $"How many {item.itemName} to move to cruiser."
                );
            }
        }

        internal static void TranslateDictionaries()
        {
            foreach (var item in ItemZoneConfig)
            {
                var itemZone = item.Value.Value;
                if (string.IsNullOrEmpty(itemZone)) continue;
                if (!CruiserPositions.Zones.ContainsKey(itemZone)) continue;

                if (CruiserPositions.PositionsDictionary.ContainsKey(item.Key))
                {
                    var oldPosition = CruiserPositions.PositionsDictionary[item.Key];
                    var oldZone = CruiserPositions.Zones.First(kv => kv.Value == oldPosition).Key;
                    CruiserPositions.ZoneAllocated[oldZone] = Math.Max(0,
                        CruiserPositions.ZoneAllocated.GetValueOrDefault(oldZone, 0) - 1);
                    if (CruiserPositions.ZoneAllocated[oldZone] == 0)
                        CruiserPositions.ZoneAllocated.Remove(oldZone);
                    CruiserPositions.PositionsDictionary.Remove(item.Key);
                }

                CruiserPositions.PositionsDictionary[item.Key] = CruiserPositions.Zones[itemZone];
                CruiserPositions.ZoneAllocated[itemZone] =
                    CruiserPositions.ZoneAllocated.GetValueOrDefault(itemZone, 0) + 1;
            }
        }
    }

    internal static class CruiserZoneManager
    {
        internal static void MoveItemsToCruiser(VehicleController cruiser, string? onlyItem = null, int overrideCount = -1, string? overrideZone = null)
        {
            if (StartOfRound.Instance == null) return;
            var player = GameNetworkManager.Instance?.localPlayerController;
            if (player == null) return;
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

            CruiserPositions.ResetPlacedCounts();

            Dictionary<string, int> alreadyInCruiser = new Dictionary<string, int>();
            GrabbableObject[] cruiserItems = cruiser.GetComponentsInChildren<GrabbableObject>();
            foreach (var cruiserItem in cruiserItems)
            {
                if (cruiserItem.itemProperties.isScrap) continue;
                string n = cruiserItem.itemProperties.itemName;
                alreadyInCruiser[n] = alreadyInCruiser.GetValueOrDefault(n, 0) + 1;
            }

            Dictionary<string, int> movedCount = new Dictionary<string, int>();
            GrabbableObject[] allItems = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            int moved = 0;

            foreach (var item in allItems)
            {
                if (item.itemProperties.isScrap && !item.itemProperties.itemName.Equals("Kitchen knife") && !item.itemProperties.itemName.Equals("Shotgun")) continue;
                if (item.transform.parent == null) continue;
                if (item.transform.parent.name != "HangarShip") continue;
                if (item.isHeld || item.isPocketed) continue;

                string itemName = item.itemProperties.itemName;

                if (onlyItem != null && itemName != onlyItem) continue;

                if (!CruiserPositions.PositionsDictionary.TryGetValue(itemName, out Vector3 _)) continue;

                int maxCount;
                bool countIsMoveAmount = false;
                if (overrideCount > 0)
                {
                    maxCount = overrideCount;
                    countIsMoveAmount = true;
                }
                else if (CruiserLoadPatch.ItemCountConfig.TryGetValue(itemName, out var countEntry))
                    maxCount = countEntry.Value;
                else
                    maxCount = 1;

                int currentlyInCruiser = alreadyInCruiser.GetValueOrDefault(itemName, 0);
                int stillNeeded = countIsMoveAmount ? maxCount : maxCount - currentlyInCruiser;
                if (stillNeeded <= 0) continue;

                movedCount.TryGetValue(itemName, out int alreadyMoved);
                if (alreadyMoved >= stillNeeded) continue;

                string zoneName;
                if (overrideZone != null)
                    zoneName = overrideZone;
                else
                    zoneName = CruiserLoadPatch.ItemZoneConfig[itemName].Value;

                Vector3 targetLocalPos = CruiserPositions.GetNextZonePosition(zoneName);

                item.transform.SetParent(cruiser.transform, worldPositionStays: true);
                item.transform.localPosition = targetLocalPos;
                item.fallTime = 1f;
                item.reachedFloorTarget = true;
                item.hasHitGround = true;
                item.targetFloorPosition = targetLocalPos;
                item.startFallingPosition = targetLocalPos;

                NetworkObject cruiserNetObj = cruiser.GetComponent<NetworkObject>();
                try
                {
                    player.PlaceObjectServerRpc(item.NetworkObject, cruiserNetObj, targetLocalPos, false);
                }
                catch (Exception ex)
                {
                    CruiserLoader.Log.LogError($"Error calling RPC for {itemName}: {ex.Message}");
                }

                if (isHost)
                {
                    var rb = item.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.position = cruiser.transform.TransformPoint(targetLocalPos);
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    Physics.SyncTransforms();
                }

                movedCount[itemName] = alreadyMoved + 1;
                moved++;
            }

            if (onlyItem != null && movedCount.TryGetValue(onlyItem, out int specificCount))
            {
                if (overrideZone != null)
                {
                    HUDManager.Instance.AddTextToChatOnServer($"[CL] Moved {specificCount} {onlyItem} to {overrideZone}.", -1);
                    return;
                }
                else
                {
                    HUDManager.Instance.AddTextToChatOnServer($"[CL] Moved {specificCount} {onlyItem} to cruiser.", -1);
                    return;
                }
            }

            HUDManager.Instance.AddTextToChatOnServer($"[CL] Moved {moved} items to cruiser.", -1);
        }
    }

    public class CruiserLoaderNetworkHandler : MonoBehaviour
    {
        internal static CruiserLoaderNetworkHandler? Instance { get; private set; }
        private bool registered;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            if (registered) return;
            if (NetworkManager.Singleton == null)
            {
                CruiserLoader.Log.LogInfo("[CL] NetworkManager not ready.");
                return;
            }

            if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("CLC", OnHostCommandReceived);
                registered = true;
                CruiserLoader.Log.LogInfo("[CL] Registered CLC handler on host.");
            }
            else
            {
                CruiserLoader.Log.LogInfo("[CL] CMM unavailable or not host when enabling handler.");
            }
        }

        private void OnDisable()
        {
            if (registered && NetworkManager.Singleton != null && NetworkManager.Singleton.CustomMessagingManager != null)
            {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("CLC");
                registered = false;
                CruiserLoader.Log.LogInfo("[CL] Unregistering CLC handler.");
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private static void OnHostCommandReceived(ulong clientId, FastBufferReader reader)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                CruiserLoader.Log.LogInfo("[CL] Received CLC on non-host, ignoring.");
                return;
            }

            reader.ReadValueSafe(out string command);
            CruiserLoader.Log.LogInfo($"[CL] Received command from client {clientId}: {command}");

            PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
            if (player == null) return;

            VehicleController cruiser = UnityEngine.Object.FindObjectOfType<VehicleController>();
            if (cruiser == null)
            {
                CruiserLoader.Log.LogInfo("No cruiser found!");
                return;
            }

            ExecuteCommandOnHost(command, player, cruiser);
        }

        private static void ExecuteCommandOnHost(string chatMessage, PlayerControllerB player, VehicleController cruiser)
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