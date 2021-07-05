using System;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace MLAPI.Prototyping
{
    /// <summary>
    /// Server authority only allows the server to update this transform
    /// Client authority only allows the client owner to update this transform
    /// </summary>
    public enum Authority
    {
        Server = 0, // default
        Owner,
    }
    public static class AuthorityUtility
    {
        public static bool HasAuthority(this NetworkBehaviour networkBehaviour, Authority authority)
        {
            return (networkBehaviour.IsClient && authority == Authority.Owner && networkBehaviour.IsOwner) || (networkBehaviour.IsServer && authority == Authority.Server);
        }

        public static void UpdateStateVariablePermission<T>(this NetworkVariable<T> variableToUpdate, Authority authority)
        {
            switch (authority)
            {
                case Authority.Owner:
                    variableToUpdate.Settings.WritePermission = NetworkVariablePermission.OwnerOnly;
                    variableToUpdate.Settings.ReadPermission = NetworkVariablePermission.Everyone;
                    break;
                case Authority.Server:
                    variableToUpdate.Settings.WritePermission = NetworkVariablePermission.ServerOnly;
                    variableToUpdate.Settings.ReadPermission = NetworkVariablePermission.Everyone;
                    break;
                default:
                    throw new NotImplementedException($"{authority} is not handled");
            }
        }
    }
}
