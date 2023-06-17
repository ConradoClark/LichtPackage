using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Editor.Licht.Common
{
    public static class CustomComponents
    {

        public static VisualElement CreateHeaderLabel(Color bgColor, string text, string @class = "MainHeaderLabel")
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"Assets/Editor/Licht/Common/{@class}.uxml");

            var container = visualTree.Instantiate();

            var bg = container.Q<VisualElement>("bg");
            bg.style.backgroundColor =
                new StyleColor(new Color(bgColor.r, bgColor.g, bgColor.b, bg.resolvedStyle.backgroundColor.a));

            var textElement = container.Q<Label>("label");
            textElement.text = text;

            return container;
        }
    }
}
