using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Tooltip("당신인가? (생성과 동시에 초기화를 해주자.)")]
    public bool isMine;
    [Tooltip("서버, 클라이언트가 둘다 되는 호스트인가?")]
    public bool isMasterClient;
    [SerializeField] private bool _isMyTurn;
    private WordInput _wordInput;

    private void Awake()
    {
        _wordInput = FindAnyObjectByType<WordInput>();
    }

    public void MyTurnStart()
    {
        _isMyTurn = true;
        _wordInput.WordInputFieldFocus();
    }

    public void MyTurnEnd()
    {
        _isMyTurn = false;
    }
}