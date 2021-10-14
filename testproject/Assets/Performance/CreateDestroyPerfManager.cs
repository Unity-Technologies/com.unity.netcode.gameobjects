using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Netcode;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class CreateDestroyPerfManager : MonoBehaviour
{
    public GameObject Dummy;
    public GameObject DummyNetworked;

    private IEnumerator Start()
    {
        const int iterations = 5000;
        var spawned = new List<GameObject>(iterations);

        NetworkManager.Singleton.StartServer();

        {
            var nonNetworkedCreateBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                spawned.Add(Instantiate(Dummy));
            }

            nonNetworkedCreateBenchmark.Stop();
            Debug.Log(nameof(nonNetworkedCreateBenchmark) + " " + nonNetworkedCreateBenchmark.ElapsedMilliseconds + " ms");
        }

        yield return new WaitForSeconds(5);

        {
            var nonNetworkedDestroyBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                Destroy(spawned[i]);
            }

            nonNetworkedDestroyBenchmark.Stop();
            Debug.Log(nameof(nonNetworkedDestroyBenchmark) + " " + nonNetworkedDestroyBenchmark.ElapsedMilliseconds + " ms");

            spawned.Clear();
        }

        yield return new WaitForSeconds(5);

        {
            var networkedCreateUnspawnedCreateBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                GameObject go = Instantiate(DummyNetworked);
                // GetComponent is semi heavy, do it to minimize differences
                go.GetComponent<NetworkObject>();
                spawned.Add(go);
            }

            networkedCreateUnspawnedCreateBenchmark.Stop();
            Debug.Log(nameof(networkedCreateUnspawnedCreateBenchmark) + " " + networkedCreateUnspawnedCreateBenchmark.ElapsedMilliseconds + " ms");
        }

        yield return new WaitForSeconds(5);

        {
            var networkedCreateUnspawnedDestroyBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                Destroy(spawned[i]);
            }

            networkedCreateUnspawnedDestroyBenchmark.Stop();
            Debug.Log(nameof(networkedCreateUnspawnedDestroyBenchmark) + " " + networkedCreateUnspawnedDestroyBenchmark.ElapsedMilliseconds + " ms");

            spawned.Clear();
        }

        yield return new WaitForSeconds(5);

        {
            var networkedCreateSpawnedCreateBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                GameObject go = Instantiate(DummyNetworked);
                go.GetComponent<NetworkObject>().Spawn();
                spawned.Add(go);
            }

            networkedCreateSpawnedCreateBenchmark.Stop();
            Debug.Log(nameof(networkedCreateSpawnedCreateBenchmark) + " " + networkedCreateSpawnedCreateBenchmark.ElapsedMilliseconds + " ms");
        }

        yield return new WaitForSeconds(5);

        {
            var networkedCreateSpawnedDestroyBenchmark = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                Destroy(spawned[i]);
            }

            networkedCreateSpawnedDestroyBenchmark.Stop();
            Debug.Log(nameof(networkedCreateSpawnedDestroyBenchmark) + " " + networkedCreateSpawnedDestroyBenchmark.ElapsedMilliseconds + " ms");

            spawned.Clear();
        }

        /*

        var networkedUnspawned1KBenchmark = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            Instantiate(Dummy);
        }

        networkedUnspawnedCreateBenchmark.Stop();
        Debug.Log("networkedUnspawned1KBenchmark " + networkedUnspawned1KBenchmark.ElapsedMilliseconds + " ms");

        var networkedSpawned1KBenchmark = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            Instantiate(Dummy);
        }

        networkedSpawned1KBenchmark.Stop();
        Debug.Log("networkedSpawned1KBenchmark " + networkedSpawned1KBenchmark.ElapsedMilliseconds + " ms");
        */

        /*
        while (NetworkManager.Singleton.ConnectedClients.Count != 1)
        {
            // Wait for 1 player
            yield return new WaitForSeconds(5);
            Debug.Log("Waiting for 1 player to be connected");
        }
        */

        yield return null;
    }
}
