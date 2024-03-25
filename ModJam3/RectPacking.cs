using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModJam3;

internal static class RectPacking
{
    //https://observablehq.com/@mourner/simple-rectangle-packing
    public static Rect[] Apply(Rect[] rects)
    {
        var area = 0f;
        var maxWidth = 0f;
        foreach (var rect in rects)
        {
            area += rect.width * rect.height;
            maxWidth = Mathf.Max(maxWidth, rect.width);
            DebugLog($"{rect.x} {rect.y} {rect.width} {rect.height}");
        }

        // Sort by height, descending
        rects = rects.OrderByDescending(rect => rect.height).ToArray();

        var squareWidth = Mathf.Ceil(Mathf.Sqrt(area / 0.95f));
        var startWidth = Mathf.Max(squareWidth, maxWidth);

        DebugLog($"Square width {squareWidth}, max width: {maxWidth}");

        var spaces = new List<Rect>() { new Rect(0, 0, startWidth, float.MaxValue) };
        var packed = new List<Rect>();

        foreach (var rect in rects)
        {
            for (int i = spaces.Count() - 1; i >= 0; i--)
            {
                var space = spaces[i];

                if (rect.width > space.width || rect.height > space.height)
                {
                    DebugLog($"Space {space} is too small for {rect}, skipping");
                    continue;
                }

                // Add the rect to this space
                var packedRect = new Rect(space.x, space.y, rect.width, rect.height);
                packed.Add(packedRect);

                // It fit perfectly
                if (rect.width == space.width && rect.height == space.height)
                {
                    DebugLog($"Fit perfectly into space");

                    // I do not understand this part at all.
                    var last = spaces.Last();
                    spaces.RemoveAt(i);
                    if (i < spaces.Count()) spaces[i] = last;
                }
                // Fit height perfectly
                else if (rect.height == space.height)
                {
                    DebugLog($"Fit perfectly into space height");

                    var newSpace = new Rect(space.x + rect.width, space.y, space.width - rect.width, space.height);
                    spaces[i] = newSpace;
                }
                // Fit width perfectly
                else if (rect.width == space.width)
                {
                    DebugLog($"Fit perfectly into space width");

                    var newSpace = new Rect(space.x, space.y + rect.height, space.width, space.height - rect.height);
                    spaces[i] = newSpace;
                }
                // Box splits the space into two
                else
                {
                    DebugLog($"Split space into two");

                    spaces.Add(new Rect(space.x + rect.width, space.y, space.width - rect.width, rect.height));
                    var newSpace = new Rect(space.x, space.y + rect.height, space.width, space.height - rect.height);
                    spaces[i] = newSpace;

                    DebugLog(newSpace.ToString());
                }

                break;
            }
        }

        foreach (var rect in packed)
        {
            DebugLog($"Packed - {rect.x} {rect.y} {rect.width} {rect.height}");
        }

        return packed.ToArray();
    }

    private static void DebugLog(string msg)
    {
#if DEBUG
        ModJam3.Instance.ModHelper.Console.WriteLine($"RectPacking: {msg}");
#endif
    }
}
