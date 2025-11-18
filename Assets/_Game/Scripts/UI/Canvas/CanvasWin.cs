using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasWin : UICanvas
{
   public void NextLVBTN()
   {
        GameManager.Instance.NextLV();
        gameObject.SetActive(false);
   }

    public void PrevLVBTN()
    {
        GameManager.Instance.RePlay();
        gameObject.SetActive(false);
    }
}
