using UnityEngine.UIElements;

namespace Unity.Netcode
{
    public static class VisualElementExtensions
    {
        public static void AddEventLifecycle(
            this VisualElement visualElement,
            EventCallback<AttachToPanelEvent> onAttach,
            EventCallback<DetachFromPanelEvent> onDetach)
        {
            visualElement.RegisterCallback(onAttach);
            visualElement.RegisterCallback(onDetach);
        }
    }
}
