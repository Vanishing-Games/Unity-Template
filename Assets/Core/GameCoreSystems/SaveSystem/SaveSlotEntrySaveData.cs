namespace Core
{
    [System.Serializable]
    public class SaveSlotEntrySaveData
    {
        public string SavePointName;
        public string ChapterId;
        public string LevelId;
    }

    public static class SaveKeys
    {
        public const string SaveSlotEntry = "SaveSlotEntry";
        public const string LevelObjectStates = "LevelObjectStates";
        public const string PlayerSnake = "PlayerSnake";
    }
}
