using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotTemplate.Presentation.Skin
{
    /// <summary>A game's look for each symbol id. Swap skins to reskin a game without touching its math.</summary>
    [CreateAssetMenu(fileName = "SymbolSkin", menuName = "Slot Template/Symbol Skin", order = 2)]
    public sealed class SymbolSkin : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string symbolId;
            public Mesh mesh;
            public Material material;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public Entry Find(string symbolId)
        {
            foreach (var entry in entries)
            {
                if (entry.symbolId == symbolId) return entry;
            }
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetup(List<Entry> newEntries) => entries = newEntries;
#endif
    }
}
