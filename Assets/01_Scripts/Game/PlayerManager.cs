using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public GamePlayerInfoData PlayerInfoData;
    [Tooltip("당신인가? (생성과 동시에 초기화를 해주자.)")]
    public bool isMine;
    [SerializeField] private bool _isMyTurn;
    [SerializeField] private int _point = 2000;
    [SerializeField] private bool _isReady;
    [SerializeField] private GameObject myPlayerImagePrefab;
    private UIManager _uiManager;
    private WordInput _wordInput;
    private GameObject _myPlayer;
    private void Awake()
    {
        _wordInput = FindAnyObjectByType<WordInput>();
        _uiManager = FindAnyObjectByType<UIManager>();
    }
    
    private void OnEnable()
    {
        StartCoroutine(SpawnPlayer());
    }

    public void UpdatePoint(int amount)
    {
        _point += amount;
        _myPlayer.transform.GetChild(1).TryGetComponent(out TextMeshProUGUI pointTMP);
        pointTMP.text = amount.ToString();
    }
    
    private IEnumerator SpawnPlayer()
    {
        yield return new WaitForSeconds(0.05f);
        _myPlayer =  Instantiate(myPlayerImagePrefab);
        Transform temp =_uiManager.GetPlayerPosTransformOrNullFromPlayerPosArrayWithIndex((int)PlayerInfoData.socketType-2);
        _myPlayer.transform.SetParent(temp);
        _myPlayer.transform.localPosition = Vector3.zero;
        _myPlayer.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI nameTMP);
        nameTMP.text = PlayerInfoData.playerName;
    }
    private void OnDisable()
    {
        Destroy(_myPlayer);
    }

    public void MyTurnStart()
    {
        _wordInput.WordInputFieldInteractive(true);
        _uiManager._turnTMP.text = $"Turn: [{PlayerInfoData.socketType}] {PlayerInfoData.playerName}";
        _isMyTurn = true;
        _wordInput.WordInputFieldFocus();
    }

    public void MyTurnEnd()
    {
        _wordInput.WordInputFieldInteractive(false);
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
