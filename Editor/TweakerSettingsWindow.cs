using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ASoliman.Utils.EditableRefs
{
    /// <summary>
    /// Provides a custom Unity Editor window for configuring the Reference Field Tweaker.
    /// Includes options for managing color palettes and toggle settings, 
    /// </summary>
    public class TweakerSettingsWindow : EditorWindow
    {
        private Vector2 _mainScrollPosition;
        private Vector2 _paletteScrollPosition;
        private string _newPaletteName = "";
        private bool _showNewPaletteInput = false;
        private bool _showEditPalette = false;
        private const float MAX_COLOR_LIST_HEIGHT = 150f;
        private const float COLOR_ELEMENT_HEIGHT = 20f;
        private const float BUTTON_PADDING_HEIGHT = 30f; // Account for padding and "Add Color" button
        
        public static event Action OnPaletteChanged;

        [MenuItem("Tools/Reference Field Tweaker/Settings", false, -100)]
        public static void ShowWindow()
        {
            var window = GetWindow<TweakerSettingsWindow>("Reference Field Tweaker");
            window.minSize = new Vector2(300, 400);
        }

        private void OnGUI()
        {
            if (focusedWindow == this)
            {
                Repaint();
            }

            _mainScrollPosition = EditorGUILayout.BeginScrollView(_mainScrollPosition);
            
            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 10) });
            
            DrawTogglesSection();
            
            EditorGUILayout.Space(10);
            
            DrawColorPaletteSection();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawTogglesSection()
        {
            var boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };
            
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            bool newEditEnabled = EditorGUILayout.Toggle(new GUIContent("Reference Editing", 
                "Toggles the ability to edit references within expandable drawers"), 
                TweakerSettings.EnableReferenceEditing);
            if (newEditEnabled != TweakerSettings.EnableReferenceEditing)
            {
                TweakerSettings.EnableReferenceEditing = newEditEnabled;
            }

            if(TweakerSettings.EnableReferenceEditing)
            {
                bool newNullRefEnabled = EditorGUILayout.Toggle(new GUIContent("Empty References", 
                "Toggles the edit button for null or empty references"), 
                TweakerSettings.AllowEmptyReferences);
                if (newNullRefEnabled != TweakerSettings.AllowEmptyReferences)
                {
                    TweakerSettings.AllowEmptyReferences = newNullRefEnabled;
                }

                bool newShowScriptNameEnabled = EditorGUILayout.Toggle(new GUIContent("Show Script Name", 
                    "Shows or hides the script name in the UI"), 
                    TweakerSettings.ShowScriptName);
                if (newShowScriptNameEnabled != TweakerSettings.ShowScriptName)
                {
                    TweakerSettings.ShowScriptName = newShowScriptNameEnabled;
                }

                bool newApplyColorEnabled = EditorGUILayout.Toggle(new GUIContent("Highlight Nested Fields", 
                    "Highlights fields with color to indicate nesting in the UI"), 
                    TweakerSettings.HighlightNestedFields);
                if (newApplyColorEnabled != TweakerSettings.HighlightNestedFields)
                {
                    TweakerSettings.HighlightNestedFields = newApplyColorEnabled;
                }

                EditorGUILayout.LabelField(new GUIContent("Outline Thickness"));
                float newOutlineThickness = EditorGUILayout.Slider(TweakerSettings.OutlineThickness, 0f, 5f);
                if(newOutlineThickness != TweakerSettings.OutlineThickness)
                {
                    TweakerSettings.OutlineThickness = newOutlineThickness;
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawColorPaletteSection()
        {
            var boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 12, 12)
            };
            
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("Color Palette Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            string currentPalette = TweakerColorManager.CurrentPaletteName;
            string[] paletteNames = TweakerColorManager.GetPaletteNames().ToArray();
            int currentIndex = Array.IndexOf(paletteNames, currentPalette);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Active Palette", GUILayout.Width(EditorGUIUtility.labelWidth));
            int newIndex = EditorGUILayout.Popup(currentIndex, paletteNames);
            EditorGUILayout.EndHorizontal();
            
            if (newIndex != currentIndex && newIndex >= 0)
            {
                TweakerColorManager.SetActivePalette(paletteNames[newIndex]);
                TriggerPaletteChanged();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Customize Current Palette", EditorStyles.boldLabel);
            
            GUIContent buttonContent = new GUIContent(
                _showEditPalette ? EditorGUIUtility.IconContent("d_SceneViewTools").image 
                            : EditorGUIUtility.IconContent("d_editicon.sml").image,
                _showEditPalette ? "Close" : "Edit"
            );
            
            if (GUILayout.Button(buttonContent, GUILayout.Width(24), GUILayout.Height(20)))
            {
                _showEditPalette = !_showEditPalette;
            }
            EditorGUILayout.EndHorizontal();

            if (_showEditPalette)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(8, 8, 8, 8) });
                
                List<Color> colors = TweakerColorManager.GetCurrentPaletteColors().ToList();
                
                float requiredHeight = (colors.Count * COLOR_ELEMENT_HEIGHT) + BUTTON_PADDING_HEIGHT;
                float actualHeight = Mathf.Min(requiredHeight, MAX_COLOR_LIST_HEIGHT);
                bool needsScrollView = requiredHeight > MAX_COLOR_LIST_HEIGHT;

                if (needsScrollView)
                {
                    _paletteScrollPosition = EditorGUILayout.BeginScrollView(_paletteScrollPosition, GUILayout.Height(actualHeight));
                }
                
                for (int i = 0; i < colors.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField($"Level {i}", GUILayout.Width(50));
                    
                    // Use delayed color field to prevent immediate closure
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        Color newColor = EditorGUILayout.ColorField(colors[i]);
                        if (check.changed)
                        {
                            TweakerColorManager.UpdateColorInPalette(i, newColor);
                            TriggerPaletteChanged();
                        }
                    }
                    
                    GUI.enabled = colors.Count > 1;
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        TweakerColorManager.RemoveColorFromPalette(i);
                        TriggerPaletteChanged();
                    }
                    GUI.enabled = true;
                    
                    EditorGUILayout.EndHorizontal();
                }

                if (needsScrollView)
                {
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Add Color"))
                {
                    TweakerColorManager.AddColorToPalette(Color.white);
                    TriggerPaletteChanged();
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Palette Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            if (_showNewPaletteInput)
            {
                EditorGUILayout.BeginHorizontal();
                _newPaletteName = EditorGUILayout.TextField("Name", _newPaletteName);
                
                if (GUILayout.Button("Create", GUILayout.Width(60)))
                {
                    if (!string.IsNullOrEmpty(_newPaletteName))
                    {
                        TweakerColorManager.CreateNewPalette(_newPaletteName);
                        TweakerColorManager.SetActivePalette(_newPaletteName);
                        _showNewPaletteInput = false;
                        _newPaletteName = "";
                        TriggerPaletteChanged();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (GUILayout.Button("Create New Palette"))
            {
                _showNewPaletteInput = true;
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = currentPalette != "Default";
            if (GUILayout.Button("Delete Current Palette"))
            {
                if (EditorUtility.DisplayDialog("Delete Palette", 
                    "Are you sure you want to delete the current palette?", 
                    "Yes", "No"))
                {
                    TweakerColorManager.DeleteCurrentPalette();
                    TriggerPaletteChanged();
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("Reset Current Palette"))
            {
                if (EditorUtility.DisplayDialog("Reset Palette", 
                    "Are you sure you want to reset the current palette to default colors?", 
                    "Yes", "No"))
                {
                    TweakerColorManager.ResetCurrentPalette();
                    TriggerPaletteChanged();
                }
            }
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void TriggerPaletteChanged()
        {
            OnPaletteChanged?.Invoke();
            EditorWindow.GetWindow<SceneView>()?.Repaint();
            
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var window in windows)
            {
                if (window.GetType().Name == "ProjectBrowser" || 
                    window.GetType().Name == "InspectorWindow")
                {
                    window.Repaint();
                }
            }
        }
    }

    [Serializable]
    public class ColorPalette
    {
        public string name;
        public List<Color> colors = new List<Color>();
    }

    [Serializable]
    public class ColorPaletteData
    {
        public List<ColorPalette> palettes = new List<ColorPalette>();
        public string activePaletteName;
    }

    /// <summary>
    /// Manages color palettes for the Reference Field Tweaker.
    /// </summary>
    public static class TweakerColorManager
    {
        private const string PALETTE_PREFS_KEY = "TweakerColorManager_ColorPalettes";
        private static ColorPaletteData _paletteData = new ColorPaletteData();
        
        static TweakerColorManager()
        {
            LoadPalettes();
        }

        public static string CurrentPaletteName => _paletteData.activePaletteName;

        public static Color GetColorForDepth(int depth)
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            if (palette == null || palette.colors.Count == 0)
                return Color.white;
                
            return depth >= palette.colors.Count ? palette.colors[palette.colors.Count - 1] : palette.colors[depth];
        }

        public static IEnumerable<string> GetPaletteNames()
        {
            return _paletteData.palettes.Select(p => p.name);
        }

        public static IEnumerable<Color> GetCurrentPaletteColors()
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            return palette?.colors ?? new List<Color>();
        }

        public static void SetActivePalette(string paletteName)
        {
            if (_paletteData.palettes.Any(p => p.name == paletteName))
            {
                _paletteData.activePaletteName = paletteName;
                SavePalettes();
            }
        }

        public static void CreateNewPalette(string name)
        {
            if (!_paletteData.palettes.Any(p => p.name == name))
            {
                var newPalette = new ColorPalette 
                { 
                    name = name,
                    colors = new List<Color> {Color.white}
                };
                _paletteData.palettes.Add(newPalette);
                SavePalettes();
            }
        }

        public static void DeleteCurrentPalette()
        {
            if (_paletteData.activePaletteName == "Default")
                return;
                
            _paletteData.palettes.RemoveAll(p => p.name == _paletteData.activePaletteName);
            _paletteData.activePaletteName = "Default";
            SavePalettes();
        }

        public static void AddColorToPalette(Color color)
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            if (palette != null)
            {
                palette.colors.Add(color);
                SavePalettes();
            }
        }

        public static void UpdateColorInPalette(int index, Color color)
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            if (palette != null && index < palette.colors.Count)
            {
                palette.colors[index] = color;
                SavePalettes();
            }
        }

        public static void RemoveColorFromPalette(int index)
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            if (palette != null && palette.colors.Count > 1 && index < palette.colors.Count)
            {
                palette.colors.RemoveAt(index);
                SavePalettes();
            }
        }

        public static void ResetCurrentPalette()
        {
            var palette = _paletteData.palettes.Find(p => p.name == _paletteData.activePaletteName);
            if (palette != null)
            {
                palette.colors = GetDefaultColors();
                SavePalettes();
            }
        }

        private static List<Color> GetDefaultColors()
        {
            return new List<Color>
            {
                new Color(0.94f, 0.76f, 0.27f),  // Golden Yellow
                new Color(0.22f, 0.71f, 0.85f),  // Bright Cyan
                new Color(0.85f, 0.28f, 0.45f),  // Vivid Red-Pink
                new Color(0.36f, 0.85f, 0.43f),  // Fresh Green
                new Color(0.64f, 0.39f, 0.87f),  // Electric Purple
                new Color(0.95f, 0.50f, 0.23f)   // Tangerine Orange
            };
        }

        private static void LoadPalettes()
        {
            string json = EditorPrefs.GetString(PALETTE_PREFS_KEY, "");
            
            if (string.IsNullOrEmpty(json))
            {
                // Initialize with default palette
                _paletteData = new ColorPaletteData
                {
                    palettes = new List<ColorPalette>
                    {
                        new ColorPalette
                        {
                            name = "Default",
                            colors = GetDefaultColors()
                        }
                    },
                    activePaletteName = "Default"
                };
            }
            else
            {
                try
                {
                    _paletteData = JsonUtility.FromJson<ColorPaletteData>(json);
                    
                    // Ensure Default palette exists
                    if (!_paletteData.palettes.Any(p => p.name == "Default"))
                    {
                        _paletteData.palettes.Add(new ColorPalette
                        {
                            name = "Default",
                            colors = GetDefaultColors()
                        });
                    }
                    
                    // Ensure active palette exists
                    if (!_paletteData.palettes.Any(p => p.name == _paletteData.activePaletteName))
                    {
                        _paletteData.activePaletteName = "Default";
                    }
                }
                catch
                {
                    // Fallback to default if json is corrupt
                    _paletteData = new ColorPaletteData
                    {
                        palettes = new List<ColorPalette>
                        {
                            new ColorPalette
                            {
                                name = "Default",
                                colors = GetDefaultColors()
                            }
                        },
                        activePaletteName = "Default"
                    };
                }
            }
        }

        private static void SavePalettes()
        {
            string json = JsonUtility.ToJson(_paletteData);
            EditorPrefs.SetString(PALETTE_PREFS_KEY, json);
            
            // Force repaint of all relevant windows
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                window.Repaint();
            }
        }

    }
}