﻿using System;
using System.Collections.Generic;
using SimpleBatch;

namespace RunnerHost
{
    internal class Configuration : IConfiguration
    {
        private IList<ICloudBlobBinderProvider> _blobBinders = new List<ICloudBlobBinderProvider>();

        private IList<ICloudTableBinderProvider> _tableBinders = new List<ICloudTableBinderProvider>();

        private IList<ICloudBinderProvider> _Binders = new List<ICloudBinderProvider>();

        public IList<ICloudBlobBinderProvider> BlobBinders
        {
            get { return _blobBinders; }
        }


        public IList<ICloudBinderProvider> Binders
        {
            get { return _Binders; }
        }

        public IFluentConfig Register(string functionName)
        {
            throw new NotImplementedException();
        }


        public IList<ICloudTableBinderProvider> TableBinders
        {
            get { return _tableBinders; }
        }
    }
}
