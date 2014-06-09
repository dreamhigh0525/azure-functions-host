﻿using System.IO;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Jobs.Host.Blobs
{
    internal interface IBlobArgumentBinding : IArgumentBinding<ICloudBlob>
    {
        FileAccess Access { get; }
    }
}
