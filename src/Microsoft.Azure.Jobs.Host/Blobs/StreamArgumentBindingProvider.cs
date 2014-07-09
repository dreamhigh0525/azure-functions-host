﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Blobs.Bindings;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Jobs.Host.Blobs
{
    internal class StreamArgumentBindingProvider : IBlobArgumentBindingProvider
    {
        public IBlobArgumentBinding TryCreate(ParameterInfo parameter, FileAccess? access)
        {
            if (parameter.ParameterType != typeof(Stream))
            {
                return null;
            }

            if (access.HasValue && access.Value == FileAccess.ReadWrite)
            {
                throw new InvalidOperationException("Cannot bind blob to Stream using access ReadWrite.");
            }

            if (!access.HasValue || access.Value == FileAccess.Read)
            {
                return new ReadStreamArgumentBinding();
            }
            else
            {
                return new WriteStreamArgumentBinding();
            }
        }

        private class ReadStreamArgumentBinding : IBlobArgumentBinding
        {
            public FileAccess Access
            {
                get { return FileAccess.Read; }
            }

            public Type ValueType
            {
                get { return typeof(Stream); }
            }

            public IValueProvider Bind(ICloudBlob blob, FunctionBindingContext context)
            {
                Stream rawStream = blob.OpenRead();
                WatchableReadStream watchableStream = new WatchableReadStream(rawStream);
                return new BlobWatchableDisposableValueProvider(blob, watchableStream, typeof(Stream),
                    watcher: watchableStream, disposable: watchableStream);
            }
        }

        private class WriteStreamArgumentBinding : IBlobArgumentBinding
        {
            public FileAccess Access
            {
                get { return FileAccess.Write; }
            }

            public Type ValueType
            {
                get { return typeof(Stream); }
            }

            public IValueProvider Bind(ICloudBlob blob, FunctionBindingContext context)
            {
                CloudBlockBlob blockBlob = blob as CloudBlockBlob;

                if (blockBlob == null)
                {
                    throw new InvalidOperationException("Cannot bind a page blob using a Stream.");
                }

                CloudBlobStream rawStream = blockBlob.OpenWrite();
                IBlobCommitedAction committedAction = new BlobCommittedAction(blob, context.FunctionInstanceId,
                    context.BlobWrittenWatcher);
                WatchableCloudBlobStream watchableStream = new WatchableCloudBlobStream(rawStream, committedAction);
                return new WriteStreamValueBinder(blob, watchableStream);
            }

            // There's no way to dispose a CloudBlobStream without committing.
            // This class intentionally does not implement IDisposable because there's nothing it can do in Dispose.
            private sealed class WriteStreamValueBinder : IValueBinder, IWatchable
            {
                private readonly ICloudBlob _blob;
                private readonly WatchableCloudBlobStream _stream;

                public WriteStreamValueBinder(ICloudBlob blob, WatchableCloudBlobStream stream)
                {
                    _blob = blob;
                    _stream = stream;
                }

                public Type Type
                {
                    get { return typeof(Stream); }
                }

                public IWatcher Watcher
                {
                    get { return _stream; }
                }

                public object GetValue()
                {
                    return _stream;
                }

                public void SetValue(object value)
                {
                    // Not ByRef, so can ignore value argument.

                    // Determine whether or not to upload the blob.
                    if (_stream.Complete())
                    {
                        _stream.Dispose(); // Can only dispose when committing; see note on class above.
                    }
                }

                public string ToInvokeString()
                {
                    return _blob.GetBlobPath();
                }
            }
        }
    }
}
