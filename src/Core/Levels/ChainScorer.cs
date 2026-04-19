using PressureChain.Core.Board;
using PressureChain.Core.Chains;

namespace PressureChain.Core.Levels;

public static class ChainScorer
{
    private const int ReleaseValuePerNode = 10;
    private const decimal DownstreamAmplifierMultiplier = 1.5m;

    public static int Score(ChainResolution resolution)
    {
        var waveMultiplier = GetWaveMultiplier(resolution.Waves.Count);
        var score = 0m;
        var amplifierTriggered = false;

        foreach (var wave in resolution.Waves)
        {
            var waveScore = wave.Count * ReleaseValuePerNode * waveMultiplier;
            if (amplifierTriggered)
            {
                waveScore *= DownstreamAmplifierMultiplier;
            }

            score += waveScore;

            if (!amplifierTriggered &&
                wave.Any(burst => resolution.FinalBoard.NodeAt(burst.Origin).Type == NodeType.Amplifier))
            {
                amplifierTriggered = true;
            }
        }

        return decimal.ToInt32(decimal.Round(score, 0, MidpointRounding.AwayFromZero));
    }

    private static decimal GetWaveMultiplier(int waveCount)
    {
        return waveCount switch
        {
            <= 1 => 1.0m,
            2 => 1.3m,
            3 => 1.7m,
            4 => 2.2m,
            _ => 3.0m
        };
    }
}
