﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMethodReturnValue.Global

namespace Lokad.Cloud.Storage
{
    public static class BlobStorageExtensions
    {
        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExists<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, t => update(t)).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist.</returns>
        public static Maybe<T> UpdateBlobIfExists<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, t => update(t)).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or no change was applied.</returns>
        public static Maybe<T> UpdateBlobIfExistsOrSkip<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or no change was applied.</returns>
        public static Maybe<T> UpdateBlobIfExistsOrSkip<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), () => Maybe<T>.Empty, update).Value;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will be deleted.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or was deleted.</returns>
        public static Maybe<T> UpdateBlobIfExistsOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T, Maybe<T>> update)
        {
            var result = provider.UpsertBlobOrSkip(containerName, blobName, () => Maybe<T>.Empty, update);
            if (!result.HasValue)
            {
                provider.DeleteBlobIfExists(containerName, blobName);
            }

            return result;
        }

        /// <summary>
        /// Updates a blob if it already exists.
        /// If the insert or update lambdas return empty, the blob will be deleted.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda, or empty if the blob did not exist or was deleted.</returns>
        public static Maybe<T> UpdateBlobIfExistsOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T, Maybe<T>> update)
        {
            return provider.UpdateBlobIfExistsOrDelete(name.ContainerName, name.ToString(), update);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<T> insert, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip<T>(containerName, blobName, () => insert(), t => update(t)).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda.</returns>
        public static T UpsertBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<T> insert, Func<T, T> update)
        {
            return provider.UpsertBlobOrSkip<T>(name.ContainerName, name.ToString(), () => insert(), t => update(t)).Value;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will not be changed.
        /// </summary>
        /// <remarks>
        /// This procedure can not be used to delete the blob. The provided lambdas can
        /// be executed multiple times in case of concurrency-related retrials, so be careful
        /// with side-effects (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then no change was applied.</returns>
        public static Maybe<T> UpsertBlobOrSkip<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrSkip(name.ContainerName, name.ToString(), insert, update);
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will be deleted (if it exists).
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then the blob has been deleted.</returns>
        public static Maybe<T> UpsertBlobOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            var result = provider.UpsertBlobOrSkip(containerName, blobName, insert, update);
            if (!result.HasValue)
            {
                provider.DeleteBlobIfExists(containerName, blobName);
            }

            return result;
        }

        /// <summary>
        /// Inserts or updates a blob depending on whether it already exists or not.
        /// If the insert or update lambdas return empty, the blob will be deleted (if it exists).
        /// </summary>
        /// <remarks>
        /// TThe provided lambdas can be executed multiple times in case of
        /// concurrency-related retrials, so be careful with side-effects
        /// (like incrementing a counter in them).
        /// </remarks>
        /// <returns>The value returned by the lambda. If empty, then the blob has been deleted.</returns>
        public static Maybe<T> UpsertBlobOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>> insert, Func<T, Maybe<T>> update)
        {
            return provider.UpsertBlobOrDelete(name.ContainerName, name.ToString(), insert, update);
        }

        public static bool DeleteBlobIfExists<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
        {
            return provider.DeleteBlobIfExists(fullName.ContainerName, fullName.ToString());
        }

        static readonly Random _rand = new Random();

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            Result<T> tmpResult = null;
            RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

            result = tmpResult;
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, string containerName, string blobName, Func<Maybe<T>, T> updater, out T result)
        {
            T tmpResult = default(T);
            RetryUpdate(() => provider.UpdateIfNotModified(containerName, blobName, updater, out tmpResult));

            result = tmpResult;
        }

        /// <summary>Retry an update method until it succeeds. Timing
        /// increases to avoid overstressing the storage for nothing. 
        /// Maximal delay is set to 10 seconds.</summary>
        [Obsolete]
        static void RetryUpdate(Func<bool> func)
        {
            // HACK: hard-coded constant, the whole counter system have to be perfected.
            const int step = 10;
            const int maxDelayInMilliseconds = 10000;

            int retryAttempts = 0;
            while (!func())
            {
                retryAttempts++;
                var sleepTime = _rand.Next(Math.Max(retryAttempts * retryAttempts * step, maxDelayInMilliseconds)).Milliseconds();
                Thread.Sleep(sleepTime);

            }
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("Use either UpsertBlobOrSkip or UpsertBlobOrDelete with clearer semantics instead")]
        public static void AtomicUpdate<T>(this IBlobStorageProvider provider, BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
        {
            AtomicUpdate(provider, name.ContainerName, name.ToString(), updater, out result);
        }

        [Obsolete("Use DeleteBlobIfExists instead")]
        public static bool DeleteBlob<T>(this IBlobStorageProvider provider, BlobName<T> fullName)
        {
            return provider.DeleteBlobIfExists(fullName.ContainerName, fullName.ToString());
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlob<T>(name.ContainerName, name.ToString());
        }

        public static Maybe<T> GetBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, out string etag)
        {
            return provider.GetBlob<T>(name.ContainerName, name.ToString(), out etag);
        }

        public static string GetBlobEtag<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlobEtag(name.ContainerName, name.ToString());
        }

        /// <summary>Gets the corresponding object. If the deserialization fails
        /// just delete the existing copy.</summary>
        public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, string containerName, string blobName)
        {
            try
            {
                return provider.GetBlob<T>(containerName, blobName);
            }
            catch (SerializationException)
            {
                provider.DeleteBlobIfExists(containerName, blobName);
                return Maybe<T>.Empty;
            }
            catch (InvalidCastException)
            {
                provider.DeleteBlobIfExists(containerName, blobName);
                return Maybe<T>.Empty;
            }
        }

        /// <summary>Gets the corresponding object. If the deserialization fails
        /// just delete the existing copy.</summary>
        public static Maybe<T> GetBlobOrDelete<T>(this IBlobStorageProvider provider, BlobName<T> name)
        {
            return provider.GetBlobOrDelete<T>(name.ContainerName, name.ToString());
        }

        public static void PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item)
        {
            provider.PutBlob(name.ContainerName, name.ToString(), item);
        }

        public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, bool overwrite)
        {
            return provider.PutBlob(name.ContainerName, name.ToString(), item, overwrite);
        }

        /// <summary>Push the blob only if etag is matching the etag of the blob in BlobStorage</summary>
        public static bool PutBlob<T>(this IBlobStorageProvider provider, BlobName<T> name, T item, string etag)
        {
            return provider.PutBlob(name.ContainerName, name.ToString(), item, etag);
        }

        public static IEnumerable<T> List<T>(
            this IBlobStorageProvider provider, T prefix) where T : UntypedBlobName
        {
            return provider.List(prefix.ContainerName, prefix.ToString())
                .Select(UntypedBlobName.Parse<T>);
        }

        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, Result<T>> updater, out Result<T> result)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
        }

        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, T> updater, out T result)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater, out result);
        }

        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, Result<T>> updater)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
        }

        public static bool UpdateIfNotModified<T>(this IBlobStorageProvider provider,
            BlobName<T> name, Func<Maybe<T>, T> updater)
        {
            return provider.UpdateIfNotModified(name.ContainerName, name.ToString(), updater);
        }
    }
}