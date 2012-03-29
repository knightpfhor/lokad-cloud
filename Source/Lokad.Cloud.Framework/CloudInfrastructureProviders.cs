﻿#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using Lokad.Cloud.Diagnostics;
using Lokad.Cloud.Management;
using Lokad.Cloud.ServiceFabric;
using Lokad.Cloud.Storage;

namespace Lokad.Cloud
{
    /// <summary>IoC argument for <see cref="CloudService"/> and other
    /// cloud abstractions.</summary>
    /// <remarks>This argument will be populated through Inversion Of Control (IoC)
    /// by the Lokad.Cloud framework itself. This class is placed in the
    /// <c>Lokad.Cloud.Framework</c> for convenience while inheriting a
    /// <see cref="CloudService"/>.</remarks>
    public class CloudInfrastructureProviders : CloudStorageProviders
    {
        /// <summary>Abstracts the Management API.</summary>
        public IProvisioningProvider Provisioning { get; set; }

        public ILog Log { get; set; }

        /// <summary>IoC constructor 2.</summary>
        public CloudInfrastructureProviders(
            CloudStorageProviders storageProviders,
            IProvisioningProvider provisioning,
            ILog log)
            : base(storageProviders.BlobStorage, storageProviders.QueueStorage, storageProviders.TableStorage)
        {
            Provisioning = provisioning;
            Log = log;
        }
    }
}
