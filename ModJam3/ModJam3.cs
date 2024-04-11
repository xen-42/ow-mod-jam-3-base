using HarmonyLib;
using NewHorizons;
using NewHorizons.Utility;
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

        PingConditionHandler.Setup();

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

            // Add previews
            PlacePicture("gruh", new Vector3(-15.5f, 6.5f, 8f), new Vector3(0.92f, -0.37f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(-6.5f, 15.5f, 8f), new Vector3(0.37f, -0.92f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(6.5f, 15.5f, 8f), new Vector3(-0.37f, -0.92f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(15.5f, 6.5f, 8f), new Vector3(-0.92f, -0.37f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(15.5f, -6.5f, 8f), new Vector3(-0.92f, 0.37f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(6.5f, -15.5f, 8f), new Vector3(-0.37f, 0.92f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(-15.5f, -6.5f, 8f), new Vector3(0.92f, 0.37f, 0f), starship.transform);

            // Winner previews
            PlacePicture("gruh", new Vector3(-25.32f, 6.49f, 1f), new Vector3(0.95f, -0.29f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(6.49f, 25.32f, 1f), new Vector3(-0.29f, -0.95f, 0f), starship.transform);
            PlacePicture("gruh", new Vector3(25.32f, -6.49f, 1f), new Vector3(-0.95f, 0.29f, 0f), starship.transform);

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
            material.name.Contains("Structure_NOM_SandStone_Dark_mat") ||
            material.name.Contains("ObservatoryInterior_HEA_VillagePlanks_mat")
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
            material.name.Contains("Structure_NOM_CopperOld_Dark_mat") ||
            material.name.Contains("ObservatoryInterior_HEA_VillageMetal_mat")
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
        else if (material.name.Contains("Props_HEA_Lightbulb_mat"))
        {
            material.SetColor("_EmissionColor", new Color(0.6f, 0.7f, 0.8f));
        }

        return material;
    }

    private void PlacePicture(string name, Vector3 position, Vector3 normal, Transform parent)
    {
        var texture = ImageUtilities.GetTexture(this, $"planets/assets/previews/{name}.png", linear: true);

        var gameObject = new GameObject(name);
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = position;
        // Rotations are really weird hence the sign thing
        gameObject.transform.localRotation = Quaternion.LookRotation(normal, parent.up) * Quaternion.Euler(180f, 0f, position.x > 0 ? -90f : 90f);

        var renderer = GameObject.CreatePrimitive(PrimitiveType.Quad).GetComponent<MeshRenderer>();
        renderer.transform.parent = gameObject.transform;
        renderer.material.mainTexture = texture;
        renderer.transform.localScale = new Vector3(5, 2, 5);
        renderer.transform.localPosition = new Vector3(0, 0, -0.051f);
        renderer.transform.localRotation = Quaternion.identity;

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshRenderer>();
        box.material = silver;
        box.transform.localScale = new Vector3(5.1f, 2.1f, 0.1f);
        box.transform.parent = gameObject.transform;
        box.transform.localPosition = Vector3.zero;
        box.transform.localRotation = Quaternion.identity;

        var light = new GameObject("Light").AddComponent<Light>();
        light.range = 5f;
        light.intensity = 1f;
        light.spotAngle = 70f;
        light.type = LightType.Spot;
        light.transform.parent = gameObject.transform;
        light.transform.localPosition = new Vector3(0f, 2.5f, -3f);
        light.transform.localRotation = Quaternion.Euler(45, 0, 0);

        // Plaque
        var plaquePath = "TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/MapSatelliteExhibit/MapSatelliteExhibit_Pivot/MapSatelliteExhibit_DLC/Prefab_HEA_MuseumPlaque";
        var plaque = _newHorizons.SpawnObject(this, parent.gameObject, parent.GetComponentInChildren<Sector>(), plaquePath, Vector3.zero, Vector3.zero, 1f, false);
        GameObject.Destroy(plaque.FindChild("InteractVolume"));
        GameObject.Destroy(plaque.FindChild("AttentionPoint_SatellitePlaque_Orbit"));
        GameObject.Destroy(plaque.FindChild("AttentionPoint_SatellitePlaque_Antenna"));
        plaque.transform.parent = gameObject.transform;
        plaque.transform.localPosition = new Vector3(-1.8f, -2f, -1.4f);
        plaque.transform.localRotation = Quaternion.Euler(0f, 145f, 0f);

        var (dialogue, _) = _newHorizons.SpawnDialogue(this, parent.gameObject, $"planets/text/previews/{name}.xml");
        dialogue.transform.parent = plaque.transform;
        dialogue.transform.localPosition = new Vector3(-0.0057f, 1.2161f, -0.168f);
        dialogue.transform.localRotation = Quaternion.identity;
        dialogue._attentionPoint = gameObject.transform;
    }
}