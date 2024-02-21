using OWML.ModHelper;
using System.Linq;

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
		//ModHelper.Events.Unity.FireOnNextUpdate(FixCompatIssues);
	}

	private void FixCompatIssues()
	{
		var jamEntries = _newHorizons.GetInstalledAddons()
			.Select(ModHelper.Interaction.TryGetMod)
			.Where(addon => addon.GetDependencies().Select(x => x.ModHelper.Manifest.UniqueName).Contains(ModHelper.Manifest.UniqueName))
			.ToArray();

		ModHelper.Console.WriteLine($"Found {jamEntries.Length} jam entries");

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