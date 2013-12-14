﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure.StorageClient;

namespace Microsoft.WindowsAzure.Jobs
{
    // Wrap facilities for logging a function's output. 
    // This means capturing console out, redirecting to a textwriter that is available at a blob.
    // Handle incremental updates to get real-time updates for long running functions. 
    internal class FunctionOutputLog
    {
        static Action empty = () => { };

        public FunctionOutputLog()
        {
            this.Output = Console.Out;
            this.CloseOutput = empty;
        }

        public TextWriter Output { get; set; }
        public Action CloseOutput { get; set; }
        public string Uri { get; set; } // Uri to refer to output 

        // Separate channel for logging structured (and updating) information about parameters
        public CloudBlobDescriptor ParameterLogBlob { get; set; }


        // Get a default instance of 
        public static FunctionOutputLog GetLogStream(FunctionInvokeRequest f, string accountConnectionString, string containerName)
        {            
            string name = f.ToString() + ".txt";

            var c = Utility.GetContainer(accountConnectionString, containerName);
            if (c.CreateIfNotExist())
            {
                c.SetPermissions(new BlobContainerPermissions() { PublicAccess = BlobContainerPublicAccessType.Off });
            }

            CloudBlob blob = c.GetBlobReference(name);            
            
            var period = TimeSpan.FromMinutes(1); // frequency to refresh
            var x = new BlobIncrementalTextWriter(blob, period);

            TextWriter tw = x.Writer;

            return new FunctionOutputLog
            {
                CloseOutput = () =>
                {
                    x.Close();
                },
                Uri = blob.Uri.ToString(),
                Output = tw,
                ParameterLogBlob = new CloudBlobDescriptor
                {
                     AccountConnectionString = accountConnectionString,
                     ContainerName = containerName,
                     BlobName = f.ToString() + ".params.txt"
                }
            };
        }
    }
}
