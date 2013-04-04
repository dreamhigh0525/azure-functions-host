﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using Newtonsoft.Json;
using Orchestrator;
using RunnerInterfaces;
using SimpleBatch;

namespace OrchestratorUnitTests
{
    [TestClass]
    public class LocalOrchestratorTests
    {
        [TestMethod]
        public void InvokeConfig()
        {
            var account = TestStorage.GetAccount();
            MethodInfo m = typeof(Program).GetMethod("TestConfig");

            LocalOrchestrator.Invoke(account, m);
        }

        [TestMethod]
        public void InvokeWithParams()
        {
            var account = TestStorage.GetAccount();

            Utility.DeleteContainer(account, "daas-test-input");
            Utility.WriteBlob(account, "daas-test-input", "note-monday.csv", "abc");

            MethodInfo m = typeof(Program).GetMethod("FuncWithNames");

            var d = new Dictionary<string, string>() {
                { "name", "note" },
                { "date" , "monday" },
                { "unbound", "test" },
                { "target", "out"}
            };
            LocalOrchestrator.Invoke(account, m, d);

            string content = Utility.ReadBlob(account, "daas-test-input", "out.csv");
            Assert.AreEqual("done", content);

            {
                // $$$ Put this in its own unit test?
                IBlobCausalityLogger logger = new BlobCausalityLogger();
                var blob = Utility.GetBlob(account, "daas-test-input", "out.csv");
                var guid = logger.GetWriter(blob);

                Assert.IsTrue(guid != Guid.Empty, "Blob is missing causality information");
            }
        }

        [TestMethod]
        public void InvokeWithParamsParser()
        {
            var account = TestStorage.GetAccount();
            Utility.DeleteContainer(account, "daas-test-input");

            MethodInfo m = typeof(Program).GetMethod("ParseArgument");

            var d = new Dictionary<string, string>() {
                { "x", "15" },
            };
            LocalOrchestrator.Invoke(account, m, d);

            string content = Utility.ReadBlob(account, "daas-test-input", "out.csv");
            Assert.AreEqual("16", content);
        }

        [TestMethod]
        public void InvokeOnBlob()
        {
            var account = TestStorage.GetAccount();

            Utility.WriteBlob(account, "daas-test-input", "input.csv", "abc");

            MethodInfo m = typeof(Program).GetMethod("Func1");
            LocalOrchestrator.Invoke(account, m);

            string content = Utility.ReadBlob(account, "daas-test-input", "input.csv");

            Assert.AreEqual("abc", content);
        }

        // Test binding a parameter to the CloudStorageAccount that a function is uploaded to. 
        [TestMethod]
        public void TestBindCloudStorageAccount()
        {
            var account = TestStorage.GetAccount();

            MethodInfo m = typeof(Program).GetMethod("FuncCloudStorageAccount");

            var args = new {
                containerName = "daas-test-input",
                blobName = "input.txt",
                value = "abc"
            };
            
            LocalOrchestrator.Invoke(account, m, ObjectBinderHelpers.ConvertObjectToDict(args));

            string content = Utility.ReadBlob(account, args.containerName, args.blobName);
            Assert.AreEqual(args.value, content);
        }

        [TestMethod]
        public void TestEnqueueMessage()
        {
            var account = TestStorage.GetAccount();

            CloudQueue queue = account.CreateCloudQueueClient().GetQueueReference("myoutputqueue");
            Utility.DeleteQueue(queue);

            MethodInfo m = typeof(Program).GetMethod("FuncEnqueue");
            LocalOrchestrator.Invoke(account, m);

            var msg = queue.GetMessage();
            Assert.IsNotNull(msg);

            QueueCausalityHelper qcm = new QueueCausalityHelper();
            string data = qcm.DecodePayload(msg);
            Payload payload = JsonConvert.DeserializeObject<Payload>(data);

            Assert.AreEqual(15, payload.Value);
        }

        [TestMethod]
        public void TestMultiEnqueueMessage_IEnumerable()
        {
            var account = TestStorage.GetAccount();

            CloudQueue queue = account.CreateCloudQueueClient().GetQueueReference("myoutputqueue");
            Utility.DeleteQueue(queue);

            MethodInfo m = typeof(Program).GetMethod("FuncMultiEnqueueIEnumerable");
            LocalOrchestrator.Invoke(account, m);

            for (int i = 10; i <= 30; i += 10)
            {
                var msg = queue.GetMessage();
                Assert.IsNotNull(msg);

                QueueCausalityHelper qcm = new QueueCausalityHelper();
                string data = qcm.DecodePayload(msg);
                queue.DeleteMessage(msg);

                Payload payload = JsonConvert.DeserializeObject<Payload>(data);

                // Technically, ordering is not gauranteed.
                Assert.AreEqual(i, payload.Value);
            }

            {
                var msg = queue.GetMessage();
                Assert.IsNull(msg); // no more messages
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestMultiEnqueueMessage_NestedIEnumerable_Throws()
        {
            var account = TestStorage.GetAccount();

            MethodInfo m = typeof(Program).GetMethod("FuncMultiEnqueueIEnumerableNested");

            LocalOrchestrator.Invoke(account, m);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestQueueOutput_IList_Throws()
        {
            var account = TestStorage.GetAccount();

            MethodInfo m = typeof(Program).GetMethod("FuncQueueOutputIList");

            LocalOrchestrator.Invoke(account, m);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestQueueOutput_Object_Throws()
        {
            var account = TestStorage.GetAccount();

            MethodInfo m = typeof(Program).GetMethod("FuncQueueOutputObject");

            LocalOrchestrator.Invoke(account, m);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestQueueOutput_IEnumerableOfObject_Throws()
        {
            var account = TestStorage.GetAccount();

            MethodInfo m = typeof(Program).GetMethod("FuncQueueOutputIEnumerableOfObject");

            LocalOrchestrator.Invoke(account, m);
        }

        // Model bind a parameter to a queueing inteface, and then enqueue multiple messages.
        [TestMethod]
        public void TestMultiEnqueueMessage()
        {
            var account = TestStorage.GetAccount();

            CloudQueue queue = account.CreateCloudQueueClient().GetQueueReference("myoutputqueue");
            Utility.DeleteQueue(queue);

            MethodInfo m = typeof(Program).GetMethod("FuncMultiEnqueue");
            LocalOrchestrator.Invoke(account, m);

            for (int i = 10; i <= 30; i += 10)
            {
                var msg = queue.GetMessage();
                Assert.IsNotNull(msg);

                string data = msg.AsString;
                queue.DeleteMessage(msg);

                Payload payload = JsonConvert.DeserializeObject<Payload>(data);

                // Technically, ordering is not gauranteed.
                Assert.AreEqual(i, payload.Value);
            }

            {
                var msg = queue.GetMessage();
                Assert.IsNull(msg); // no more messages
            }
        }

        [TestMethod]
        public void TestEnqueueMessage_UsingCloudQueue()
        {
            var account = TestStorage.GetAccount();

            CloudQueue queue = account.CreateCloudQueueClient().GetQueueReference("myoutputqueue");
            Utility.DeleteQueue(queue);

            MethodInfo m = typeof(Program).GetMethod("FuncCloudQueueEnqueue");
            LocalOrchestrator.Invoke(account, m);

            for (int i = 10; i <= 30; i += 10)
            {
                var msg = queue.GetMessage();
                Assert.IsNotNull(msg);

                string data = msg.AsString;
                queue.DeleteMessage(msg);

                Assert.AreEqual(i.ToString(), data);
            }

            {
                var msg = queue.GetMessage();
                Assert.IsNull(msg); // no more messages
            }
        }

        // $$$ Test with directories
        [TestMethod]
        public void Aggregate()
        {
            var account = TestStorage.GetAccount();

            Utility.DeleteContainer(account, "daas-test-input");
            Utility.WriteBlob(account, "daas-test-input", "1.csv", "abc");
            Utility.WriteBlob(account, "daas-test-input", "2.csv", "def");

            MethodInfo m = typeof(Program).GetMethod("Aggregate1");
            LocalOrchestrator.Invoke(account, m);

            string content = Utility.ReadBlob(account, "daas-test-input", "output.csv");
            Assert.AreEqual("abcdef", content);
        }

        [TestMethod]
        public void Aggregate2()
        {
            var account = TestStorage.GetAccount();

            Utility.DeleteContainer(account, "daas-test-input");
            Utility.WriteBlob(account, "daas-test-input", @"test\1.csv", "abc");
            Utility.WriteBlob(account, "daas-test-input", @"test\2.csv", "def");

            var d = new Dictionary<string, string>() {
                { "deployId", "test" },
                { "outdir", "testoutput" }
            };
            MethodInfo m = typeof(Program).GetMethod("Aggregate2");
            LocalOrchestrator.Invoke(account, m, d);

            string content = Utility.ReadBlob(account, "daas-test-input", @"testoutput\output.csv");
            Assert.AreEqual("abcdef", content);
        }

        private class Program
        {
            public static void Aggregate1(
                [BlobInputs(@"daas-test-input\{names}.csv")] TextReader[] inputs,

                //string[] names, // $$$ How do we get this?
                [BlobOutput(@"daas-test-input\output.csv")] TextWriter output
                )
            {
                int len = 2;
                Assert.AreEqual(len, inputs.Length);

                //Assert.AreEqual(len, names.Length);

                StringBuilder sb = new StringBuilder();
                foreach (var input in inputs)
                {
                    string content = input.ReadToEnd();
                    sb.Append(content);
                }
                output.Write(sb.ToString());
            }

            // Test with user-provided parameters
            public static void Aggregate2(
                string deployId, // user provided? or from attribute (like names?)
                [BlobInputsAttribute(@"daas-test-input\{deployId}\{names}.csv")] TextReader[] inputs,

                //string[] names
                [BlobOutput(@"daas-test-input\{outdir}\output.csv")] TextWriter output
            )
            {
                Aggregate1(inputs, output);
            }

            // $$$ What does this mean? Multiple inputs. Do they need to match? Is there a precedence?
            public static void Aggregate3(
                string deployId, // user provided
                [BlobInputsAttribute(@"daas-test-input\{deployId}\{names}.csv")] TextReader[] inputs,
                [BlobInputsAttribute(@"daas-test-input\other\{names}.csv")] TextReader[] inputs2,
                string[] names // $$$ which populates this? are arrays parallel?
            )
            {
            }

            // This can be invoked explicitly (and providing parameters)
            // or it can be invoked implicitly by triggering on input. // (assuming no unbound parameters)
            public static void FuncWithNames(
                string name, string date,  // used by input
                string unbound, // not used by in/out
                string target, // only used by output
                [BlobInput(@"daas-test-input\{name}-{date}.csv")] TextReader values,
                [BlobOutput(@"daas-test-input\{target}.csv")] TextWriter output
                )
            {
                Assert.AreEqual("test", unbound);
                Assert.AreEqual("note", name);
                Assert.AreEqual("monday", date);

                string content = values.ReadToEnd();
                Assert.AreEqual("abc", content);

                output.Write("done");
            }

            [NoAutomaticTrigger]
            public static void ParseArgument(int x, [BlobOutput(@"daas-test-input\out.csv")] TextWriter output)
            {
                output.Write(x + 1);
            }

            public static void Func1(
                [BlobInput(@"daas-test-input\input.csv")] TextReader values,
                [BlobOutput(@"daas-test-input\output.csv")] TextWriter output)
            {
                string content = values.ReadToEnd();
                output.Write(content);
            }

            public static void FuncEnqueue(
                [QueueOutput] out Payload myoutputqueue)
            {
                // Send payload to Azure queue "myoutputqueue"
                myoutputqueue = new Payload { Value = 15 };
            }

            public static void FuncMultiEnqueueIEnumerable(
                [QueueOutput] out IEnumerable<Payload> myoutputqueue)
            {
                List<Payload> payloads = new List<Payload>();
                payloads.Add(new Payload { Value = 10 });
                payloads.Add(new Payload { Value = 20 });
                payloads.Add(new Payload { Value = 30 });
                myoutputqueue = payloads;
            }

            public static void FuncMultiEnqueueIEnumerableNested(
                [QueueOutput] out IEnumerable<IEnumerable<Payload>> myoutputqueue)
            {
                List<IEnumerable<Payload>> payloads = new List<IEnumerable<Payload>>();
                myoutputqueue = payloads;
            }

            public static void FuncQueueOutputIEnumerableOfObject(
                [QueueOutput] out IEnumerable<object> myoutputqueue)
            {
                List<object> payloads = new List<object>();
                myoutputqueue = payloads;
            }

            public static void FuncQueueOutputObject(
                [QueueOutput] out object myoutputqueue)
            {
                List<IEnumerable<Payload>> payloads = new List<IEnumerable<Payload>>();
                myoutputqueue = payloads;
            }

            public static void FuncQueueOutputIList(
                [QueueOutput] out IList<Payload> myoutputqueue)
            {
                List<Payload> payloads = new List<Payload>();
                myoutputqueue = payloads;
            }

            [SimpleBatch.Description("cloud queue function")] // needed for indexing since we have no other attrs
            public static void FuncCloudQueueEnqueue(
                CloudQueue myoutputqueue)
            {
                myoutputqueue.AddMessage(new CloudQueueMessage("10"));
                myoutputqueue.AddMessage(new CloudQueueMessage("20"));
                myoutputqueue.AddMessage(new CloudQueueMessage("30"));
            }

            [SimpleBatch.Description("queue function")] // needed for indexing since we have no other attrs
            public static void FuncMultiEnqueue(
               IQueueOutput<Payload> myoutputqueue)
            {
                myoutputqueue.Add(new Payload { Value = 10 });
                myoutputqueue.Add(new Payload { Value = 20 });
                myoutputqueue.Add(new Payload { Value = 30 });
            }

            public static void TestConfig([Config("ConfigTest.txt")] Payload payload)
            {
                Assert.AreEqual(payload.Value, 15);
            }

            // Test binding to CloudStorageAccount 
            [SimpleBatch.Description("test")]
            public static void FuncCloudStorageAccount(CloudStorageAccount account, string value, string containerName, string blobName)
            {
                Utility.WriteBlob(account, containerName, blobName, value);
            }
        }

        private class Payload
        {
            public int Value { get; set; }
        }
    }
}