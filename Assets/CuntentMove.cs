using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CuntentMove : MonoBehaviour
{
    // Start is called before the first frame update

    public static CuntentMove instnace;


    public RectTransform content;
    public int selectedIndex = 0;
    public float smoothSpeed = 2.5f;
    public GameObject SelectedObj;
    public float[] positions;
    public Button[] lvlButtons;
    private void Awake()
    {
        instnace = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Contentmove()
    {
        if (content == null || SelectedObj == null)
            return;

        Level_Id  level = SelectedObj.GetComponent<Level_Id>();

        if (level == null)
            return;

        int id = level.id;

        if (id < 0 || id >= positions.Length)
            return;

        // Vertical movement
        Vector2 targetPosition = new Vector2(
content.anchoredPosition.x,
positions[id]
);

        content.anchoredPosition = Vector2.Lerp(
        content.anchoredPosition,
        targetPosition,
        smoothSpeed * Time.deltaTime
        );
    }

    public void SelectNext()
    {
        if (positions != null && selectedIndex < positions.Length - 1)
            selectedIndex++;
    }

    public void SelectPrevious()
    {
        if (selectedIndex > 0)
            selectedIndex--;
    }

}
