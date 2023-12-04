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

    public void SetWordInputFieldTMP(string tmp)
    {
        _wordInputFieldTMP.text = tmp;
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

    public void OnValueChanged()
    {
        _client.ChangeWord(_wordInputFieldTMP.text);
        Debug.Log(_wordInputFieldTMP.text);
    }
    public void WordInputFieldFocus()
    {
        _wordInputFieldTMP.ActivateInputField();
    }

    public void WordInputFieldInteractive(bool canInteractive)
    {
        _wordInputFieldTMP.interactable = canInteractive;
    }
    public void SetPlaceHolder(string temp)
    {
        _placeHolderTMP.text = temp;
    }
}
