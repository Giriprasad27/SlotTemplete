using SlotTemplate.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace SlotTemplate.Editor
{
    public static class SlotDefinitionValidator
    {
        [MenuItem("Tools/Slot Template/Validate Selected Definition")]
        public static void ValidateSelected()
        {
            if (!(Selection.activeObject is SlotDefinition definition))
            {
                EditorUtility.DisplayDialog("Slot Template", "Select a SlotDefinition asset first.", "OK");
                return;
            }

            var errors = definition.Validate();
            if (errors.Count == 0) Debug.Log($"[Slot] '{definition.name}' is valid.", definition);
            else Debug.LogError($"[Slot] '{definition.name}' has {errors.Count} problem(s):\n- " + string.Join("\n- ", errors), definition);
        }

        [MenuItem("Tools/Slot Template/Validate Selected Definition", true)]
        private static bool CanValidate() => Selection.activeObject is SlotDefinition;
    }
}
