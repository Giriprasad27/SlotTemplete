using System;
using System.Collections.Generic;
using SlotTemplate.Core.Model;

namespace SlotTemplate.Core.Definition
{
    // Plain serializable data. Unity serializes these inside the SlotDefinition asset, and tests
    // and the RTP simulator can build them in code. Field names stay lowerCamel for the inspector.

    /// <summary>The math of one slot game: symbols, reel sets and paylines.</summary>
    [Serializable]
    public sealed class SlotDefinitionData
    {
        public int rows = 3;
        public List<SymbolData> symbols = new List<SymbolData>();

        /// <summary>Usually "base" plus one per feature, e.g. "free".</summary>
        public List<ReelSetData> reelSets = new List<ReelSetData>();

        public List<PaylineData> paylines = new List<PaylineData>();
    }

    [Serializable]
    public sealed class SymbolData
    {
        /// <summary>Short unique id used in reel strips, e.g. "CH" or "WILD".</summary>
        public string id;
        public SymbolKind kind = SymbolKind.Regular;

        /// <summary>
        /// Pay multiplier by match count: element 0 is 1-of-a-kind, element 4 is 5-of-a-kind.
        /// Line wins multiply the line bet; scatter wins multiply the total bet.
        /// </summary>
        public long[] pays = new long[5];

        public SymbolData() { }

        public SymbolData(string id, SymbolKind kind, params long[] pays)
        {
            this.id = id;
            this.kind = kind;
            this.pays = pays;
        }
    }

    [Serializable]
    public sealed class ReelSetData
    {
        public string id = "base";

        /// <summary>One entry per reel: comma-separated symbol ids, top to bottom, e.g. "CH,LE,BE,CH".</summary>
        public List<string> reels = new List<string>();

        public ReelSetData() { }

        public ReelSetData(string id, params string[] reels)
        {
            this.id = id;
            this.reels = new List<string>(reels);
        }
    }

    [Serializable]
    public sealed class PaylineData
    {
        /// <summary>Row on each reel, left to right. 0 is the top row.</summary>
        public int[] rows = Array.Empty<int>();

        public PaylineData() { }
        public PaylineData(params int[] rows) => this.rows = rows;
    }
}
