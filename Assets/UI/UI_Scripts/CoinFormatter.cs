
using UnityEngine;

public static class CoinFormatter
{
    public static string FormatCoins(int coins)
    {
        if (coins >= 1000000000)
            return (coins / 1000000000f).ToString("0.#") + "B";

        if (coins >= 1000000)
            return (coins / 1000000f).ToString("0.#") + "M";

        if (coins >= 1000)
            return (coins / 1000f).ToString("0.#") + "K";

        return coins.ToString();
    }
}
