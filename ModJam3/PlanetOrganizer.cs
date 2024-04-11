using NewHorizons.External;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModJam3;

internal static class PlanetOrganizer
{
    public const float STARTING_ORBIT = 5500f;
    public const float ORBIT_SPACING = 1750f;

    public const float STATIC_BODY_RADIUS = 7000f;

    public const float BRAMBLE_PLANE_DISTANCE = 20000f;

    public static void Apply(IEnumerable<NewHorizonsBody> bodies)
    {
        // Only allow one spawn point for now ig and let them override the sun one immediately
        // Only include spawns that set both because idk why don't you want a ship bro?
        var bodiesWithSpawns = bodies.Where(x => x.Config.Spawn?.playerSpawn != null && x.Config.Spawn?.shipSpawn != null);
        if (bodiesWithSpawns.Count() > 1)
        {
            var foundSpawnFlag = false;
            foreach (var body in bodiesWithSpawns)
            {
                var isDefaultSpawn = body.Config.name == "Starship Community";
                var isValidSpawn = (isDefaultSpawn && !ModJam3.Instance.AllowSpawnOverride) || (!isDefaultSpawn && ModJam3.Instance.AllowSpawnOverride);
                var keepSpawn = isValidSpawn && !foundSpawnFlag;
                if (keepSpawn)
                {
                    foundSpawnFlag = true;
                }
                else
                {
                    body.Config.Spawn = null;
                }
            }
        }

        foreach (var body in bodies)
        {
            var hadShipLog = body.Config.ShipLog?.mapMode != null;

            // Force all bodies to have shiplogs so we can remove them if need be
            body.Config.ShipLog ??= new();
            body.Config.ShipLog.mapMode ??= new();

            // Force all planets to be automatic placement
            var mapMode = body.Config.ShipLog.mapMode;

            mapMode.manualPosition = null;
            mapMode.manualNavigationPosition = null;

            // If it's a moon with no ship logs, just get rid of it
            var isMoon = !string.IsNullOrEmpty(body.Config.Orbit.primaryBody) && body.Config.Orbit.primaryBody != "Jam 3 Sun";
            var shouldRemove = isMoon && !hadShipLog;
            if (shouldRemove)
            {
                mapMode.remove = true;
            }
            else if (isMoon)
            {
                mapMode.scale = 0.5f;
            }
        }

        var staticBodies = bodies.Where(x => (x.Config.Orbit.isStatic || x.Config.Orbit.staticPosition != null) && x.Config.Bramble?.dimension == null && !x.Config.Base.centerOfSolarSystem);
        var brambleDimensions = bodies.Where(x => x.Config.Bramble?.dimension != null);
        var regularPlanets = bodies.Where(x => (!x.Config.Orbit.isStatic && x.Config.Orbit.staticPosition == null) && x.Config.Bramble?.dimension == null);

        HandleStaticBodies(staticBodies);
        HandleBrambleDimensions(brambleDimensions);
        HandleRegularPlanets(regularPlanets);
    }

    private static bool GetBool(string name, JObject extras)
    {
        return extras.GetValue(name)?.ToObject<bool>() is bool result && result;
    }

    private static int GetOrder(NewHorizonsBody body)
    {
        if (body.Config.extras is JObject extras)
        {
            try
            {
                if (GetBool("farFromSun", extras))
                {
                    return 1;
                }
                if (GetBool("closeToSun", extras))
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                ModJam3.Instance.ModHelper.Console.WriteLine($"Failed to evaluate planet order? {e}");
            }
        }
        return 0;
    }

    private static void HandleRegularPlanets(IEnumerable<NewHorizonsBody> regularPlanets)
    {
        ModJam3.Instance.ModHelper.Console.WriteLine($"Handling {regularPlanets.Count()} regular planets");

        // So it actually starts at the starting orbit since it always adds the spacing
        var lastSemiMajorAxis = STARTING_ORBIT - ORBIT_SPACING;

        // Handle order
        var orderedPlanets = regularPlanets.OrderBy(GetOrder);

        // Use constant seed so orientations are the same each time
        UnityEngine.Random.InitState(3);

        foreach (var body in orderedPlanets)
        {
            // Space out the orbits to prevent overlap
            var orbit = body.Config.Orbit;
            if (orbit.primaryBody?.ToLower()?.Replace(" ", "") == "jam3sun")
            {
                var planetSOI = Mathf.Max(
                    body.Config.Base.soiOverride,
                    body.Config.Atmosphere?.size ?? 0f,
                    body.Config.Base.surfaceSize * 2f,
                    body.Config.Bramble?.dimension?.radius ?? 0f);

                orbit.eccentricity = 0;
                orbit.inclination = Mathf.Clamp(orbit.inclination, -33f, 33f);

                var semiMajorAxis = lastSemiMajorAxis + ORBIT_SPACING + planetSOI * 2f;
                orbit.semiMajorAxis = semiMajorAxis;

                // If they didn't set the true anomaly, randomize it
                orbit.trueAnomaly = orbit.trueAnomaly == 0f ? UnityEngine.Random.Range(0f, 360f) : orbit.trueAnomaly;

                // Add our SOI to the spacing after us
                lastSemiMajorAxis = semiMajorAxis + planetSOI;
            }
        }
    }

    private static void HandleBrambleDimensions(IEnumerable<NewHorizonsBody> brambleDimensions)
    {
        ModJam3.Instance.ModHelper.Console.WriteLine($"Handling {brambleDimensions.Count()} hidden dimensions");

        var brambleDimensionRects = new List<Rect>();

        foreach (var body in brambleDimensions)
        {
            // Take radius with padding
            // Have to add a lot of padding to include the repel volume around the dimension (about 3.2x the radius)
            var radius = body.Config.Bramble.dimension.radius * 4f;
            brambleDimensionRects.Add(new Rect(-radius, -radius, radius * 2f, radius * 2f));
        }

        var packedRectPositions = RectPacking.Apply(brambleDimensionRects.ToArray());

        for (int i = 0; i < brambleDimensions.Count(); i++)
        {
            var packedRect = packedRectPositions[i];
            var body = brambleDimensions.ElementAt(i);

            var center = packedRect.center;
            body.Config.Orbit.staticPosition = new Vector3(center.x, -BRAMBLE_PLANE_DISTANCE, center.y);
        }
    }

    private static void HandleStaticBodies(IEnumerable<NewHorizonsBody> staticBodies)
    {
        ModJam3.Instance.ModHelper.Console.WriteLine($"Handling {staticBodies.Count()} static bodies");

        for (int i = 0; i < staticBodies.Count(); i++)
        {
            var body = staticBodies.ElementAt(i);
            var angle = 360f * (i / (float)staticBodies.Count());

            // Static bodies will appear in a ring around the sun
            body.Config.Orbit.staticPosition = Quaternion.AngleAxis(angle, Vector3.up) * (new Vector3(1f, 1f, 0f).normalized * STATIC_BODY_RADIUS);
        }
    }
}
