using Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ButtonHighlighter : MonoBehaviour
{
    private Button previousButton;
    [SerializeField] private float scaleAmount = 1f;
    public GameObject defaultButton;
    public GameObject singleplaybtn;

    [SerializeField] Button[] levels;
    [SerializeField] bool levelSelection;

    private const string HighlighterKey = "currentHighlighter";

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(HighlighterKey))
        {
            PlayerPrefs.SetInt(HighlighterKey, 0);
        }
    }

    void Start()
    {
        if (levelSelection)
        {
            DefaultHighlighter_Levels();
            return;
        }

        if (defaultButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }
    }

    private void OnEnable()
    {
        // Clear any stale reference (e.g. a button from a panel that was closed)
        // so the next selection is always treated as a change.
        if (previousButton != null)
            UnHighlightButton(previousButton);
        previousButton = null;

        if (EventSystem.current == null)
            return;

        if (levelSelection)
        {
            DefaultHighlighter_Levels();
            return;
        }

        if (defaultButton != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }
    }

    void Update()
    {
        if (EventSystem.current == null)
            return;

        var selectedObj = EventSystem.current.currentSelectedGameObject;

        if (selectedObj == null)
        {
            if (previousButton != null)
            {
                UnHighlightButton(previousButton);
                previousButton = null;
            }

            return;
        }

        var selectedAsButton = selectedObj.GetComponent<Button>();

        if (selectedAsButton != previousButton)
        {
            if (previousButton != null)
                UnHighlightButton(previousButton);

            if (selectedAsButton != null)
            {
                if (selectedAsButton.transform.name != "PauseButton")
                    HighlightButton(selectedAsButton);
            }

            previousButton = selectedAsButton;

            if (levelSelection && selectedAsButton != null)
                SaveSelectedLevelIndex(selectedAsButton);
        }

        if (selectedAsButton == null)
            return;

        if (levelSelection && MainUI.Instance != null)
        {
            MainUI.Instance.UpdateSelectedLevel(selectedAsButton.gameObject);
        }
    }

    private void SaveSelectedLevelIndex(Button selected)
    {
        if (levels == null)
            return;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == selected)
            {
                PlayerPrefs.SetInt(HighlighterKey, i);
                return;
            }
        }
    }

    public static GameObject FindGameObjectInChildWithTag(GameObject parent, string tag)
    {
        Transform t = parent.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject.tag == tag)
            {
                return t.GetChild(i).gameObject;
            }
        }
        return null;
    }

    void HighlightButton(Button butt)
    {
        butt.transform.localScale = new Vector3(scaleAmount, scaleAmount, scaleAmount);
        if (butt.transform.tag == "Giveborder")
        {
            var border = FindGameObjectInChildWithTag(butt.gameObject, "border");
            if (border != null)
                border.SetActive(true);
        }
        if (butt.GetComponent<Outline>() != null)
        {
            butt.GetComponent<Outline>().enabled = true;
        }
    }

    void UnHighlightButton(Button butt)
    {
        if (butt == null)
            return;

        butt.transform.localScale = new Vector3(1, 1, 1);
        if (butt.transform.tag == "Giveborder")
        {
            var border = FindGameObjectInChildWithTag(butt.gameObject, "border");
            if (border != null)
                border.SetActive(false);
        }
        if (butt.GetComponent<Outline>() != null)
        {
            butt.GetComponent<Outline>().enabled = false;
        }
    }

    public void setHighlighter(GameObject defaultButton)
    {
        EventSystem.current.SetSelectedGameObject(defaultButton);
    }

    public void SetDefaultButton(GameObject _defaultButton)
    {
        defaultButton = _defaultButton;
    }

    //default highlight for levels selection

    public void DefaultHighlighter_Levels()
    {
        if (levels == null || levels.Length == 0)
            return;

        if (EventSystem.current == null)
            return;

        int index = PlayerPrefs.GetInt(HighlighterKey, 0);
        index = Mathf.Clamp(index, 0, levels.Length - 1);

        if (levels[index] == null || !levels[index].interactable)
        {
            index = -1;

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].interactable)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(levels[index].gameObject);
    }

    public void BuyFullHigh()
    {
        int currentIndex = PlayerPrefs.GetInt(HighlighterKey, 0);

        if (EventSystem.current == null)
            return;

        if (levels == null || levels.Length == 0)
            return;

        currentIndex = Mathf.Clamp(currentIndex, 0, levels.Length - 1);

        if (levels[currentIndex] != null)
        {
            EventSystem.current.SetSelectedGameObject(levels[currentIndex].gameObject);
        }
    }
}