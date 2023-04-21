using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace TestProject.ManualTests
{
    public class ChildPosition : NetworkBehaviour
    {
        public Text ChildText;
        public GameObject Child;

        private void Update()
        {
            if (!IsSpawned)
            {
                return;
            }

            ChildText.text = $"[{Child.name}] ({Child.transform.localPosition.x}, {Child.transform.localPosition.y}, {Child.transform.localPosition.z})";
        }
    }
}
