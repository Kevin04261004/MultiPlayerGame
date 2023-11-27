using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public bool YellWord(string str)
    {
        if (!HasKey(str))
        {
            print("키가 존재하지 않음!");
            return false;
        }
        
        _dataDictionary.TryGetValue(str, out bool isUsed);
        if (isUsed)
        {
            print("이미 사용한 단어!");
            return false;
        }
        print(str);
        isUsed = true;
        return true;
    }
    
}
