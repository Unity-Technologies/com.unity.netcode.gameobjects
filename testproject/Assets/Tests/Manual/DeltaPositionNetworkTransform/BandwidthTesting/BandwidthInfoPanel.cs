using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Multiplayer.Tools.NetStats;
using Unity.Multiplayer.Tools.NetStatsMonitor;

namespace TestProject.ManualTests
{
    [Serializable]
    public class InfoEntry
    {
        public enum InfoEntryTypes
        {
            Direciton,
            Position,
            Rotation
        }

        public Text Text;
        public string Prefix;
        public InfoEntryTypes InfoEntryType;
    }

    [Serializable]
    public class ToggleEntry
    {
        public enum ToggleEntryTypes
        {
            HalfFloat,
            QuatSynch,
            QuatComp
        }

        public Toggle Toggle;
        public ToggleEntryTypes ToggleEntryType;
    }

    [MetricTypeEnum(DisplayName = "NetworkTransformStats")]
    public enum NetworkTransformStats
    {
        [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Gauge)]
        BytesPerUpdate,
        [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Counter)]
        BytesPerSecond,
        [MetricMetadata(Units = Units.Bytes, MetricKind = MetricKind.Gauge)]
        BPSNumeric,
    }


    public class BandwidthInfoPanel : NetworkBehaviour
    {
        public GameObject Root;
        public BandwidthMover BandwidthMover;

        public List<InfoEntry> InfoEntries;

        public List<ToggleEntry> ToggleEntries;

        private RuntimeNetStatsMonitor m_RuntimeNetStatsMonitor;
        private Image m_Background;

        private int m_SizeOfNetworkTransform;
        private float m_NetworkTickPeriod;

        private void Start()
        {
            m_RuntimeNetStatsMonitor = FindObjectOfType<RuntimeNetStatsMonitor>();
            m_RuntimeNetStatsMonitor.enabled = false;
            BandwidthMover.NotifySerializedSize = NotifySerializedSize;
            m_Background = GetComponent<Image>();
            m_Background.enabled = false;
            Root.SetActive(false);
        }

        private void Update()
        {
            if (IsSpawned)
            {
                m_RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(NetworkTransformStats.BPSNumeric), m_SizeOfNetworkTransform * 30);
                m_RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(NetworkTransformStats.BytesPerUpdate), m_SizeOfNetworkTransform);
            }
        }

        private void NotifySerializedSize(int size)
        {
            m_SizeOfNetworkTransform = size;
            m_RuntimeNetStatsMonitor.AddCustomValue(MetricId.Create(NetworkTransformStats.BytesPerSecond), m_SizeOfNetworkTransform);
        }

        public void OnUpdateHalfFloat(bool isOn)
        {
            if (BandwidthMover != null && IsServer)
            {
                BandwidthMover.UseHalfFloatPrecision = isOn;
            }
        }

        public void OnUpdateQuatSync(bool isOn)
        {
            if (BandwidthMover != null && IsServer)
            {
                BandwidthMover.UseQuaternionSynchronization = isOn;
            }
        }

        public void OnUpdateQuatComp(bool isOn)
        {
            if (BandwidthMover != null && IsServer)
            {
                BandwidthMover.UseQuaternionCompression = isOn;
            }
        }

        private void UpdateTextInfo(ref InfoEntry infoEntry, ref Vector3 info)
        {
            infoEntry.Text.text  = $"{infoEntry.Prefix} ({info.x}, {info.y}, {info.z})";
        }

        public override void OnNetworkSpawn()
        {
            m_Background.enabled = true;
            Root.SetActive(true);
            m_NetworkTickPeriod = 1.0f / NetworkManager.NetworkConfig.TickRate;
            m_RuntimeNetStatsMonitor.enabled = true;
            if (!IsServer)
            {
                UpdateClientSideToggles();
            }
            base.OnNetworkSpawn();
        }

        private void UpdateClientSideToggles()
        {
            foreach (var entry in ToggleEntries)
            {
                entry.Toggle.enabled = true;
                switch (entry.ToggleEntryType)
                {
                    case ToggleEntry.ToggleEntryTypes.HalfFloat:
                        {
                            entry.Toggle.isOn = BandwidthMover.UseHalfFloatPrecision;
                            break;
                        }
                    case ToggleEntry.ToggleEntryTypes.QuatSynch:
                        {
                            entry.Toggle.isOn = BandwidthMover.UseQuaternionSynchronization;
                            break;
                        }
                    case ToggleEntry.ToggleEntryTypes.QuatComp:
                        {
                            entry.Toggle.isOn = BandwidthMover.UseQuaternionCompression;
                            break;
                        }
                }
                entry.Toggle.enabled = false;
            }
        }

        private void OnGUI()
        {
            if (IsSpawned )
            {
                if (!BandwidthMover.CanCommitToTransform)
                {
                    UpdateClientSideToggles();
                }

                for(int i = 0; i <  InfoEntries.Count; i++)
                {
                    var infoEntry = InfoEntries[i];
                    var infoValue = Vector3.zero;
                    switch(infoEntry.InfoEntryType)
                    {
                        case InfoEntry.InfoEntryTypes.Direciton:
                            {
                                infoValue = BandwidthMover.Direction;
                                break;
                            }
                        case InfoEntry.InfoEntryTypes.Position:
                            {
                                infoValue = BandwidthMover.transform.position;
                                break;
                            }
                        case InfoEntry.InfoEntryTypes.Rotation:
                            {
                                infoValue = BandwidthMover.transform.eulerAngles;
                                break;
                            }
                    }
                    UpdateTextInfo(ref infoEntry,ref infoValue);
                }
            }
        }
    }
}
