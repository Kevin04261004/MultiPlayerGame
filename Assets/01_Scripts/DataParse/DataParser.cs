using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

public class DataParser : MonoBehaviour
{
    private DataManager _dataManager;
    const string URL = "https://docs.google.com/spreadsheets/d/1YaHM8DXAnrpBN8bjXSm8VY-Gk4OURelhiTdyOpYnb10/export?format=csv";

    [SerializeField] private GameObject _loadingImage;
    private void Awake()
    {
        TryGetComponent(out _dataManager);
    }

    public void SetDataDictionary()
    {
        StartCoroutine(SetDataDictionaryWithURL());
    }

    private IEnumerator SetDataDictionaryWithURL()
    {
        _loadingImage.SetActive(true);
        
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
        _loadingImage.SetActive(false);
        stopwatch.Stop();
        print($"{stopwatch.ElapsedMilliseconds}ms 걸림");
        _dataManager.FinishAddKey();
    }
}
