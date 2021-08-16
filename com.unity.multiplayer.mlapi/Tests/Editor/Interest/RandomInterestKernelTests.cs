using System.Collections.Generic;
using MLAPI.RuntimeTests;
using NUnit.Framework;
using UnityEngine;

namespace MLAPI.EditorTests.Interest
{
    public class RandomInterestKernelTests
    {

        /// <summary>
        /// We need to make sure our tests are deterministic, so seeding Random.
        /// Would be nice if in the future RandomInterestKernel takes an interface or delegate for random.
        /// </summary>
        [SetUp]
        public void InitRandom()
        {
            Random.InitState(2);
            // first .value will be 0.414878547
            // second .value will be 0.0996678025
            // third .value will be 0.851285815
        }

        [Test]
        public void RandomInterestShouldAddObjectToResultsWhenRandomMeetsDropRate()
        {
            // Arrange
            var randomKernel = ScriptableObject.CreateInstance<RandomInterestKernel>();
            randomKernel.DropRate = 0.2f; // 0.4148785 > 0.2, should pass interest test
            // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
            var no = new GameObject().AddComponent<NetworkObject>();
            no.name = "FOO" ;
            var results = new HashSet<NetworkObject>();

            // Act
            randomKernel.QueryFor(null, no, results);

            // Assert
            Assert.IsNotEmpty(results);
            Assert.That(results, Has.Exactly(1).Matches<NetworkObject>(x => x.name == no.name));
        }

        [Test]
        public void RandomInterestShouldNotAddObjectToResultsWhenRandomMissesDropRate()
        {
            // Arrange
            var randomKernel = ScriptableObject.CreateInstance<RandomInterestKernel>();
            randomKernel.DropRate = 0.5f; // 0.4148785 < 0.5, should fail interest test
            // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
            var no = new GameObject().AddComponent<NetworkObject>();
            var results = new HashSet<NetworkObject>();

            // Act
            randomKernel.QueryFor(null, no, results);

            // Assert
            Assert.IsEmpty(results);
        }

        [Test]
        [TestCase(0.2f, Description = "Object fails test, is not added, results should not be modified")]
        [TestCase(0.5f, Description = "Object passes test, is added, should not modify existing result entries")]
        public void RandomInterestShouldNotOverwriteResults(float dropRate)
        {
            // Arrange
            var randomKernel = ScriptableObject.CreateInstance<RandomInterestKernel>();
            randomKernel.DropRate = dropRate;
            // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
            var firstObj = new GameObject().AddComponent<NetworkObject>();
            var no = new GameObject().AddComponent<NetworkObject>();
            var results = new HashSet<NetworkObject> { firstObj };

            // Act
            randomKernel.QueryFor(null, no, results);

            // Assert
            Assert.IsNotEmpty(results);
            CollectionAssert.Contains(results, firstObj);
        }

        [Test]
        public void RandomInterestCanBeCalledMoreThanOnce()
        {
            // Arrange
            var randomKernel = ScriptableObject.CreateInstance<RandomInterestKernel>();
            randomKernel.DropRate = .7f; // first 2 calls should "fail", 3rd call should pass interest test
            // ReSharper disable once Unity.IncorrectMonoBehaviourInstantiation
            var no1 = new GameObject().AddComponent<NetworkObject>();
            var no2 = new GameObject().AddComponent<NetworkObject>();
            var no3 = new GameObject().AddComponent<NetworkObject>();
            var results = new HashSet<NetworkObject>();

            // Act
            randomKernel.QueryFor(null, no1, results); // not added
            randomKernel.QueryFor(null, no2, results); // not added
            randomKernel.QueryFor(null, no3, results); // added

            // Assert
            CollectionAssert.AreEquivalent(new HashSet<NetworkObject> { no3 }, results);
        }
    }
}
