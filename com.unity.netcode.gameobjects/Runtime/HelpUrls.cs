namespace Unity.Netcode.Runtime
{
    internal static class HelpUrls
    {
        private const string k_BaseUrl = "https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest/?subfolder=/";
        internal const string BaseManualUrl = k_BaseUrl + "manual/";
        internal const string BaseApiUrl = k_BaseUrl + "api/Unity.Netcode";

        // The HelpUrls have to be defined as public for the test to work
        public const string NetworkManager = BaseManualUrl + "components/core/networkmanager.html";
        public const string NetworkObject = BaseManualUrl + "components/core/networkobject.html";
        public const string NetworkAnimator = BaseManualUrl + "components/helper/networkanimator.html";
        public const string NetworkRigidbody = BaseManualUrl + "components/helper/networkrigidbody.html";
        public const string NetworkRigidbody2D = BaseManualUrl + "components/helper/networkrigidbody.html";
        public const string RigidbodyContactEventManager = BaseApiUrl + ".Components.RigidbodyContactEventManager.html";
        public const string NetworkTransform = BaseManualUrl + "components/helper/networktransform.html";
        public const string AnticipatedNetworkTransform = BaseManualUrl + "advanced-topics/client-anticipation.html";
        public const string UnityTransport = BaseApiUrl + ".Transports.UTP.UnityTransport.html";
        public const string SecretsLoaderHelper = BaseApiUrl + ".Transports.UTP.SecretsLoaderHelper.html";
        public const string SinglePlayerTransport = BaseApiUrl + ".Transports.SinglePlayer.SinglePlayerTransport.html";
    }
}
