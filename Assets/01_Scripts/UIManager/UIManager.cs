using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject _inGameCanvas;
    [SerializeField] private GameObject _roomCanvas;
    [SerializeField] private TextMeshProUGUI _errorTMP;
    [field:SerializeField] public GameObject _loadingImage { get; private set; }
    [field:SerializeField] public TextMeshProUGUI _turnTMP { get; private set; }
    [field:SerializeField] public TextMeshProUGUI _timeTMP { get; private set; }

    public void ChangeCanvas()
    {
        if (_inGameCanvas.activeSelf)
        {
            _roomCanvas.SetActive(true);
            _inGameCanvas.SetActive(false);
        }
        else
        {
            _inGameCanvas.SetActive(true);
            _roomCanvas.SetActive(false);
        }
    }

    public bool IsInGameCanvasActiveTrue()
    {
        return _inGameCanvas.activeSelf;
    }
    public void SetErrorTMP(string str)
    {
        _errorTMP.text = str;
    }
}
