using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private GameObject Player_prefab;
    [SerializeField] private Transform playerList_transform;
    private TurnManager _turnManager;
    [SerializeField] private GameObject _playerPanelPrefab;
    [SerializeField] private Transform _playerListBG;
    [SerializeField] private UIManager _uiManger;
    private List<GameObject> playerPanelList = new List<GameObject>();
    private void Awake()
    {
        _turnManager = FindAnyObjectByType<TurnManager>();
        _uiManger = FindAnyObjectByType<UIManager>();
    }

    public void PlayerEnter(in GamePlayerInfoData playerInfo, bool isMine = false)
    {
        if (!_uiManger.ReadyGameBtn.gameObject.activeSelf && !_uiManger.StartGameBtn.gameObject.activeSelf)
        {
            // 방장이면 겜시작. 아니면 레디
            if (playerInfo.socketType == ESocketType.Client1)
            {
                _uiManger.ReadyGameBtn.gameObject.SetActive(false);
                _uiManger.StartGameBtn.gameObject.SetActive(true);
            }
            else
            {
                _uiManger.ReadyGameBtn.gameObject.SetActive(true);
                _uiManger.StartGameBtn.gameObject.SetActive(false);
            }   
        }
        InstantiatePlayer(in playerInfo, isMine);
        AddPlayerPanel(in playerInfo);
    }
    
    private void InstantiatePlayer(in GamePlayerInfoData playerInfo, bool isMine = false)
    {
        if (_turnManager.IsPlayerAlreadyEnter(in playerInfo))
        {
            return;
        }
        GameObject temp = Instantiate(Player_prefab, Vector3.zero, Quaternion.identity);
        temp.transform.parent = playerList_transform;
        temp.TryGetComponent(out PlayerManager playerManager);
        playerManager.PlayerInfoData = playerInfo;
        playerManager.isMine = isMine;
        _turnManager.TryAddPlayer(in playerManager);
    }

    public void PlayerExit(in GamePlayerInfoData playerInfo)
    {
        if (!_turnManager.IsPlayerAlreadyEnter(playerInfo))
        {
            return;
        }

        PlayerManager temp = _turnManager.GetPlayerManagerOrNullWithPlayerInfoData(in playerInfo);
        if (playerInfo == null)
        {
            return;
        }
        _turnManager.PlayerExit(temp);
        RemovePlayerPanel(playerInfo);
    }
    
    public PlayerManager GetMyPlayerManagerOrNull()
    {
        return _turnManager.GetMyPlayerManagerOrNull();
    }

    public void ProcessPacket(EServerToClientListPacketType serverToClientListPacketType, ESocketType socketType, byte[] data = null)
    {
        string str;
        int point;
        PlayerManager tempPlayerManager;
        switch (serverToClientListPacketType)
        {
            case EServerToClientListPacketType.NoneWord:
                Debug.Log($"[{socketType}] 존재하지 않는 단어!");
                str = Encoding.Default.GetString(data);
                _uiManger.SetCodeTMP($"[알림] 존재하지 않는 단어: {str}");
                break;
            case EServerToClientListPacketType.UsedWord:
                Debug.Log($"[{socketType}] 이미 사용한 단어!");
                str = Encoding.Default.GetString(data);
                _uiManger.SetCodeTMP($"[알림] 이미 사용한 단어: {str}");
                break;
            case EServerToClientListPacketType.DifferentFirstLetter:
                Debug.Log($"[{socketType}] 앞 글자가 다릅니다!");
                str = Encoding.Default.GetString(data);
                _uiManger.SetCodeTMP($"[알림] 앞 글자가 다릅니다: {str}");
                break;
            case EServerToClientListPacketType.GoodWord:
                Debug.Log($"[{socketType}] 성공!!!");
                str = Encoding.Default.GetString(data);
                _uiManger.SetGoodCodeTMP($"[성공] {str}");
                _turnManager.NextTurn();
                _uiManger.AddMinusPoint(100);
                break;
            case EServerToClientListPacketType.ReadyGame:
                ReadyGame(socketType);
                break;
            case EServerToClientListPacketType.AddPoint:
                point = BitConverter.ToInt32(data);
                tempPlayerManager = _turnManager.GetPlayerManagerOrNullWithESocketType(socketType);
                tempPlayerManager.UpdatePoint(point);
                Debug.Log($"{socketType}가 {point}점을 획득하였습니다.");
                break;
            case EServerToClientListPacketType.MinusPoint:
                point = BitConverter.ToInt32(data);
                tempPlayerManager = _turnManager.GetPlayerManagerOrNullWithESocketType(socketType);
                tempPlayerManager.UpdatePoint(-point);
                _uiManger.ResetMinusPoint();
                break;
            case EServerToClientListPacketType.InputWordFailed:
                _turnManager.ResetMaxTime();
                _turnManager.NextTurn();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serverToClientListPacketType), serverToClientListPacketType, null);
        }
    }

    private void ReadyGame(ESocketType socketType)
    {
        _turnManager.ReadyGame(socketType);
        for (int i = 0; i < playerPanelList.Count; ++i)
        {
            playerPanelList[i].TryGetComponent(out PlayerPanel_GO go);
            if (go.SocketType == socketType)
            {
                go.isReady = true;
                go.SetPanel();
            }
        }
    }
    public bool IsAllReady()
    {
        return _turnManager.IsAllReady();
    }

    public void StartGame()
    {
        Debug.Log("게임 시작!!!");
        _turnManager.StartGame();
    }

    private void AddPlayerPanel(in GamePlayerInfoData playerInfoData)
    {
        if (GetGameObjectOrNullFromPlayerPanelList(playerInfoData) != null)
        {
            return;
        }
        GameObject temp = Instantiate(_playerPanelPrefab);
        temp.transform.SetParent(_playerListBG);
        temp.transform.TryGetComponent(out PlayerPanel_GO playerPanel);
        playerPanel.name = playerInfoData.playerName;
        playerPanel.SocketType = playerInfoData.socketType;
        playerPanel.isReady = playerInfoData.isReady;
        playerPanel.SetPanel();
        playerPanelList.Add(temp);  
    }

    private GameObject GetGameObjectOrNullFromPlayerPanelList(in GamePlayerInfoData playerInfoData)
    {
        for (int i = 0; i <playerPanelList.Count; ++i)
        {
            playerPanelList[i].TryGetComponent(out PlayerPanel_GO go);
            if (go.name == playerInfoData.playerName && go.SocketType == playerInfoData.socketType)
            {
                return playerPanelList[i];
            }
        }

        return null;
    }
    private void RemovePlayerPanel(in GamePlayerInfoData playerInfoData)
    {
        GameObject delGO = GetGameObjectOrNullFromPlayerPanelList(in playerInfoData);
        playerPanelList.Remove(delGO);
        Destroy(delGO);
    }
}
