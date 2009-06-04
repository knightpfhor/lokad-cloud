﻿#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Autofac.Builder;
using Microsoft.Samples.ServiceHosting.StorageClient;
using Microsoft.ServiceHosting.ServiceRuntime;

namespace Lokad.Cloud.Core
{
	/// <summary>IoC module that auto-load <see cref="StorageAccountInfo"/>, 
	/// <see cref="BlobStorage"/> and <see cref="QueueStorage"/> from the 
	/// properties.</summary>
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

             		return new BlobStorageProvider(
             			c.Resolve<BlobStorage>(),
             			c.Resolve<ActionPolicy>(),
             			formatter);
             	});

				builder.Register(c =>
				{
					IFormatter formatter;
					if (!c.TryResolve(out formatter))
					{
						formatter = new BinaryFormatter();
					}

					return new QueueStorageProvider(
						c.Resolve<QueueStorage>(),
						c.Resolve<BlobStorage>(),
						c.Resolve<ActionPolicy>(),
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
			//// get overrides from the role manager's settings
			//var settings = RoleManagerOpened
			//    .GetSettings()
			//    .ToDictionary(s => s.Key, s => s.Value);

			var properties = typeof(StorageModule).GetProperties();

			foreach (var info in properties)
			{
				var value = RoleManager.GetConfigurationSetting(info.Name);
				if (!string.IsNullOrEmpty(value))
				{
					info.SetValue(this, value, null);
				}
			}
		}
	}
}