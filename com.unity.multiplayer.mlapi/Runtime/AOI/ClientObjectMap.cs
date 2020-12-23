using System.Collections.Generic;
using NUnit.Framework;

// Although this is (currently) inside the MLAPI package, it is intentionally
//  totally decoupled from MLAPI with the intention of allowing it to live
//  in its own package
namespace MLAPI.AOI
{
    // To establish a Client Object Map, instantiate a ClientObjMapNodeBase, then
    //  add more nodes to it (and those nodes) as desired
   public class ClientObjMapNode<CLIENT, OBJECT>
   {
        // set this delegate if you want a function called when
        //  object 'obj' is being de-spawned
        public delegate void DespawnDelegate(OBJECT obj);
        public DespawnDelegate OnDespawn;

        // to dynamically compute objects to be added each 'QueryFor' call,
        //  assign this delegate to your handler
        public delegate void QueryDelegate(CLIENT c, HashSet<OBJECT> results);
        public QueryDelegate OnQuery;

        public delegate void BypassDelegate(HashSet<OBJECT> results);
        public BypassDelegate OnBypass;

        public ClientObjMapNode() : base()
        {
            ChildNodes = new List<ClientObjMapNode<CLIENT, OBJECT>>();
        }

        // externally-called object query function.  Call this on your root
        //  ClientObjectMapNode.  The passed-in hash set will contain the results.
        public void QueryFor(CLIENT client, HashSet<OBJECT> results)
        {
            if (Bypass)
            {
                OnBypass(results);
            }
            else
            {
                if (OnQuery != null)
                {
                    OnQuery(client, results);
                }

                foreach (var c in ChildNodes)
                {
                    c.QueryFor(client, results);
                }
            }
        }

        // Called when a given object is about to be despawned.  The OnDespawn
        //  delegate gives each node a chance to do its own handling (e.g. removing
        //  the object from a cache)
        public void DespawnCleanup(OBJECT o)
        {
            if (OnDespawn != null)
            {
                OnDespawn(o);
            }

            foreach (var c in ChildNodes)
            {
                c.DespawnCleanup(o);
            }
        }

        // Add a new child node.  Currently, there is no way to remove a node
        public void AddNode(ClientObjMapNode<CLIENT, OBJECT> @new)
        {
            ChildNodes.Add(@new);
        }

        private List<ClientObjMapNode<CLIENT, OBJECT>> ChildNodes;
        public bool Bypass = false;
   }

   // Static node type.  Objects can be added / removed as desired.
   //  When the Query is done, these objects are grafted in without
   //  any per-object computation.
   public class ClientObjMapNodeStatic<CLIENT, OBJECT> : ClientObjMapNode<CLIENT, OBJECT>
    {
        public ClientObjMapNodeStatic() : base()
        {
            alwaysRelevant = new HashSet<OBJECT>();

            // when we are told an object is despawning, remove it from our list
            OnDespawn = delegate(OBJECT o)
            {
                alwaysRelevant.Remove(o);
            };

            // for our query, we simply union our static objects with the results
            //  more sophisticated methods might be explored later, like having the results
            //  list contain not ust
            OnQuery = delegate(CLIENT client, HashSet<OBJECT> results)
            {
                results.UnionWith(alwaysRelevant);
            };
        }

        // Add a new item to our static list
        public void Add(OBJECT o) // ref?
        {
            alwaysRelevant.Add(o);
        }

        public void Remove(OBJECT o)
        {
            alwaysRelevant.Remove(o);
        }

        private HashSet<OBJECT> alwaysRelevant;
    }
}
