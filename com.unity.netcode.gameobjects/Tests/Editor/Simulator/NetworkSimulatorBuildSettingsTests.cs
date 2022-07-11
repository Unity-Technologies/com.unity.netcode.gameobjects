using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode.Editor;
using UnityEditor;
using UnityEditor.Build;

namespace Unity.Netcode.EditorTests.Simulator
{
    [TestFixture]
    [Explicit("Running these tests modifies compile flags and can trigger a recompile, " +
        "so should only be run when needed. " +
        "They are also pretty simple so don't need to be run at a all times.")]
    public class NetworkSimulatorBuildSettingsTests
    {
        private readonly IReadOnlyList<NetworkSimulatorBuildSymbol> k_AllBuildSymbols = new List<NetworkSimulatorBuildSymbol>()
        {
            NetworkSimulatorBuildSymbol.DisableInDevelop,
            NetworkSimulatorBuildSymbol.EnableInRelease
        };

        readonly Dictionary<NamedBuildTarget, string[]> m_BuildSettingsPerTarget
            = new Dictionary<NamedBuildTarget, string[]>();

        [OneTimeSetUp]
        public void SaveScriptingDefineSymbols()
        {
            m_BuildSettingsPerTarget.Clear();
            foreach (var target in NetworkSimulatorBuildSettings.k_AllBuildTargets)
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out string[] symbols);
                m_BuildSettingsPerTarget[target] = symbols;
            }
        }

        [OneTimeTearDown]
        public void RestoreScriptingDefineSymbols()
        {
            foreach (var (target, symbols) in m_BuildSettingsPerTarget)
            {
                PlayerSettings.SetScriptingDefineSymbols(target, symbols);
            }
        }

        [Test]
        public void When_NetworkSimulatorSymbolIsEnabled_ItIsEnabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetworkSimulatorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                Assert.IsTrue(NetworkSimulatorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsTrue(NetworkSimulatorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetworkSimulatorSymbolIsDisabled_ItIsDisabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetworkSimulatorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);
                Assert.IsFalse(NetworkSimulatorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsFalse(NetworkSimulatorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetworkSimulatorIsEnabledThenDisabled_ItIsDisabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetworkSimulatorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                NetworkSimulatorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);

                Assert.IsFalse(NetworkSimulatorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsFalse(NetworkSimulatorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }

        [Test]
        public void When_NetworkSimulatorIsEnabledThenDisabledThenEnabled_ItIsEnabled()
        {
            foreach (var symbol in k_AllBuildSymbols)
            {
                NetworkSimulatorBuildSettings.AddSymbolToAllBuildTargets(symbol);
                NetworkSimulatorBuildSettings.RemoveSymbolFromAllBuildTargets(symbol);
                NetworkSimulatorBuildSettings.AddSymbolToAllBuildTargets(symbol);

                Assert.IsTrue(NetworkSimulatorBuildSettings.GetEnabledForAnyBuildTargets(symbol));
                Assert.IsTrue(NetworkSimulatorBuildSettings.GetSymbolInAllBuildTargets(symbol));
            }
        }
    }
}
