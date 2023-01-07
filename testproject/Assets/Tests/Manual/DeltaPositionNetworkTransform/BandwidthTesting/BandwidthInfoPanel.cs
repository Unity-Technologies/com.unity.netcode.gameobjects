using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

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


    public class BandwidthInfoPanel : NetworkBehaviour
    {
        public BandwidthMover BandwidthMover;

        public List<InfoEntry> InfoEntries;



        public void OnUpdateHalfFloat(bool isOn)
        {
            if (BandwidthMover != null)
            {
                BandwidthMover.UseHalfFloatPrecision = isOn;
            }
        }

        public void OnUpdateQuatSync(bool isOn)
        {
            if (BandwidthMover != null)
            {
                BandwidthMover.UseQuaternionSynchronization = isOn;
            }
        }

        public void OnUpdateQuatComp(bool isOn)
        {
            if (BandwidthMover != null)
            {
                BandwidthMover.UseQuaternionCompression = isOn;
            }
        }

        private void UpdateTextInfo(ref InfoEntry infoEntry, ref Vector3 info)
        {
            infoEntry.Text.text  = $"{infoEntry.Prefix} ({info.x}, {info.y}, {info.z})";
        }

        private void OnGUI()
        {
            if (IsSpawned)
            {
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
