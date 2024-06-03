using HarmonyLib;
using NewHorizons;
using NewHorizons.Utility;
using NewHorizons.Utility.Files;
using NewHorizons.Utility.OWML;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.IO;
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

    private GameObject _starship;

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

            _starship = _newHorizons.GetPlanet("Starship Community");

            // Add previews
            PlacePicture("EchoHike", new Vector3(-15.5f, 6.5f, 8f), new Vector3(0.92f, -0.37f, 0f));
            PlacePicture("Axiom", new Vector3(-6.5f, 15.5f, 8f), new Vector3(0.37f, -0.92f, 0f));
            PlacePicture("Callis", new Vector3(6.5f, 15.5f, 8f), new Vector3(-0.37f, -0.92f, 0f));
            PlacePicture("Finis", new Vector3(15.5f, 6.5f, 8f), new Vector3(-0.92f, -0.37f, 0f));
            PlacePicture("JamHub", new Vector3(15.5f, -6.5f, 8f), new Vector3(-0.92f, 0.37f, 0f));
            PlacePicture("Symbiosis", new Vector3(6.5f, -15.5f, 8f), new Vector3(-0.37f, 0.92f, 0f));
            PlacePicture("BandTogether", new Vector3(-15.5f, -6.5f, 8f), new Vector3(0.92f, 0.37f, 0f));

            // Winner previews
            PlacePicture("SolarRangers", new Vector3(-25f, 6.8f, 1f), new Vector3(0.75f, -0.15f, 0f));
            PlacePicture("Reflections", new Vector3(6.8f, 25f, 1f), new Vector3(-0.15f, -0.75f, 0f));
            PlacePicture("Magistarium", new Vector3(25f, -6.8f, 1f), new Vector3(-0.75f, 0.15f, 0f));

            try
            {
                SpawnCompletionItems();
            }
            catch { }

            // Replace materials on the starship community
            foreach (var renderer in _starship.GetComponentsInChildren<Renderer>())
            {
                renderer.materials = renderer.materials.Select(GetReplacementMaterial).ToArray();
            }

            // Replace campfires
            foreach (var campfire in _starship.GetComponentsInChildren<Campfire>())
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

            // Check for Symbiosis ship logs addon
            var symbiosisShipLogsEnabled = ModHelper.Interaction.ModExists("GameWyrm.SymbiosisShipLogs");
            ModHelper.Console.WriteLine($"Has symbiosis ship log addon?: {symbiosisShipLogsEnabled}");
            ModHelper.Events.Unity.FireOnNextUpdate(() => DialogueConditionManager.SharedInstance.SetConditionState("SymbiosisShipLogsInstalled", symbiosisShipLogsEnabled));
        }
    }

    private void SpawnCompletionItems()
    {
        // ECHO HIKE
        // Trifid.TrifidJam3
        if(IsModComplete("Trifid.TrifidJam3"))
        {
            try
            {
                SpawnObject(_starship, "EchoHike_Body/Sector/PlanetInterior/EntranceRoot2/Interior/GrappleSpawn/Grapple",
                    new Vector3(-14.89f, 4.43f, 6.6f), new Vector3(315, 90, 90));
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // MAGISTARIUM
        // GameWyrm.HearthsNeighbor2
        if (IsModComplete("GameWyrm.HearthsNeighbor2"))
        {
            try
            {
                // Custom memory cube
                var memoryCube = SpawnObject(_starship, "MAGISTARIUM_Body/Sector/Magistration/Sectors/DockingBay/EntranceCube",
                    new Vector3(21f, 3.5f, 0f), new Vector3(30, 270, 270));
                memoryCube.name = "PingMemoryCube";
                _newHorizons.CreateDialogueFromXML(null, File.ReadAllText(Path.Combine(ModHelper.Manifest.ModFolderPath, "planets/text/NomaiMemoryCube.xml")),
                    "{ pathToExistingDialogue: \"Sector/PingMemoryCube/Dialogue\" }", _starship);

                // Disco ball
                var discoBall = SpawnObject(_starship, "MAGISTARIUM_Body/Sector/Magistration/Sectors/Library/Props/DiscoBall",
                    new Vector3(19.3f, -7.3f, -1.1f), new Vector3(330, 270, 270));
                // Disco ball is jank and needs a root object
                var discoBallRoot = new GameObject("DiscoBallRoot");
                discoBallRoot.transform.parent = discoBall.transform.parent;
                discoBallRoot.transform.localPosition = discoBall.transform.localPosition;
                discoBallRoot.transform.localRotation = discoBall.transform.localRotation;
                discoBall.transform.parent = discoBallRoot.transform;
                discoBall.transform.localRotation = Quaternion.identity;
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // REFLECTIONS -> Anchor
        // TeamErnesto.OWJam3ModProject
        if (IsModComplete("TeamErnesto.OWJam3ModProject"))
        {
            try
            {
                var anchor = SpawnObject(_starship, "ProjectionSimulation_Body/Sector/SimulationPrefab/NomaiInteriorRoot/SignalCenter/Signal3Root/TileAnchor/SignalPosition/ToyBall",
                    new Vector3(9.5f, 20f, 1f), new Vector3(15f, 270f, 270f));
                anchor.name = "Anchor";
                anchor.transform.localScale = Vector3.one * 5f;

                anchor.AddComponent<Oscillator>();
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // RANGERS -> Mini Egg star + medal
        // Hawkbar.SolarRangers
        if (IsModComplete("Hawkbar.SolarRangers"))
        {
            try
            {
                var medal = SpawnObject(_starship, "EggStar_Body/Sector/PREFAB_Medal/SolarRanger_Medal",
                    new Vector3(-23.9f, 9.9f, 0.42f), new Vector3(25f, 90f, 90f));
                medal.transform.localScale = Vector3.one * 0.4f;

                var body = SpawnObject(_starship, "EggStar_Body/Sector/Prefab_NOM_Airlock (1)",
                    new Vector3(-21, -4.6f, 0f), new Vector3(0, 180, 350));
                var cap = SpawnObject(_starship, "EggStar_Body/Sector/Airlock_Cap_Empty",
                    new Vector3(-21, -4.6f, 0f), new Vector3(20, 0, 185));

                var eggStarRoot = new GameObject("EggStarModel");
                eggStarRoot.transform.parent = body.transform.parent;
                eggStarRoot.transform.localPosition = body.transform.localPosition;
                body.transform.parent = eggStarRoot.transform;
                cap.transform.parent = eggStarRoot.transform;
                cap.transform.localScale = Vector3.one * 1.025f;
                eggStarRoot.transform.localScale = Vector3.one * 0.2f;
                var rotate = eggStarRoot.AddComponent<RotateTransform>();
                rotate._degreesPerSecond = 15f;
                rotate._localAxis = new Vector3(0, 0, 1);
                eggStarRoot.transform.localPosition = new Vector3(-21, -4.6f, 2f);

                var oscillator = eggStarRoot.AddComponent<Oscillator>();
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // CALLIS -> Copy of the thesis
        // Echatsum.CallisThesis
        if (IsModComplete("Echatsum.CallisThesis"))
        {
            // Wait a frame to not replace materials
            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                var modFolder = ModHelper.Interaction.TryGetMod("Echatsum.CallisThesis").ModHelper.Manifest.ModFolderPath;
                var text = File.ReadAllText(Path.Combine(modFolder, "text/main_text/scrolls/Scroll_Thesis.xml"));
                var scroll = _newHorizons.CreateNomaiText(text, "{ type: \"scroll\", seed: 1230000 }", _starship);
                scroll.transform.localPosition = new Vector3(5.0911f, 15.4626f, 6.1342f);
                scroll.transform.localRotation = Quaternion.Euler(30.319f, 90f, 153.435f);
            });
        }

        // FINIS -> The staff + a crystal
        // orclecle.Finis
        // For some reason the RodItem component is added programatically after some frames
        // We know it's done once a collider is added
        if (IsModComplete("orclecle.Finis"))
        {
            var rod = GameObject.Find("FinisPlateau_Body/Sector/finis_plateau/Rod");
            ModHelper.Events.Unity.RunWhen(() => rod.GetComponent<OWCollider>() != null, () =>
            {
                SpawnObject(_starship, "FinisPlateau_Body/Sector/finis_plateau/Rod",
                    new Vector3(13.8f, 7.5f, 6.7f), new Vector3(335f, 270f, 270f));
                ModHelper.Console.WriteLine("Copied Finis rod item");
            });
        }

        // AXIOM -> Nomai codex
        // MegaPiggy.Axiom
        if (IsModComplete("MegaPiggy.Axiom"))
        {
            try
            {
                // Put it on a table
                var table = SpawnObject(_starship, "BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Props_NOM_Table",
                    new Vector3(-7.3f, 13.6f, 6f), new Vector3(140f, 90f, 90f));
                var codex = SpawnObject(_starship, "Axiom_Body/Sector/IcePlanet/Interior/Observatory/Interior/Exhibits/AncientCultureExhibit/ExhibitRoot/Rosetta Stone",
                    new Vector3(-7.3f, 13.6f, 7.35f), new Vector3(80f, 90f, 90f));
                codex.transform.localScale = 100f * Vector3.one;
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // JAM HUB -> just cleric
        // coderCleric.JamHub
        if (IsModComplete("coderCleric.JamHub"))
        {
            try
            {
                SpawnObject(_starship, "ModJamHub_Body/Sector/jamplanet/modder_shack_area/moddershack/building/modders/coderCleric",
                    new Vector3(15.3f, -3.4f, 6f), new Vector3(350f, 270f, 270f));
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // SYMBIOSIS -> a talking dead anglerfish skull
        // CrypticBird.Jam3
        if (IsModComplete("CrypticBird.Jam3"))
        {
            try
            {
                GameObject SpawnAnglerfish(Vector3 pos, Vector3 euler)
                {
                    var anglerfish = SpawnObject(_starship.transform.Find("Sector/VeilLiftedRoot").gameObject,
                        "CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_SouthUnderground/Sector_FossilCave/Interactables_FossilCave/Structure_DB_AnglerfishSkeleton",
                        pos, euler);
                    return anglerfish;
                }

                var table = SpawnObject(_starship, "BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_BlackHoleForge/BlackHoleForgePivot/Props_BlackHoleForge/Props_NOM_Table",
                    new Vector3(8.5f, -13.15f, 6f), new Vector3(20f, 270f, 270f));

                // Wait a frame for the shiplog addon to fix it
                Delay.FireOnNextUpdate(() =>
                {
                    var bloom = SpawnObject(_starship, "ALTTH_Body/Sector/Flower", new Vector3(8.5f, -13.15f, 7f), new Vector3(330f, 270f, 270f));
                });

                var interiorFish = SpawnAnglerfish(new Vector3(2.6f, -15f, 6.5f), new Vector3(290f, 90f, 90f));
                interiorFish.transform.localScale = Vector3.one * 0.02f;
                var (dialogue, _) = _newHorizons.SpawnDialogue(this, interiorFish, "planets/text/Anglerfish.xml", radius: 2, range: 2);
                dialogue.transform.localScale = Vector3.one / 0.02f;

                SpawnAnglerfish(new Vector3(-84f, -15f, 6f), new Vector3(330f, 90f, 90f));
                SpawnAnglerfish(new Vector3(31f, -101f, 34f), new Vector3(320f, 170f, 6f));
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
            }
        }

        // Band together -> A shrubbery ig
        // pikpik_carrot.BandTogether
        if (IsModComplete("pikpik_carrot.BandTogether"))
        {
            try
            {
                var shrub = SpawnObject(_starship, "JamPlanet(Clone)/Shrubbery",
                    new Vector3(-13.5f, -7.65f, 6.5f), new Vector3(50, 270, 270));
                Component.DestroyImmediate(shrub.GetComponent("BandTogether.Shrubbery"));
                SpawnObject(_starship, "DB_VesselDimension_Body/Sector_VesselDimension/Props_VesselDimension/Prefab_NOM_NomaiTree_VarDRY_3/GEO_Treepot_3",
                    new Vector3(-13.5f, -7.65f, 6f), new Vector3(310, 90, 90));
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine(e.ToString(), MessageType.Error);
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
            material.name.Contains("Props_NOM_Mask_Trim_mat") ||
            material.name.Contains("Structure_NOM_Airlock_mat")
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

    private void PlacePicture(string name, Vector3 position, Vector3 normal)
    {
        var texture = ImageUtilities.GetTexture(this, $"planets/assets/previews/{name}.png", linear: true);

        var parent = _starship.GetComponentInChildren<Sector>().transform;

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

    private bool IsModComplete(string modID)
    {
        var conditionName = $"{modID.Replace(".", "_")}_Complete";

        Delay.FireOnNextUpdate(() => 
            DialogueConditionManager.SharedInstance.SetConditionState(modID.Replace(".", "_") + "_NotInstalled", !ModHelper.Interaction.ModExists(modID)));

        if (ModHelper.Interaction.ModExists(modID) && PlayerData.GetPersistentCondition(conditionName))
        {
            // We update the plaques, so make sure they have the dialogue condition set now
            // Have to wait else it wont work
            // Uses a separate condition than persistent so that can get set, then only next loop does the object spawn and we get the dialogue
            // Setting a persistent condition in dialogue automatically sets a dialogue condition with the same name
            // Also was breaking until waiting a frame
            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                DialogueConditionManager.SharedInstance.SetConditionState(conditionName + "_EntryCondition", true);
            });
            return true;
        }
        return false;
    }

    private GameObject SpawnObject(GameObject root, string path, Vector3 position, Vector3 euler)
    {
        return _newHorizons.SpawnObject(this, root, root.GetComponentInChildren<Sector>(), path, position, euler, 1f, false);
    }
}