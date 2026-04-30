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
    private const float MessageIntervalSeconds = 2f;
    private const float GracePeriodSeconds = 8f;
    private const string HostMissingReason = "host does not have InfectionBar installed";
    private const string MissingClientsReason = "one or more clients do not have InfectionBar installed";

    private static readonly InfectionBarCompatibilityState hostClientState = new InfectionBarCompatibilityState();
    private static readonly List<ulong> hostStateRecipients = new List<ulong>();

    private static ManualLogSource logger;
    private static CustomMessagingManager registeredMessagingManager;
    private static NetworkManager observedNetworkManager;
    private static ulong observedLocalClientId = ulong.MaxValue;
    private static bool observedIsServer;
    private static bool observedIsConnectedClient;
    private static bool hudAllowed = true;
    private static string disableReason = string.Empty;
    private static float nextClientHelloTime;
    private static float nextHostStateTime;
    private static float clientConnectionStartTime = -1f;
    private static float lastHostStateReceivedTime = -1f;
    private static bool receivedHostState;
    private static bool hostStateAllowed = true;
    private static int hostStateMissingClientCount;
    private static bool clientStoppedHelloForMissingHost;
    private static bool loggedHostMissingWarning;
    private static bool loggedMissingClientsWarning;
    private static bool loggedHandlerRegistrationWarning;

    internal static bool HudAllowed
    {
        get { return hudAllowed; }
    }

    internal static string DisableReason
    {
        get { return disableReason; }
    }

    internal static void Initialize(ManualLogSource pluginLogger)
    {
        logger = pluginLogger;
        SetHudAllowed(true, string.Empty);
    }

    internal static void Tick()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.ShutdownInProgress || !networkManager.IsListening)
        {
            UnregisterHandlers();
            ResetSessionState(0f);
            observedNetworkManager = null;
            return;
        }

        EnsureHandlers(networkManager.CustomMessagingManager);

        float now = Time.unscaledTime;
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
        if (observedNetworkManager == networkManager
            && observedLocalClientId == localClientId
            && observedIsServer == networkManager.IsServer
            && observedIsConnectedClient == networkManager.IsConnectedClient)
        {
            return;
        }

        observedNetworkManager = networkManager;
        observedLocalClientId = localClientId;
        observedIsServer = networkManager.IsServer;
        observedIsConnectedClient = networkManager.IsConnectedClient;
        ResetSessionState(now);
    }

    private static void ResetSessionState(float now)
    {
        hostClientState.Reset();
        hostStateRecipients.Clear();
        nextClientHelloTime = 0f;
        nextHostStateTime = 0f;
        clientConnectionStartTime = now;
        lastHostStateReceivedTime = -1f;
        receivedHostState = false;
        hostStateAllowed = true;
        hostStateMissingClientCount = 0;
        clientStoppedHelloForMissingHost = false;
        SetHudAllowed(true, string.Empty);
    }

    private static void TickHost(NetworkManager networkManager, float now)
    {
        IReadOnlyList<ulong> connectedClientIds = networkManager.ConnectedClientsIds;
        hostClientState.UpdateConnectedClients(connectedClientIds, NetworkManager.ServerClientId, now);

        int missingClientCount = hostClientState.CountMissingClients(now, GracePeriodSeconds);
        if (missingClientCount > 0)
        {
            SetHudAllowed(false, MissingClientsReason);
            LogMissingClientsWarningOnce();
        }
        else
        {
            SetHudAllowed(true, string.Empty);
        }

        if (now < nextHostStateTime)
        {
            return;
        }

        nextHostStateTime = now + MessageIntervalSeconds;
        SendHostState(networkManager.CustomMessagingManager, connectedClientIds, hudAllowed, missingClientCount);
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
            SetHudAllowed(false, HostMissingReason);
            LogHostMissingWarningOnce();
            return;
        }

        if (hostStateAllowed)
        {
            SetHudAllowed(true, string.Empty);
            return;
        }

        SetHudAllowed(false, MissingClientsReason);
        if (hostStateMissingClientCount > 0)
        {
            LogMissingClientsWarningOnce();
        }
    }

    private static void SendClientHelloIfNeeded(CustomMessagingManager messagingManager, float now)
    {
        if (clientStoppedHelloForMissingHost || messagingManager == null || now < nextClientHelloTime)
        {
            return;
        }

        nextClientHelloTime = now + MessageIntervalSeconds;
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

    private static void SendHostState(CustomMessagingManager messagingManager, IReadOnlyList<ulong> connectedClientIds, bool allowed, int missingClientCount)
    {
        if (messagingManager == null)
        {
            return;
        }

        hostClientState.CollectInstalledClientIds(connectedClientIds, NetworkManager.ServerClientId, hostStateRecipients);
        if (hostStateRecipients.Count == 0)
        {
            return;
        }

        try
        {
            using (FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp))
            {
                bool allowedValue = allowed;
                int missingClientCountValue = missingClientCount;
                writer.WriteValueSafe(Plugin.PluginVersion);
                writer.WriteValueSafe(in allowedValue);
                writer.WriteValueSafe(in missingClientCountValue);
                messagingManager.SendNamedMessage(HostStateMessageName, hostStateRecipients, writer, NetworkDelivery.Reliable);
            }
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to send InfectionBar HostState: " + exception.Message);
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

        hostClientState.RecordClientHello(senderClientId);
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
            lastHostStateReceivedTime = Time.unscaledTime;
            hostStateAllowed = allowed;
            hostStateMissingClientCount = missingClientCount;
        }
        catch (Exception exception)
        {
            LogHandlerWarningOnce("Failed to read InfectionBar HostState: " + exception.Message);
        }
    }

    private static void SetHudAllowed(bool allowed, string reason)
    {
        hudAllowed = allowed;
        disableReason = allowed ? string.Empty : reason ?? string.Empty;
    }

    private static void LogHostMissingWarningOnce()
    {
        if (loggedHostMissingWarning)
        {
            return;
        }

        if (logger != null)
        {
            logger.LogWarning(HostMissingReason);
        }

        loggedHostMissingWarning = true;
    }

    private static void LogMissingClientsWarningOnce()
    {
        if (loggedMissingClientsWarning)
        {
            return;
        }

        if (logger != null)
        {
            logger.LogWarning(MissingClientsReason);
        }

        loggedMissingClientsWarning = true;
    }

    private static void LogHandlerWarningOnce(string message)
    {
        if (loggedHandlerRegistrationWarning)
        {
            return;
        }

        if (logger != null)
        {
            logger.LogWarning(message);
        }

        loggedHandlerRegistrationWarning = true;
    }
}

internal sealed class InfectionBarCompatibilityState
{
    private readonly Dictionary<ulong, float> firstSeenTimes = new Dictionary<ulong, float>();
    private readonly HashSet<ulong> installedClients = new HashSet<ulong>();
    private readonly HashSet<ulong> connectedClientSet = new HashSet<ulong>();
    private readonly List<ulong> idsToRemove = new List<ulong>();

    internal void Reset()
    {
        firstSeenTimes.Clear();
        installedClients.Clear();
        connectedClientSet.Clear();
        idsToRemove.Clear();
    }

    internal void UpdateConnectedClients(IReadOnlyList<ulong> connectedClientIds, ulong serverClientId, float now)
    {
        connectedClientSet.Clear();
        if (connectedClientIds != null)
        {
            for (int i = 0; i < connectedClientIds.Count; i++)
            {
                ulong clientId = connectedClientIds[i];
                connectedClientSet.Add(clientId);
                if (clientId == serverClientId || firstSeenTimes.ContainsKey(clientId))
                {
                    continue;
                }

                firstSeenTimes[clientId] = now;
            }
        }

        PruneDisconnectedClients(firstSeenTimes, serverClientId);
        PruneDisconnectedClients(installedClients, serverClientId);
    }

    internal void RecordClientHello(ulong clientId)
    {
        installedClients.Add(clientId);
    }

    internal int CountMissingClients(float now, float gracePeriodSeconds)
    {
        int missingClientCount = 0;
        foreach (KeyValuePair<ulong, float> firstSeenTime in firstSeenTimes)
        {
            if (installedClients.Contains(firstSeenTime.Key))
            {
                continue;
            }

            if (now - firstSeenTime.Value >= gracePeriodSeconds)
            {
                missingClientCount++;
            }
        }

        return missingClientCount;
    }

    internal void CollectInstalledClientIds(IReadOnlyList<ulong> connectedClientIds, ulong serverClientId, List<ulong> results)
    {
        results.Clear();
        if (connectedClientIds == null)
        {
            return;
        }

        for (int i = 0; i < connectedClientIds.Count; i++)
        {
            ulong clientId = connectedClientIds[i];
            if (clientId == serverClientId || !installedClients.Contains(clientId))
            {
                continue;
            }

            results.Add(clientId);
        }
    }

    private void PruneDisconnectedClients(Dictionary<ulong, float> clients, ulong serverClientId)
    {
        idsToRemove.Clear();
        foreach (KeyValuePair<ulong, float> client in clients)
        {
            if (client.Key != serverClientId && !connectedClientSet.Contains(client.Key))
            {
                idsToRemove.Add(client.Key);
            }
        }

        for (int i = 0; i < idsToRemove.Count; i++)
        {
            clients.Remove(idsToRemove[i]);
        }
    }

    private void PruneDisconnectedClients(HashSet<ulong> clients, ulong serverClientId)
    {
        idsToRemove.Clear();
        foreach (ulong clientId in clients)
        {
            if (clientId != serverClientId && !connectedClientSet.Contains(clientId))
            {
                idsToRemove.Add(clientId);
            }
        }

        for (int i = 0; i < idsToRemove.Count; i++)
        {
            clients.Remove(idsToRemove[i]);
        }
    }
}
