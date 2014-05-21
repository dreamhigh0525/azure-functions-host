﻿using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.Azure.Jobs.Host.UnitTests
{
    // Unit test the static parameter bindings. This primarily tests the indexer.
    public class FlowUnitTests
    {
        // Helper to do the indexing.
        private static FunctionDefinition Get(string methodName)
        {
            MethodInfo m = typeof(FlowUnitTests).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(m);

            FunctionDefinition func = Indexer.GetFunctionDefinition(m);
            return func;
        }

        [NoAutomaticTrigger]
        private static void NoAutoTrigger1([BlobInput(@"daas-test-input/{name}.csv")] TextReader inputs) { }

        [Fact]
        public void TestNoAutoTrigger1()
        {
            FunctionDefinition func = Get("NoAutoTrigger1");
            Assert.Equal(false, func.Trigger.ListenOnBlobs);
        }

        public static void AutoTrigger1([BlobInput(@"daas-test-input/{name}.csv")] TextReader inputs) { }
                   
        [Fact]
        public void TestAutoTrigger1()        
        {
            FunctionDefinition func = Get("AutoTrigger1");
            Assert.Equal(true, func.Trigger.ListenOnBlobs);
        }

        [NoAutomaticTrigger]
        public static void NoAutoTrigger2(int x, int y) { }
                   
        [Fact]
        public void TestNoAutoTrigger2()
        {
            FunctionDefinition func = Get("NoAutoTrigger2");
            Assert.Equal(false, func.Trigger.ListenOnBlobs);
        }

        // Nothing about this method that is indexable.
        // No function (no trigger)
        public static void NoIndex(int x, int y) { }
        
        [Fact]
        public void TestNoIndex()
        {
            FunctionDefinition func = Get("NoIndex");
            Assert.Null(func);
        }

        // Runtime type is irrelevant for table. 
        private static void Table([Table("TableName")] object reader) { }

        [Fact]
        public void TestTable()
        {
            FunctionDefinition func = Get("Table");

            var flows = func.Flow.Bindings;
            Assert.Equal(1, flows.Length);

            var t = (TableParameterStaticBinding ) flows[0];
            Assert.Equal("TableName", t.TableName);
            Assert.Equal("reader", t.Name);
        }

        // Queue inputs with implicit names.
        public static void QueueIn([QueueInput] int inputQueue) { }

        [Fact]
        public void TestQueueInput()
        {
            FunctionDefinition func = Get("QueueIn");

            var flows = func.Flow.Bindings;
            Assert.Equal(1, flows.Length);

            var t = (QueueParameterStaticBinding)flows[0];            
            Assert.Equal("inputqueue", t.QueueName); // queue name gets normalized. 
            Assert.Equal("inputQueue", t.Name); // parameter name does not.
            Assert.True(t.IsInput);
        }

        // Queue inputs with implicit names.
        public static void QueueOutput([QueueOutput] out int inputQueue)
        {
            inputQueue = 0;
        }                  

        [Fact]
        public void TestQueueOutput()
        {
            FunctionDefinition func = Get("QueueOutput");
                        
            var flows = func.Flow.Bindings;
            Assert.Equal(1, flows.Length);

            var t = (QueueParameterStaticBinding)flows[0];
            Assert.Equal("inputqueue", t.QueueName);
            Assert.Equal("inputQueue", t.Name); // parameter name does not.
            Assert.False(t.IsInput);
        }

        [Jobs.Description("This is a description")]
        public static void DescriptionOnly(string stuff) { }

        [Fact]
        public void TestDescriptionOnly()
        {
            FunctionDefinition func = Get("DescriptionOnly");
            
            Assert.Equal(false, func.Trigger.ListenOnBlobs); // no blobs
            
            var flows = func.Flow.Bindings;
            Assert.Equal(1, flows.Length);

            // Assumes any unrecognized parameters are supplied by the user
            var t = (InvokeParameterStaticBinding)flows[0];
            Assert.Equal("stuff", t.Name);
        }

        // Has an unbound parameter, so this will require an explicit invoke.  
        // Trigger: NoListener, explicit
        public static void HasBlobAndUnboundParameter([BlobInput("container")] Stream input, int unbound) { }
        
        [Fact]
        public void TestHasBlobAndUnboundParameter()
        {
            FunctionDefinition func = Get("HasBlobAndUnboundParameter");

            Assert.Equal(true, func.Trigger.ListenOnBlobs); // no blobs

            var flows = func.Flow.Bindings;
            Assert.Equal(2, flows.Length);

            var t0 = (BlobParameterStaticBinding)flows[0];
            Assert.Equal("container", t0.Path.ContainerName);
                        
            var t1 = (InvokeParameterStaticBinding)flows[1];
            Assert.Equal("unbound", t1.Name);
        }

        // Both parameters are bound. 
        // Trigger: Automatic listener
        public static void HasBlobAndBoundParameter([BlobInput(@"container/{bound}")] Stream input, int bound) { }

        [Fact]
        public void TestHasBlobAndBoundParameter()
        {
            FunctionDefinition func = Get("HasBlobAndBoundParameter");

            Assert.Equal(true, func.Trigger.ListenOnBlobs); // all parameters are bound

            var flows = func.Flow.Bindings;
            Assert.Equal(2, flows.Length);

            var t0 = (BlobParameterStaticBinding)flows[0];
            Assert.Equal("container", t0.Path.ContainerName);

            var t1 = (NameParameterStaticBinding)flows[1];
            Assert.Equal("bound", t1.Name);
        }        
    }
}
