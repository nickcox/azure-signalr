﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.Azure.SignalR.Protocol;

namespace Microsoft.Azure.SignalR.Common.ServiceConnections
{
    internal class WeakServiceConnectionContainer : ServiceConnectionContainerBase
    {
        protected override ServerConnectionType InitialConnectionType => ServerConnectionType.Weak;

        public WeakServiceConnectionContainer(IServiceConnectionFactory serviceConnectionFactory,
            int fixedConnectionCount, HubServiceEndpoint endpoint)
            : base(serviceConnectionFactory, fixedConnectionCount, endpoint)
        {
        }

        public override Task HandlePingAsync(PingMessage pingMessage)
        {
            return Task.CompletedTask;
        }
    }
}
