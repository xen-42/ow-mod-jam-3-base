using NewHorizons;
using OWML.ModHelper;
using System.Linq;
using UnityEngine;

namespace ModJam3;

public class ModJam3 : ModBehaviour
{
	public static string SystemName = "Jam3";
	private INewHorizons _newHorizons;

	public void Start()
	{
		// Get the New Horizons API and load configs
		_newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
		_newHorizons.LoadConfigs(this);

		_newHorizons.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);

		// Wait til next frame so all dependants have run Start
		ModHelper.Events.Unity.FireOnNextUpdate(FixCompatIssues);
	}

	private void FixCompatIssues()
	{
		var jamEntries = _newHorizons.GetInstalledAddons()
			.Select(ModHelper.Interaction.TryGetMod)
			.Where(addon => addon.GetDependencies().Select(x => x.ModHelper.Manifest.UniqueName).Contains(ModHelper.Manifest.UniqueName))
			.ToArray();

		ModHelper.Console.WriteLine($"Found {jamEntries.Length} jam entries");

		var lastSemiMajorAxis = 3000f;
		var orbitSpacing = 500f;

		foreach (var body in Main.BodyDict[SystemName])
		{
			// Force all planets to be automatic placement
			var mapMode = body.Config.ShipLog?.mapMode;
			if (mapMode != null)
			{
				mapMode.manualPosition = null;
				mapMode.manualNavigationPosition = null;
			}

			// Space out the orbits to prevent overlap
			var orbit = body.Config.Orbit;
			if (orbit.primaryBody?.ToLower()?.Replace(" ", "") == "jam3sun")
			{
				if (orbit.isStatic || orbit.staticPosition != null)
				{
					// TODO: Handle this later as mods come out and we can figure out what to do with them
					// Maybe nobody will even make a statically positioned planet
				}
				else
				{
					orbit.eccentricity = 0;
					orbit.inclination = 0;

					var planetSOI = Mathf.Max(body.Config.Base.soiOverride, body.Config.Atmosphere?.size ?? 0f, body.Config.Base.surfaceSize * 2f);

					var semiMajorAxis = lastSemiMajorAxis + orbitSpacing + planetSOI;
					orbit.semiMajorAxis = semiMajorAxis;
					// Add our SOI to the spacing after us
					lastSemiMajorAxis = semiMajorAxis + planetSOI;
				}
			}

		}

		// Make sure all ship log entries don't overlap
		ShipLogPacking.Pack(jamEntries);

		ModHelper.Console.WriteLine($"Finished packing jam entry ship logs");
	}


	private void OnStarSystemLoaded(string name)
	{
		if (name == SystemName)
		{
			// Do stuff potentially
		}
	}
}