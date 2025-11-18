using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class CanvasGameplay : UICanvas
{
    public void NextLVBTN()
    {
        GameManager.Instance.NextLV();
    }

    public void RePlayBTN()
    {
        GameManager.Instance.RePlay();
    }

    public void SettingBTN()
    {
        GameManager.Instance.PauseGame();
        UIManager.Instance.OpenUI<CanvasSetting>();
    }
}
