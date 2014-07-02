﻿using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Dashboard.Data
{
    public static class VersionedDocumentStore
    {
        [CLSCompliant(false)]
        public static IVersionedMetadataDocumentStore<TDocument> CreateJsonBlobStore<TDocument>(CloudBlobClient client,
            string containerName, string directoryName, IVersionMetadataMapper versionMapper)
        {
            IVersionedMetadataTextStore innerStore =
                VersionedTextStore.CreateBlobStore(client, containerName, directoryName, versionMapper);
            return new JsonVersionedDocumentStore<TDocument>(innerStore);
        }
    }
}
