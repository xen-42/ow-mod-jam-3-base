using NewHorizons;
using NewHorizons.External.Configs;
using NewHorizons.External.Modules;
using OWML.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ModJam3;

internal static class ShipLogPacking
{
    public static void Apply(IModBehaviour[] jamEntries)
    {
        var jamShipLogEntries = new Dictionary<IModBehaviour, ShipLogModule.EntryPositionInfo[]>();
        var jamShipLogRects = new List<(IModBehaviour, Rect)>();

        // Add a slight margin
        // Ship log cards are kinda big and the rects dont account for their actual sizes
        var margin = 150;

        // Collect all ship log entries from each mod
        foreach (var jamEntry in jamEntries)
        {
            try
            {
                var starSystem = jamEntry.ModHelper.Storage.Load<StarSystemConfig>(Path.Combine("systems", $"{ModJam3.SystemName}.json"));

                if (starSystem.entryPositions == null || starSystem.entryPositions.Length == 0)
                {
                    continue;
                }

                jamShipLogEntries[jamEntry] = starSystem.entryPositions;

                var xPositions = starSystem.entryPositions.Select(x => x.position.x);
                var yPositions = starSystem.entryPositions.Select(x => x.position.y);

                var xMax = xPositions.Max() + margin;
                var xMin = xPositions.Min() - margin;
                var yMax = yPositions.Max() + margin;
                var yMin = yPositions.Min() - margin;

                jamShipLogRects.Add((jamEntry, new Rect(xMin, yMin, xMax - xMin, yMax - yMin)));
            }
            catch { }
        }

        // Some mods might not have ship logs
        var numValidMods = jamShipLogEntries.Keys.Count;

        // Might not have to do anything at all
        if (numValidMods <= 1)
        {
            return;
        }

        // Need to alter the values NH is actually using
        var finalEntryPositions = Main.SystemDict[ModJam3.SystemName].Config.entryPositions;
        var finalEntryLookup = finalEntryPositions.ToDictionary(x => x.id, x => x);

        // Each rect holds all of an addons ship log entires, optimally packed
        var packedRectPositions = RectPacking.Apply(jamShipLogRects.Select(x => x.Item2).ToArray());
        for (int i = 0; i < numValidMods; i++)
        {
            // We adjust the positions of all ship log entires to be in the new packed rectangles
            var (mod, originalRect) = jamShipLogRects[i];
            var packedRect = packedRectPositions[i];
            var offset = packedRect.position - originalRect.position;

            foreach (var shipLogEntry in jamShipLogEntries[mod])
            {
                var shipLogID = shipLogEntry.id;
                var finalShipLogEntry = finalEntryLookup[shipLogID];
                finalShipLogEntry.position += offset;
            }
        }
    }
}
