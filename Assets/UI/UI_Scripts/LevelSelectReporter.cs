using UnityEngine;
using UnityEngine.EventSystems;

// Added automatically to every level button by MainUI.
// Reports to MainUI whenever this button becomes the selected one.
public class LevelSelectReporter : MonoBehaviour, ISelectHandler
{
    public int index;

    public void OnSelect(BaseEventData eventData)
    {
        if (MainUI.Instance != null)
            MainUI.Instance.SetSelectedIndex(index);
    }
}