
using Script;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ButtonHighlighter : MonoBehaviour
{
    private Button previousButton;
    [SerializeField] private float scaleAmount = 1.1f;
    public GameObject defaultButton;
    public GameObject singleplaybtn;

    [SerializeField] Button[] levels;
    [SerializeField] bool levelSelection;

    private void Awake()
    {
        if (!AndroidTV.IsAndroidOrFireTv())
        {
            //this.gameObject.GetComponent<ButtonHighlighter>().enabled = false;
        }

        if (!PlayerPrefs.HasKey("currentHighlighter"))
        {
            PlayerPrefs.SetInt("currentHighlighter", 0);
        }
    }
    void Start()
    {
        if (defaultButton != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultButton);
        }

        if (levelSelection)
        {
            EventSystem.current.SetSelectedGameObject(levels[PlayerPrefs.GetInt("currentHighlighter")].gameObject);
        }
    }
    private void OnEnable()
    {
        if (defaultButton != null && EventSystem.current != null)
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
        }

        if (selectedAsButton == null)
            return;

        if (levelSelection && MainUI.Instance != null)
        {
            MainUI.Instance.UpdateSelectedLevel(
                selectedAsButton.gameObject
            );
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
            FindGameObjectInChildWithTag(butt.gameObject, "border").SetActive(true);
        }
        if (butt.GetComponent<Outline>() != null)
        {
            butt.GetComponent<Outline>().enabled = true;
        }
    }
    void UnHighlightButton(Button butt)
    {
        butt.transform.localScale = new Vector3(1, 1, 1);
        if (butt.transform.tag == "Giveborder")
        {
            FindGameObjectInChildWithTag(butt.gameObject, "border").SetActive(false);
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

        int index = PlayerPrefs.GetInt("currentHighlighter", 0);
        index = Mathf.Clamp(index, 0, levels.Length - 1);

        if (levels[index] == null || !levels[index].interactable)
        {
            index = 0;

            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] != null && levels[i].interactable)
                {
                    index = i;
                    break;
                }
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(levels[index].gameObject);
    }

}