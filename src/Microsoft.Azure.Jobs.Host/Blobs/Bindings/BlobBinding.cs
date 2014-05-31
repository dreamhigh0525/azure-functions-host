﻿using System;
using System.IO;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Converters;
using Microsoft.Azure.Jobs.Host.Protocols;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Jobs.Host.Blobs.Bindings
{
    internal class BlobBinding : IBinding
    {
        private readonly IArgumentBinding<ICloudBlob> _argumentBinding;
        private readonly CloudBlobClient _client;
        private readonly string _containerName;
        private readonly string _blobName;
        private readonly IObjectToTypeConverter<ICloudBlob> _converter;
        private readonly bool _outParameter;

        public BlobBinding(IArgumentBinding<ICloudBlob> argumentBinding, CloudStorageAccount account,
            string containerName, string blobName, bool outParameter)
        {
            _argumentBinding = argumentBinding;
            _client = account.CreateCloudBlobClient();
            _containerName = containerName;
            _blobName = blobName;
            _outParameter = outParameter;
            _converter = CreateConverter(_client);
        }

        private static IObjectToTypeConverter<ICloudBlob> CreateConverter(CloudBlobClient client)
        {
            return new CompositeObjectToTypeConverter<ICloudBlob>(
                new OutputConverter<ICloudBlob>(new IdentityConverter<ICloudBlob>()),
                new OutputConverter<string>(new StringToCloudBlobConverter(client)));
        }

        public string ContainerName
        {
            get { return _containerName; }
        }

        public string BlobName
        {
            get { return _containerName; }
        }

        public string BlobPath
        {
            get { return _containerName + "/" + _blobName; }
        }

        public bool IsInput
        {
            get
            {
                return _argumentBinding.ValueType != typeof(TextWriter) &&
                    _argumentBinding.ValueType != typeof(CloudBlobStream) && !_outParameter;
            }
        }

        private IValueProvider Bind(ICloudBlob value, ArgumentBindingContext context)
        {
            return _argumentBinding.Bind(value, context);
        }

        public IValueProvider Bind(BindingContext context)
        {
            string resolvedPath = RouteParser.ApplyBindingData(BlobPath, context.BindingData);
            CloudBlobPath parsedResolvedPath = new CloudBlobPath(resolvedPath);
            CloudBlobContainer container = _client.GetContainerReference(parsedResolvedPath.ContainerName);

            Type argumentType = _argumentBinding.ValueType;
            string blobName = parsedResolvedPath.BlobName;
            ICloudBlob blob;

            if (argumentType == typeof(CloudBlockBlob))
            {
                blob = container.GetBlockBlobReference(blobName);
            }
            else if (argumentType == typeof(CloudPageBlob))
            {
                blob = container.GetPageBlobReference(blobName);
            }
            else
            {
                blob = container.GetExistingOrNewBlockBlobReference(blobName);
            }

            return Bind(blob, context);
        }

        public IValueProvider Bind(object value, ArgumentBindingContext context)
        {
            ICloudBlob blob = null;

            if (!_converter.TryConvert(value, out blob))
            {
                throw new InvalidOperationException("Unable to convert value to ICloudBlob.");
            }

            return Bind(blob, context);
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new BlobParameterDescriptor
            {
                ContainerName = _containerName,
                BlobName = _blobName,
                IsInput = IsInput
            };
        }
    }
}
