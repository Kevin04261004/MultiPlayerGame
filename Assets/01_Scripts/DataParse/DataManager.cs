using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

public enum EYellReturnType
{
    Good,
    NonWord,
    UsedWord,
}

public class DataManager : MonoBehaviour
{
    private Dictionary<string, bool> _dataDictionary = new Dictionary<string, bool>();
    private bool _canAddkey = true;
    public void TryAddKey(string str)
    {
        if (!_canAddkey)
        {
            Debug.Assert(true,"키를 넣을 수 없음.");
            return;
        }

        str = str.Substring(0, str.Length - 1);
        _dataDictionary.TryAdd(str, false);
    }
    public void FinishAddKey()
    {
        _canAddkey = false;
    }
    private bool HasKey(string str)
    {
        return _dataDictionary.ContainsKey(str);
    }
    public EYellReturnType YellWord(string str)
    {
        if (!HasKey(str))
        {
            print("키가 존재하지 않음!");
            return EYellReturnType.NonWord;
        }
        
        _dataDictionary.TryGetValue(str, out bool isUsed);
        if (isUsed)
        {
            print("이미 사용한 단어!");
            return EYellReturnType.UsedWord;
        }
        print(str);
        _dataDictionary[str] = true;
        return EYellReturnType.Good;
    }
    public void ResetDictionary()
    {
        /* 반복중에 딕셔너리가 변경되어서 오류가 남.
        foreach (var i in _dataDictionary.Keys)
        {
            _dataDictionary[i] = false;
        }
        */

        List<string> keyToReset = new List<string>(_dataDictionary.Keys);
        foreach (var key in keyToReset)
        {
            _dataDictionary[key] = false;
        }
    }
}
