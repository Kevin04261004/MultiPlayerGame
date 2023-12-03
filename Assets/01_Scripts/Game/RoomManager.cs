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
        switch (serverToClientListPacketType)
        {
            case EServerToClientListPacketType.NoneWord:
                Debug.Log($"[{socketType}] 존재하지 않는 단어!");
                _uiManger.SetCodeTMP("[알림] 존재하지 않는 단어");
                break;
            case EServerToClientListPacketType.UsedWord:
                Debug.Log($"[{socketType}] 이미 사용한 단어!");
                _uiManger.SetCodeTMP("[알림] 이미 사용한 단어");
                break;
            case EServerToClientListPacketType.DifferentFirstLetter:
                Debug.Log($"[{socketType}] 앞 글자가 다릅니다!");
                _uiManger.SetCodeTMP("[알림] 앞 글자가 다릅니다");
                break;
            case EServerToClientListPacketType.GoodWord:
                Debug.Log($"[{socketType}] 성공!!!");
                string str = Encoding.Default.GetString(data);
                _uiManger.SetGoodCodeTMP($"[성공] {str}");
                break;
            case EServerToClientListPacketType.ReadyGame:
                ReadyGame(socketType);
                break;
            case EServerToClientListPacketType.AddPoint:
                int point = BitConverter.ToInt32(data);
                Debug.Log($"{socketType}가 {point}점을 획득하였습니다.");
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
    public void ClearPlayerListBG()
    {
        foreach (Transform child in _playerListBG)
        {
            playerPanelList.Remove(child.gameObject);
            Destroy(child.gameObject);
        }
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
        PlayerManager temp2 = _turnManager.GetPlayerManagerOrNullWithPlayerInfoData(playerInfoData);
        if (temp2 != null)
        {
            playerPanel.isReady = false;
            playerPanel.SetPanel();
            playerPanelList.Add(temp);   
        }
        else
        {
            playerPanel.isReady = temp2.IsReady();
            playerPanel.SetPanel();
            playerPanelList.Add(temp); 
        }
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
