namespace Unity.Netcode.Runtime
{
    internal static class HelpUrls
    {
        private const string k_BaseUrl = "https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest/?subfolder=/";
        private const string k_BaseManualUrl = k_BaseUrl + "manual/";
        private const string k_BaseApiUrl = k_BaseUrl + "api/Unity.Netcode";

        internal const string NetworkManager = k_BaseManualUrl + "components/core/networkmanager.html";
        internal const string NetworkObject = k_BaseManualUrl + "components/core/networkobject.html";
        internal const string NetworkAnimator = k_BaseManualUrl + "components/helper/networkanimator.html";
        internal const string NetworkRigidbody = k_BaseManualUrl + "advanced-topics/physics.html#networkrigidbody";
        internal const string NetworkRigidbody2D = k_BaseManualUrl + "advanced-topics/physics.html#networkrigidbody2d";
        internal const string RigidbodyContactEventManager = k_BaseApiUrl + ".Components.RigidbodyContactEventManager.html";
        internal const string NetworkTransform = k_BaseManualUrl + "components/helper/networktransform.html";
        internal const string AnticipatedNetworkTransform = k_BaseManualUrl + "advanced-topics/client-anticipation.html";
        internal const string UnityTransport = k_BaseApiUrl + ".Transports.UTP.UnityTransport.html";
        internal const string SecretsLoaderHelper = k_BaseManualUrl + ".Transports.UTP.SecretsLoaderHelper.html";
        internal const string SinglePlayerTransport = k_BaseApiUrl + ".Transports.SinglePlayer.SinglePlayerTransport.html";
    }
}
