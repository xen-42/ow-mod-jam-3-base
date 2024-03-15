using NewHorizons;
using NewHorizons.External.Configs;
using NewHorizons.External.Modules;
using OWML.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ModJam3;

public static class ShipLogPacking
{
	public static void Pack(IModBehaviour[] jamEntries)
	{
		var jamShipLogEntries = new Dictionary<IModBehaviour, ShipLogModule.EntryPositionInfo[]>();
		var jamShipLogRects = new List<(IModBehaviour, Rect)>();

		// Collect all ship log entries from each mod
		foreach (var jamEntry in jamEntries)
		{
			var starSystem = jamEntry.ModHelper.Storage.Load<StarSystemConfig>(Path.Combine("systems", $"{ModJam3.SystemName}.json"));

			if (starSystem.entryPositions == null || starSystem.entryPositions.Length == 0)
			{
				continue;
			}

			jamShipLogEntries[jamEntry] = starSystem.entryPositions;

			var xPositions = starSystem.entryPositions.Select(x => x.position.x);
			var yPositions = starSystem.entryPositions.Select(x => x.position.y);

			// Add a slight margin
			// Ship log cards are kinda big and the rects dont account for their actual sizes
			var margin = 100;
			var xMax = xPositions.Max() + margin;
			var xMin = xPositions.Min() - margin;
			var yMax = yPositions.Max() + margin;
			var yMin = yPositions.Min() - margin;

			jamShipLogRects.Add((jamEntry, new Rect(xMin, yMin, xMax - xMin, yMax - yMin)));
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
		var packedRectPositions = ShipLogPacking.PackRects(jamShipLogRects.Select(x => x.Item2).ToArray());
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

	//https://observablehq.com/@mourner/simple-rectangle-packing
	public static Rect[] PackRects(Rect[] rects)
	{
		var area = 0f;
		var maxWidth = 0f;
		foreach (var rect in rects)
		{
			area += rect.width * rect.height;
			maxWidth = Mathf.Max(maxWidth, rect.width);
		}

		// Sort by height, descending
		rects.OrderByDescending(rect => rect.height);

		var startWidth = Mathf.Max(Mathf.Ceil(Mathf.Sqrt(area / 0.95f)), maxWidth);

		var spaces = new List<Rect>() { new Rect(0, 0, startWidth, float.MaxValue) };
		var packed = new List<Rect>();

		foreach (var rect in rects)
		{
			for (int i = spaces.Count() - 1; i >= 0; i--)
			{
				var space = spaces[i];

				if (rect.width > space.width || rect.height > space.height) continue;

				// Add the rect to this space
				var packedRect = new Rect(space.x, space.y, rect.width, rect.height);
				packed.Add(packedRect);

				// It fit perfectly
				if (rect.width == space.width && rect.height == space.height)
				{
					// I do not understand this part at all.
					var last = spaces.Last();
					spaces.RemoveAt(i);
					if (i < spaces.Count()) spaces[i] = last;
				}
				// Fit height perfectly
				else if (rect.height == space.height)
				{
					var newSpace = new Rect(space.x + rect.width, space.y, space.width - rect.width, space.height);
					spaces[i] = newSpace;
				}
				// Fit width perfectly
				else if (rect.width == space.width)
				{
					var newSpace = new Rect(space.x, space.y + rect.height, space.width, space.height - rect.height);
					spaces[i] = newSpace;
				}
				// Box splits the space into two
				else
				{
					spaces.Add(new Rect(space.x + rect.width, space.y, space.width - rect.width, rect.height));
					var newSpace = new Rect(space.x, space.y + rect.height, space.width, space.height - rect.height);
					spaces[i] = newSpace;
				}

				break;
			}
		}

		return packed.ToArray();
	}
}
