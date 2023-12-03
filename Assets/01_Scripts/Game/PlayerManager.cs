using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public GamePlayerInfoData PlayerInfoData;
    [Tooltip("당신인가? (생성과 동시에 초기화를 해주자.)")]
    public bool isMine;
    [SerializeField] private bool _isMyTurn;
    [SerializeField] private int _point = 2000;
    [SerializeField] private bool _isReady;
    private WordInput _wordInput;

    private void Awake()
    {
        _wordInput = FindAnyObjectByType<WordInput>();
    }

    public void MyTurnStart()
    {
        Debug.Log($"{PlayerInfoData.socketType}턴입니다.");
        _isMyTurn = true;
        _wordInput.WordInputFieldFocus();
    }

    public void MyTurnEnd()
    {
        _isMyTurn = false;
    }

    public bool IsMyTurn()
    {
        return _isMyTurn;
    }

    public void ReadyTrigger()
    {
        _isReady = !_isReady;
        Debug.Log($"{PlayerInfoData.socketType}님이 준비를 ={_isReady}하였습니다.");
    }

    public bool IsReady()
    {
        return _isReady;
    }
}
