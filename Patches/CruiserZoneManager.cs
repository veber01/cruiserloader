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
        { "Ammo",            "C3" },
        { "Weed killer",     "D1" },
        { "Key",             "E2" },
        { "Extension ladder","C2" },
        { "Lockpicker",      "G2" },
        { "TZP-Inhalant",    "F3" },
    };

            foreach (Item item in allItemsList)
            {
                if (item.isScrap && !item.itemName.Equals("Kitchen knife") && !item.itemName.Equals("Shotgun")) continue;
                if (item.itemName == "Binoculars" || item.itemName == "box" || item.itemName == "Mapper") continue;

                string defaultZone = defaultZones.GetValueOrDefault(item.itemName, "");

                ItemZoneConfig[item.itemName] = CruiserLoader.ItemZoneConfig.Bind(
                    "Items",
                    item.itemName,
                    defaultZone,
                    $"Set cruiser zone for {item.itemName}. Leave empty to not move. (e.g. A1, B2, C3)"
                );

                ItemCountConfig[item.itemName] = CruiserLoader.ItemZoneConfig.Bind(
                    "ItemCounts",
                    item.itemName,
                    1,
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
        internal static void MoveItemsToCruiser(VehicleController cruiser, string? onlyItem = null, int overrideCount = -1)
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
                if (overrideCount > 0)
                    maxCount = overrideCount;
                else if (CruiserLoadPatch.ItemCountConfig.TryGetValue(itemName, out var countEntry))
                    maxCount = countEntry.Value;
                else
                    maxCount = 1;

                int currentlyInCruiser = alreadyInCruiser.GetValueOrDefault(itemName, 0);
                int stillNeeded = maxCount - currentlyInCruiser;
                if (stillNeeded <= 0) continue;

                movedCount.TryGetValue(itemName, out int alreadyMoved);
                if (alreadyMoved >= stillNeeded) continue;

                string zoneName = CruiserLoadPatch.ItemZoneConfig[itemName].Value;
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

            CruiserLoader.Log.LogInfo($"Moved {moved} items to cruiser.");
        }
    }

}