using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Netcode.Editor
{
    class NetworkScenarioTypesLibrary
    {
        internal IList<Type> Types { get; private set; }

        internal NetworkScenarioTypesLibrary()
        {
            RefreshTypes();
        }

        internal INetworkSimulatorScenario GetInstanceForTypeName(string typeName)
        {
            var scenario = Types.First(x => x.Name == typeName);
            return (INetworkSimulatorScenario)Activator.CreateInstance(scenario);
        }
        
        internal void RefreshTypes()
        {
            Types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(TypeIsValidNetworkScenario)
                .ToList();
        }

        bool TypeIsValidNetworkScenario(Type type)
        {
            return type.IsClass && type.IsAbstract == false && typeof(INetworkSimulatorScenario).IsAssignableFrom(type);
        }
    }
}
