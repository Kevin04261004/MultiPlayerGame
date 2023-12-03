using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerPanel_GO : MonoBehaviour
{
    public ESocketType SocketType;
    public string name;
    public bool isReady;

    public void SetPanel()
    {
        transform.GetChild(0).TryGetComponent(out TextMeshProUGUI tmp);
        tmp.text = name;
        transform.GetChild(1).TryGetComponent(out TextMeshProUGUI tmp2);
        if (isReady)
        {
            tmp2.color = Color.green;
            tmp2.text = "v";
        }
        else
        {
            tmp2.color = Color.red;
            tmp2.text = "x";
        }

        if (SocketType == ESocketType.Client1)
        {
            tmp2.color = Color.yellow;
            tmp2.text = "@";
        }
    }
}
