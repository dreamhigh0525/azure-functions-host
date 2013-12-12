﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using SimpleBatch;

namespace RunnerInterfaces
{
    internal static class IConfigurationExtensions
    {
        public static ICloudBinder GetBinder(this IConfiguration config, Type targetType)
        {
            foreach (var provider in config.Binders)
            {
                var binder = provider.TryGetBinder(targetType);
                if (binder != null)
                {
                    return binder;
                }
            }
            return null;
        }

        public static ICloudTableBinder GetTableBinder(this IConfiguration config, Type targetType, bool isReadOnly)
        {
            foreach (var provider in config.TableBinders)
            {
                var binder = provider.TryGetBinder(targetType, isReadOnly);
                if (binder != null)
                {
                    return binder;
                }
            }
            return null;
        }

        public static ICloudBlobBinder GetBlobBinder(this IConfiguration config, Type targetType, bool isInput)
        {
            foreach (var provider in config.BlobBinders)
            {
                var binder = provider.TryGetBinder(targetType, isInput);
                if (binder != null)
                {
                    return binder;
                }
            }
            return null;
        }
    }

}