using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    // this is a very basic interest node in terms of its storage.  It simply takes all the objects it is told to
    //  manage, stores them and runs its associated kernels over all of them.
    //
    // for example, if you'd like a Radius-based scheme, create one of these and then add a RadiusInterestKernel to it.
    //  On the other hand, a more sophisticated node would take the AddObject / RemoveObject calls and then store
    //  those object in more strategic ways - see the Odds / Evens scheme in the InterestTests
    public class InterestNodeStatic<TObject> : IInterestNode<TObject>
    {
        public List<IInterestKernel<TObject>> InterestKernels = new();

        // these are the objects under my purview
        protected HashSet<TObject> m_ManagedObjects;

        public InterestNodeStatic()
        {
            m_ManagedObjects = new HashSet<TObject>();
        }

        public void AddObject(TObject obj)
        {
            m_ManagedObjects.Add(obj);
        }

        public void RemoveObject(TObject obj)
        {
            m_ManagedObjects.Remove(obj);
        }

        public void QueryFor(TObject client, HashSet<TObject> results)
        {
            if (InterestKernels.Count > 0)
            {
                foreach (var obj in m_ManagedObjects)
                {
                    foreach (var ik in InterestKernels)
                    {
                        ik.QueryFor(client, obj, results);
                    }
                }
            }
            else
            {
                results.UnionWith(m_ManagedObjects);
            }
        }

        public void UpdateObject(TObject obj)
        {
        }
    }
}
