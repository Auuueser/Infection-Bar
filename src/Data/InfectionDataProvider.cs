using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;

namespace IndependentCadaverInfectionBar;

internal sealed class InfectionDataProvider
{
    private const float SuccessfulCadaverGrowthLookupIntervalSeconds = 0.25f;
    private const float MissingCadaverGrowthLookupIntervalSeconds = 2f;

    private readonly ManualLogSource logger;

    private CadaverGrowthAI cachedCadaverGrowth;
    private float nextCadaverGrowthLookupTime;

    internal InfectionDataProvider(ManualLogSource logger)
    {
        this.logger = logger;
    }

    internal PlayerControllerB GetLocalPlayer()
    {
        return GameNetworkManager.Instance?.localPlayerController;
    }

    internal bool TryGetInfectionNormalized(PlayerControllerB player, out float infectionNormalized)
    {
        infectionNormalized = 0f;
        if (player == null)
        {
            return false;
        }

        int playerId = (int)player.playerClientId;
        CadaverGrowthAI cadaverGrowth = ResolveCadaverGrowth(playerId);
        if (!TryReadInfectionMeter(cadaverGrowth, playerId, out infectionNormalized))
        {
            return false;
        }

        return true;
    }

    private CadaverGrowthAI ResolveCadaverGrowth(int playerId)
    {
        if (CanReadInfectionMeter(cachedCadaverGrowth, playerId))
        {
            return cachedCadaverGrowth;
        }

        if (Time.unscaledTime < nextCadaverGrowthLookupTime)
        {
            return cachedCadaverGrowth;
        }

        float now = Time.unscaledTime;
        CadaverGrowthAI firstCadaverGrowth = null;
        var spawnedEnemies = RoundManager.Instance?.SpawnedEnemies;
        if (spawnedEnemies != null)
        {
            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                CadaverGrowthAI cadaverGrowth = spawnedEnemies[i] as CadaverGrowthAI;
                if (cadaverGrowth == null)
                {
                    continue;
                }

                if (firstCadaverGrowth == null)
                {
                    firstCadaverGrowth = cadaverGrowth;
                }

                if (!CanReadInfectionMeter(cadaverGrowth, playerId))
                {
                    continue;
                }

                cachedCadaverGrowth = cadaverGrowth;
                nextCadaverGrowthLookupTime = now + SuccessfulCadaverGrowthLookupIntervalSeconds;
                return cachedCadaverGrowth;
            }
        }

        cachedCadaverGrowth = firstCadaverGrowth;
        nextCadaverGrowthLookupTime = now + (firstCadaverGrowth != null
            ? SuccessfulCadaverGrowthLookupIntervalSeconds
            : MissingCadaverGrowthLookupIntervalSeconds);
        return cachedCadaverGrowth;
    }

    private static bool TryReadInfectionMeter(CadaverGrowthAI cadaverGrowth, int playerId, out float infectionNormalized)
    {
        infectionNormalized = 0f;
        if (!CanReadInfectionMeter(cadaverGrowth, playerId))
        {
            return false;
        }

        infectionNormalized = Mathf.Clamp01(cadaverGrowth.playerInfections[playerId].infectionMeter);
        return true;
    }

    private static bool CanReadInfectionMeter(CadaverGrowthAI cadaverGrowth, int playerId)
    {
        return cadaverGrowth != null
            && cadaverGrowth.playerInfections != null
            && playerId >= 0
            && playerId < cadaverGrowth.playerInfections.Length;
    }

    internal void ResetCadaverGrowthCache()
    {
        if (cachedCadaverGrowth == null)
        {
            return;
        }

        if (ModConfig.DebugLogging.Value)
        {
            logger.LogDebug("Resetting cached CadaverGrowthAI reference.");
        }

        cachedCadaverGrowth = null;
        nextCadaverGrowthLookupTime = 0f;
    }
}
