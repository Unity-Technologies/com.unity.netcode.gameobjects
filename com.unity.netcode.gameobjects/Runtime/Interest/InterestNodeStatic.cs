using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    public class InterestNodeStatic<TObject> : IInterestNode<TObject>
    {
        public List<IInterestKernel<TObject>> InterestKernels = new List<IInterestKernel<TObject>>();

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
