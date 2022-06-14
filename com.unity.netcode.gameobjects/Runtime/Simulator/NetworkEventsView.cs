using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Netcode.Simulator
{
    public class NetworkEventsView : VisualElement
    {
        const string UXML = "Packages/com.unity.netcode.gameobjects/Runtime/Simulator/NetworkEventsView.uxml";

        Button DisconnectButton => this.Q<Button>(nameof(DisconnectButton));
        Button LagSpikeButton => this.Q<Button>(nameof(LagSpikeButton));
        SliderInt LagSpikeDurationSlider => this.Q<SliderInt>(nameof(LagSpikeDurationSlider));

        public NetworkEventsView(INetworkEventsApi networkEventsApi)
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML).CloneTree(this);

            DisconnectButton.RegisterCallback<MouseUpEvent>(_ =>
            {
                if (networkEventsApi.IsDisabledBySimulator)
                {
                    networkEventsApi.TriggerReconnect();
                }
                else
                {
                    networkEventsApi.TriggerDisconnect();
                }
            });
            LagSpikeButton.RegisterCallback<MouseUpEvent>(
                _ => networkEventsApi.TriggerLagSpike(TimeSpan.FromMilliseconds(LagSpikeDurationSlider.value)));
            LagSpikeDurationSlider.RegisterCallback<ChangeEvent<int>>(evt => LagSpikeButton.SetEnabled(evt.newValue != 0));

            LagSpikeButton.SetEnabled(LagSpikeDurationSlider.value != 0);

            EditorApplication.update += () =>
            {
                DisconnectButton.text = networkEventsApi.IsDisabledBySimulator
                    ? "Re-enable Connection"
                    : "Disable Connection";
            };
        }
    }
}