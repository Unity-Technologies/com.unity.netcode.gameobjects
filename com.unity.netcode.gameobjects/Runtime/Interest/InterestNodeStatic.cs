using System;
using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    // this is a very basic interest node in terms of its storage.  It simply takes all the objects it is told to
    //  manage, stores them and runs its associated kernels over all of them.
    //
    // for example, if you'd like a Radius-based scheme, create one of these and then add a RadiusInterestKernel to it.
    //  On the other hand, a more sophisticated node would take the AddObject / RemoveObject calls and then store
    //  those object in more strategic ways - see the Odds / Evens scheme in the InterestTests
    public class InterestNodeStatic<TObject> : IInterestNode<TObject>, IStatefulInterestNode<TObject>
    {
        // these are the objects under my purview
        private HashSet<TObject> m_ManagedObjects;

        // these are the interest kernels that we will run on the objects under my purview
        private List<Tuple<bool, IInterestKernel<TObject>>> m_InterestKernels;

        // these are the result sets that correspond to each of the kernels I'll run.
        //  they are then reduced
        private List<HashSet<TObject>> m_ResultSets;

        public InterestNodeStatic()
        {
            m_InterestKernels = new List<Tuple<bool, IInterestKernel<TObject>>>();
            m_ManagedObjects = new HashSet<TObject>();
            m_ResultSets = new List<HashSet<TObject>>();
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
            if (m_InterestKernels.Count > 0)
            {
                // run all the kernels.  We don't care whether they are additive or
                //  subtractive...yet
                for (var i = 0; i < m_InterestKernels.Count; i++)
                {
                    var thisKernel = m_InterestKernels[i].Item2;
                    var theseResults = m_ResultSets[i];
                    theseResults.Clear();
                    foreach (var obj in m_ManagedObjects)
                    {
                        if (thisKernel.QueryFor(client, obj))
                        {
                            theseResults.Add(obj);
                        }
                    }
                }
                // reduce.  Note, order is important to support subtractive results
                for (var i = 0; i < m_InterestKernels.Count; i++)
                {
                    // additive
                    if (m_InterestKernels[i].Item1)
                    {
                        results.UnionWith(m_ResultSets[i]);
                    }
                    // subtractive
                    else
                    {
                        results.ExceptWith(m_ResultSets[i]);
                    }
                }
            }
            else
            {
                results.UnionWith(m_ManagedObjects);
            }
        }

        public void AddAdditiveKernel(IInterestKernel<TObject> kernel)
        {
            m_ResultSets.Add(new HashSet<TObject>());
            m_InterestKernels.Add(new Tuple<bool, IInterestKernel<TObject>>(true, kernel));
        }

        public void AddSubtractiveKernel(IInterestKernel<TObject> kernel)
        {
            m_ResultSets.Add(new HashSet<TObject>());
            m_InterestKernels.Add(new Tuple<bool, IInterestKernel<TObject>>(false, kernel));
        }

        public void UpdateObject(TObject obj)
        {
        }

        public void GetManagedObjects(IList<TObject> buffer)
        {
            foreach (var obj in m_ManagedObjects)
            {
                buffer.Add(obj);
            }
        }
    }
}
