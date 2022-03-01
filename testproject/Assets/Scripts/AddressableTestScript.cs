using Unity.Netcode;

namespace DefaultNamespace
{
    public class AddressableTestScript : NetworkBehaviour
    {
        public int AnIntVal;
        public string AStringVal;

        public string GetValue()
        {
            return AStringVal + AnIntVal.ToString();
        }
    }
}
