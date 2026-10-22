using LLMVRPrototype.EditorTools;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class DatabaseToolSection
    {
        protected readonly AssetDatabaseWindowContext context;
        protected readonly VisualElement root;

        protected DatabaseToolSection(AssetDatabaseWindowContext context, VisualElement root)
        {
            this.context = context;
            this.root = root;
        }

        /// <summary>
        /// Builds the standard section layout and then its content.
        /// </summary>
        public void Build()
        {
            root.Clear();

            BuildHeader();
            BuildContent();
        }

        /// <summary>
        /// Override this method to implement section-specific UI.
        /// </summary>
        protected virtual void BuildContent()
        {

        }

        protected virtual void BuildHeader()
        {
            AddHeading(GetSectionTitle());

            AddMessage(GetSectionDescription());
        }

        protected virtual string GetSectionTitle()
        {
            return "Empty Section";
        }

        protected virtual string GetSectionDescription()
        {
            return string.Empty;
        }

        protected void AddHeading(string text)
        {
            var label = new Label(text);

            label.style.fontSize = 16;
            label.style.marginTop = 12;
            label.style.marginBottom = 5;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;

            root.Add(label);
        }

        protected void AddMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            var label = new Label(text);

            label.style.marginTop = 5;
            label.style.marginBottom = 8;
            label.style.whiteSpace = WhiteSpace.Normal;

            root.Add(label);
        }

        protected void AddReferenceRow(string label, Object referencedObject)
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 3;

            var nameLabel = new Label(label);
            nameLabel.style.minWidth = 170;

            var objectField = new ObjectField
            {
                objectType = referencedObject != null ? referencedObject.GetType() : typeof(Object),

                value = referencedObject, allowSceneObjects = false
            };

            objectField.SetEnabled(false);
            objectField.style.flexGrow = 1;

            row.Add(nameLabel);
            row.Add(objectField);

            root.Add(row);
        }

        protected void AddStatistic(string label, int value)
        {
            var row = new VisualElement();

            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 3;

            var nameLabel = new Label(label);
            nameLabel.style.minWidth = 170;

            var valueLabel = new Label(value.ToString());
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(nameLabel);
            row.Add(valueLabel);

            root.Add(row);
        }

        protected Button AddButton(string text, System.Action onClick)
        {
            var button = new Button(onClick)
            {
                text = text
            };

            button.style.marginTop = 5;
            button.style.marginBottom = 5;

            root.Add(button);

            return button;
        }

        protected bool HasValidConfiguration()
        {
            if (context.HasValidConfiguration)
                return true;

            AddMessage("Select a database configuration and ensure all required references are assigned.");

            return false;
        }
    }
}
