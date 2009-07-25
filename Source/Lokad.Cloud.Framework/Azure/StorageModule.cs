﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autofac.Builder;
using Lokad.Cloud.Core;
using Lokad.Quality;
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;

// TODO: add here a default exception policy (too complex to be figured out in client code).

namespace Lokad.Cloud.Azure
{
	/// <summary>IoC module that auto-load <see cref="StorageAccountInfo"/>, 
	/// <see cref="BlobStorage"/> and <see cref="QueueStorage"/> from the 
	/// properties.</summary>
	[NoCodeCoverage]
	public sealed class StorageModule : Module
	{
		/// <summary>Account name of the Azure Storage.</summary>
		public string AccountName { get; set; }

		/// <summary>Key to access the Azure Storage.</summary>
		public string AccountKey { get; set; }

		/// <summary>Indicates whether the account key is encrypted with DBAPI.</summary>
		public string IsStorageKeyEncrypted { get; set; }

		/// <summary>URL of the Blob Storage.</summary>
		public string BlobEndpoint { get; set; }

		/// <summary>URL of the Queue Storage.</summary>
		public string QueueEndpoint { get; set; }

		protected override void Load(ContainerBuilder builder)
		{
			if (RoleManager.IsRoleManagerRunning)
				ApplyOverridesFromRuntime();

			if (!string.IsNullOrEmpty(QueueEndpoint))
			{
				var queueUri = new Uri(QueueEndpoint);
				var accountInfo = new StorageAccountInfo(queueUri, null, AccountName, GetAccountKey());

				builder.Register(c =>
				{
					var queueService = QueueStorage.Create(accountInfo);

					ActionPolicy policy;
					if (c.TryResolve(out policy))
					{
						queueService.RetryPolicy = policy.Do;
					}

					return queueService;
				});
			}

			if (!string.IsNullOrEmpty(BlobEndpoint))
			{
				var blobUri = new Uri(BlobEndpoint);
				var accountInfo = new StorageAccountInfo(blobUri, null, AccountName, GetAccountKey());

				builder.Register(c =>
				{
					var storage = BlobStorage.Create(accountInfo);
					var policy = c.Resolve<ActionPolicy>();
					storage.RetryPolicy = policy.Do;
					return storage;
				});
			}

			// registering the Lokad.Cloud providers
			if (!string.IsNullOrEmpty(QueueEndpoint) && !string.IsNullOrEmpty(BlobEndpoint))
			{
				builder.Register(c =>
             	{
             		IFormatter formatter;
             		if (!c.TryResolve(out formatter))
             		{
             			formatter = new BinaryFormatter();
             		}

             		return (IBlobStorageProvider)new BlobStorageProvider(c.Resolve<BlobStorage>(), formatter);
             	});

				builder.Register(c =>
				{
					IFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new BinaryFormatter();
					}

					return (IQueueStorageProvider)new QueueStorageProvider(
						c.Resolve<QueueStorage>(),
						c.Resolve<BlobStorage>(),
						formatter);
				});
			}
		}

		string GetAccountKey()
		{
			return "true".Equals((IsStorageKeyEncrypted ?? string.Empty).ToLower()) ? 
				DBAPI.Decrypt(AccountKey) : AccountKey;
		}

		void ApplyOverridesFromRuntime()
		{
			// HACK: listing all properties is brittle
			var properties = typeof(StorageModule).GetProperties();

			foreach (var info in properties)
			{
				// HACK: ignoring the encryption property
				if("IsStorageKeyEncrypted" == info.Name) continue;

				var value = RoleManager.GetConfigurationSetting(info.Name);
				if (!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}
	}
}
