#if !DISABLE_CRYPTOGRAPHY
using MLAPI.Cryptography;
#endif

namespace MLAPI.Data
{
    public class PendingClient
    {
        public uint ClientId;
        
#if !DISABLE_CRYPTOGRAPHY
        internal EllipticDiffieHellman KeyExchange;
#endif
        
        public byte[] AesKey;

        public State ConnectionState;

        public enum State
        {
            PendingHail,
            PendingConnection
        }
    }
}