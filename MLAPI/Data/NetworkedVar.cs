using MLAPI.Data;
using MLAPI.NetworkingManagerComponents.Binary;
using MLAPI.NetworkingManagerComponents.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using MLAPI.MonoBehaviours.Core;

namespace MLAPI.Data
{
    /// <summary>
    /// A variable that can be synchronized over the network.
    /// </summary>
    public class NetworkedVar<T> : INetworkedVar
    {
        public delegate void OnValueChangedByRemoteDelegate(T newValue);
        public OnValueChangedByRemoteDelegate OnValueChangedByRemote;
        private NetworkedBehaviour networkedBehaviour;

        internal NetworkedVar()
        {
        }

        private T InternalValue;
        public T Value
        {
            get
            {
                return InternalValue;
            }
            set
            {
                if (!EqualityComparer<T>.Default.Equals(InternalValue, value)) // Note: value types of T should implement IEquatable to avoid boxing by default comparer
                {
                    BitWriter writer = BitWriter.Get();
                    FieldTypeHelper.WriteFieldType(writer, value, InternalValue);
                    InternalValue = value;
                    networkedBehaviour.SendNetworkedVar(this, writer);
                    MonoBehaviour.print("sending networked var to remote");
                }
            }
        }

        void INetworkedVar.SetNetworkedBehaviour(NetworkedBehaviour behaviour)
        {
            networkedBehaviour = behaviour;
        }

        void INetworkedVar.HandleValueChangedByRemote(BitReader reader)
        {
            // TODO TwoTen - Boxing sucks
            T newValue = (T)FieldTypeHelper.ReadFieldType(reader, typeof(T), (object)InternalValue);
            if (!EqualityComparer<T>.Default.Equals(InternalValue, newValue)) // Note: value types of T should implement IEquatable to avoid boxing by default comparer
                                                                              // Could allow a non default comparer to be specified by the user for this case?
            {
                InternalValue = newValue;
                OnValueChangedByRemote(Value);
            }
            MonoBehaviour.print("value received from remote");
        }
    }

    internal interface INetworkedVar
    {
        void HandleValueChangedByRemote(BitReader reader);
        void SetNetworkedBehaviour(NetworkedBehaviour behaviour);
    }
}
