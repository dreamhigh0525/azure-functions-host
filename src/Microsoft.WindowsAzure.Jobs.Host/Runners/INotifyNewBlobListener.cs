﻿using System;
using System.Threading;

namespace Microsoft.WindowsAzure.Jobs
{
    // Listen on new blobs, invoke a callback when they're detected.
    // This is a fast-path form of blob listening. 
    // ### Can this be merged with the other general blob listener or IBlobListener?     
    internal interface INotifyNewBlobListener
    {
        void ProcessMessages(Action<BlobWrittenMessage, CancellationToken> fpOnNewBlob, CancellationToken token);
    }
}
