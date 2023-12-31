using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

public class DataParser : MonoBehaviour
{
    private DataManager _dataManager;
    [SerializeField] private GameObject _tempImage; // 시간 없어서 빨리 만듬.
    const string URL = "https://docs.google.com/spreadsheets/d/1YaHM8DXAnrpBN8bjXSm8VY-Gk4OURelhiTdyOpYnb10/export?format=csv";

    private UIManager _uiManager;
    private void Awake()
    {
        TryGetComponent(out _dataManager);
        _uiManager = FindAnyObjectByType<UIManager>();
    }

    public void SetDataDictionary()
    {
        StartCoroutine(SetDataDictionaryWithURL());
    }

    private IEnumerator SetDataDictionaryWithURL()
    {
        _tempImage.SetActive(true);
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
        UnityWebRequest www = UnityWebRequest.Get(URL);
        yield return www.SendWebRequest();

        string data = www.downloadHandler.text;
        string[] words = data.Split("\n");
        for (int i = 0; i < words.Length; ++i)
        {
            _dataManager.TryAddKey(words[i]);
        }
        _tempImage.SetActive(false);
        stopwatch.Stop();
        print($"{stopwatch.ElapsedMilliseconds}ms 걸림");
        _dataManager.FinishAddKey();
    }
}
