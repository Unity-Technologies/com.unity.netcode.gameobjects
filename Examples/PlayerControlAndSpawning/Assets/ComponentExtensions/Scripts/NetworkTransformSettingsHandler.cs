using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.UI;

public class NetworkTransformSettingsHandler : NetworkBehaviour
{
    public bool SessionOwnerControls = true;
    public KeyCode IncreaseTickBufferOffset = KeyCode.RightBracket;
    public KeyCode DecreaseTickBufferOffset = KeyCode.LeftBracket;
    public KeyCode ToggleSmoothLerp = KeyCode.Backslash;
    private float m_Smoothing;
    [SerializeField]
    private Slider m_SmoothingValue;
    [SerializeField]
    private Text m_SmoothingRateText;
    [SerializeField]
    private Canvas m_SmoothingUICanvas;
    [SerializeField]
    private Dropdown m_DropDown;
    [SerializeField]
    private Button m_ClientCanControlButton;
    [SerializeField]
    private Text m_ClientCanControlText;
    [SerializeField]
    private Text m_SmoothingEnabledText;

    private NetworkVariable<float> m_SmoothingRate = new NetworkVariable<float>(0.1f);
    private NetworkVariable<int> m_InterpolationType = new NetworkVariable<int>(1);
    private NetworkVariable<bool> m_ClientCanControl = new NetworkVariable<bool>();
    private NetworkVariable<bool> m_SmoothLerp = new NetworkVariable<bool>();

    private float m_LastUpdatedTime;
    private bool m_SmoothingUpdated;

    private bool m_SmoothLerpIsEnabled = true;

    private Dictionary<string, NetworkTransform.InterpolationTypes> m_InterpolationTypesTable = new Dictionary<string, NetworkTransform.InterpolationTypes>();

    private void Awake()
    {
        m_SmoothingUICanvas.gameObject.SetActive(false);
        var types = Enum.GetNames(typeof(NetworkTransform.InterpolationTypes));
        var options = new List<Dropdown.OptionData>();
        foreach (var type in types)
        {
            options.Add(new Dropdown.OptionData(type.ToString()));
            m_InterpolationTypesTable.Add(type.ToString(), Enum.Parse<NetworkTransform.InterpolationTypes>(type));
        }
        m_DropDown.ClearOptions();
        m_DropDown.AddOptions(options);
        m_DropDown.gameObject.SetActive(false);
    }

    private bool m_PostSpawn;
    protected override void OnNetworkPostSpawn()
    {
        m_PostSpawn = true;
        AuthorityTrackedNetworkTransform.InstanceSpawned += OnInstanceSpawned;

        ConfigureControls(IsSessionOwner);

        NetworkManager.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
        if (!IsSessionOwner)
        {
            m_ClientCanControl.OnValueChanged += OnClientCanControlChanged;
        }
        base.OnNetworkPostSpawn();
    }

    public override void OnNetworkDespawn()
    {
        m_PostSpawn = false;
        m_SmoothingUICanvas.gameObject.SetActive(false);
        m_SmoothingValue.onValueChanged.RemoveAllListeners();
        m_DropDown.onValueChanged.RemoveAllListeners();
        m_ClientCanControlButton.onClick.RemoveAllListeners();
        m_ClientCanControl.OnValueChanged -= OnClientCanControlChanged;
        NetworkManager.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        m_SmoothingRate.OnValueChanged -= OnSmootRateValueChanged;
        m_InterpolationType.OnValueChanged -= OnInterpolationTypeChanged;
        StopAllCoroutines();
        base.OnNetworkDespawn();
    }

    private void OnClientCanControlChanged(bool previous, bool current)
    {
        ConfigureControls();
    }

    private bool CanInteractWithControls()
    {
        if (!m_ClientCanControl.Value)
        {
            return IsSessionOwner;
        }
        return true;
    }

    private void ConfigureControls(bool initialize = false)
    {
        if ((!m_PostSpawn || !IsSpawned) && !initialize)
        {
            return;
        }
        Debug.Log($"ConfigurControls invoked.");
        if (IsSessionOwner)
        {
            m_ClientCanControl.OnValueChanged -= OnClientCanControlChanged;
            if (initialize)
            {
                m_ClientCanControl.Value = !SessionOwnerControls;
            }
            m_ClientCanControlButton.onClick.RemoveAllListeners();
            m_ClientCanControlButton.onClick.AddListener(delegate { OnClientCanControlClicked(); });
        }
        m_SmoothingValue.onValueChanged.RemoveAllListeners();
        m_DropDown.onValueChanged.RemoveAllListeners();
        m_SmoothingUICanvas.gameObject.SetActive(true);
        m_DropDown.gameObject.SetActive(true);
        m_SmoothingValue.enabled = CanInteractWithControls();
        m_DropDown.interactable = CanInteractWithControls();
        m_SmoothingValue.interactable = CanInteractWithControls();
        m_ClientCanControlButton.interactable = IsSessionOwner;
        AuthorityTrackedNetworkTransform.UpdateSmoothLerp(m_SmoothingRate.Value);
        StopAllCoroutines();
        if (CanInteractWithControls())
        {
            m_SmoothingRate.OnValueChanged -= OnSmootRateValueChanged;
            m_InterpolationType.OnValueChanged -= OnInterpolationTypeChanged;
            m_SmoothLerp.OnValueChanged -= OnSmoothLerpChanged;
            m_SmoothingValue.onValueChanged.AddListener(delegate { SmoothRateSliderValueChanged(); });
            StartCoroutine(OnSmoothingChanged());
            m_Smoothing = m_SmoothingValue.value;
            SetInterpolationType(m_InterpolationType.Value);
            if (initialize)
            {
                m_DropDown.value = (int)NetworkTransform.InterpolationTypes.SmoothDampening;
            }
            m_DropDown.enabled = true;
            m_DropDown.onValueChanged.AddListener(delegate { OnInterpolationTypeChanged(); });
        }
        else
        {
            m_SmoothingRate.OnValueChanged -= OnSmootRateValueChanged;
            m_InterpolationType.OnValueChanged -= OnInterpolationTypeChanged;
            m_SmoothLerp.OnValueChanged -= OnSmoothLerpChanged;
            m_SmoothingRate.OnValueChanged += OnSmootRateValueChanged;
            m_InterpolationType.OnValueChanged += OnInterpolationTypeChanged;
            m_SmoothLerp.OnValueChanged += OnSmoothLerpChanged;
            SetInterpolationType(m_InterpolationType.Value);
            m_Smoothing = m_SmoothingRate.Value;
            m_SmoothLerpIsEnabled = m_SmoothLerp.Value;
        }
        UpdateSmoothRate(m_Smoothing);
        UpdateSmoothLerp();
        UpdateClientCanControlText();
    }

    private void UpdateSmoothLerp()
    {
        if (IsSessionOwner)
        {
            m_SmoothLerp.Value = m_SmoothLerpIsEnabled;
        }
        m_SmoothingEnabledText.text = m_SmoothLerpIsEnabled ? "Sm-Lerp: On" : "Sm-Lerp: Off";
        AuthorityTrackedNetworkTransform.ToggleSmoothLerp(m_SmoothLerpIsEnabled);
    }

    private void OnSmoothLerpChanged(bool previous, bool current)
    {
        m_SmoothLerpIsEnabled = current;
        UpdateSmoothLerp();
    }

    private void OnClientCanControlClicked()
    {
        m_ClientCanControl.Value = !m_ClientCanControl.Value;
        ConfigureControls();
    }

    private void UpdateClientCanControlText()
    {
        m_ClientCanControlText.text = m_ClientCanControl.Value ? "Client - Controls" : "Session Owner";
    }

    private void OnInstanceSpawned(AuthorityTrackedNetworkTransform instance)
    {
        instance.PositionLerpSmoothing = m_SmoothLerpIsEnabled;
        instance.PositionInterpolationType = GetInterpolationType(m_InterpolationType.Value);
        instance.PositionMaxInterpolationTime = m_SmoothingRate.Value;
    }

    private NetworkTransform.InterpolationTypes GetInterpolationType(int index)
    {
        if (m_DropDown.options.Count > index)
        {
            var name = m_DropDown.options[index].text;
            if (m_InterpolationTypesTable.ContainsKey(name))
            {
                return m_InterpolationTypesTable[name];
            }
        }
        Debug.LogError($"[GetInterpolationType] Failed to find index {index} within interpolation type drop down box!");
        return NetworkTransform.InterpolationTypes.SmoothDampening;
    }

    private void OnInterpolationTypeChanged()
    {
        if (HasAuthority)
        {
            m_InterpolationType.Value = m_DropDown.value;
        }

        AuthorityTrackedNetworkTransform.ChangeInterplationType(GetInterpolationType(m_DropDown.value));
    }

    private void SetInterpolationType(int index)
    {
        m_DropDown.value = index;
        AuthorityTrackedNetworkTransform.ChangeInterplationType(GetInterpolationType(index));
    }

    private void OnInterpolationTypeChanged(int previous, int current)
    {
        SetInterpolationType(current);
    }

    private void OnSessionOwnerPromoted(ulong sessionOwner)
    {
        ConfigureControls();
    }

    private void OnSmootRateValueChanged(float previous, float current)
    {
        UpdateSmoothRate(current);
    }

    private void UpdateSmoothRate(float rate)
    {
        m_SmoothingValue.value = rate;
        m_SmoothingRateText.text = $"{rate:F2}";
        AuthorityTrackedNetworkTransform.UpdateSmoothLerp(rate);
    }

    private void SmoothRateSliderValueChanged()
    {
        if (m_SmoothingValue.value != m_Smoothing)
        {
            m_Smoothing = m_SmoothingValue.value;
            m_SmoothingRateText.text = $"{m_Smoothing:F2}";
            if (!m_SmoothingUpdated)
            {
                // Flag the last time this value updated for the OwnerMonitorPingRateChange coroutine
                m_LastUpdatedTime = Time.realtimeSinceStartup;
                m_SmoothingUpdated = true;
            }
        }
    }

    private IEnumerator OnSmoothingChanged()
    {
        var waitForOneSecond = new WaitForSeconds(1.0f);
        var changeWaitPeriod = new WaitForSeconds(0.1f);
        var continueToMonitor = true;
        var networkManager = NetworkManager;

        while (continueToMonitor)
        {
            if (!m_SmoothingUpdated)
            {
                yield return waitForOneSecond;
            }
            else
            {
                yield return changeWaitPeriod;
            }

            // Terminate if shutting down, the local client is disconnected, or the targeted client is no longer connected
            continueToMonitor = !networkManager.ShutdownInProgress && networkManager.IsConnectedClient && IsSpawned && CanInteractWithControls();
            if (!continueToMonitor)
            {
                continue;
            }

            if (!m_SmoothingUpdated)
            {
                continue;
            }

            // Just continue to wait for any slider changes to stop before
            // triggering an update to non-authority instances and resetting
            // the inbound client queue
            if ((m_LastUpdatedTime + 0.25f) > Time.realtimeSinceStartup)
            {
                continue;
            }

            // Unset this flag
            m_SmoothingUpdated = false;

            // If the user slide the slider around but landed back to the same
            // value we were already on, then just ignore this change update
            if (m_SmoothingRate.Value == m_Smoothing)
            {
                continue;
            }

            if (HasAuthority)
            {
                // Update remote clients
                m_SmoothingRate.Value = m_Smoothing;
            }

            // Update locally on the session owner side
            UpdateSmoothRate(m_Smoothing);
        }
        yield break;
    }

    private void UpdateInterpolationType(bool isIncrement)
    {
        var currentIndex = m_DropDown.value;
        var nextIndex = (isIncrement ? currentIndex + 1 : currentIndex - 1) % m_DropDown.options.Count;
        m_DropDown.value = nextIndex;
    }

    private void Update()
    {
        if (!IsSpawned)
        {
            return;
        }

        var interpolationBufferTickOffset = NetworkTransform.InterpolationBufferTickOffset;
        if (Input.GetKeyDown(IncreaseTickBufferOffset))
        {
            NetworkTransform.InterpolationBufferTickOffset = Mathf.Min(NetworkTransform.InterpolationBufferTickOffset + 1, 6);
        }
        if (Input.GetKeyDown(DecreaseTickBufferOffset))
        {
            NetworkTransform.InterpolationBufferTickOffset = Mathf.Max(NetworkTransform.InterpolationBufferTickOffset - 1, 0);
        }

        if (interpolationBufferTickOffset != NetworkTransform.InterpolationBufferTickOffset)
        {
            ExtendedNetworkManager.Instance.LogMessage($"Tick offset changed from {interpolationBufferTickOffset} to {NetworkTransform.InterpolationBufferTickOffset}.");
        }


        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            AuthorityTrackedNetworkTransform.DespawnOwnedObjects();
        }

        // Exit early if this instance cannot interact with the controls at this time.
        if (!CanInteractWithControls())
        {
            return;
        }

        if (Input.GetKeyDown(ToggleSmoothLerp))
        {
            m_SmoothLerpIsEnabled = !m_SmoothLerpIsEnabled;
            UpdateSmoothLerp();
        }

        if (Input.GetKeyDown(KeyCode.Comma)) 
        {
            UpdateInterpolationType(false);
        }
        else if (Input.GetKeyDown(KeyCode.Period)) 
        {
            Debug.Log($"Client-{NetworkManager.LocalClientId} Pressed Key Period.");
            UpdateInterpolationType(true);
        }
    }
}
