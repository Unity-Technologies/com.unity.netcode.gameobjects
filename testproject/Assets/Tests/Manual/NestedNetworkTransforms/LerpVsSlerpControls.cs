using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TestProject.ManualTests
{
    public class LerpVsSlerpControls : NetworkBehaviour
    {
        public Toggle UseSlerpPositionToggle;
        public Slider Slider;
        public Text SliderValue;
        public Text SliderValueBack;
        public ChildMover ChildMover;
        public ChildMoverManager ChildMoverManager;

        private Color m_OriginalTextColor;

        private void Start()
        {
            m_OriginalTextColor = SliderValue.color;
            UseSlerpPositionToggle.gameObject.SetActive(false);
            Slider.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                UseSlerpPositionToggle.gameObject.SetActive(true);
                Slider.gameObject.SetActive(true);
                UseSlerpPositionToggle.onValueChanged.AddListener(new UnityAction<bool>(InterpolateToggleUpdated));
                Slider.onValueChanged.AddListener(new UnityAction<float>(SliderUpdated));
            }
            base.OnNetworkSpawn();
        }

        private void SliderUpdated(float value)
        {
            if (IsServer)
            {
                return;
            }
            if (value >= 30.0f)
            {
                SliderValue.color = Color.red;
            }
            else
            {
                SliderValue.color = m_OriginalTextColor;
            }
            SliderValue.text = $"{value}";
            SliderValueBack.text = $"{value}";
            OnSliderUpdatedServerRpc(value);
            UpdateRotationSpeed(value);
        }

        private void UpdateRotationSpeed(float speed)
        {
            ChildMover.RotationSpeed = speed;
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnSliderUpdatedServerRpc(float sliderValue)
        {
            UpdateRotationSpeed(sliderValue);
        }


        private void InterpolateToggleUpdated(bool state)
        {
            if (!IsServer)
            {
                OnInterpolateToggleUpdatedServerRpc(state);
            }
        }

        private void UpdateSlerPosition(bool isOn)
        {
            ChildMover.SlerpPosition = isOn;
        }

        [ServerRpc(RequireOwnership = false)]
        private void OnInterpolateToggleUpdatedServerRpc(bool toggleState)
        {
            UpdateSlerPosition(toggleState);
        }
    }
}
