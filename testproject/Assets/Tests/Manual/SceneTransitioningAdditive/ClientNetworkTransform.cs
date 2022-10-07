using Unity.Netcode.Components;

namespace TestProject.ManualTests
{
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
