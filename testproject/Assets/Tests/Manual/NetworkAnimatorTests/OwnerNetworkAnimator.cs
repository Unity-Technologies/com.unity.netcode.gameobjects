using Unity.Netcode.Components;

namespace Tests.Manual.NetworkAnimatorTests
{
    public class OwnerNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
