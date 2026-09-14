using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal static class InfectionBarCompatibility
{
    private const string ClientHelloMessageName = "InfectionBar_ClientHello_v1";
    private const string HostStateMessageName = "InfectionBar_HostState_v1";
    private const float CompatibilityTickIntervalSeconds = 0.25f;
    private const float ClientHelloRetryIntervalSeconds = 2f;
    private const float HostStateHeartbeatIntervalSeconds = 6f;
    private const float HostReconciliationIntervalSeconds = 15f;
    private const float GracePeriodSeconds = 8f;
    private const string HostMissingReason = "host does not have InfectionBar installed";
    private const string MissingClientsReason = "one or more clients do not have InfectionBar installed";

    private static readonly InfectionBarCompatibilityState hostClientState = new InfectionBarCompatibilityState();
    private static readonly List<ulong> hostStateRecipients = new List<ulong>();

    private static ManualLogSource logger;
    private static CustomMessagingManager registeredMessagingManager;
    private static NetworkManager subscribedNetworkManager;
    private static NetworkManager observedNetworkManager;
    private static ulong observedLocalClientId = ulong.MaxValue;
    private static bool observedIsServer;
    private static bool observedIsConnectedClient;
    private static bool hudAllowed = true;
    private static string disableReason = string.Empty;
    private static float nextClientHelloTime;
    private static float nextHostStateHeartbeatTime;
    private static float nextHostReconciliationTime;
    private static float nextHostMissingClientDeadline = float.PositiveInfinity;
    private static float nextCompatibilityTickTime;
    private static float clientConnectionStartTime = -1f;
    private static float lastHostStateReceivedTime = -1f;
    private static bool receivedHostState;
    private static bool clientHelloAcknowledged;
    private static bool hostStateAllowed = true;
    private static int hostStateMissingClientCount;
    private static int currentHostMissingClientCount;
    private static bool hostStateDirty;
    private static bool clientStoppedHelloForMissingHost;
    private static bool loggedHostMissingWarning;
    private static bool loggedMissingClientsWarning;
    private static bool loggedHandlerRegistrationWarning;
    private static bool sessionStateInitialized;

    internal static bool HudAllowed => hudAllowed;

    internal static string DisableReason => disableReason;

    internal static void Initialize(ManualLogSource pluginLogger)
    {
        logger = pluginLogger;
        nextCompatibilityTickTime = 0f;
        SetHudAllowed(true, string.Empty);
    }

    internal static void Shutdown()
    {
        UnsubscribeNetworkCallbacks();
        UnregisterHandlers();
        ResetSessionState(0f);
        observedNetworkManager = null;
        sessionStateInitialized = false;
    }

    internal static void Tick(float now)
    {
        if (now < nextCompatibilityTickTime)
        {
            return;
        }

        nextCompatibilityTickTime = now + CompatibilityTickIntervalSeconds;
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.ShutdownInProgress || !networkManager.IsListening)
        {
            if (!sessionStateInitialized
                && registeredMessagingManager == null
                && subscribedNetworkManager == null
                && observedNetworkManager == null)
            {
                return;
            }

            UnsubscribeNetworkCallbacks();
            UnregisterHandlers();
            ResetSessionState(0f);
            observedNetworkManager = null;
            sessionStateInitialized = false;
            return;
        }

        EnsureHandlers(networkManager.CustomMessagingManager);
        ResetSessionIfNeeded(networkManager, now);

        if (networkManager.IsServer)
        {
            TickHost(networkManager, now);
            return;
        }

        if (networkManager.IsClient && networkManager.IsConnectedClient)
        {
            TickClient(networkManager, now);
            return;
        }

        SetHudAllowed(true, string.Empty);
    }

    private static void ResetSessionIfNeeded(NetworkManager networkManager, float now)
    {
        ulong localClientId = networkManager.IsClient ? networkManager.LocalClientId : NetworkManager.ServerClientId;
        if (sessionStateInitialized
            && observedNetworkManager == networkManager
            && observedLocalClientId == localClientId
            && observedIsServer == networkManager.IsServer
            && observedIsConnectedClient == networkManager.IsConnectedClient)
        {
            return;
        }

        UnsubscribeNetworkCallbacks();
        observedNetworkManager = networkManager;
        observedLocalClientId = localClientId;
        observedIsServer = networkManager.IsServer;
        observedIsConnectedClient = networkManager.IsConnectedClient;
        ResetSessionState(now);
        SubscribeNetworkCallbacks(networkManager);
        sessionStateInitialized = true;

        if (networkManager.IsServer)
        {
            hostClientState.SynchronizeConnectedClients(networkManager.ConnectedClientsIds, NetworkManager.ServerClientId, now);
            RefreshHostCompatibilityState(now);
        }
    }

    private static void ResetSessionState(float now)
    {
        hostClientState.Reset();
        hostStateRecipients.Clear();
        nextClientHelloTime = now;
        nextHostStateHeartbeatTime = now + HostStateHeartbeatIntervalSeconds;
        nextHostReconciliationTime = now + HostReconciliationIntervalSeconds;
        nextHostMissingClientDeadline = float.PositiveInfinity;
        clientConnectionStartTime = now;
        lastHostStateReceivedTime = -1f;
        receivedHostState = false;
        clientHelloAcknowledged = false;
        hostStateAllowed = true;
        hostStateMissingClientCount = 0;
        currentHostMissingClientCount = 0;
        hostStateDirty = false;
        clientStoppedHelloForMissingHost = false;
        loggedHostMissingWarning = false;
        loggedMissingClientsWarning = false;
        SetHudAllowed(true, string.Empty);
    }

    private static void TickHost(NetworkManager networkManager, float now)
    {
        if (now >= nextHostReconciliationTime)
        {
            nextHostReconciliationTime = now + HostReconciliationIntervalSeconds;
            if (hostClientState.SynchronizeConnectedClients(networkManager.ConnectedClientsIds, NetworkManager.ServerClientId, now))
            {
                RefreshHostCompatibilityState(now);
            }
        }

        if (now >= nextHostMissingClientDeadline)
        {
            RefreshHostCompatibilityState(now);
        }

        if (!hostStateDirty && now < nextHostStateHeartbeatTime)
        {
            return;
        }

        if (SendHostStateToInstalledClients(networkManager.CustomMessagingManager))
        {
            hostStateDirty = false;
            nextHostStateHeartbeatTime = now + HostStateHeartbeatIntervalSeconds;
        }
    }

    private static void TickClient(NetworkManager networkManager, float now)
    {
        if (clientConnectionStartTime < 0f)
        {
            clientConnectionStartTime = now;
        }

        SendClientHelloIfNeeded(networkManager.CustomMessagingManager, now);

        if (!receivedHostState)
        {
            if (now - clientConnectionStartTime >= GracePeriodSeconds)
            {
                clientStoppedHelloForMissingHost = true;
                SetHudAllowed(false, HostMissingReason);
                LogHostMissingWarningOnce();
                return;
            }

            SetHudAllowed(true, string.Empty);
            return;
        }

        if (lastHostStateReceivedTime >= 0f && now - lastHostStateReceivedTime >= GracePeriodSeconds)
        {
            receivedHostState = false;
            clientHelloAcknowledged = false;
            clientStoppedHelloForMissingHost = false;
            clientConnectionStartTime = now;
            nextClientHelloTime = now;
            SetHudAllowed(false, HostMissingReason);
            LogHostMissingWarningOnce();
            return;
        }

        ApplyClientHostState();
    }

    private static void SendClientHelloIfNeeded(CustomMessagingManager messagingManager, float now)
    {
        if (clientHelloAcknowledged || clientStoppedHelloForMissingHost || messagingManager == null || now < nextClientHelloTime)
        {
            return;
        }

        nextClientHelloTime = now + ClientHelloRetryIntervalSeconds;
        try
        {
            using (FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp))
            {
                writer.WriteValueSafe(Plugin.PluginVersion);
                messagingManager.SendNamedMessage(ClientHelloMessageName, NetworkManager.ServerClientId, writer, NetworkDelivery.Reliable);
            }
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to send InfectionBar ClientHello: " + exception.Message);
        }
    }

    private static bool SendHostStateToInstalledClients(CustomMessagingManager messagingManager)
    {
        if (messagingManager == null)
        {
            return false;
        }

        hostClientState.CollectInstalledClientIds(hostStateRecipients);
        if (hostStateRecipients.Count == 0)
        {
            return true;
        }

        return SendHostState(messagingManager, hostStateRecipients);
    }

    private static bool SendHostStateToClient(CustomMessagingManager messagingManager, ulong clientId)
    {
        if (messagingManager == null)
        {
            return false;
        }

        try
        {
            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                bool allowedValue = hudAllowed;
                int missingClientCountValue = currentHostMissingClientCount;
                writer.WriteValueSafe(Plugin.PluginVersion);
                writer.WriteValueSafe(in allowedValue);
                writer.WriteValueSafe(in missingClientCountValue);
                messagingManager.SendNamedMessage(HostStateMessageName, clientId, writer, NetworkDelivery.Reliable);
            }

            return true;
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to send InfectionBar HostState: " + exception.Message);
            return false;
        }
    }

    private static bool SendHostState(CustomMessagingManager messagingManager, IReadOnlyList<ulong> recipients)
    {
        try
        {
            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                bool allowedValue = hudAllowed;
                int missingClientCountValue = currentHostMissingClientCount;
                writer.WriteValueSafe(Plugin.PluginVersion);
                writer.WriteValueSafe(in allowedValue);
                writer.WriteValueSafe(in missingClientCountValue);
                messagingManager.SendNamedMessage(HostStateMessageName, recipients, writer, NetworkDelivery.Reliable);
            }

            return true;
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to send InfectionBar HostState: " + exception.Message);
            return false;
        }
    }

    private static void RefreshHostCompatibilityState(float now)
    {
        int missingClientCount = hostClientState.CountMissingClients(now, GracePeriodSeconds);
        bool allowed = missingClientCount == 0;
        bool stateChanged = currentHostMissingClientCount != missingClientCount || hudAllowed != allowed;

        currentHostMissingClientCount = missingClientCount;
        nextHostMissingClientDeadline = hostClientState.GetNextMissingClientDeadline(now, GracePeriodSeconds);
        SetHudAllowed(allowed, allowed ? string.Empty : MissingClientsReason);
        hostStateDirty |= stateChanged;

        if (missingClientCount > 0)
        {
            LogMissingClientsWarningOnce();
        }
    }

    private static void ApplyClientHostState()
    {
        SetHudAllowed(hostStateAllowed, hostStateAllowed ? string.Empty : MissingClientsReason);
        if (!hostStateAllowed && hostStateMissingClientCount > 0)
        {
            LogMissingClientsWarningOnce();
        }
    }

    private static void EnsureHandlers(CustomMessagingManager messagingManager)
    {
        if (messagingManager == null || registeredMessagingManager == messagingManager)
        {
            return;
        }

        UnregisterHandlers();
        try
        {
            messagingManager.RegisterNamedMessageHandler(ClientHelloMessageName, OnClientHelloMessage);
            messagingManager.RegisterNamedMessageHandler(HostStateMessageName, OnHostStateMessage);
            registeredMessagingManager = messagingManager;
        }
        catch (Exception exception)
        {
            try
            {
                messagingManager.UnregisterNamedMessageHandler(ClientHelloMessageName);
                messagingManager.UnregisterNamedMessageHandler(HostStateMessageName);
            }
            catch
            {
            }

            registeredMessagingManager = null;
            LogHandlerWarningOnce("Failed to register InfectionBar network handlers: " + exception.Message);
        }
    }

    private static void UnregisterHandlers()
    {
        if (registeredMessagingManager == null)
        {
            return;
        }

        try
        {
            registeredMessagingManager.UnregisterNamedMessageHandler(ClientHelloMessageName);
            registeredMessagingManager.UnregisterNamedMessageHandler(HostStateMessageName);
        }
        catch
        {
            // NetworkManager teardown can invalidate the messaging manager before the plugin sees it.
        }

        registeredMessagingManager = null;
    }

    private static void SubscribeNetworkCallbacks(NetworkManager networkManager)
    {
        if (networkManager == null || subscribedNetworkManager == networkManager)
        {
            return;
        }

        UnsubscribeNetworkCallbacks();
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        subscribedNetworkManager = networkManager;
    }

    private static void UnsubscribeNetworkCallbacks()
    {
        NetworkManager networkManager = subscribedNetworkManager;
        subscribedNetworkManager = null;
        if (ReferenceEquals(networkManager, null))
        {
            return;
        }

        try
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
        catch
        {
            // Network teardown can invalidate callback storage before the controller notices.
        }
    }

    private static void OnClientConnected(ulong clientId)
    {
        NetworkManager networkManager = observedNetworkManager;
        if (networkManager == null)
        {
            return;
        }

        float now = Time.unscaledTime;
        if (networkManager.IsServer)
        {
            if (hostClientState.TrackConnectedClient(clientId, NetworkManager.ServerClientId, now))
            {
                RefreshHostCompatibilityState(now);
            }

            return;
        }

        if (networkManager.IsClient && clientId == networkManager.LocalClientId)
        {
            clientConnectionStartTime = now;
            receivedHostState = false;
            clientHelloAcknowledged = false;
            clientStoppedHelloForMissingHost = false;
            nextClientHelloTime = now;
        }
    }

    private static void OnClientDisconnected(ulong clientId)
    {
        NetworkManager networkManager = observedNetworkManager;
        if (networkManager == null)
        {
            return;
        }

        if (networkManager.IsServer)
        {
            if (hostClientState.RemoveClient(clientId))
            {
                RefreshHostCompatibilityState(Time.unscaledTime);
            }

            return;
        }

        if (clientId == observedLocalClientId)
        {
            receivedHostState = false;
            clientHelloAcknowledged = false;
            clientStoppedHelloForMissingHost = true;
            SetHudAllowed(true, string.Empty);
        }
    }

    private static void OnClientHelloMessage(ulong senderClientId, FastBufferReader reader)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || senderClientId == NetworkManager.ServerClientId)
        {
            return;
        }

        try
        {
            string clientVersion = string.Empty;
            reader.ReadValueSafe(out clientVersion);
        }
        catch
        {
            // The sender still proved that InfectionBar registered the named message.
        }

        float now = Time.unscaledTime;
        hostClientState.TrackConnectedClient(senderClientId, NetworkManager.ServerClientId, now);
        bool newlyInstalled = hostClientState.RecordClientHello(senderClientId);
        if (newlyInstalled)
        {
            RefreshHostCompatibilityState(now);
        }

        if (hostStateDirty)
        {
            if (SendHostStateToInstalledClients(networkManager.CustomMessagingManager))
            {
                hostStateDirty = false;
                nextHostStateHeartbeatTime = now + HostStateHeartbeatIntervalSeconds;
            }

            return;
        }

        SendHostStateToClient(networkManager.CustomMessagingManager, senderClientId);
    }

    private static void OnHostStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.IsServer || senderClientId != NetworkManager.ServerClientId)
        {
            return;
        }

        try
        {
            string hostVersion = string.Empty;
            bool allowed = false;
            int missingClientCount = 0;
            reader.ReadValueSafe(out hostVersion);
            reader.ReadValueSafe(out allowed);
            reader.ReadValueSafe(out missingClientCount);

            receivedHostState = true;
            clientHelloAcknowledged = true;
            clientStoppedHelloForMissingHost = false;
            nextClientHelloTime = float.PositiveInfinity;
            lastHostStateReceivedTime = Time.unscaledTime;
            hostStateAllowed = allowed;
            hostStateMissingClientCount = missingClientCount;
            ApplyClientHostState();
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to read InfectionBar HostState: " + exception.Message);
        }
    }

    private static void SetHudAllowed(bool allowed, string reason)
    {
        string normalizedReason = allowed ? string.Empty : reason ?? string.Empty;
        if (hudAllowed == allowed && string.Equals(disableReason, normalizedReason, StringComparison.Ordinal))
        {
            return;
        }

        hudAllowed = allowed;
        disableReason = normalizedReason;
    }

    private static void LogHostMissingWarningOnce()
    {
        if (loggedHostMissingWarning)
        {
            return;
        }

        logger?.LogWarning(HostMissingReason);
        loggedHostMissingWarning = true;
    }

    private static void LogMissingClientsWarningOnce()
    {
        if (loggedMissingClientsWarning)
        {
            return;
        }

        logger?.LogWarning(MissingClientsReason);
        loggedMissingClientsWarning = true;
    }

    private static void LogHandlerWarningOnce(string message)
    {
        if (loggedHandlerRegistrationWarning)
        {
            return;
        }

        logger?.LogWarning(message);
        loggedHandlerRegistrationWarning = true;
    }
}
