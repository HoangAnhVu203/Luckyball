using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class CanvasGameplay : MonoBehaviour
{
    public void NextLVBTN()
    {
        
    }

    public void RePlayBTN()
    {
        
    }

    public void SettingBTN()
    {
        UIManager.Instance.OpenUI<CanvasSetting>();
    }
}
