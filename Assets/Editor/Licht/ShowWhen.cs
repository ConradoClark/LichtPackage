using System.Reflection;
using Licht.Unity.PropertyAttributes;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    [CustomPropertyDrawer(typeof(ShowWhenAttribute))]
    public class ShowWhenDrawer : PropertyDrawer
    {
        private bool _showing;

        private bool ShouldShowProperty(SerializedProperty property)
        {
            if (attribute is not ShowWhenAttribute showWhen) return true;

            var targetObject = property.serializedObject.targetObject;
            var targetObjectClassType = targetObject.GetType();

            var prop = property.serializedObject.FindProperty(showWhen.FieldName)
                       ?? property.serializedObject.FindProperty($"<{showWhen.FieldName}>k__BackingField");

            if (prop == null) return true;

            FieldInfo field;
            var currentType = targetObjectClassType;

            do
            {
                field = currentType.GetField(prop.propertyPath,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                currentType = currentType.BaseType;
            } while (field == null && currentType != null);

            var pRef = field == null ? targetObjectClassType.GetProperty(prop.propertyPath) : null;

            if (field == null && pRef == null) return true;

            var value = field?.GetValue(targetObject) ?? pRef?.GetValue(targetObject);

            return value is not bool b || (b && !showWhen.Reverse);
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            _showing = ShouldShowProperty(property);

            return _showing ? EditorGUI.GetPropertyHeight(property, label) : -2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (_showing) EditorGUI.PropertyField(position, property, label);
        }
    }
}
