using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    public interface IStatefulInterestNode<TObject>
    {
        void GetManagedObjects(IList<TObject> buffer);
    }
}
