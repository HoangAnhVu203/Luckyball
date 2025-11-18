using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasSetting : UICanvas
{
    public void CloseUI()
    {
        GameManager.Instance.ResumeGame();
        UIManager.Instance.CloseUIDirectly<CanvasSetting>();
    }
}
