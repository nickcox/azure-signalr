﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Protocol;

namespace Microsoft.Azure.SignalR
{
    internal abstract class ServiceLifetimeManagerBase<THub> : HubLifetimeManager<THub> where THub : Hub
    {
        private readonly IReadOnlyList<IHubProtocol> _allProtocols;

        protected readonly IServiceConnectionManager<THub> ServiceConnectionContainer;

        protected const string NullOrEmptyStringErrorMessage = "Argument cannot be null or empty.";

        public ServiceLifetimeManagerBase(IServiceConnectionManager<THub> serviceConnectionManager, IHubProtocolResolver protocolResolver)
        {
            ServiceConnectionContainer = serviceConnectionManager;
            _allProtocols = protocolResolver.AllProtocols;
        }

        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            return Task.CompletedTask;
        }

        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new BroadcastDataMessage(null, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedIds, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new BroadcastDataMessage(excludedIds, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(connectionId))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(connectionId));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new MultiConnectionDataMessage(new[] { connectionId }, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(connectionIds))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(connectionIds));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new MultiConnectionDataMessage(connectionIds, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(groupName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(groupName));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            var message = new GroupBroadcastDataMessage(groupName, null, SerializeAllProtocols(methodName, args));

            return ServiceConnectionContainer.WriteAsync(message);
        }

        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(groupNames))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(groupNames));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            // Send this message from a random service connection because this message involves of multiple groups.
            // Unless we send message for each group one by one, we can not guarantee the message order for all groups.
            return ServiceConnectionContainer.WriteAsync(
                new MultiGroupBroadcastDataMessage(groupNames, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedIds, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(groupName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(groupName));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            var message = new GroupBroadcastDataMessage(groupName, excludedIds, SerializeAllProtocols(methodName, args));

            return ServiceConnectionContainer.WriteAsync(message);
        }

        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(userId))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(userId));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new UserDataMessage(userId, SerializeAllProtocols(methodName, args)));
        }

        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args,
            CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(userIds))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(userIds));
            }

            if (IsInvalidArgument(methodName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(methodName));
            }

            return ServiceConnectionContainer.WriteAsync(
                new MultiUserDataMessage(userIds, SerializeAllProtocols(methodName, args)));
        }

        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(connectionId))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(connectionId));
            }

            if (IsInvalidArgument(groupName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(groupName));
            }

            var message = new JoinGroupWithAckMessage(connectionId, groupName);

            return ServiceConnectionContainer.WriteAckableMessageAsync(message, cancellationToken);
        }

        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (IsInvalidArgument(connectionId))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(connectionId));
            }

            if (IsInvalidArgument(groupName))
            {
                throw new ArgumentException(NullOrEmptyStringErrorMessage, nameof(groupName));
            }

            var message = new LeaveGroupWithAckMessage(connectionId, groupName);

            return ServiceConnectionContainer.WriteAckableMessageAsync(message, cancellationToken);
        }

        protected static bool IsInvalidArgument(string value)
        {
            return string.IsNullOrEmpty(value);
        }

        protected static bool IsInvalidArgument(IReadOnlyList<object> list)
        {
            return list == null || list.Count == 0;
        }

        protected IDictionary<string, ReadOnlyMemory<byte>> SerializeAllProtocols(string method, object[] args)
        {
            var payloads = new Dictionary<string, ReadOnlyMemory<byte>>();
            var message = new InvocationMessage(method, args);
            foreach (var hubProtocol in _allProtocols)
            {
                payloads.Add(hubProtocol.Name, hubProtocol.GetMessageBytes(message));
            }
            return payloads;
        }
    }
}
