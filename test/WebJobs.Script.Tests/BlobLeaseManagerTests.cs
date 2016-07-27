﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class BlobLeaseManagerTests
    {
        [Fact]
        [Trait("Category", "E2E")]
        public async Task HasLease_WhenLeaseIsAcquired_ReturnsTrue()
        {
            string connectionString = AmbientConnectionStringProvider.Instance.GetConnectionString(ConnectionStringNames.Storage);
            string hostId = Guid.NewGuid().ToString();
            var traceWriter = new TestTraceWriter(System.Diagnostics.TraceLevel.Verbose);

            using (var manager = await BlobLeaseManager.CreateAsync(connectionString, TimeSpan.FromSeconds(15), hostId, traceWriter))
            {
                await TestHelpers.Await(() => manager.HasLease);

                Assert.Equal(hostId, manager.LeaseId);
            }
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task HasLeaseChanged_WhenLeaseIsAcquiredAndStateChanges_IsFired()
        {
            string hostId = Guid.NewGuid().ToString();
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var resetEvent = new ManualResetEventSlim();

            string connectionString = AmbientConnectionStringProvider.Instance.GetConnectionString(ConnectionStringNames.Storage);
            ICloudBlob blob = await GetLockBlobAsync(connectionString);

            // Acquire a lease on the host lock blob
            string leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(15));

            BlobLeaseManager manager = null;

            try
            {
                manager = await BlobLeaseManager.CreateAsync(connectionString, TimeSpan.FromSeconds(15), hostId, traceWriter);
                manager.HasLeaseChanged += (s, a) => resetEvent.Set();
            }
            finally
            {
                await blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = leaseId });
            }

            resetEvent.Wait(TimeSpan.FromSeconds(15));

            manager.Dispose();

            Assert.True(resetEvent.IsSet);
            Assert.True(manager.HasLease, $"{nameof(BlobLeaseManager.HasLease)} was not correctly set to 'true' when lease was acquired.");
            Assert.Equal(hostId, manager.LeaseId);
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task HasLeaseChanged_WhenLeaseIsLostAndStateChanges_IsFired()
        {
            string connectionString = AmbientConnectionStringProvider.Instance.GetConnectionString(ConnectionStringNames.Storage);
            ICloudBlob blob = await GetLockBlobAsync(connectionString);

            string hostId = Guid.NewGuid().ToString();
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var resetEvent = new ManualResetEventSlim();

            BlobLeaseManager manager = null;
            string tempLeaseId = null;

            using (manager = new BlobLeaseManager(blob, TimeSpan.FromSeconds(15), hostId, traceWriter, TimeSpan.FromSeconds(3)))
            {
                try
                {
                    await TestHelpers.Await(() => manager.HasLease);

                    manager.HasLeaseChanged += (s, a) => resetEvent.Set();

                    // Release the manager's lease and acquire one with a different id
                    await blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = manager.LeaseId });
                    tempLeaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(30), Guid.NewGuid().ToString());
                }
                finally
                {
                    if (tempLeaseId != null)
                    {
                        await blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = tempLeaseId });
                    }
                }

                resetEvent.Wait(TimeSpan.FromSeconds(15));
            }

            Assert.True(resetEvent.IsSet);
            Assert.False(manager.HasLease, $"{nameof(BlobLeaseManager.HasLease)} was not correctly set to 'false' when lease lost.");
        }

        [Fact]
        public void AcquiringLease_WithServerError_LogsAndRetries()
        {
            var hostId = "123";
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var renewResetEvent = new ManualResetEventSlim();

            var results = new Queue<Task<string>>();
            results.Enqueue(Task.FromException<string>(new StorageException(new RequestResult { HttpStatusCode = 500 }, "test", null)));
            results.Enqueue(Task.FromResult(hostId));
            
            var blobMock = new Mock<ICloudBlob>();
            blobMock.Setup(b => b.AcquireLeaseAsync(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(() => results.Dequeue());

            blobMock.Setup(b => b.RenewLeaseAsync(It.IsAny<AccessCondition>()))
                .Returns(() => Task.Delay(1000))
                .Callback(() => renewResetEvent.Set());

            BlobLeaseManager manager;
            using (manager = new BlobLeaseManager(blobMock.Object, TimeSpan.FromSeconds(5), hostId, traceWriter))
            {
                renewResetEvent.Wait(TimeSpan.FromSeconds(10));
            }

            Assert.True(renewResetEvent.IsSet);
            Assert.True(manager.HasLease);
            Assert.True(traceWriter.Traces.Any(t => t.Message.Contains("Server error")));
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task Renew_WhenBlobIsDeleted_RecreatesBlob()
        {
            var hostId = Guid.NewGuid().ToString();
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var renewResetEvent = new ManualResetEventSlim();
            string connectionString = AmbientConnectionStringProvider.Instance.GetConnectionString(ConnectionStringNames.Storage);

            ICloudBlob blob = await GetLockBlobAsync(connectionString);

            var blobMock = new Mock<ICloudBlob>();
            blobMock.Setup(b => b.AcquireLeaseAsync(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(hostId));

            blobMock.Setup(b => b.RenewLeaseAsync(It.IsAny<AccessCondition>()))
                .Returns(() => Task.FromException<string>(new StorageException(new RequestResult { HttpStatusCode = 404 }, "test", null)))
                .Callback(() => Task.Delay(1000).ContinueWith(t => renewResetEvent.Set()));

            blobMock.SetupGet(b => b.ServiceClient).Returns(blob.ServiceClient);

            // Delete the blob
            await blob.DeleteIfExistsAsync();

            using (var manager = new BlobLeaseManager(blobMock.Object, TimeSpan.FromSeconds(15), hostId, traceWriter, TimeSpan.FromSeconds(3)))
            {
                renewResetEvent.Wait(TimeSpan.FromSeconds(10));

                await TestHelpers.Await(() => manager.HasLease);
            }

            bool blobExists = await blob.ExistsAsync();

            Assert.True(renewResetEvent.IsSet);
            Assert.True(blobExists);
        }

        [Fact]
        [Trait("Category", "E2E")]
        public async Task Dispose_ReleasesBlobLease()
        {
            string connectionString = AmbientConnectionStringProvider.Instance.GetConnectionString(ConnectionStringNames.Storage);

            string hostId = Guid.NewGuid().ToString();
            var traceWriter = new TestTraceWriter(System.Diagnostics.TraceLevel.Verbose);

            using (var manager = await BlobLeaseManager.CreateAsync(connectionString, TimeSpan.FromSeconds(15), hostId, traceWriter))
            {
                await TestHelpers.Await(() => manager.HasLease);
            }

            ICloudBlob blob = await GetLockBlobAsync(connectionString);

            string leaseId = null;
            try
            {
                // Acquire a lease on the host lock blob
                leaseId = await blob.AcquireLeaseAsync(TimeSpan.FromSeconds(15));

                await blob.ReleaseLeaseAsync(new AccessCondition { LeaseId = leaseId });
            }
            catch (StorageException exc) when (exc.RequestInformation.HttpStatusCode == 409)
            {
            }

            Assert.False(string.IsNullOrEmpty(leaseId), "Failed to acquire a blob lease. The lease was not properly released.");
        }

        [Fact]
        public async Task TraceOutputsMessagesWhenLeaseIsAcquired()
        {
            var hostId = "123";
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var renewResetEvent = new ManualResetEventSlim();

            var blobMock = new Mock<ICloudBlob>();
            blobMock.Setup(b => b.AcquireLeaseAsync(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(hostId));

            blobMock.Setup(b => b.RenewLeaseAsync(It.IsAny<AccessCondition>()))
                .Returns(() => Task.Delay(1000))
                .Callback(() => renewResetEvent.Set());
            
            var manager = new BlobLeaseManager(blobMock.Object, TimeSpan.FromSeconds(5), hostId, traceWriter);
            renewResetEvent.Wait(TimeSpan.FromSeconds(10));
            manager.Dispose();

            // Make sure we have enough time to trace the renewal
            await TestHelpers.Await(() => traceWriter.Traces.Count == 2, 5000, 500);

            TraceEvent acquisitionEvent = traceWriter.Traces.First();
            Assert.Contains($"Host lock lease acquired by host ID '{hostId}'.", acquisitionEvent.Message);
            Assert.Equal(TraceLevel.Info, acquisitionEvent.Level);

            TraceEvent renewalEvent = traceWriter.Traces.Skip(1).First();
            Assert.Contains("Host lock lease renewed.", renewalEvent.Message);
            Assert.Equal(TraceLevel.Verbose, renewalEvent.Level);
        }

        [Fact]
        public async Task TraceOutputsMessagesWhenLeaseAcquisitionFails()
        {
            var hostId = "123";
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var acquisitionResetEvent = new ManualResetEventSlim();

            var blobMock = new Mock<ICloudBlob>();
            blobMock.Setup(b => b.AcquireLeaseAsync(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(() => Task.FromException<string>(new StorageException(new RequestResult { HttpStatusCode = 409 }, "test", null)))
                .Callback(() => acquisitionResetEvent.Set());

            using (var manager = new BlobLeaseManager(blobMock.Object, TimeSpan.FromSeconds(10), hostId, traceWriter))
            {
                acquisitionResetEvent.Wait(TimeSpan.FromSeconds(5));
            }

            // Make sure we have enough time to trace the renewal
            await TestHelpers.Await(() => traceWriter.Traces.Count == 1, 5000, 500);

            TraceEvent acquisitionEvent = traceWriter.Traces.First();
            Assert.Contains($"Host '{hostId}' failed to acquire host lock lease: Another host has an active lease.", acquisitionEvent.Message);
            Assert.Equal(TraceLevel.Verbose, acquisitionEvent.Level);
        }

        [Fact]
        public async Task TraceOutputsMessagesWhenLeaseRenewalFails()
        {
            var hostId = "123";
            var traceWriter = new TestTraceWriter(TraceLevel.Verbose);
            var renewResetEvent = new ManualResetEventSlim();

            var blobMock = new Mock<ICloudBlob>();
            blobMock.Setup(b => b.AcquireLeaseAsync(It.IsAny<TimeSpan>(), It.IsAny<string>()))
                .Returns(() => Task.FromResult(hostId));

            blobMock.Setup(b => b.RenewLeaseAsync(It.IsAny<AccessCondition>()))
                .Returns(() => Task.FromException(new StorageException(new RequestResult { HttpStatusCode = 409 }, "test", null)))
                .Callback(() => renewResetEvent.Set());

            using (var manager = new BlobLeaseManager(blobMock.Object, TimeSpan.FromSeconds(5), hostId, traceWriter))
            {
                renewResetEvent.Wait(TimeSpan.FromSeconds(10));
                // Make sure we have enough time to trace the renewal
                await TestHelpers.Await(() => traceWriter.Traces.Count == 2, 5000, 500);
            }

            TraceEvent acquisitionEvent = traceWriter.Traces.First();
            Assert.Contains($"Host lock lease acquired by host ID '{hostId}'.", acquisitionEvent.Message);
            Assert.Equal(TraceLevel.Info, acquisitionEvent.Level);

            TraceEvent renewalEvent = traceWriter.Traces.Skip(1).First();
            Assert.Contains("Failed to renew host lock lease", renewalEvent.Message);
            Assert.Equal(TraceLevel.Info, renewalEvent.Level);
        }

        private static async Task<ICloudBlob> GetLockBlobAsync(string accountConnectionString)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(accountConnectionString);
            CloudBlobClient client = account.CreateCloudBlobClient();

            var container = client.GetContainerReference("azure-functions-host");

            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blob = container.GetBlockBlobReference("hostlock");
            if (!await blob.ExistsAsync())
            {
                await blob.UploadFromStreamAsync(new MemoryStream());
            }

            return blob;
        }
    }
}
