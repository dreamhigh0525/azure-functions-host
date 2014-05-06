﻿using System;
using System.IO;
using Microsoft.Azure.Jobs.Host;
using Microsoft.Azure.Jobs.Host.Runners;

namespace Microsoft.Azure.Jobs
{
    /// <summary>Represents the configuration settings for a <see cref="JobHost"/>.</summary>
    public class JobHostConfiguration : IJobHostConfiguration
    {
        private readonly DefaultConnectionStringProvider _connectionStringProvider;
        private readonly IStorageValidator _storageValidator = new DefaultStorageValidator();

        private ITypeLocator _typeLocator = new DefaultTypeLocator();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHostConfiguration"/> class, using a single Microsoft Azure
        /// Storage connection string for both reading and writing data as well as logging.
        /// </summary>
        public JobHostConfiguration()
            : this(new DefaultConnectionStringProvider())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHostConfiguration"/> class, using a single Microsoft Azure
        /// Storage connection string for both reading and writing data as well as logging.
        /// </summary>
        /// <param name="dataAndRuntimeConnectionString">
        /// The Azure Storage connection string for accessing data and logging.
        /// </param>
        public JobHostConfiguration(string dataAndRuntimeConnectionString)
            : this(new DefaultConnectionStringProvider(dataAndRuntimeConnectionString))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHostConfiguration"/> class, using one Microsoft Azure
        /// Storage connection strings for reading and writing data and another connection string for logging.
        /// </summary>
        /// <param name="dataConnectionString">The Azure Storage connection string for accessing data.</param>
        /// <param name="runtimeConnectionString">The Azure Storage connection string for accessing logging.</param>
        public JobHostConfiguration(string dataConnectionString, string runtimeConnectionString)
            : this(new DefaultConnectionStringProvider(dataConnectionString, runtimeConnectionString))
        {
        }

        private JobHostConfiguration(DefaultConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;

            WriteAntaresManifest();
        }

        /// <summary>Gets or sets the Azure Storage connection string used for reading and writing data.</summary>
        public string DataConnectionString
        {
            get { return _connectionStringProvider.DataConnectionString; }
            set { _connectionStringProvider.DataConnectionString = value; }
        }

        /// <summary>Gets or sets the Azure Storage connection string used for logging and diagnostics.</summary>
        public string RuntimeConnectionString
        {
            get { return _connectionStringProvider.RuntimeConnectionString; }
            set { _connectionStringProvider.RuntimeConnectionString = value; }
        }

        /// <summary>Gets or sets the type locator.</summary>
        public ITypeLocator TypeLocator
        {
            get { return _typeLocator; }
            set { _typeLocator = value; }
        }

        IStorageValidator IJobHostConfiguration.StorageValidator
        {
            get { return _storageValidator; }
        }

        IConnectionStringProvider IJobHostConfiguration.ConnectionStringProvider
        {
            get { return _connectionStringProvider; }
        }

        // When running in Antares, write out a manifest file.
        private static void WriteAntaresManifest()
        {
            string jobDataPath = Environment.GetEnvironmentVariable(WebSitesKnownKeyNames.JobDataPath);
            if (jobDataPath == null)
            {
                // we're not in antares, bye bye.
                return;
            }

            const string filename = "WebJobsSdk.marker";
            var path = Path.Combine(jobDataPath, filename);

            File.WriteAllText(path, DateTime.UtcNow.ToString("s") + "Z"); // content is not really important, this would help debugging though
        }
    }
}
