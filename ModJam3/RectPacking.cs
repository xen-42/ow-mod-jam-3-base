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
