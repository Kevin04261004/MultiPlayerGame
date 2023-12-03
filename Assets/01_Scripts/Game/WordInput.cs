using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField _wordInputFieldTMP;
    private TextMeshProUGUI _placeHolderTMP;
    private DataManager _dataManager;
    private GameClient _client;
    private void Awake()
    {
        TryGetComponent(out _wordInputFieldTMP);
        _dataManager = FindAnyObjectByType<DataManager>();
        _client = FindAnyObjectByType<GameClient>();
        _placeHolderTMP = transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
        _wordInputFieldTMP.onSubmit.AddListener(delegate { OnSubmit(); });
    }
    private void OnSubmit()
    {
        if (string.IsNullOrEmpty(_wordInputFieldTMP.text))
        {
            return;
        }

        _client.SendWord(_wordInputFieldTMP.text);
        _wordInputFieldTMP.text = "";
        WordInputFieldFocus();
    }

    public void WordInputFieldFocus()
    {
        _wordInputFieldTMP.ActivateInputField();
    }

    public void SetPlaceHolder(string temp)
    {
        _placeHolderTMP.text = temp;
    }
}
