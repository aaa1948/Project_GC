namespace Vampire
{
    public static class CrossSceneData
    {
        public static CharacterBlueprint CharacterBlueprint { get; set; }

        public static MerchantItemBlueprint[] StartingLobbyItems { get; set; } = new MerchantItemBlueprint[0];

        public static void ClearStartingLobbyItems()
        {
            StartingLobbyItems = new MerchantItemBlueprint[0];
        }
    }
}