using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Assets.Editor.Licht.Common;
using Licht.Unity.CharacterControllers;
using Licht.Unity.Objects;
using Licht.Unity.Physics;
using Licht.Unity.Pooling;
using Licht.Unity.PropertyAttributes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CanEditMultipleObjects]
[UnityEditor.CustomEditor(typeof(BaseGameObject), true)]
public class CustomEditor : Editor
{
    private static readonly Color LichtObjectColor = new(157 / 255f, 175 / 255f, 255 / 255f, 1f);
    private static readonly Color LichtRunnerColor = new(255 / 255f, 157 / 255f, 238 / 255f, 1f);
    private static readonly Color LichtAgentColor = new(157 / 255f, 255 / 255f, 162 / 255f, 1f);
    private static readonly Color LichtActorColor = new(255 / 255f, 157 / 255f, 238 / 255f, 1f);
    private static readonly Color LichtMovementControllerColor = new(255 / 255f, 150 / 255f, 150 / 255f, 1f);
    private static readonly Color LichtPhysicsForceColor = new(157 / 255f, 242 / 255f, 255 / 255f, 1f);
    private static readonly Color LichtBaseAIActionColor = new(157 / 255f, 112 / 255f, 112 / 255f, 1f);
    private static readonly Color LichtBaseAIConditionColor = new(112 / 255f, 112 / 255f, 157 / 255f, 1f);
    private static readonly Color LichtPooledObjectColor = new(157 / 255f, 139 / 255f, 181 / 255f, 1f);

    private static readonly Type[] CustomLichtTypes = new[]
    {
        typeof(BaseGameObject),
        typeof(BaseGameRunner),
        typeof(BaseGameAgent),
        typeof(LichtMovementController),
        typeof(BaseActor),
        typeof(LichtCustomPhysicsForce),
        typeof(BaseAIAction),
        typeof(BaseAICondition),
        typeof(PooledObject)
    };

    public override VisualElement CreateInspectorGUI()
    {
        // Create a new VisualElement to be the root of our inspector UI
        var myInspector = new VisualElement();

        var header = CreateHeaderByType(targets.First().GetType());
        if (header != null) myInspector.Add(header);

        var lichtParams = new[]
        {
            $"<{nameof(BaseGameRunner.RunOnEnable)}>k__BackingField",
            $"<{nameof(BaseGameRunner.Loop)}>k__BackingField",
            $"<{nameof(BaseGameRunner.TimerReference)}>k__BackingField",
            $"<{nameof(BaseGameRunner.UseCustomTimer)}>k__BackingField",
        };

        var hasProps = HasAnyProps(serializedObject, true, lichtParams);
        if (hasProps)
        {
            myInspector.Add(CustomComponents.CreateHeaderLabel(Color.grey, "Basic Properties", "FirstHeaderLabel"));
        }

        foreach (var element in DrawSpecificProperties(serializedObject, lichtParams))
        {
            myInspector.Add(element);
        }

        var exclusion = lichtParams.Concat(new[] { "m_Script" }).ToArray();
        if (hasProps && HasAnyProps(serializedObject, false, exclusion))
        {
            myInspector.Add(CustomComponents.CreateHeaderLabel(Color.grey, "Custom Properties", "HeaderLabel"));
        }
         
        foreach (var element in DrawPropertiesExcluding(serializedObject, exclusion))
        {
            myInspector.Add(element);
        }

        // Return the finished inspector UI
        return myInspector;
    }

    public bool HasAnyProps(SerializedObject obj,
        bool include,
        params string[] propertiesToInclude)
    {
        var iterator = obj.GetIterator();
        var enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if ((include && propertiesToInclude.Contains(iterator.name))
                || (!include && !propertiesToInclude.Contains(iterator.name)))
                return true;
        }

        return false;
    }


    protected internal static IEnumerable<VisualElement> DrawProps(
        SerializedObject obj,
        bool include,
        params string[] propertiesToInclude)
    {
        var iterator = obj.GetIterator();
        var enterChildren = true;

        var parents = new Stack<VisualElement>();

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (include && !propertiesToInclude.Contains(iterator.name) || 
                (!include && propertiesToInclude.Contains(iterator.name))) continue;

            if (EndFoldout(iterator))
            {
                var end = parents.Pop();
                if (parents.Count == 0) yield return end;
            }

            var header = DrawHeaderIfNeeded(iterator);
            if (header != null) yield return header;

            var foldout = BeginFoldout(iterator);
            if (foldout != null)
            {
                parents.Push(new Foldout { text = foldout.Name });
            }

            foreach (var elem in DrawLabelsIfNeeded(iterator))
            {
                if (parents.Count == 0) yield return elem;
                else parents.Peek().Add(elem);
            }

            if (parents.Count == 0) yield return DrawField(iterator);
            else parents.Peek().Add(DrawField(iterator));
        }

        if (parents.Count > 0) yield return parents.ToArray().Last();
    }

    protected internal static IEnumerable<VisualElement> DrawSpecificProperties(
        SerializedObject obj,
        params string[] propertiesToInclude)
    {
        return DrawProps(obj, true, propertiesToInclude);
    }

    protected internal new static IEnumerable<VisualElement> DrawPropertiesExcluding(
        SerializedObject obj,
        params string[] propertyToExclude)
    {
        return DrawProps(obj, false, propertyToExclude);
    }

    private static BeginFoldoutAttribute BeginFoldout(SerializedProperty prop)
    {
        return prop.GetUnderlyingField().GetAttribute<BeginFoldoutAttribute>();
    }

    private static bool EndFoldout(SerializedProperty prop)
    {
        return prop.GetUnderlyingField().GetAttribute<EndFoldoutAttribute>() != null;
    }

    private static VisualElement DrawHeaderIfNeeded(SerializedProperty prop)
    {
        var attr = prop.GetUnderlyingField().GetAttribute<CustomHeaderAttribute>();
        return attr != null ? CustomComponents.CreateHeaderLabel(Color.grey, attr.Name, "HeaderLabel") : null;
    }

    private static IEnumerable<VisualElement> DrawLabelsIfNeeded(SerializedProperty prop)
    {
        var attrs = prop.GetUnderlyingField().GetAttributes<CustomLabelAttribute>();
        foreach (var attr in attrs)
        {
            var label = new Label(attr.Text)
            {
                style =
                {
                    color = Color.grey,
                    unityFontStyleAndWeight = FontStyle.BoldAndItalic,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            yield return label;
        }
    }

    private static VisualElement DrawField(SerializedProperty prop)
    {
        var field = prop.GetUnderlyingField();
        var inspectorName = field.GetAttribute<InspectorNameAttribute>();
        var readOnly = field.GetAttribute<ReadOnlyAttribute>();
        var propField = new PropertyField(prop, inspectorName?.displayName ?? prop.displayName);
        propField.SetEnabled(readOnly == null);
        return propField;
    }

    private static VisualElement CreateHeaderByType(Type baseType)
    {
        var type = baseType;
        while (!CustomLichtTypes.Contains(type))
        {
            type = type?.BaseType;
            if (type == null) return null;
        }

        if (type == typeof(BaseGameObject))
        {
            return CustomComponents.CreateHeaderLabel(LichtObjectColor, "Licht Object");
        }

        if (type == typeof(BaseGameRunner))
        {
            return CustomComponents.CreateHeaderLabel(LichtRunnerColor, "Licht Runner");
        }

        if (type == typeof(BaseGameAgent))
        {
            return CustomComponents.CreateHeaderLabel(LichtAgentColor, "Licht Agent");
        }

        if (type == typeof(LichtMovementController))
        {
            return CustomComponents.CreateHeaderLabel(LichtMovementControllerColor, "Licht Movement Controller");
        }

        if (type == typeof(BaseActor))
        {
            return CustomComponents.CreateHeaderLabel(LichtActorColor, "Licht Actor");
        }

        if (type == typeof(LichtCustomPhysicsForce))
        {
            return CustomComponents.CreateHeaderLabel(LichtPhysicsForceColor, "Licht Physics Force");
        }

        if (type == typeof(BaseAIAction))
        {
            return CustomComponents.CreateHeaderLabel(LichtBaseAIActionColor, "Licht AI Action");
        }

        if (type == typeof(BaseAICondition))
        {
            return CustomComponents.CreateHeaderLabel(LichtBaseAIConditionColor, "Licht AI Condition");
        }

        if (type == typeof(PooledObject))
        {
            return CustomComponents.CreateHeaderLabel(LichtPooledObjectColor, "Licht Pooled Object");
        }

        return null;
    }
}

[CanEditMultipleObjects]
[UnityEditor.CustomEditor(typeof(LichtCustomPhysicsForce), true)]
public class CustomEditorPhysicsForce : CustomEditor
{
}