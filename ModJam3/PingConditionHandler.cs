namespace ModJam3;

internal static class PingConditionHandler
{
    // Since Dialogue ship log required conditions default to "true" if the log doesn't exist (mod isn't loaded) we have to make custom conditions and manually check the save file
    private static (string shipLog, string condition)[] _optionalShipLogToCondition = new (string, string)[] 
    {
        ("CALLISTHESIS_SHIPLOG_THESIS_DOWNLOADED", "CallisModComplete"),
        ("PARTY_EXISTENCE_FACT_CCL", "JamHubComplete"),
        ("EH_PHOSPHORS_X3", "EchoHikeComplete"),
        ("HN2_Device3", "MagistariumComplete"),
        ("RANGER_EGGSTAR_VICTORY", "SolarRangersComplete"),
        ("AXIOM_ESCAPE_POD_X2", "AxiomComplete"),
        ("TeamErnesto_10", "ReflectionsComplete"),
        ("BIRD_MAKE_A_SHIP_LOG_PLEASE", "SymbiosisComplete"),
        ("finis_king_text", "FinisComplete"),
        ("BT_KEY_COMPLETE", "BandTogetherComplete")
    };

    public static void Setup()
    {
        GlobalMessenger.AddListener("ExitConversation", OnExitConversation);
        GlobalMessenger.AddListener("EnterConversation", OnEnterConversation);
    }

    private static void OnEnterConversation()
    {
        foreach (var pair in _optionalShipLogToCondition)
        {
            var logRevealed = (PlayerData.GetShipLogFactSave(pair.shipLog)?.revealOrder ?? -1) > -1;
            DialogueConditionManager.SharedInstance.SetConditionState(pair.condition, logRevealed);
        }

        var spokeToNomai = "SpokeToStarshipCommunityNomaiEver";
        DialogueConditionManager.SharedInstance.SetConditionState(spokeToNomai, PlayerData.GetPersistentCondition(spokeToNomai));
    }

    private static void OnExitConversation()
    {
        if (DialogueConditionManager.SharedInstance.GetConditionState("PingStartMeditation"))
        {
            Locator.GetDeathManager().KillPlayer(DeathType.Meditation);
            PlayerData.SetPersistentCondition("KNOWS_MEDITATION", true);
        }
    }
}
