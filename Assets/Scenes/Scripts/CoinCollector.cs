using UnityEngine;
using TMPro;

public class CoinCollector : MonoBehaviour
{
    private TextMeshProUGUI coinText;
    private AudioSource pickupSource;

    public AudioClip coinClip;

    private int levelCoins = 0; // 👈 Level-only coins

    void Start()
    {
        // 🔄 Reset level coins
        levelCoins = 0;

        // 🔍 Find Coin UI
        GameObject textObj = GameObject.FindGameObjectWithTag("CoinText");
        if (textObj != null)
        {
            coinText = textObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogError("❌ CoinText not found! Make sure tag is set.");
        }

        // 🔊 Find AudioSource
        //GameObject audioObj = GameObject.FindGameObjectWithTag("Sounds");
        //if (audioObj != null)
        //{
        //    pickupSource = audioObj.GetComponent<AudioSource>();
        //}

        UpdateCoinUI(); // will show 0 at start
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            // 🚫 Prevent double collection
            if (other.GetComponent<CollectedFlag>() != null)
                return;

            other.gameObject.AddComponent<CollectedFlag>();

            // 🎇 Play particles
            ParticleSystem[] particles = other.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particles)
            {
                ps.transform.parent = null;
                ps.Play();
                Destroy(ps.gameObject, ps.main.duration);
            }

            // 🔊 Play sound
            if (pickupSource != null && coinClip != null)
            {
                pickupSource.pitch = Random.Range(0.95f, 1.05f);
                pickupSource.PlayOneShot(coinClip);
            }

          

            // 💰 Add to LEVEL coins (for UI only)
            levelCoins++;

            UpdateCoinUI();

            // ❌ Destroy coin
            Destroy(other.gameObject);
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text = levelCoins.ToString(); // 👈 ONLY LEVEL COINS
        }
    }
    public int GetLevelCoins()
    {
        return levelCoins;
    }
}

// ✅ Prevent double trigger
public class CollectedFlag : MonoBehaviour { }