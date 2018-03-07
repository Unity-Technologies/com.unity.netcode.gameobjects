using MLAPI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MLAPI.NetworkingManagerComponents
{
    internal static class SyncedVarManager
    {
        internal static void Init()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly = Assembly.GetExecutingAssembly();
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].FullName == "Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null")
                {
                    assembly = assemblies[i];
                    break;
                }
            }
            IEnumerable<Type> types = from t in assembly.GetTypes()
                                      where t.IsClass && t.IsSubclassOf(typeof(NetworkedBehaviour))
                                      select t;
            List<Type> behaviourTypes = types.OrderBy(x => x.FullName).ToList();
            for (ushort i = 0; i < behaviourTypes.Count; i++)
            {
                FieldInfo networkedBehaviourId = behaviourTypes[i].GetField("networkedBehaviourId", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                networkedBehaviourId.SetValue(null, i);
            }
        }
    }
}
