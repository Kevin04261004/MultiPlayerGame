using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    [SerializeField] private RectTransform _chatContent;
    [field:SerializeField] public TextMeshProUGUI _chatTMP { get; private set; }
    [SerializeField] private ScrollRect _chatScrollRect;
    
    public void AddMessage(string data)
    {
        _chatTMP.text = string.IsNullOrEmpty(_chatTMP.text) ? data : _chatTMP.text + "\n" + data;
        Fit(_chatContent);
        Fit(_chatTMP.GetComponent<RectTransform>());
        //Invoke(nameof(SetVerticalScrollBar),0.03f);
        SetVerticalScrollBar();
    }

    private void Fit(RectTransform rect)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }
    private void SetVerticalScrollBar()
    {
        _chatScrollRect.verticalScrollbar.value = 0;
    }
}
