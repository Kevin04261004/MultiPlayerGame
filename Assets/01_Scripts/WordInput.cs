using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _wordInputFieldTMP;
    private DataManager _dataManager;

    private void Awake()
    {
        TryGetComponent(out _wordInputFieldTMP);
        _dataManager = FindAnyObjectByType<DataManager>();
    }

    public void UpdateInputField()
    {
        print(_wordInputFieldTMP.text);
        if (!_wordInputFieldTMP.text.EndsWith("\n"))
        {
            return;
        }
#if (UNITY_EDITOR || UNITY_STANDALONE)
        if (!Input.GetButtonDown("Submit"))
        {
            return;            
        }
        _wordInputFieldTMP.ActivateInputField();
#endif
        if (string.IsNullOrEmpty(_wordInputFieldTMP.text.Trim()))
        {
            return;
        }
        _dataManager.YellWord(_wordInputFieldTMP.text);
        _wordInputFieldTMP.text = "";
    }
}
