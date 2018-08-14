using MLAPI.Cryptography;

namespace MLAPI.Data
{
    public class PendingClient
    {
        public uint ClientId;

        internal EllipticDiffieHellman KeyExchange;

        public byte[] AesKey;

        public State ConnectionState;

        public enum State
        {
            PendingHail,
            PendingConnection
        }
    }
}