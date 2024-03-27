using HarmonyLib;
using NewHorizons;
using NewHorizons.Utility.Files;
using OWML.Common;
using OWML.ModHelper;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ModJam3;

public class ModJam3 : ModBehaviour
{
    public static string SystemName = "Jam3";
    private INewHorizons _newHorizons;

    public static ModJam3 Instance { get; private set; }

    public bool AllowSpawnOverride { get; private set; }

    public void Start()
    {
        Instance = this;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        // Get the New Horizons API and load configs
        _newHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
        _newHorizons.LoadConfigs(this);

        _newHorizons.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);

        nomaiSuit = ModHelper.Assets.GetTexture("planets/assets/Character_NOM_Nomai_v2_d 1.png");
        ember = ModHelper.Assets.GetTexture("planets/assets/Props_HEA_CampfireEmbers_d.png");
        emberEmission = ModHelper.Assets.GetTexture("planets/assets/Props_HEA_CampfireEmbers_e.png");
        ash = ModHelper.Assets.GetTexture("planets/assets/Props_HEA_CampfireAsh_e.png");

        // Wait til next frame so all dependants have run Start
        ModHelper.Events.Unity.FireOnNextUpdate(FixCompatIssues);
    }

    public override void Configure(IModConfig config)
    {
        base.Configure(config);

        AllowSpawnOverride = config.GetSettingsValue<bool>("allowSpawnOverride");
    }

    public void FixCompatIssues()
    {
        var jamEntries = _newHorizons.GetInstalledAddons()
            .Select(ModHelper.Interaction.TryGetMod)
            .Where(addon => addon.GetDependencies().Select(x => x.ModHelper.Manifest.UniqueName).Contains(ModHelper.Manifest.UniqueName))
            .Append(this)
            .ToArray();

        ModHelper.Console.WriteLine($"Found {jamEntries.Length} jam entries");

        // Make sure orbits don't overlap
        PlanetOrganizer.Apply(Main.BodyDict[SystemName]);

        // Make sure all ship log entries don't overlap
        ShipLogPacking.Apply(jamEntries);

        // Make sure that the root mod for the system remains us
        Main.SystemDict[SystemName].Mod = this;

        ModHelper.Console.WriteLine($"Finished packing jam entry ship logs");
    }

    public Material porcelain, silver, black;
    public Texture2D nomaiSuit, ember, emberEmission, ash;

    private void OnStarSystemLoaded(string name)
    {
        if (name == SystemName)
        {
            porcelain = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.Contains("Structure_NOM_PorcelainClean_mat"));
            silver = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.Contains("Structure_NOM_Silver_mat"));
            black = Resources.FindObjectsOfTypeAll<Material>().First(x => x.name.Contains("Structure_NOM_SilverPorcelain_mat"));

            var starship = _newHorizons.GetPlanet("Starship Community");

            // Replace materials on the starship community
            foreach (var renderer in starship.GetComponentsInChildren<Renderer>())
            {
                renderer.materials = renderer.materials.Select(GetReplacementMaterial).ToArray();
            }

            // Replace campfires
            foreach (var campfire in starship.GetComponentsInChildren<Campfire>())
            {
                var emberMaterial = campfire.transform.parent.Find("Props_HEA_Campfire/Campfire_Embers").GetComponent<MeshRenderer>().material;
                emberMaterial.SetTexture("_MainTex", ember);
                emberMaterial.SetTexture("_EmissionMap", emberEmission);

                var ashMaterial = campfire.transform.parent.Find("Props_HEA_Campfire/Campfire_Ash").GetComponent<MeshRenderer>().material;
                ashMaterial.SetTexture("_EmissionMap", ash);

                foreach (var light in campfire._lightController.lights)
                {
                    light.gameObject.GetComponent<Light>().color = new Color(0f, 0f, 0f);
                }
                
                campfire._flames.material.color = new Color(0f, 0.2f, 1f);
            }
        }
    }

    private Material GetReplacementMaterial(Material material)
    {
        if (material.name.Contains("Structure_NOM_Whiteboard_mat") ||
            material.name.Contains("Structure_NOM_SandStone_mat") ||
            material.name.Contains("Structure_NOM_SandStone_Dark_mat")
            )
        {
            return porcelain;
        }
        else if (material.name.Contains("Structure_NOM_PropTile_Color_mat") ||
            material.name.Contains("Structure_NOM_SandStone_Darker_mat")
            )
        {
            return black;
        }
        else if (material.name.Contains("Structure_NOM_CopperOld_mat") ||
            material.name.Contains("Structure_NOM_TrimPattern_mat") ||
            material.name.Contains("Structure_NOM_CopperOld_Dark_mat")
            )
        {
            return silver;
        }
        else if (material.name.Contains("Props_NOM_Scroll_mat") ||
            material.name.Contains("Props_NOM_Mask_Trim_mat")
            )
        {
            material.color = new Color(0.05f, 0.05f, 0.05f);
        }
        else if (material.name.Contains("Character_NOM_Nomai_v2_mat"))
        {
            material.mainTexture = nomaiSuit;
        }

        return material;
    }
}