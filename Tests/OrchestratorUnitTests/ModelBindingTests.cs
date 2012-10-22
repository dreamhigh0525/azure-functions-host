﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunnerInterfaces;
using Microsoft.WindowsAzure.StorageClient;
using System.Reflection;
using Orchestrator;
using SimpleBatch;

namespace OrchestratorUnitTests
{
    /// <summary>
    /// Summary description for ModelbindingTests
    /// </summary>
    [TestClass]
    public class ModelBindingTests
    {
        [TestMethod]
        public void TestModelBinding()
        {
            var account = TestStorage.GetAccount();

            Utility.DeleteContainer(account, "daas-test-input");
            Utility.WriteBlob(account, "daas-test-input", "input.txt", "abc");

            MethodInfo m = typeof(Program).GetMethod("Func");

            // ### Get default config, then add to it. 
            IConfiguration config = RunnerHost.Program.InitBinders();
            config.BlobBinders.Add(new ModelBlobBinderProvider());

            LocalOrchestrator.InvokeOnBlob(account, config, m, @"daas-test-input\input.txt");

            string content = Utility.ReadBlob(account, "daas-test-input", "output.txt");
            Assert.AreEqual("*abc*", content);
        }

        class ModelBlobBinderProvider : ICloudBlobBinderProvider
        {
            // Helper to include a cleanup function with bind result
            class BindCleanupResult : BindResult
            {
                public Action<object> Cleanup;

                public override void OnPostAction()
                {
                    if (Cleanup != null)
                    {
                        Cleanup(this.Result);
                    }
                }
            }

            class ModelInputBlobBinder : ICloudBlobBinder
            {
                public BindResult Bind(IBinder bindingContext, string containerName, string blobName, Type targetType)
                {
                    CloudBlob blob = GetBlob(bindingContext.AccountConnectionString, containerName, blobName);

                    var content = blob.DownloadText();
                    return new BindResult { Result = new Model { Value = content }  };
                }
            }

            class ModelOutputBlobBinder : ICloudBlobBinder
            {
                public BindResult Bind(IBinder bindingContext, string containerName, string blobName, Type targetType)
                {
                    CloudBlob blob = GetBlob(bindingContext.AccountConnectionString, containerName, blobName);

                    // On input
                    return new BindCleanupResult
                    {
                        Result = null,
                        Cleanup = (newResult) =>
                        {
                            Model model = (Model)newResult;
                            blob.UploadText(model.Value);
                        }
                    };
                }
            }

            public ICloudBlobBinder TryGetBinder(Type targetType, bool isInput)
            {
                if (targetType == typeof(Model))
                {
                    if (isInput)
                    {
                        return new ModelInputBlobBinder();
                    }
                    else
                    {
                        return new ModelOutputBlobBinder();
                    }
                }
                return null;
            }

            private static CloudBlob GetBlob(string accountConnectionString, string containerName, string blobName)
            {
                var account = Utility.GetAccount(accountConnectionString);
                var client = account.CreateCloudBlobClient();
                var c = client.GetContainerReference(containerName);
                var blob = c.GetBlobReference(blobName);
                return blob;
            }
        }

        class Program
        {
            public static void Func(
                [BlobInput(@"daas-test-input\input.txt")] Model input,
                [BlobOutput(@"daas-test-input\output.txt")] out Model output)
            {
                output = new Model { Value = "*" + input.Value + "*" };
            }
        }

        class Model
        {
            public string Value;
        }
    }
}
