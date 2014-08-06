﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Jobs.Host.Bindings;
using Microsoft.Azure.Jobs.Host.Executors;

namespace Microsoft.Azure.Jobs.Host.Listeners
{
    internal class ListenerFactoryListener : IListener
    {
        private readonly IListenerFactory _factory;
        private readonly IFunctionExecutor _executor;
        private readonly HostBindingContext _context;
        private readonly CancellationTokenSource _cancellationSource;

        private IListener _listener;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _disposed;

        public ListenerFactoryListener(IListenerFactory factory, IFunctionExecutor executor, HostBindingContext context)
        {
            _factory = factory;
            _executor = executor;
            _context = context;
            _cancellationSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_listener != null)
            {
                throw new InvalidOperationException("The listener has already been started.");
            }

            return StartAsyncCore(cancellationToken);
        }

        private async Task StartAsyncCore(CancellationToken cancellationToken)
        {
            ListenerFactoryContext factoryContext = new ListenerFactoryContext(_context,
                new SharedListenerContainer(), cancellationToken);
            _listener = await _factory.CreateAsync(_executor, factoryContext);
            _cancellationRegistration = _cancellationSource.Token.Register(_listener.Cancel);
            await _listener.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            if (_listener == null)
            {
                throw new InvalidOperationException("The listener has not been started.");
            }

            return _listener.StopAsync(cancellationToken);
        }

        public void Cancel()
        {
            ThrowIfDisposed();
            _cancellationSource.Cancel();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cancellationRegistration.Dispose();
                _cancellationSource.Dispose();

                if (_listener != null)
                {
                    _listener.Dispose();
                }

                _disposed = true;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }
    }
}
