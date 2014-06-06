﻿using System.Collections.Generic;
using Microsoft.Azure.Jobs.Protocols;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Dashboard.Data
{
    internal class FunctionInstanceLogger : IFunctionInstanceLogger, IFunctionQueuedLogger
    {
        private readonly IVersionedDocumentStore<FunctionInstanceSnapshot> _store;

        public FunctionInstanceLogger(CloudBlobClient client)
            : this(client.GetContainerReference(DashboardContainerNames.FunctionLogContainer))
        {
        }

        private FunctionInstanceLogger(CloudBlobContainer container)
            : this(new VersionedDocumentStore<FunctionInstanceSnapshot>(container))
        {
        }

        private FunctionInstanceLogger(IVersionedDocumentStore<FunctionInstanceSnapshot> store)
        {
            _store = store;
        }

        // Using FunctionStartedSnapshot is a slight abuse; StartTime here is really QueueTime.
        public void LogFunctionQueued(FunctionStartedMessage message)
        {
            FunctionInstanceSnapshot snapshot = CreateSnapshot(message);
            // Fix the abuse of FunctionStartedSnapshot.
            snapshot.StartTime = null;

            // Ignore the return result. Only the dashboard calls this method, and it does so before enqueuing the
            // message to the host to run the function. So realistically the blob can't already exist. And even if the
            // host did see the message before this call, an existing blob just means something more recent than
            // "queued" status, so there's nothing to do here in that case anyway.
            _store.TryCreate(GetId(message), snapshot);
        }

        public void LogFunctionStarted(FunctionStartedMessage message)
        {
            FunctionInstanceSnapshot snapshot = CreateSnapshot(message);

            // Which operation to run depends on whether or not the entity currently exists in the "queued" status.
            VersionedDocument<FunctionInstanceSnapshot> existingSnapshot = _store.Read(GetId(message));

            bool previouslyQueued;

            // If the existing entity doesn't contain a StartTime, it must be in the "queued" status.
            if (existingSnapshot != null && existingSnapshot.Document != null && !existingSnapshot.Document.StartTime.HasValue)
            {
                previouslyQueued = true;
            }
            else
            {
                previouslyQueued = false;
            }

            if (!previouslyQueued)
            {
                LogFunctionStartedWhenNotPreviouslyQueued(snapshot);
            }
            else
            {
                LogFunctionStartedWhenPreviouslyQueued(snapshot, existingSnapshot.ETag);
            }
        }

        private void LogFunctionStartedWhenNotPreviouslyQueued(FunctionInstanceSnapshot snapshot)
        {
            // LogFunctionStarted and LogFunctionCompleted may run concurrently. Ensure LogFunctionStarted loses by just
            // doing a simple Insert that will fail if the entity already exists. LogFunctionCompleted wins by having it
            // replace the LogFunctionStarted record, if any.
            // Ignore the return value: if the item already exists, LogFunctionCompleted already ran, so there's no work
            // to do here.
            _store.TryCreate(GetId(snapshot), snapshot);
        }

        private void LogFunctionStartedWhenPreviouslyQueued(FunctionInstanceSnapshot snapshot, string etag)
        {
            // LogFunctionStarted and LogFunctionCompleted may run concurrently. LogFunctionQueued does not run
            // concurrently. Ensure LogFunctionStarted wins over LogFunctionQueued but loses to LogFunctionCompleted by
            // doing a Replace with ETag check that will fail if the entity has been changed.
            // LogFunctionCompleted wins by doing a Replace without an ETag check, so it will replace the
            // LogFunctionStarted (or Queued) record, if any.
            // Ignore the return value: if the ETag doesn't match, LogFunctionCompleted already ran, so there's no work
            // to do here.
            _store.TryUpdate(GetId(snapshot), snapshot, etag);
        }

        public void LogFunctionCompleted(FunctionCompletedMessage message)
        {
            FunctionInstanceSnapshot snapshot = CreateSnapshot(message);

            // LogFunctionStarted and LogFunctionCompleted may run concurrently. Ensure LogFunctionCompleted wins by
            // having it replace the LogFunctionStarted record, if any.
            _store.CreateOrUpdate(GetId(snapshot), snapshot);
        }

        private static FunctionInstanceSnapshot CreateSnapshot(FunctionStartedMessage message)
        {
            return new FunctionInstanceSnapshot
            {
                Id = message.FunctionInstanceId,
                HostInstanceId = message.HostInstanceId,
                FunctionId = new FunctionIdentifier(message.SharedQueueName, message.Function.Id).ToString(),
                FunctionFullName = message.Function.FullName,
                FunctionShortName = message.Function.ShortName,
                Arguments = CreateArguments(message.Function.Parameters, message.Arguments),
                ParentId = message.ParentId,
                Reason = message.Reason,
                QueueTime = message.StartTime,
                StartTime = message.StartTime,
                StorageConnectionString = message.StorageConnectionString,
                OutputBlobUrl = message.OutputBlobUrl,
                ParameterLogBlobUrl = message.ParameterLogBlobUrl,
                WebSiteName = message.WebJobRunIdentifier != null ? message.WebJobRunIdentifier.WebSiteName : null,
                WebJobType = message.WebJobRunIdentifier != null ? message.WebJobRunIdentifier.JobType.ToString() : null,
                WebJobName = message.WebJobRunIdentifier != null ? message.WebJobRunIdentifier.JobName : null,
                WebJobRunId = message.WebJobRunIdentifier != null ? message.WebJobRunIdentifier.RunId : null
            };
        }

        private static IDictionary<string, FunctionInstanceArgument> CreateArguments(
            IDictionary<string, ParameterDescriptor> parameters, IDictionary<string, string> argumentValues)
        {
            IDictionary<string, FunctionInstanceArgument> arguments =
                new Dictionary<string, FunctionInstanceArgument>();

            foreach (KeyValuePair<string, string> item in argumentValues)
            {
                string name = item.Key;
                arguments.Add(name, new FunctionInstanceArgument
                {
                    Value = item.Value,
                    IsBlob = parameters != null && parameters.ContainsKey(name)
                        && (parameters[name] is BlobParameterDescriptor
                        || parameters[name] is BlobTriggerParameterDescriptor),
                });
            }

            return arguments;
        }

        private static FunctionInstanceSnapshot CreateSnapshot(FunctionCompletedMessage message)
        {
            FunctionInstanceSnapshot entity = CreateSnapshot((FunctionStartedMessage)message);
            entity.EndTime = message.EndTime;
            entity.Succeeded = message.Succeeded;
            entity.ExceptionType = message.ExceptionType;
            entity.ExceptionMessage = message.ExceptionMessage;
            return entity;
        }

        private static string GetId(FunctionInstanceSnapshot snapshot)
        {
            return snapshot.Id.ToString("N");
        }

        private static string GetId(FunctionStartedMessage message)
        {
            return message.FunctionInstanceId.ToString("N");
        }
    }
}
