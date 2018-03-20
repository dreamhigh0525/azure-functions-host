﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.MobileServices;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public abstract class EndToEndTestsBase<TTestFixture> :
        IClassFixture<TTestFixture> where TTestFixture : EndToEndTestFixture, new()
    {
        private INameResolver _nameResolver = new DefaultNameResolver();
        private static readonly ScriptSettingsManager SettingsManager = ScriptSettingsManager.Instance;

        public EndToEndTestsBase(TTestFixture fixture)
        {
            Fixture = fixture;
        }

        protected TTestFixture Fixture { get; private set; }

        protected async Task TableInputTest()
        {
            var input = new JObject
            {
                { "Region", "West" },
                { "Status", 1 }
            };

            await Fixture.Host.BeginFunctionAsync("TableIn", input);

            var result = await WaitForTraceAsync("TableIn", log =>
            {
                return log.FormattedMessage.Contains("Result:");
            });
            string message = result.FormattedMessage.Substring(result.FormattedMessage.IndexOf('{'));

            // verify singleton binding
            JObject resultObject = JObject.Parse(message);
            JObject single = (JObject)resultObject["single"];
            Assert.Equal("AAA", (string)single["PartitionKey"]);
            Assert.Equal("001", (string)single["RowKey"]);

            // verify partition binding
            JArray partition = (JArray)resultObject["partition"];
            Assert.Equal(3, partition.Count);
            foreach (var entity in partition)
            {
                Assert.Equal("BBB", (string)entity["PartitionKey"]);
            }

            // verify query binding
            JArray query = (JArray)resultObject["query"];
            Assert.Equal(2, query.Count);
            Assert.Equal("003", (string)query[0]["RowKey"]);
            Assert.Equal("004", (string)query[1]["RowKey"]);

            // verify input validation
            input = new JObject
            {
                { "Region", "West" },
                { "Status", "1 or Status neq 1" }
            };

            await Fixture.Host.BeginFunctionAsync("TableIn", input);

            // Watch for the expected error.

            var errorLog = await WaitForTraceAsync(log =>
            {
                return log.Category == LogCategories.CreateFunctionCategory("TableIn") &&
                       log.Exception is FunctionInvocationException;
            });

            Assert.Equal("An invalid parameter value was specified for filter parameter 'Status'.", errorLog.Exception.InnerException.Message);
        }

        protected async Task TableOutputTest()
        {
            CloudTable table = Fixture.TableClient.GetTableReference("testoutput");
            await Fixture.DeleteEntities(table);

            JObject item = new JObject
            {
                { "partitionKey", "TestOutput" },
                { "rowKey", 1 },
                { "stringProp", "Mathew" },
                { "intProp", 123 },
                { "boolProp", true },
                { "guidProp", Guid.NewGuid() },
                { "floatProp", 68756.898 }
            };

            await Fixture.Host.BeginFunctionAsync("TableOut", item);

            // read the entities and verify schema
            TableQuery tableQuery = new TableQuery();
            DynamicTableEntity[] entities = null;

            await TestHelpers.Await(async () =>
            {
                var results = await table.ExecuteQuerySegmentedAsync(tableQuery, null);
                entities = results.ToArray();
                return entities.Length == 3;
            });

            foreach (var entity in entities)
            {
                Assert.Equal(EdmType.String, entity.Properties["stringProp"].PropertyType);
                Assert.Equal(EdmType.Int32, entity.Properties["intProp"].PropertyType);
                Assert.Equal(EdmType.Boolean, entity.Properties["boolProp"].PropertyType);

                // Guids end up roundtripping as strings
                Assert.Equal(EdmType.String, entity.Properties["guidProp"].PropertyType);

                Assert.Equal(EdmType.Double, entity.Properties["floatProp"].PropertyType);
            }
        }

        protected async Task ManualTrigger_Invoke_SucceedsTest()
        {
            string testData = Guid.NewGuid().ToString();

            await Fixture.Host.BeginFunctionAsync("ManualTrigger", testData);

            await TestHelpers.Await(() =>
            {
                // make sure the input string made it all the way through
                var logs = Fixture.Host.GetLogMessages();
                return logs.Any(p => p.FormattedMessage != null && p.FormattedMessage.Contains(testData));
            }, userMessageCallback: Fixture.Host.GetLog);
        }

        //public async Task FileLogging_SucceedsTest()
        //{
        //    string functionName = "Scenarios";
        //    TestHelpers.ClearFunctionLogs(functionName);

        //    string guid1 = Guid.NewGuid().ToString();
        //    string guid2 = Guid.NewGuid().ToString();

        //    ScenarioInput input = new ScenarioInput
        //    {
        //        Scenario = "fileLogging",
        //        Container = "scenarios-output",
        //        Value = $"{guid1};{guid2}"
        //    };
        //    Dictionary<string, object> arguments = new Dictionary<string, object>
        //        {
        //            { "input", JsonConvert.SerializeObject(input) }
        //        };

        //    await Fixture.Host.CallAsync(functionName, arguments);

        //    // wait for logs to flush
        //    await Task.Delay(FileTraceWriter.LogFlushIntervalMs);

        //    IList<string> logs = null;
        //    await TestHelpers.Await(() =>
        //    {
        //        logs = TestHelpers.GetFunctionLogsAsync(functionName, throwOnNoLogs: false).Result;
        //        return logs.Count > 0;
        //    });

        //    Assert.True(logs.Count == 4, string.Join(Environment.NewLine, logs));

        //    // No need for assert; this will throw if there's not one and only one
        //    logs.Single(p => p.EndsWith($"From TraceWriter: {guid1}"));
        //    logs.Single(p => p.EndsWith($"From ILogger: {guid2}"));
        //}

        public async Task QueueTriggerToBlobTest()
        {
            TestHelpers.ClearFunctionLogs("QueueTriggerToBlob");

            string id = Guid.NewGuid().ToString();
            string messageContent = string.Format("{{ \"id\": \"{0}\" }}", id);
            CloudQueueMessage message = new CloudQueueMessage(messageContent);

            await Fixture.TestQueue.AddMessageAsync(message);

            var resultBlob = Fixture.TestOutputContainer.GetBlockBlobReference(id);
            string result = await TestHelpers.WaitForBlobAndGetStringAsync(resultBlob);
            Assert.Equal(TestHelpers.RemoveByteOrderMarkAndWhitespace(messageContent), TestHelpers.RemoveByteOrderMarkAndWhitespace(result));

            LogMessage traceEvent = await WaitForTraceAsync(p => p?.FormattedMessage != null && p.FormattedMessage.Contains(id));
            Assert.Equal(LogLevel.Information, traceEvent.Level);

            string trace = traceEvent.FormattedMessage;
            Assert.Contains("script processed queue message", trace);
            Assert.Contains(messageContent.Replace(" ", string.Empty), trace.Replace(" ", string.Empty));
        }

        //protected async Task TwilioReferenceInvokeSucceedsImpl(bool isDotNet)
        //{
        //    if (isDotNet)
        //    {
        //        TestHelpers.ClearFunctionLogs("TwilioReference");

        //        string testData = Guid.NewGuid().ToString();
        //        string inputName = "input";
        //        Dictionary<string, object> arguments = new Dictionary<string, object>
        //        {
        //            { inputName, testData }
        //        };
        //        await Fixture.Host.CallAsync("TwilioReference", arguments);

        //        // make sure the input string made it all the way through
        //        var logs = await TestHelpers.GetFunctionLogsAsync("TwilioReference");
        //        Assert.True(logs.Any(p => p.Contains(testData)));
        //    }
        //}

        //protected async Task ServiceBusQueueTriggerToBlobTestImpl()
        //{
        //    var resultBlob = Fixture.TestOutputContainer.GetBlockBlobReference("completed");
        //    await resultBlob.DeleteIfExistsAsync();

        //    string id = Guid.NewGuid().ToString();
        //    JObject message = new JObject
        //    {
        //        { "count", 0 },
        //        { "id", id }
        //    };

        //    using (Stream stream = new MemoryStream())
        //    using (TextWriter writer = new StreamWriter(stream))
        //    {
        //        writer.Write(message.ToString());
        //        writer.Flush();
        //        stream.Position = 0;

        //        await Fixture.ServiceBusQueueClient.SendAsync(new BrokeredMessage(stream) { ContentType = "text/plain" });
        //    }

        //    // now wait for function to be invoked
        //    string result = await TestHelpers.WaitForBlobAndGetStringAsync(resultBlob);

        //    Assert.Equal(TestHelpers.RemoveByteOrderMarkAndWhitespace(id), TestHelpers.RemoveByteOrderMarkAndWhitespace(result));
        //}

        //protected async Task NotificationHubTest(string functionName)
        //{
        //    // NotificationHub tests need the following environment vars:
        //    // "AzureWebJobsNotificationHubsConnectionString" -- the connection string for NotificationHubs
        //    // "AzureWebJobsNotificationHubName"  -- NotificationHubName
        //    Dictionary<string, object> arguments = new Dictionary<string, object>
        //    {
        //        { "input",  "Hello" }
        //    };

        //    try
        //    {
        //        // Only verifying the call succeeds. It is not possible to verify
        //        // actual push notificaiton is delivered as they are sent only to
        //        // client applications that registered with NotificationHubs
        //        await Fixture.Host.CallAsync(functionName, arguments);
        //    }
        //    catch (Exception ex)
        //    {
        //        // Node: Check innerException, CSharp: check innerExcpetion.innerException
        //        if ((ex.InnerException != null && VerifyNotificationHubExceptionMessage(ex.InnerException)) ||
        //            (ex.InnerException != null & ex.InnerException.InnerException != null && VerifyNotificationHubExceptionMessage(ex.InnerException.InnerException)))
        //        {
        //            // Expected if using NH without any registrations
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //}

        //protected async Task MobileTablesTest(bool isDotNet = false)
        //{
        //    // MobileApps needs the following environment vars:
        //    // "AzureWebJobsMobileAppUri" - the URI to the mobile app

        //    // The Mobile App needs an anonymous 'Item' table

        //    // First manually create an item.
        //    string id = Guid.NewGuid().ToString();
        //    Dictionary<string, object> arguments = new Dictionary<string, object>
        //    {
        //        { "input", id }
        //    };
        //    await Fixture.Host.CallAsync("MobileTableOut", arguments);
        //    var item = await WaitForMobileTableRecordAsync("Item", id);

        //    Assert.Equal(item["id"], id);

        //    string messageContent = string.Format("{{ \"recordId\": \"{0}\" }}", id);
        //    await Fixture.MobileTablesQueue.AddMessageAsync(new CloudQueueMessage(messageContent));

        //    // Only .NET fully supports updating from input bindings. Others will
        //    // create a new item with -success appended to the id.
        //    // https://github.com/Azure/azure-webjobs-sdk-script/issues/49
        //    var idToCheck = id + (isDotNet ? string.Empty : "-success");
        //    var textToCheck = isDotNet ? "This was updated!" : null;
        //    await WaitForMobileTableRecordAsync("Item", idToCheck, textToCheck);
        //}

        protected async Task<IEnumerable<CloudBlockBlob>> Scenario_RandGuidBinding_GeneratesRandomIDs()
        {
            var container = await GetEmptyContainer("scenarios-output");

            // Call 3 times - expect 3 separate output blobs
            for (int i = 0; i < 3; i++)
            {
                JObject input = new JObject
                {
                    { "scenario", "randGuid" },
                    { "container", "scenarios-output" },
                    { "value", i }
                };

                await Fixture.Host.BeginFunctionAsync("Scenarios", input);
            }

            IEnumerable<CloudBlockBlob> blobs = null;

            await TestHelpers.Await(async () =>
            {
                blobs = await TestHelpers.ListBlobsAsync(container);
                return blobs.Count() == 3;
            });

            // Different languages write different content, so let them validate the blobs.
            return blobs;
        }

        protected async Task<CloudBlobContainer> GetEmptyContainer(string containerName)
        {
            var container = Fixture.BlobClient.GetContainerReference(containerName);
            await TestHelpers.ClearContainerAsync(container);
            return container;
        }

        protected async Task<JToken> WaitForMobileTableRecordAsync(string tableName, string itemId, string textToMatch = null)
        {
            // We know the tests are using the default INameResolver and this setting.
            var mobileAppUri = _nameResolver.Resolve("AzureWebJobs_TestMobileUri");
            var client = new MobileServiceClient(new Uri(mobileAppUri));
            JToken item = null;
            var table = client.GetTable(tableName);
            await TestHelpers.Await(() =>
            {
                bool result = false;
                try
                {
                    item = Task.Run(() => table.LookupAsync(itemId)).Result;
                    if (textToMatch != null)
                    {
                        result = item["Text"].ToString() == textToMatch;
                    }
                    else
                    {
                        result = true;
                    }
                }
                catch (AggregateException aggEx)
                {
                    var ex = (MobileServiceInvalidOperationException)aggEx.InnerException;
                    if (ex.Response.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }

                return result;
            });

            return item;
        }

        protected async Task<Document> WaitForDocumentAsync(string itemId, string textToMatch = null)
        {
            var docUri = UriFactory.CreateDocumentUri("ItemDb", "ItemCollection", itemId);

            // We know the tests are using the default INameResolver and the default setting.
            var connectionString = _nameResolver.Resolve("AzureWebJobsDocumentDBConnectionString");
            var builder = new DbConnectionStringBuilder();
            builder.ConnectionString = connectionString;
            var serviceUri = new Uri(builder["AccountEndpoint"].ToString());
            var client = new DocumentClient(serviceUri, builder["AccountKey"].ToString());

            Document doc = null;
            await TestHelpers.Await(() =>
            {
                bool result = false;
                try
                {
                    var response = Task.Run(() => client.ReadDocumentAsync(docUri)).Result;
                    doc = response.Resource;

                    if (textToMatch != null)
                    {
                        result = doc.GetPropertyValue<string>("text") == textToMatch;
                    }
                    else
                    {
                        result = true;
                    }
                }
                catch (Exception)
                {
                    string logs = string.Join(Environment.NewLine, Fixture.Host.GetLogMessages());
                }

                return result;
            });

            return doc;
        }

        protected static bool VerifyNotificationHubExceptionMessage(Exception exception)
        {
            if ((exception.Source == "Microsoft.Azure.NotificationHubs")
                && exception.Message.Contains("notification has no target applications"))
            {
                // Expected if using NH without any registrations
                return true;
            }
            return false;
        }

        protected async Task<LogMessage> WaitForTraceAsync(string functionName, Func<LogMessage, bool> filter)
        {
            LogMessage logMessage = null;

            await TestHelpers.Await(() =>
            {
                logMessage = Fixture.Host.GetLogMessages(LogCategories.CreateFunctionUserCategory(functionName)).SingleOrDefault(filter);
                return logMessage != null;
            });

            return logMessage;
        }

        protected async Task<LogMessage> WaitForTraceAsync(Func<LogMessage, bool> filter)
        {
            LogMessage logMessage = null;

            await TestHelpers.Await(() =>
            {
                logMessage = Fixture.Host.GetLogMessages().SingleOrDefault(filter);
                return logMessage != null;
            });

            return logMessage;
        }

        protected async Task<JObject> GetFunctionTestResult(string functionName)
        {
            string logEntry = null;

            await TestHelpers.Await(() =>
           {
               // search the logs for token "TestResult:" and parse the following JSON
               var logs = Fixture.Host.GetLogMessages(LogCategories.CreateFunctionUserCategory(functionName));
               if (logs != null)
               {
                   logEntry = logs.Select(p => p.FormattedMessage).SingleOrDefault(p => p != null && p.Contains("TestResult:"));
               }
               return logEntry != null;
           });

            int idx = logEntry.IndexOf("{");
            logEntry = logEntry.Substring(idx);

            return JObject.Parse(logEntry);
        }

        public class ScenarioInput
        {
            [JsonProperty("scenario")]
            public string Scenario { get; set; }

            [JsonProperty("container")]
            public string Container { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}