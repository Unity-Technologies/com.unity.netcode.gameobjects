using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    public class NetworkEventsView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Editor/Simulator/NetworkEventsView.uxml";

        Button DisconnectButton => this.Q<Button>(nameof(DisconnectButton));
        Button LagSpikeButton => this.Q<Button>(nameof(LagSpikeButton));
        SliderInt LagSpikeDurationSlider => this.Q<SliderInt>(nameof(LagSpikeDurationSlider));

        INetworkEventsApi m_NetworkEventsApi;
        readonly NetworkSimulator m_NetworkSimulator;
        const string k_LagSpikeValueString = "LagSpikeValue";
        const string k_LagSpikeButtonStateString = "LagSpikeButtonState";

        public NetworkEventsView(NetworkSimulator networkSimulator)
        {
            m_NetworkSimulator = networkSimulator;

            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);
            LagSpikeDurationSlider.viewDataKey = k_LagSpikeValueString;
            LagSpikeButton.viewDataKey = k_LagSpikeButtonStateString;
            LagSpikeButton.SetEnabled(LagSpikeDurationSlider.value != 0);
            this.AddEventLifecycle(OnAttach, OnDetached);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            DisconnectButton.RegisterCallback<MouseUpEvent>(OnDisconnectMouseUp);
            LagSpikeButton.RegisterCallback<MouseUpEvent>(OnLagSpikeMouseUp);
            LagSpikeDurationSlider.RegisterCallback<ChangeEvent<int>>(OnLagSpikeChange);

            EditorApplication.update += OnEditorUpdate;
        }

        void OnDetached(DetachFromPanelEvent evt)
        {
            DisconnectButton.UnregisterCallback<MouseUpEvent>(OnDisconnectMouseUp);
            LagSpikeButton.UnregisterCallback<MouseUpEvent>(OnLagSpikeMouseUp);
            LagSpikeDurationSlider.UnregisterCallback<ChangeEvent<int>>(OnLagSpikeChange);

            EditorApplication.update -= OnEditorUpdate;
        }

        void OnLagSpikeChange(ChangeEvent<int> evt)
        {
            LagSpikeButton.SetEnabled(evt.newValue != 0);
        }

        void OnLagSpikeMouseUp(MouseUpEvent _)
        {
            m_NetworkEventsApi.TriggerLagSpike(TimeSpan.FromMilliseconds(LagSpikeDurationSlider.value));
        }

        void OnDisconnectMouseUp(MouseUpEvent _)
        {
            if (m_NetworkEventsApi.IsDisabledBySimulator)
            {
                m_NetworkEventsApi.TriggerReconnect();
            }
            else
            {
                m_NetworkEventsApi.TriggerDisconnect();
            }
        }

        void OnEditorUpdate()
        {
            m_NetworkEventsApi = m_NetworkSimulator.NetworkEventsApi;
            DisconnectButton.text = m_NetworkEventsApi.IsDisabledBySimulator
                ? "Re-enable Connection"
                : "Disable Connection";
        }
    }
}
