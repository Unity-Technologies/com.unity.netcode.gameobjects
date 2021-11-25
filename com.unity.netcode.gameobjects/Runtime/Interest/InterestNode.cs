using System.Collections.Generic;

namespace Unity.Netcode.Interest
{
    public interface IInterestNode<TObject>
    {
        public void QueryFor(TObject client, HashSet<TObject> results);
        public void AddObject(TObject obj);
        public void RemoveObject(TObject obj);
        public void UpdateObject(TObject obj);
        public void AddAdditiveKernel(IInterestKernel<TObject> kernel);
        public void AddSubtractiveKernel(IInterestKernel<TObject> kernel);
    };

    public interface IInterestKernel<TObject>
    {
        public void QueryFor(TObject client, TObject obj, HashSet<TObject> results);
    }
}
