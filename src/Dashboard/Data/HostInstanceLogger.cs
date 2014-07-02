using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Azure.Jobs.Protocols;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Dashboard.Data
{
    internal class HostInstanceLogger : IHostInstanceLogger
    {
        private readonly IVersionedMetadataDocumentStore<HostSnapshot> _store;
        private readonly IVersionMetadataMapper _versionMapper;

        public HostInstanceLogger(CloudBlobClient client)
            : this(VersionedDocumentStore.CreateJsonBlobStore<HostSnapshot>(client,
                DashboardContainerNames.Dashboard, DashboardDirectoryNames.Hosts, VersionMetadataMapper.Instance),
                VersionMetadataMapper.Instance)
        {
        }

        private HostInstanceLogger(IVersionedMetadataDocumentStore<HostSnapshot> store,
            IVersionMetadataMapper versionMapper)
        {
            _store = store;
            _versionMapper = versionMapper;
        }

        public void LogHostStarted(HostStartedMessage message)
        {
            string hostId = message.SharedQueueName;
            HostSnapshot newSnapshot = CreateSnapshot(message);
            _store.UpdateOrCreateIfLatest(hostId, CreateMetadata(newSnapshot.HostVersion), newSnapshot);
        }

        private static HostSnapshot CreateSnapshot(HostStartedMessage message)
        {
            return new HostSnapshot
            {
                HostVersion = message.EnqueuedOn,
                Functions = CreateFunctionSnapshots(message.SharedQueueName, message.Heartbeat, message.Functions)
            };
        }

        private static IEnumerable<FunctionSnapshot> CreateFunctionSnapshots(string queueName,
            HeartbeatDescriptor heartbeat, IEnumerable<FunctionDescriptor> functions)
        {
            List<FunctionSnapshot> snapshots = new List<FunctionSnapshot>();

            foreach (FunctionDescriptor function in functions)
            {
                snapshots.Add(CreateFunctionSnapshot(queueName, heartbeat, function));
            }

            return snapshots;
        }

        private static FunctionSnapshot CreateFunctionSnapshot(string queueName, HeartbeatDescriptor heartbeat, FunctionDescriptor function)
        {
            return new FunctionSnapshot
            {
                Id = String.Format(CultureInfo.InvariantCulture, "{0}_{1}", queueName, function.Id),
                QueueName = queueName,
                HeartbeatSharedContainerName = heartbeat != null ? heartbeat.SharedContainerName : null,
                HeartbeatSharedDirectoryName = heartbeat != null ? heartbeat.SharedDirectoryName : null,
                HeartbeatExpirationInSeconds = heartbeat != null ? (int?)heartbeat.ExpirationInSeconds : null,
                HostFunctionId = function.Id,
                FullName = function.FullName,
                ShortName = function.ShortName,
                Parameters = CreateParameterSnapshots(function.Parameters)
            };
        }

        private IDictionary<string, string> CreateMetadata(DateTimeOffset version)
        {
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            _versionMapper.SetVersion(version, metadata);
            return metadata;
        }

        private static IDictionary<string, ParameterSnapshot> CreateParameterSnapshots(
            IEnumerable<ParameterDescriptor> parameters)
        {
            IDictionary<string, ParameterSnapshot> snapshots = new Dictionary<string, ParameterSnapshot>();

            foreach (ParameterDescriptor parameter in parameters)
            {
                ParameterSnapshot snapshot = CreateParameterSnapshot(parameter);

                if (snapshot != null)
                {
                    snapshots.Add(parameter.Name, snapshot);
                }
            }

            return snapshots;
        }

        private static ParameterSnapshot CreateParameterSnapshot(ParameterDescriptor parameter)
        {
            switch (parameter.Type)
            {
                case "Blob":
                    BlobParameterDescriptor blobParameter = (BlobParameterDescriptor)parameter;
                    return new BlobParameterSnapshot
                    {
                        ContainerName = blobParameter.ContainerName,
                        BlobName = blobParameter.BlobName,
                        IsInput = blobParameter.Access == FileAccess.Read
                    };
                case "BlobTrigger":
                    BlobTriggerParameterDescriptor blobTriggerParameter = (BlobTriggerParameterDescriptor)parameter;
                    return new BlobParameterSnapshot
                    {
                        ContainerName = blobTriggerParameter.ContainerName,
                        BlobName = blobTriggerParameter.BlobName,
                        IsInput = true
                    };
                case "Queue":
                    QueueParameterDescriptor queueParameter = (QueueParameterDescriptor)parameter;
                    return new QueueParameterSnapshot
                    {
                        QueueName = queueParameter.QueueName,
                        IsInput = queueParameter.Access == FileAccess.Read
                    };
                case "QueueTrigger":
                    QueueTriggerParameterDescriptor queueTriggerParameter = (QueueTriggerParameterDescriptor)parameter;
                    return new QueueParameterSnapshot
                    {
                        QueueName = queueTriggerParameter.QueueName,
                        IsInput = true
                    };
                case "Table":
                    TableParameterDescriptor tableParameter = (TableParameterDescriptor)parameter;
                    return new TableParameterSnapshot
                    {
                        TableName = tableParameter.TableName
                    };
                case "TableEntity":
                    TableEntityParameterDescriptor tableEntityParameter = (TableEntityParameterDescriptor)parameter;
                    return new TableEntityParameterSnapshot
                    {
                        TableName = tableEntityParameter.TableName,
                        PartitionKey = tableEntityParameter.PartitionKey,
                        RowKey = tableEntityParameter.RowKey
                    };
                case "ServiceBus":
                    ServiceBusParameterDescriptor serviceBusParameter = (ServiceBusParameterDescriptor)parameter;
                    return new ServiceBusParameterSnapshot
                    {
                        EntityPath = serviceBusParameter.QueueOrTopicName,
                        IsInput = false
                    };
                case "ServiceBusTrigger":
                    ServiceBusTriggerParameterDescriptor serviceBusTriggerParameter = (ServiceBusTriggerParameterDescriptor)parameter;
                    return new ServiceBusParameterSnapshot
                    {
                        EntityPath = serviceBusTriggerParameter.QueueName != null ?
                            serviceBusTriggerParameter.QueueName :
                            serviceBusTriggerParameter.TopicName + "/Subscriptions/" + serviceBusTriggerParameter.SubscriptionName,
                        IsInput = true
                    };
                case "CallerSupplied":
                case "BindingData":
                    return new InvokeParameterSnapshot();
                default:
                    // Don't convert parameters that aren't used for invoke purposes.
                    return null;
            }
        }
    }
}
