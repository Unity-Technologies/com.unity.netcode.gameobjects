namespace Unity.Netcode.Runtime
{
    internal static class HelpUrls
    {
        private const string k_BaseUrl = "https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest/?subfolder=/";
        private const string k_BaseManualUrl = k_BaseUrl + "manual/";
        private const string k_BaseApiUrl = k_BaseUrl + "api/Unity.Netcode";

        // The HelpUrls have to be defined as public for the test to work
        public const string NetworkManager = k_BaseManualUrl + "components/core/networkmanager.html";
        public const string NetworkObject = k_BaseManualUrl + "components/core/networkobject.html";
        public const string NetworkAnimator = k_BaseManualUrl + "components/helper/networkanimator.html";
        public const string NetworkRigidbody = k_BaseManualUrl + "advanced-topics/physics.html#networkrigidbody";
        public const string NetworkRigidbody2D = k_BaseManualUrl + "advanced-topics/physics.html#networkrigidbody2d";
        public const string RigidbodyContactEventManager = k_BaseApiUrl + ".Components.RigidbodyContactEventManager.html";
        public const string NetworkTransform = k_BaseManualUrl + "components/helper/networktransform.html";
        public const string AnticipatedNetworkTransform = k_BaseManualUrl + "advanced-topics/client-anticipation.html";
        public const string UnityTransport = k_BaseApiUrl + ".Transports.UTP.UnityTransport.html";
        public const string SecretsLoaderHelper = k_BaseApiUrl + ".Transports.UTP.SecretsLoaderHelper.html";
        public const string SinglePlayerTransport = k_BaseApiUrl + ".Transports.SinglePlayer.SinglePlayerTransport.html";
    }
}
