using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    class InspectorView : GraphSubWindow
    {
        const float k_InspectorUpdateInterval = 0.25f;
        const int k_InspectorElementLimit = 20;

        int m_CurrentlyInspectedElementsCount = 0;

        readonly List<Type> m_PropertyDrawerList = new List<Type>();

        // There's persistent data that is stored in the graph settings property drawer that we need to hold onto between interactions
        IPropertyDrawer m_graphSettingsPropertyDrawer = new GraphDataPropertyDrawer();
        public override string windowTitle => "Graph Inspector";
        public override string elementName => "InspectorView";
        public override string styleName => "InspectorView";
        public override string UxmlName => "GraphInspector";
        public override string layoutKey => "UnityEditor.ShaderGraph.InspectorWindow";

        TabbedView m_GraphInspectorView;
        TabbedView m_NodeSettingsTab;
        protected VisualElement m_GraphSettingsContainer;
        protected VisualElement m_NodeSettingsContainer;

        Label m_MaxItemsMessageLabel;

        internal static bool forceNodeView = true;

        void RegisterPropertyDrawer(Type newPropertyDrawerType)
        {
            if (typeof(IPropertyDrawer).IsAssignableFrom(newPropertyDrawerType) == false)
            {
                Debug.Log("Attempted to register a property drawer that doesn't inherit from IPropertyDrawer!");
                return;
            }

            var newPropertyDrawerAttribute = newPropertyDrawerType.GetCustomAttribute<SGPropertyDrawerAttribute>();

            if (newPropertyDrawerAttribute != null)
            {
                foreach (var existingPropertyDrawerType in m_PropertyDrawerList)
                {
                    var existingPropertyDrawerAttribute = existingPropertyDrawerType.GetCustomAttribute<SGPropertyDrawerAttribute>();
                    if (newPropertyDrawerAttribute.propertyType.IsSubclassOf(existingPropertyDrawerAttribute.propertyType))
                    {
                        // Derived types need to be at start of list
                        m_PropertyDrawerList.Insert(0, newPropertyDrawerType);
                        return;
                    }

                    if (existingPropertyDrawerAttribute.propertyType.IsSubclassOf(newPropertyDrawerAttribute.propertyType))
                    {
                        // Add new base class type to end of list
                        m_PropertyDrawerList.Add(newPropertyDrawerType);
                        // Shift already added existing type to the beginning of the list
                        m_PropertyDrawerList.Remove(existingPropertyDrawerType);
                        m_PropertyDrawerList.Insert(0, existingPropertyDrawerType);
                        return;
                    }
                }

                m_PropertyDrawerList.Add(newPropertyDrawerType);
            }
            else
                Debug.Log("Attempted to register property drawer: " + newPropertyDrawerType + " that isn't marked up with the SGPropertyDrawer attribute!");
        }

        public InspectorView(InspectorViewModel viewModel) : base(viewModel)
        {
            m_GraphInspectorView = m_MainContainer.Q<TabbedView>("GraphInspectorView");
            m_GraphSettingsContainer = m_GraphInspectorView.Q<VisualElement>("GraphSettingsContainer");
            m_NodeSettingsContainer = m_GraphInspectorView.Q<VisualElement>("NodeSettingsContainer");
            m_MaxItemsMessageLabel = m_GraphInspectorView.Q<Label>("maxItemsMessageLabel");
            m_ContentContainer.Add(m_GraphInspectorView);

            isWindowScrollable = true;
            isWindowResizable = true;

            var unregisteredPropertyDrawerTypes = TypeCache.GetTypesDerivedFrom<IPropertyDrawer>().ToList();

            foreach (var type in unregisteredPropertyDrawerTypes)
            {
                RegisterPropertyDrawer(type);
            }

            // By default at startup, show graph settings
            m_GraphInspectorView.Activate(m_GraphInspectorView.Q<TabButton>("GraphSettingsButton"));
        }

        public void InitializeGraphSettings()
        {
            ShowGraphSettings_Internal(m_GraphSettingsContainer);
        }

        public bool doesInspectorNeedUpdate { get; set; }

        public void TriggerInspectorUpdate(IEnumerable<ISelectable> selectionList)
        {
            // An optimization that prevents inspector updates from getting triggered every time a selection event is issued in the event of large selections
            // As beyond a certain number of selections
            if (selectionList?.Count() > k_InspectorElementLimit)
                return;
            doesInspectorNeedUpdate = true;
        }

        public void Update()
        {
            ShowGraphSettings_Internal(m_GraphSettingsContainer);

            m_NodeSettingsContainer.Clear();
            m_CurrentlyInspectedElementsCount = 0;

            try
            {
                bool anySelectables = false;
                foreach (var selectable in selection)
                {
                    if (selectable is IInspectable inspectable)
                    {
                        DrawInspectable(m_NodeSettingsContainer, inspectable);
                        m_CurrentlyInspectedElementsCount++;
                        anySelectables = true;
                    }
                    if (m_CurrentlyInspectedElementsCount == k_InspectorElementLimit)
                        m_NodeSettingsContainer.Add(m_MaxItemsMessageLabel);
                }
                if (anySelectables && forceNodeView)
                {
                    // Anything selectable in the graph (GraphSettings not included) is only ever interacted with through the
                    // Node Settings tab so we can make the assumption they want to see that tab
                    m_GraphInspectorView.Activate(m_GraphInspectorView.Q<TabButton>("NodeSettingsButton"));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (doesInspectorNeedUpdate)
                doesInspectorNeedUpdate = false;

            m_NodeSettingsContainer.MarkDirtyRepaint();
        }

        void DrawInspectable(
            VisualElement outputVisualElement,
            IInspectable inspectable,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            InspectorUtils.GatherInspectorContent(m_PropertyDrawerList, outputVisualElement, inspectable, TriggerInspectorUpdate, propertyDrawerToUse);
        }

        internal void HandleGraphChanges()
        {
            float timePassed = (float)(EditorApplication.timeSinceStartup % k_InspectorUpdateInterval);
            // Don't update for selections beyond a certain amount as they are no longer visible in the inspector past a certain point and only cost performance as the user performs operations
            if (timePassed < 0.01f && selection.Count < k_InspectorElementLimit && selection.Count != m_CurrentlyInspectedElementsCount)
                Update();
        }

        void TriggerInspectorUpdate()
        {
            Update();
        }

        // This should be implemented by any inspector class that wants to define its own GraphSettings
        // which for SG, is a representation of the settings in GraphData
        protected virtual void ShowGraphSettings_Internal(VisualElement contentContainer)
        {
            var graphEditorView = ParentView.GetFirstAncestorOfType<GraphEditorView>();
            if (graphEditorView == null)
                return;

            contentContainer.Clear();
            DrawInspectable(contentContainer, (IInspectable)ParentView, m_graphSettingsPropertyDrawer);
            contentContainer.MarkDirtyRepaint();
        }
    }

    public static class InspectorUtils
    {
        internal static void GatherInspectorContent(
            List<Type> propertyDrawerList,
            VisualElement outputVisualElement,
            IInspectable inspectable,
            Action propertyChangeCallback,
            IPropertyDrawer propertyDrawerToUse = null)
        {
            var dataObject = inspectable.GetObjectToInspect();
            if (dataObject == null)
                throw new NullReferenceException("DataObject returned by Inspectable is null!");

            var properties = inspectable.GetType().GetProperties(BindingFlags.Default | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (properties == null)
                throw new NullReferenceException("PropertyInfos returned by Inspectable is null!");

            foreach (var propertyInfo in properties)
            {
                var attribute = propertyInfo.GetCustomAttribute<InspectableAttribute>();
                if (attribute == null)
                    continue;

                var propertyType = propertyInfo.GetGetMethod(true).Invoke(inspectable, new object[] {}).GetType();

                if (IsPropertyTypeHandled(propertyDrawerList, propertyType, out var propertyDrawerTypeToUse))
                {
                    var propertyDrawerInstance = propertyDrawerToUse ??
                        (IPropertyDrawer)Activator.CreateInstance(propertyDrawerTypeToUse);
                    // Assign the inspector update delegate so any property drawer can trigger an inspector update if it needs it
                    propertyDrawerInstance.inspectorUpdateDelegate = propertyChangeCallback;
                    // Supply any required data to this particular kind of property drawer
                    inspectable.SupplyDataToPropertyDrawer(propertyDrawerInstance, propertyChangeCallback);
                    var propertyGUI = propertyDrawerInstance.DrawProperty(propertyInfo, dataObject, attribute);
                    outputVisualElement.Add(propertyGUI);
                }
            }
        }

        static bool IsPropertyTypeHandled(
            List<Type> propertyDrawerList,
            Type typeOfProperty,
            out Type propertyDrawerToUse)
        {
            propertyDrawerToUse = null;

            // Check to see if a property drawer has been registered that handles this type
            foreach (var propertyDrawerType in propertyDrawerList)
            {
                var typeHandledByPropertyDrawer = propertyDrawerType.GetCustomAttribute<SGPropertyDrawerAttribute>();
                // Numeric types and boolean wrapper types like ToggleData handled here
                if (typeHandledByPropertyDrawer.propertyType == typeOfProperty)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
                // Generics and Enumerable types are handled here
                else if (typeHandledByPropertyDrawer.propertyType.IsAssignableFrom(typeOfProperty))
                {
                    // Before returning it, check for a more appropriate type further
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
                // Enums are weird and need to be handled explicitly as done below as their runtime type isn't the same as System.Enum
                else if (typeHandledByPropertyDrawer.propertyType == typeOfProperty.BaseType)
                {
                    propertyDrawerToUse = propertyDrawerType;
                    return true;
                }
            }
            return false;
        }
    }
}
