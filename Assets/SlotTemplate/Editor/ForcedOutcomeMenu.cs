using SlotTemplate.DebugTools;
using UnityEditor;
using UnityEngine;

namespace SlotTemplate.Editor
{
    /// <summary>Menu shortcuts for the in-game cheat menu while in Play mode.</summary>
    public static class ForcedOutcomeMenu
    {
        [MenuItem("Tools/Slot Template/Force Next Spin/Free Spins")]
        private static void ForceFreeSpins() => Run("Force free spins");

        [MenuItem("Tools/Slot Template/Force Next Spin/Big Win (50x)")]
        private static void ForceBigWin() => Run("Force 50x win");

        [MenuItem("Tools/Slot Template/Force Next Spin/No Win")]
        private static void ForceNoWin() => Run("Force no win");

        [MenuItem("Tools/Slot Template/Force Next Spin/Free Spins", true)]
        [MenuItem("Tools/Slot Template/Force Next Spin/Big Win (50x)", true)]
        [MenuItem("Tools/Slot Template/Force Next Spin/No Win", true)]
        private static bool InPlayMode() => Application.isPlaying;

        private static void Run(string cheat)
        {
            var menu = Object.FindFirstObjectByType<DebugCheatMenu>();
            if (menu == null)
            {
                Debug.LogWarning("[Slot] No DebugCheatMenu in the scene; is a SlotBootstrap running?");
                return;
            }
            menu.Run(cheat);
        }
    }
}
