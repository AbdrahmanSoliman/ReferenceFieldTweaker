using UnityEditor;

namespace ASoliman.Utils.EditableRefs
{
    /// <summary>
    /// Provides menu items for configuring the Reference Field Tweaker settings.
    /// </summary>
    public static class TweakerMenu
    {
        private const string MENU_PATH = "Tools/Reference Field Tweaker/";

        [MenuItem(MENU_PATH + "Reference Editing &1", false, 0)]
        public static void ToggleReferenceEditing()
        {
            TweakerSettings.EnableReferenceEditing = !TweakerSettings.EnableReferenceEditing;
        }

        [MenuItem(MENU_PATH + "Reference Editing &1", true, 0)]
        public static bool ValidateToggleReferenceEditing()
        {
            Menu.SetChecked(MENU_PATH + "Reference Editing", TweakerSettings.EnableReferenceEditing);
            return true;
        }
        
        [MenuItem(MENU_PATH + "Empty References", false, 1)]
        public static void AllowEmptyReferences()
        {
            TweakerSettings.AllowEmptyReferences = !TweakerSettings.AllowEmptyReferences;
        }

        [MenuItem(MENU_PATH + "Empty References", true, 1)]
        public static bool ValidateAllowEmptyReferences()
        {
            Menu.SetChecked(MENU_PATH + "Empty References", TweakerSettings.AllowEmptyReferences);
            return true;
        }

        [MenuItem(MENU_PATH + "Show Script Name", false, 2)]
        public static void DisplayScriptName()
        {
            TweakerSettings.ShowScriptName = !TweakerSettings.ShowScriptName;
        }

        [MenuItem(MENU_PATH + "Show Script Name", true, 2)]
        public static bool ValidateDisplayScriptName()
        {
            Menu.SetChecked(MENU_PATH + "Show Script Name", TweakerSettings.ShowScriptName);
            return true;
        }

        [MenuItem(MENU_PATH + "Highlight Nested Fields", false, 3)]
        public static void HighlightNestedFields()
        {
            TweakerSettings.HighlightNestedFields = !TweakerSettings.HighlightNestedFields;
        }

        [MenuItem(MENU_PATH + "Highlight Nested Fields", true, 3)]
        public static bool ValidateHighlightNestedFields()
        {
            Menu.SetChecked(MENU_PATH + "Highlight Nested Fields", TweakerSettings.HighlightNestedFields);
            return true;
        }
    }
}
