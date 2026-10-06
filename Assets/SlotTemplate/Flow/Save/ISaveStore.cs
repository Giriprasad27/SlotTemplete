namespace SlotTemplate.Flow.Save
{
    /// <summary>Persists <see cref="SaveData"/>. <see cref="Save"/> must be durable before it returns.</summary>
    public interface ISaveStore
    {
        /// <summary>Returns the saved data, or null when nothing has been saved yet.</summary>
        SaveData Load();

        void Save(SaveData data);
    }
}
