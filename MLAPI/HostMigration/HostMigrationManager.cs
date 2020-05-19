using System;
using System.Collections.Generic;

namespace MLAPI.HostMigration
{
    public class HostMigrationManager
    {
        public static HostMigrationManager Singleton => NetworkingManager.Singleton.HostMigrationManager;

        /// <summary>
        /// Gets Whether or not we are syncing for host migration.
        /// </summary>
        public bool IsHostMigrationEnabled { get; internal set; }

        internal ulong? IsMigratingFromClientId = null;

        internal readonly List<MigratableHost> MigratableHosts = new List<MigratableHost>();
    }
}
