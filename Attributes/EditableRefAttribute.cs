using System;
using UnityEngine;

namespace ASoliman.Utils.EditableRefs
{
    /// <summary>
    /// Enables in-place reference editing in the Unity Inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class EditableRefAttribute : PropertyAttribute
    {
        public bool EnableReferenceEditing { get; set; } = true;

        public EditableRefAttribute(bool enableReferenceEditing = true)
        {
            EnableReferenceEditing = enableReferenceEditing;
        }
    }
}
