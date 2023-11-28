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
        _wordInputFieldTMP.onSubmit.AddListener(delegate { OnSubmit(); });
    }

    private void OnSubmit()
    {
        if (string.IsNullOrEmpty(_wordInputFieldTMP.text))
        {
            return;
        }
        AfterYell(_dataManager.YellWord(_wordInputFieldTMP.text)); 
        _wordInputFieldTMP.text = "";
        _wordInputFieldTMP.ActivateInputField();
    }

    private void AfterYell(EYellReturnType e)
    {
        switch (e)
        {
            case EYellReturnType.Good:
                
                break;
            case EYellReturnType.NonWord:
                
                break;
            case EYellReturnType.UsedWord:
                
                break;
            default:
                Debug.Assert(true, "Enum 값 부족");
                break;
        }
    }
}
