﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Azure.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SignalR.AspNet.Tests
{
    internal class TestServiceConnectionProxy : ServiceConnection, IDisposable
    {
        private static readonly ServiceProtocol SharedServiceProtocol = new ServiceProtocol();

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ConnectionContext>> _waitForConnectionOpen = new ConcurrentDictionary<string, TaskCompletionSource<ConnectionContext>>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _waitForConnectionClose = new ConcurrentDictionary<string, TaskCompletionSource<object>>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ServiceMessage>> _waitForApplicationMessage = new ConcurrentDictionary<string, TaskCompletionSource<ServiceMessage>>();

        private TestConnectionContext _connectionContext;

        public TestServiceConnectionProxy(IClientConnectionManager clientConnectionManager, ILoggerFactory loggerFactory, ConnectionDelegate callback = null, PipeOptions clientPipeOptions = null, IServiceMessageHandler serviceMessageHandler = null) :
            base(
                Guid.NewGuid().ToString("N"),
                null,
                SharedServiceProtocol,
                new TestConnectionFactory(),
                clientConnectionManager,
                loggerFactory,
                serviceMessageHandler ?? new TestServiceMessageHandler())
        {
        }

        public async Task StartServiceAsync()
        {
            _ = StartAsync();

            await ConnectionInitializedTask;
        }

        protected override async Task<ConnectionContext> CreateConnection(string target = null)
        {
            _connectionContext = await base.CreateConnection() as TestConnectionContext;

            await WriteMessageAsync(new HandshakeResponseMessage());
            return _connectionContext;
        }

        protected override async Task OnConnectedAsync(OpenConnectionMessage openConnectionMessage)
        {
            await base.OnConnectedAsync(openConnectionMessage);

            var tcs = _waitForConnectionOpen.GetOrAdd(openConnectionMessage.ConnectionId, i => new TaskCompletionSource<ConnectionContext>());

            tcs.TrySetResult(null);
        }

        protected override async Task OnDisconnectedAsync(CloseConnectionMessage closeConnectionMessage)
        {
            await base.OnDisconnectedAsync(closeConnectionMessage);
            var tcs = _waitForConnectionClose.GetOrAdd(closeConnectionMessage.ConnectionId, i => new TaskCompletionSource<object>());

            tcs.TrySetResult(null);
        }

        protected override async Task OnMessageAsync(ConnectionDataMessage connectionDataMessage)
        {
            await base.OnMessageAsync(connectionDataMessage);

            var tcs = _waitForApplicationMessage.GetOrAdd(connectionDataMessage.ConnectionId, i => new TaskCompletionSource<ServiceMessage>());

            tcs.TrySetResult(connectionDataMessage);
        }

        public Task WaitForClientConnectAsync(string connectionId)
        {
            var tcs = _waitForConnectionOpen.GetOrAdd(connectionId, i => new TaskCompletionSource<ConnectionContext>());

            return tcs.Task;
        }

        public Task WaitForApplicationMessageAsync(string connectionId)
        {
            var tcs = _waitForApplicationMessage.GetOrAdd(connectionId, i => new TaskCompletionSource<ServiceMessage>());

            return tcs.Task;
        }

        public Task WaitForClientDisconnectAsync(string connectionId)
        {
            var tcs = _waitForConnectionClose.GetOrAdd(connectionId, i => new TaskCompletionSource<object>());

            return tcs.Task;
        }

        public async Task WriteMessageAsync(ServiceMessage message)
        {
            if (_connectionContext == null)
            {
                throw new InvalidOperationException("Server connection is not yet established.");
            }

            ServiceProtocol.WriteMessage(message, _connectionContext.Application.Output);
            await _connectionContext.Application.Output.FlushAsync();
        }

        public void Dispose()
        {
            _ = StopAsync();
        }
    }
}
