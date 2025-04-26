
namespace Unity.Netcode
{
	public interface INetworkCustomSpawnDataSynchronizer
	{
		/// <summary>
		/// Called on the client side after receiving custom spawn metadata during the instantiation process.
		/// This extends the <see cref="INetworkPrefabInstanceHandler"/> interface to allow for custom spawn data handling.
		/// This method is used to pass additional data from the server to the client to help identify or configure
		/// the local object instance that should be linked to the spawned NetworkObject.
		///
		/// This is invoked just before <see cref="INetworkPrefabInstanceHandler.Instantiate"/> is called,
		/// allowing you to cache or prepare information needed during instantiation.
		/// </summary>
		/// <param name="customSpawnData">The metadata buffer sent from the server during the spawn message.</param>
		void OnSynchronize<T>(ref BufferSerializer<T> serializer) where T : IReaderWriter;
	}
}
