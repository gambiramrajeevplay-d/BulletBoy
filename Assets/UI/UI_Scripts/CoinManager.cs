using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    [Header("Coin Rewards")]
    public int levelPassCoins = 100;
    public int levelFailCoins = 0;

    [Header("Coin UI")]
    public TMP_Text coinText;

    private int totalCoins;
    private bool rewardGiven;

    public int TotalCoins => totalCoins;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        UpdateCoinUI();
        //UpdateMenuCoinUI();
    }

    private void Start()
    {
        RefreshCoins();
    }

    private void OnEnable()
    {
        if (Instance == this)
            RefreshCoins();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RewardLevelPass()
    {
        if (rewardGiven)
            return;

        rewardGiven = true;
        AddCoins(levelPassCoins);
    }

    public void RewardLevelFail()
    {
        if (rewardGiven)
            return;

        rewardGiven = true;

        // Failed levels always award exactly 0 coins.
        // This intentionally ignores any old Inspector value on levelFailCoins.
        RefreshCoins();
    }

    public void RewardTutorialComplete()
    {
        if (rewardGiven)
            return;

        rewardGiven = true;

        // Tutorial completion awards 0 coins.
        RefreshCoins();
    }

    public void AddCoins(int amount)
    {
        // Even a zero-value update refreshes the displayed saved balance.
        if (amount <= 0)
        {
            RefreshCoins();
            return;
        }

        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        totalCoins += amount;
        SaveCoins();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
            return false;

        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);

        if (totalCoins < amount)
            return false;

        totalCoins -= amount;
        SaveCoins();

        Debug.Log("Coins spent: " + amount + " | Remaining coins: " + totalCoins);
        return true;
    }

    public void RefreshCoins()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        UpdateCoinUI();
        // UpdateMenuCoinUI();
    }

    private void SaveCoins()
    {
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        PlayerPrefs.Save();

        UpdateCoinUI();
        // UpdateMenuCoinUI();
    }

    public void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = CoinFormatter.FormatCoins(totalCoins);

        // If MainUI exists in this scene, refresh all of its coin labels too.
        if (MainUI.Instance != null)
            MainUI.Instance.UpdateTotalCoins();
    }

    //private void UpdateMenuCoinUI()
    //{
    //    if (MenuScript.instance != null)
    //        MenuScript.instance.UpdateTotalCoins();
    //}
}