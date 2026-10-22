using LLMVRPrototype.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype
{
    public class TagsSection : DatabaseToolSection
    {
        

        public TagsSection(AssetDatabaseWindowContext context, VisualElement root) : base(context, root)
        {
            
        }

        public void Build()
        {
            root.Clear();

            AddHeading("Database Overview");

            if (!context.HasValidConfiguration)
            {
                AddMessage("Select a database configuration and assign all required references.");

                return;
            }
        }
    }
}
