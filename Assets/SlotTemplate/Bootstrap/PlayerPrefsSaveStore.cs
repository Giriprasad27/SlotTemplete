using SlotTemplate.Flow.Save;
using UnityEngine;

namespace SlotTemplate.Bootstrap
{
    /// <summary>Saves to PlayerPrefs as JSON and flushes on every write, so a crash right after is safe.</summary>
    public sealed class PlayerPrefsSaveStore : ISaveStore
    {
        private readonly string _key;

        public PlayerPrefsSaveStore(string key) => _key = key;

        public SaveData Load()
        {
            if (!PlayerPrefs.HasKey(_key)) return null;

            try
            {
                return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(_key));
            }
            catch (System.ArgumentException e)
            {
                Debug.LogError($"[Slot] Save '{_key}' is unreadable and was ignored: {e.Message}");
                return null;
            }
        }

        public void Save(SaveData data)
        {
            PlayerPrefs.SetString(_key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public void Delete() => PlayerPrefs.DeleteKey(_key);
    }
}
