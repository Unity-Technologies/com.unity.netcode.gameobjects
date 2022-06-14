using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Editor
{
    public class NetworkEventsView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Editor/Simulator/NetworkEventsView.uxml";

        Button DisconnectButton => this.Q<Button>(nameof(DisconnectButton));
        Button ReconnectButton => this.Q<Button>(nameof(ReconnectButton));
        Button LagSpikeButton => this.Q<Button>(nameof(LagSpikeButton));
        SliderInt LagSpikeDurationSlider => this.Q<SliderInt>(nameof(LagSpikeDurationSlider));

        public NetworkEventsView(INetworkEventsApi networkEventsApi)
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            DisconnectButton.RegisterCallback<MouseUpEvent>(_ => networkEventsApi.TriggerDisconnect());
            ReconnectButton.RegisterCallback<MouseUpEvent>(_ => networkEventsApi.TriggerReconnect());
            LagSpikeButton.RegisterCallback<MouseUpEvent>(
                _ => networkEventsApi.TriggerLagSpike(TimeSpan.FromMilliseconds(LagSpikeDurationSlider.value)));
            LagSpikeDurationSlider.RegisterCallback<ChangeEvent<int>>(evt => LagSpikeButton.SetEnabled(evt.newValue != 0));

            LagSpikeButton.SetEnabled(LagSpikeDurationSlider.value != 0);
        }
    }
}
