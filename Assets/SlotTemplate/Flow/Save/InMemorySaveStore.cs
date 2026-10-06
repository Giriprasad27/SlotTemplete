namespace SlotTemplate.Flow.Save
{
    /// <summary>Keeps the save in memory. For tests, and for a "guest" mode that should not persist.</summary>
    public sealed class InMemorySaveStore : ISaveStore
    {
        private SaveData _data;

        public int SaveCount { get; private set; }

        public SaveData Load() => _data?.Clone();

        public void Save(SaveData data)
        {
            _data = data.Clone();
            SaveCount++;
        }
    }
}
