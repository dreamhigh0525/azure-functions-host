﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json.Linq;
using Xunit;

namespace WebJobs.Script.EndToEndTests
{
    [Collection(Constants.FunctionAppCollectionName)]
    public class ProxyEndToEndTests
    {
        private readonly FunctionAppFixture _fixture;

        public ProxyEndToEndTests(FunctionAppFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        [TestTrace]
        public async Task FileExtension()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync($"test.txt");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("test", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task RootCheck()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync("/");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("Root", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task LocalFunctionCall()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync($"myhttptrigger?code={_fixture.FunctionDefaultKey}");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("Pong", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task LocalFunctionCallForNonAlphanumericProxyName()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync($"MyHttpWithNonAlphanumericProxyName?code={_fixture.FunctionDefaultKey}");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("Pong", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task CatchAllApis()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync($"api/proxy/blahblah?code={_fixture.FunctionDefaultKey}");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("Pong", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task CatchAll()
        {
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync($"proxy/blahblah?code={_fixture.FunctionDefaultKey}");

                string content = await response.Content.ReadAsStringAsync();
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
                _fixture.Assert.Equals("Pong", content);
            }
        }

        [Fact]
        [TestTrace]
        public async Task LongRoute()
        {
            var longRoute = "test123412341234123412341234123412341234123412341234123412341234123412341234123421341234123423141234123412341234123412341234123412341234123412341234123412341234123412341234123412341234213423141234123412341234123412341234123412341234123412341234123412341234123412341234123412341234";
            using (var client = CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync(longRoute);

                string content = await response.Content.ReadAsStringAsync();
                
                // This is to make sure the url is greater than the default asp.net 260 characters.
                _fixture.Assert.True(longRoute.Length > 260);
                _fixture.Assert.Equals("200", response.StatusCode.ToString("D"));
            }
        }

        public HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60),
                BaseAddress = Settings.SiteBaseAddress
            };
        }
    }
}
