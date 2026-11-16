using UnityEditor;
using UnityEngine;

namespace HierarchyDecorator
{
    public class ComponentItem
    {
        // --- Properties
        public Settings HDSettings => HierarchyDecorator.Settings;

        public Component Component { get; private set; }
        public ComponentType Type => GetComponentInfo(HDSettings);
        public bool IsNullComponent { get; private set; }

        public readonly bool IsBehaviour;
        private Behaviour behaviour;

        // - Visual

        public GUIContent Content => Type != null ? Type.Content : GUIContent.none;

        public string DisplayName => Type != null ? Type.DisplayName : string.Empty;
        public bool IsBuiltIn => Type != null && Type.IsBuiltIn;
        public bool HasToggle => Type != null && Type.HasToggle;

        public bool Active { get; private set; }

        // --- Constructor 

        public ComponentItem(Component component)
        {
            Component = component;
            IsNullComponent = component == null;

            IsBehaviour = Component is Behaviour;
            if (IsBehaviour)
            {
                behaviour = Component as Behaviour;
            }

            if (IsNullComponent)
            {
                return;
            }

            IsNullComponent = Type == null;
            Active = GetActiveState();
        }

        // --- Methods

        public bool IsValid()
        {
            return Component == null == IsNullComponent;
        }

        private ComponentType GetComponentInfo(Settings settings)
        {
            if (IsNullComponent)
                return null;

            var type = Component.GetType();
            if (settings.Components.TryGetComponent(type, out ComponentType c))
            {
                return c;
            }

            if (!settings.Components.TryGetCustomComponent(type, out c))
            {
                settings.Components.RegisterCustomComponent(Component);
                settings.Components.TryGetCustomComponent(type, out c);
            }

            return c;
        }

        private bool GetActiveState()
        {
            // Default as enabled 

            if (IsNullComponent || !HasToggle)
            {
                return true;
            }

            if (IsBehaviour)
            {
                return behaviour.enabled;
            }

            if (Type.ToggleProperty == null)
            {
                return true;
            }

            return (bool)Type.ToggleProperty.GetValue(Component);
        }

        public void ToggleActive()
        {
            SetActive(!Active);
        }

        public void SetActive(bool active)
        {
            if (!HasToggle)
            {
                return;
            }

            if (Active != active)
            {
                Active = active;
                Type.ToggleProperty.SetValue(Component, active);
                EditorUtility.SetDirty(Component.gameObject);
            }
        }

        public void UpdateActiveState()
        {
            if (IsNullComponent || !HasToggle)
            {
                return;
            }

            Active = GetActiveState();
        }
    }
}