using LLMVRPrototype.Assets;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LLMVRPrototype.EditorTools
{
    public class AssetDatabaseWindow : EditorWindow
    {
        private const string WindowTitle = "Asset Database";
        private const string ConfigPreferenceKey = "LLMVRPrototype.AssetDatabaseConfig";

        private AssetDatabaseConfig databaseConfig;

        private VisualElement contentContainer;

        [MenuItem("LLM VR/Asset Database")]
        public static void ShowWindow()
        {
            AssetDatabaseWindow window = GetWindow<AssetDatabaseWindow>();

            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(500, 350);
        }

        public void CreateGUI()
        {
            LoadSavedConfiguration();

            VisualElement root = rootVisualElement;

            root.Clear();

            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            CreateHeader(root);
            CreateConfigurationSelector(root);
            CreateNavigation(root);

            contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            contentContainer.style.marginTop = 12;

            root.Add(contentContainer);

            ShowSection("Overview");
        }

        private void CreateHeader(VisualElement root)
        {
            var title = new Label("Database Asset Import");

            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            root.Add(title);

            var description = new Label("Add new assets, operations, categories, and tags to the database for LLM-assisted VR environment generation.");

            description.style.marginTop = 4;
            description.style.marginBottom = 12;
            description.style.whiteSpace = WhiteSpace.Normal;

            root.Add(description);
        }

        private void CreateConfigurationSelector(VisualElement root) 
        { 
            var configField = new ObjectField("Database Configuration") 
            { 
                objectType = typeof(AssetDatabaseConfig), 
                allowSceneObjects = false, 
                value = databaseConfig 
            }; 
            
            configField.RegisterValueChangedCallback(evt => 
            { 
                databaseConfig = evt.newValue as AssetDatabaseConfig; 
                SaveConfiguration(); 
                ShowSection("Overview"); 
            }); 
            root.Add(configField); 
        }

        private void CreateNavigation(VisualElement root)
        {
            var navigation = new VisualElement();

            navigation.style.flexDirection = FlexDirection.Row;

            navigation.style.flexWrap = Wrap.Wrap;

            AddNavigationButton(navigation, "Overview");
            AddNavigationButton(navigation, "Assets");
            AddNavigationButton(navigation, "Operations");
            AddNavigationButton(navigation, "Categories");
            AddNavigationButton(navigation, "Tags");

            root.Add(navigation);
        }

        private void AddNavigationButton(VisualElement parent, string sectionName)
        {
            var button = new Button(() => ShowSection(sectionName))
            {
                text = sectionName
            };

            button.style.marginRight = 5;
            button.style.marginBottom = 5;

            parent.Add(button);
        }

        private void ShowSection(string sectionName)
        {
            if (contentContainer == null)
                return;

            contentContainer.Clear();

            var context = new AssetDatabaseWindowContext(databaseConfig);

            switch (sectionName)
            {
                case "Overview":
                    new OverviewSection(context, contentContainer).Build();
                    break;

                case "Assets":
                    new AssetsSection(context, contentContainer).Build();
                    break;

                case "Operations":
                    new OperationsSection(context, contentContainer).Build();
                    break;

                case "Categories":
                    new CategoriesSection(context, contentContainer).Build();
                    break;

                case "Tags":
                    new TagsSection(context, contentContainer).Build();
                    break;
            }
        }

        private void LoadSavedConfiguration()
        {
            string assetPath = EditorPrefs.GetString(ConfigPreferenceKey, string.Empty);

            databaseConfig = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<AssetDatabaseConfig>(assetPath);
        }

        private void SaveConfiguration() 
        { 
            string assetPath = databaseConfig != null ? AssetDatabase.GetAssetPath(databaseConfig) : string.Empty; 
            EditorPrefs.SetString(ConfigPreferenceKey, assetPath); 
        }
    }
}
