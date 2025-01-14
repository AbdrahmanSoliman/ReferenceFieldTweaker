using UnityEditor;

namespace ASoliman.Utils.EditableRefs
{
    /// <summary>
    /// Manages persistent settings for the Reference Field Tweaker using EditorPrefs.
    /// </summary>
    public static class TweakerSettings 
    {
        private const string ENABLE_REFERENCE_EDITING_PREF = "TweakerSettings_EnableReferenceEditing";
        private const string ALLOW_EMPTY_REFERENCES_PREF = "TweakerSettings_AllowEmptyReferences";
        private const string SHOW_SCRIPT_NAME_PREF = "TweakerSettings_ShowScriptName";
        private const string HIGHLIGHT_NESTED_FIELDS_PREF = "TweakerSettings_HighlightNestedFields";
        private const string OUTLINE_THICKNESS_PREF = "TweakerSettings_OutlineThickness";

        
        public static bool EnableReferenceEditing
        {
            get => EditorPrefs.GetBool(ENABLE_REFERENCE_EDITING_PREF, true);
            set => EditorPrefs.SetBool(ENABLE_REFERENCE_EDITING_PREF, value);
        }
        
        public static bool AllowEmptyReferences
        {
            get => EditorPrefs.GetBool(ALLOW_EMPTY_REFERENCES_PREF, false);
            set => EditorPrefs.SetBool(ALLOW_EMPTY_REFERENCES_PREF, value);
        }

        public static bool ShowScriptName
        {
            get => EditorPrefs.GetBool(SHOW_SCRIPT_NAME_PREF, false);
            set => EditorPrefs.SetBool(SHOW_SCRIPT_NAME_PREF, value);
        }

        public static bool HighlightNestedFields
        {
            get => EditorPrefs.GetBool(HIGHLIGHT_NESTED_FIELDS_PREF, false);
            set => EditorPrefs.SetBool(HIGHLIGHT_NESTED_FIELDS_PREF, value);
        }

        public static float OutlineThickness
        {
            get => EditorPrefs.GetFloat(OUTLINE_THICKNESS_PREF, 1f); // 1f is the default value
            set => EditorPrefs.SetFloat(OUTLINE_THICKNESS_PREF, value);
        }
    }
}