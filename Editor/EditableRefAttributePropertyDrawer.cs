using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ASoliman.Utils.EditableRefs
{
    /// <summary>
    /// Static class that maintains the hierarchical context for editable property drawers.
    /// Handles depth tracking and color management for nested editable references.
    /// </summary>
    public static class EditableDrawerContext
    {
        private static readonly Stack<EditableContext> _hierarchyStack = new Stack<EditableContext>();
        private static readonly Dictionary<string, ColorInfo> _colorCache = new Dictionary<string, ColorInfo>();

        private class EditableContext
        {
            public string propertyPath;
            public int depth;
            public int classNestLevel;

            public EditableContext(string path, int depth, int classNest)
            {
                propertyPath = path;
                this.depth = depth;
                classNestLevel = classNest;
            }
        }

        private class ColorInfo
        {
            public int depth;
            public Color color;

            public ColorInfo(int depth, Color color)
            {
                this.depth = depth;
                this.color = color;
            }
        }
        
        /// <summary>
        /// Pushes a new editable context onto the hierarchy stack.
        /// </summary>
        /// <param name="propertyPath">The path of the serialized property</param>
        /// <param name="property">The serialized property being processed</param>
        public static void PushEditable(string propertyPath, SerializedProperty property)
        {
            // Calculate class nesting level
            int classNestLevel = CalculateClassNestLevel(property);
            
            // If this is the first expandable in its context or a deeper class nesting
            if (_hierarchyStack.Count == 0 || classNestLevel > _hierarchyStack.Peek().classNestLevel)
            {
                int newDepth = _hierarchyStack.Count > 0 ? 
                    _hierarchyStack.Peek().depth + 1 : 0;
                    
                _hierarchyStack.Push(new EditableContext(propertyPath, newDepth, classNestLevel));
            }
            // If it's at the same class level, use the next depth
            else if (classNestLevel == _hierarchyStack.Peek().classNestLevel)
            {
                int newDepth = _hierarchyStack.Peek().depth + 1;
                _hierarchyStack.Push(new EditableContext(propertyPath, newDepth, classNestLevel));
            }
            // If it's at a lower class level, use the parent's depth + 1
            else
            {
                var parentDepth = GetParentDepth(classNestLevel);
                _hierarchyStack.Push(new EditableContext(propertyPath, parentDepth + 1, classNestLevel));
            }

            // Cache the color for this property
            if (!_colorCache.ContainsKey(propertyPath))
            {
                var color = TweakerColorManager.GetColorForDepth(_hierarchyStack.Peek().depth);
                _colorCache[propertyPath] = new ColorInfo(_hierarchyStack.Peek().depth, color);
            }
        }

        /// <summary>
        /// Gets the depth of the parent context for a given class level.
        /// </summary>
        private static int GetParentDepth(int classLevel)
        {
            foreach (var context in _hierarchyStack)
            {
                if (context.classNestLevel < classLevel)
                {
                    return context.depth;
                }
            }
            return 0;
        }

        /// <summary>
        /// Calculates the nesting level of a serialized property within its class hierarchy.
        /// </summary>
        private static int CalculateClassNestLevel(SerializedProperty property)
        {
            int level = 0;
            string[] pathParts = property.propertyPath.Split('.');
            
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                SerializedProperty parent = property.serializedObject.FindProperty(
                    string.Join(".", pathParts.Take(i + 1)));
                    
                if (parent != null && parent.propertyType == SerializedPropertyType.Generic)
                {
                    level++;
                }
            }
            
            return level;
        }

        /// <summary>
        /// Removes the most recently added editable context from the stack.
        /// </summary>
        public static void PopEditable()
        {
            if (_hierarchyStack.Count > 0)
            {
                _hierarchyStack.Pop();
            }
        }

        public static Color GetColor(string propertyPath)
        {
            if (_colorCache.TryGetValue(propertyPath, out var colorInfo))
            {
                return colorInfo.color;
            }
            return TweakerColorManager.GetColorForDepth(0);
        }

        public static void Clear()
        {
            _hierarchyStack.Clear();
            _colorCache.Clear();
        }
    }

    /// <summary>
    /// Custom property drawer for EditableRefAttribute that enables inline editing of referenced objects.
    /// Provides a hierarchical editing interface with visual feedback for nested properties.
    /// </summary>
    [CustomPropertyDrawer(typeof(EditableRefAttribute))]
    public class EditableRefAttributePropertyDrawer : PropertyDrawer
    {
        private Editor _editor = null;
        private float _iconFieldPadding = 5f;
        private const float ICON_SIZE = 16f;
        private const float CONTENT_MARGIN = 4f;
        private static readonly Color UNITY_BOX_COLOR = EditorGUIUtility.isProSkin ? 
            new Color(0.22f, 0.22f, 0.22f, 0.6f) : 
            new Color(0.8f, 0.8f, 0.8f, 0.6f);

        private bool ShouldShowEditButton(SerializedProperty property)
        {
            var expandableAttr = attribute as EditableRefAttribute;
            bool isNull = property.objectReferenceValue == null;
            
            if (!expandableAttr.EnableReferenceEditing || !TweakerSettings.EnableReferenceEditing)
                return false;
                
            if (isNull && !TweakerSettings.AllowEmptyReferences)
                return false;
                
            return true;
        }

        /// <summary>
        /// Calculates the total height needed to draw the property.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if(!TweakerSettings.EnableReferenceEditing)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            float totalHeight = EditorGUI.GetPropertyHeight(property, label, true);
            
            if (!property.isExpanded || property.objectReferenceValue == null)
            {
                if (property.isExpanded && property.objectReferenceValue == null)
                {
                    totalHeight += EditorGUIUtility.singleLineHeight * 2;
                }
                return totalHeight;
            }

            if (_editor == null || _editor.target != property.objectReferenceValue)
            {
                Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
            }

            SerializedObject serializedObject = _editor.serializedObject;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" && !TweakerSettings.ShowScriptName) continue;
                totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            // Add margin and padding to total height
            totalHeight += CONTENT_MARGIN * 2 + EditorGUIUtility.standardVerticalSpacing * 2;
            
            return totalHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(!TweakerSettings.EnableReferenceEditing)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (Event.current.type == EventType.Layout)
            {
                EditableDrawerContext.Clear();
            }

            if (property.isExpanded)
            {
                EditableDrawerContext.PushEditable(property.propertyPath, property);
            }

            try
            {
                DrawEditableProperty(position, property, label);
            }
            finally
            {
                if (property.isExpanded)
                {
                    EditableDrawerContext.PopEditable();
                }
            }
        }

        private void DrawEditableProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            bool showButton = ShouldShowEditButton(property);
            bool isNull = property.objectReferenceValue == null;
            int currentIndent = EditorGUI.indentLevel;
            float indentWidth = currentIndent * 15f;

            // Get color based on hierarchical depth
            Color propertyColor = EditableDrawerContext.GetColor(property.propertyPath);

            float buttonSpace = showButton ? ICON_SIZE + _iconFieldPadding : 0f;
            
            Rect buttonRect = new Rect(
                position.x + indentWidth,
                position.y + 1f,
                ICON_SIZE + _iconFieldPadding,
                ICON_SIZE + 2f
            );
            
            Rect propertyRect = new Rect(
                position.x + buttonSpace,
                position.y,
                position.width - buttonSpace,
                EditorGUI.GetPropertyHeight(property, label, false)
            );

            // Draw Edit button if enabled
            if (showButton)
            {
                GUIContent buttonContent = new GUIContent(
                    property.isExpanded ? EditorGUIUtility.IconContent("d_SceneViewTools").image 
                                    : EditorGUIUtility.IconContent("d_editicon.sml").image,
                    property.isExpanded ? "Close" : "Edit"
                );

                GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    fixedWidth = ICON_SIZE + _iconFieldPadding,
                    fixedHeight = ICON_SIZE + 2f,
                    alignment = TextAnchor.MiddleCenter
                };

                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = property.isExpanded ? propertyColor : Color.white;

                if (GUI.Button(buttonRect, buttonContent, buttonStyle))
                {
                    property.isExpanded = !property.isExpanded;
                    property.serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }

                GUI.backgroundColor = originalColor;
            }

            // Draw the main property field
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(propertyRect, property, label, false);
            if (EditorGUI.EndChangeCheck() && property.isExpanded)
            {
                property.isExpanded = false;
                GUIUtility.ExitGUI();
            }

            // Handle expanded state
            if (property.isExpanded)
            {            
                if (isNull)
                {
                    float warningBoxHeight = EditorGUIUtility.singleLineHeight * 2;
                    Rect warningRect = new Rect(
                        position.x + indentWidth,
                        position.y + propertyRect.height + EditorGUIUtility.standardVerticalSpacing,
                        position.width - indentWidth,
                        warningBoxHeight
                    );
                    
                    EditorGUI.HelpBox(warningRect, "Reference is null. Fields are not editable.", MessageType.Warning);
                }
                else
                {
                    DrawExpandedContent(position, property, propertyRect, indentWidth, propertyColor);
                }
            }
        }

        private void DrawExpandedContent(Rect position, SerializedProperty property, Rect propertyRect, float indentWidth, Color propertyColor)
        {
            float yOffset = propertyRect.height + EditorGUIUtility.standardVerticalSpacing;
            
            // Calculate total height of all properties first
            float totalContentHeight = 0f;
            SerializedObject serializedObject = null;
            
            if (_editor == null || _editor.target != property.objectReferenceValue)
            {
                Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
            }
            
            serializedObject = _editor.serializedObject;
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" && !TweakerSettings.ShowScriptName) continue;
                totalContentHeight += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            
            // Add padding to total height
            totalContentHeight += CONTENT_MARGIN * 2;

            // Main box area with correct height
            Rect boxRect = new Rect(
                position.x + indentWidth,
                position.y + yOffset,
                position.width - indentWidth,
                totalContentHeight // Use calculated height
            );

            // Draw the background
            GUI.backgroundColor = UNITY_BOX_COLOR;
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;

            // Content area with balanced margins
            Rect contentRect = new Rect(
                boxRect.x + CONTENT_MARGIN + 4f, // Reduced left margin
                boxRect.y + CONTENT_MARGIN,
                boxRect.width - (CONTENT_MARGIN * 2) - 4f,
                boxRect.height - (CONTENT_MARGIN * 2)
            );

            serializedObject.Update();
            float currentY = contentRect.y;
            iterator = serializedObject.GetIterator();
            enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" && !TweakerSettings.ShowScriptName)
                {
                    continue;
                }
                else if(iterator.name == "m_Script" && TweakerSettings.ShowScriptName)
                {
                    GUI.enabled = false;
                }

                float propertyHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect propertyPosition = new Rect(
                    contentRect.x + CONTENT_MARGIN * 2,
                    currentY,
                    contentRect.width - CONTENT_MARGIN * 2,
                    propertyHeight
                );

                // Draw field background if enabled
                if (TweakerSettings.HighlightNestedFields)
                {
                    Color fieldColor = new Color(propertyColor.r, propertyColor.g, propertyColor.b, 0.1f);
                    Rect backgroundRect = new Rect(
                        propertyPosition.x - 2,
                        propertyPosition.y,
                        propertyPosition.width + 4,
                        propertyHeight
                    );
                    EditorGUI.DrawRect(backgroundRect, fieldColor);
                }

                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(propertyPosition, iterator, true);
                GUI.enabled = true; // Just for m_Script field to be read-only if visible
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.objectReferenceValue);
                }

                currentY += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            // Draw subtle outline if alpha > 0
            if (propertyColor.a > 0)
            {
                DrawOutlineBox(boxRect, propertyColor, TweakerSettings.OutlineThickness);
            }
        }
        
        private static void DrawOutlineBox(Rect rect, Color color, float thickness)
        {
            float x = rect.x + 1;
            float y = rect.y + 1;
            float width = rect.width - 2;
            float height = rect.height - 2;
            
            var drawRect = new Rect();
            
            // Top
            drawRect.Set(x, y, width, thickness);
            EditorGUI.DrawRect(drawRect, color);
            
            // Bottom
            drawRect.y = y + height - thickness;
            EditorGUI.DrawRect(drawRect, color);
            
            // Left
            drawRect.Set(x, y, thickness, height);
            EditorGUI.DrawRect(drawRect, color);
            
            // Right
            drawRect.x = x + width - thickness;
            EditorGUI.DrawRect(drawRect, color);
        }
    }
}