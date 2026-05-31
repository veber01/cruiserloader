using System.Collections.Generic;
using UnityEngine;

namespace CruiserLoader.Patches
{
    internal static class CruiserPositions
    {
        internal static Dictionary<string, Vector3> Zones = new Dictionary<string, Vector3>();
        internal static Dictionary<string, Vector3> PositionsDictionary = new Dictionary<string, Vector3>();
        internal static Dictionary<string, int> ZoneAllocated = new Dictionary<string, int>();
        internal static Dictionary<string, int> ZonePlacedCount = new Dictionary<string, int>();

        internal static void CreateZones()
        {
            Zones.Clear();

            //Left side cru from door | bottom to top
            Zones["A1"] = new Vector3(-1.15f, -0.4f,  -2.6f);
            Zones["A2"] = new Vector3(-1.15f,  0.46f, -2.6f);
            Zones["A3"] = new Vector3(-1.15f,  1.22f, -2.6f);
            Zones["B1"] = new Vector3(-1.15f, -0.4f,  -1.7f);
            Zones["B2"] = new Vector3(-1.15f,  0.46f, -1.7f);
            Zones["B3"] = new Vector3(-1.15f,  1.22f, -1.7f);
            Zones["C1"] = new Vector3(-1.15f, -0.4f,  -0.8f);
            Zones["C2"] = new Vector3(-1.15f,  0.46f, -0.8f);
            Zones["C3"] = new Vector3(-1.15f,  1.22f, -0.8f);

            //Middle cru from left
            Zones["D1"] = new Vector3(-0.91f,  0f,    0.3f);
            Zones["D2"] = new Vector3( 0f,    -0.5f, -0.55f); //radarbooster
            Zones["D3"] = new Vector3( 0.82f,  0f,    0.3f);

            //Right side cru from door | bottom to top
            Zones["E1"] = new Vector3( 1.15f, -0.4f,  -2.6f);
            Zones["E2"] = new Vector3( 1.15f,  0.46f, -2.6f);
            Zones["E3"] = new Vector3( 1.15f,  1.22f, -2.6f);
            Zones["F1"] = new Vector3( 1.15f, -0.4f,  -1.7f);
            Zones["F2"] = new Vector3( 1.15f,  0.46f, -1.7f);
            Zones["F3"] = new Vector3( 1.15f,  1.22f, -1.7f);
            Zones["G1"] = new Vector3( 1.15f, -0.4f,  -0.8f);
            Zones["G2"] = new Vector3( 1.15f,  0.46f, -0.8f);
            Zones["G3"] = new Vector3( 1.15f,  1.22f, -0.8f);
        }

        private static readonly HashSet<string> ZStackZones = new HashSet<string>
        {
            "A1","A2","A3","B1","B2","B3","C1","C2","C3",
            "E1","E2","E3","F1","F2","F3","G1","G2","G3"
        };

        private static readonly HashSet<string> XStackZonesPositive = new HashSet<string> { "D1" };

        private static readonly HashSet<string> XStackZonesNegative = new HashSet<string> { "D3" };

        internal static Vector3 GetNextZonePosition(string zoneName)
        {
            Vector3 basePos = Zones[zoneName];
            int count = ZonePlacedCount.GetValueOrDefault(zoneName, 0);
            int offset = count % 5;
            ZonePlacedCount[zoneName] = count + 1;

            if (ZStackZones.Contains(zoneName))
                return new Vector3(basePos.x, basePos.y, basePos.z + (offset * 0.1f));

            if (XStackZonesPositive.Contains(zoneName))
                return new Vector3(basePos.x + (offset * 0.1f), basePos.y, basePos.z);

            if (XStackZonesNegative.Contains(zoneName))
                return new Vector3(basePos.x - (offset * 0.1f), basePos.y, basePos.z);

            return basePos;
        }

        internal static void ResetPlacedCounts()
        {
            ZonePlacedCount.Clear();
        }
    }
}