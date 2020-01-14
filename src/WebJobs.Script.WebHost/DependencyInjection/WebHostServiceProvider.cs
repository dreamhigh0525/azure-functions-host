﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Script.WebHost.DependencyInjection
{
    public class WebHostServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        private static readonly Rules _defaultContainerRules;
        private readonly Container _container;
        private ScopedResolver _currentResolver;

        static WebHostServiceProvider()
        {
            _defaultContainerRules = Rules.Default
                .With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithFactorySelector(Rules.SelectLastRegisteredFactory())
                .WithTrackingDisposableTransients();
        }

        public WebHostServiceProvider(IServiceCollection descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            // preferInterpretation will be set to true to significanly improve cold start in consumption mode
            // it will be set to false for premium and appservice plans to make sure throughput is not impacted
            // there is no throughput drop in consumption with this setting.
            var preferInterpretation = SystemEnvironment.Instance.IsWindowsConsumption() ? true : false;
            _container = new Container(_defaultContainerRules, preferInterpretation: preferInterpretation);
            _container.Populate(descriptors);
            _container.UseInstance<IServiceProvider>(this);
            _container.UseInstance<IServiceScopeFactory>(this);

            _currentResolver = new ScopedResolver(_container);
        }

        public object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType, IfUnresolved.ReturnDefault);
        }

        public IServiceScope CreateScope()
        {
            return new JobHostServiceScope(_container.OpenScope(preferInterpretation: _container.PreferInterpretation));
        }
    }
}
