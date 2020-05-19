using System;
using System.Collections.Generic;

namespace MLAPI.HostMigration
{
    public class HostMigrationManager
    {
        public static HostMigrationManager Singleton => NetworkingManager.Singleton.HostMigrationManager;

        internal readonly List<MigratableHost> MigratableHosts = new List<MigratableHost>();
    }
}
