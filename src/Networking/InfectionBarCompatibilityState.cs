using System;
using System.Collections.Generic;

namespace IndependentCadaverInfectionBar;

internal sealed class InfectionBarCompatibilityState
{
    private readonly Dictionary<ulong, float> firstSeenTimes = new Dictionary<ulong, float>();
    private readonly HashSet<ulong> installedClients = new HashSet<ulong>();
    private readonly HashSet<ulong> connectedClients = new HashSet<ulong>();
    private readonly HashSet<ulong> reconciledClients = new HashSet<ulong>();
    private readonly List<ulong> idsToRemove = new List<ulong>();

    internal int ConnectedClientCount => connectedClients.Count;

    internal int InstalledClientCount => installedClients.Count;

    internal void Reset()
    {
        firstSeenTimes.Clear();
        installedClients.Clear();
        connectedClients.Clear();
        reconciledClients.Clear();
        idsToRemove.Clear();
    }

    internal bool SynchronizeConnectedClients(IReadOnlyList<ulong> connectedClientIds, ulong serverClientId, float now)
    {
        reconciledClients.Clear();
        if (connectedClientIds != null)
        {
            for (int i = 0; i < connectedClientIds.Count; i++)
            {
                ulong clientId = connectedClientIds[i];
                if (clientId != serverClientId)
                {
                    reconciledClients.Add(clientId);
                }
            }
        }

        bool changed = !connectedClients.SetEquals(reconciledClients);
        if (!changed)
        {
            // Stable membership needs no clear/reinsert pass. Still prune stale acknowledgements.
            return PruneDisconnectedClients(firstSeenTimes) | PruneDisconnectedClients(installedClients);
        }
        connectedClients.Clear();
        foreach (ulong clientId in reconciledClients)
        {
            connectedClients.Add(clientId);
            if (!installedClients.Contains(clientId) && !firstSeenTimes.ContainsKey(clientId))
            {
                firstSeenTimes[clientId] = now;
            }
        }

        changed |= PruneDisconnectedClients(firstSeenTimes);
        changed |= PruneDisconnectedClients(installedClients);
        return changed;
    }

    internal bool TrackConnectedClient(ulong clientId, ulong serverClientId, float now)
    {
        if (clientId == serverClientId)
        {
            return false;
        }

        bool changed = connectedClients.Add(clientId);
        if (!installedClients.Contains(clientId) && !firstSeenTimes.ContainsKey(clientId))
        {
            firstSeenTimes[clientId] = now;
            changed = true;
        }

        return changed;
    }

    internal bool RemoveClient(ulong clientId)
    {
        bool changed = connectedClients.Remove(clientId);
        changed |= firstSeenTimes.Remove(clientId);
        changed |= installedClients.Remove(clientId);
        return changed;
    }

    internal bool RecordClientHello(ulong clientId)
    {
        if (!connectedClients.Contains(clientId))
        {
            return false;
        }

        bool changed = installedClients.Add(clientId);
        firstSeenTimes.Remove(clientId);
        return changed;
    }

    internal int CountMissingClients(float now, float gracePeriodSeconds)
    {
        int missingClientCount = 0;
        foreach (KeyValuePair<ulong, float> firstSeenTime in firstSeenTimes)
        {
            if (now - firstSeenTime.Value >= gracePeriodSeconds)
            {
                missingClientCount++;
            }
        }

        return missingClientCount;
    }

    internal float GetNextMissingClientDeadline(float now, float gracePeriodSeconds)
    {
        float nextDeadline = float.PositiveInfinity;
        foreach (KeyValuePair<ulong, float> firstSeenTime in firstSeenTimes)
        {
            float deadline = firstSeenTime.Value + gracePeriodSeconds;
            if (deadline > now)
            {
                nextDeadline = Math.Min(nextDeadline, deadline);
            }
        }

        return nextDeadline;
    }

    internal void CollectInstalledClientIds(List<ulong> results)
    {
        results.Clear();
        foreach (ulong clientId in installedClients)
        {
            if (connectedClients.Contains(clientId))
            {
                results.Add(clientId);
            }
        }
    }

    private bool PruneDisconnectedClients(Dictionary<ulong, float> clients)
    {
        idsToRemove.Clear();
        foreach (KeyValuePair<ulong, float> client in clients)
        {
            if (!connectedClients.Contains(client.Key))
            {
                idsToRemove.Add(client.Key);
            }
        }

        for (int i = 0; i < idsToRemove.Count; i++)
        {
            clients.Remove(idsToRemove[i]);
        }

        return idsToRemove.Count > 0;
    }

    private bool PruneDisconnectedClients(HashSet<ulong> clients)
    {
        idsToRemove.Clear();
        foreach (ulong clientId in clients)
        {
            if (!connectedClients.Contains(clientId))
            {
                idsToRemove.Add(clientId);
            }
        }

        for (int i = 0; i < idsToRemove.Count; i++)
        {
            clients.Remove(idsToRemove[i]);
        }

        return idsToRemove.Count > 0;
    }
}
