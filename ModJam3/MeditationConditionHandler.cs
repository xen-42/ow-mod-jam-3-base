namespace ModJam3;

internal static class MeditationConditionHandler
{
    public static void Setup()
    {
        GlobalMessenger.AddListener("ExitConversation", OnExitConversation);
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
