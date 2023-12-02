using System;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private GameObject Player_prefab;
    [SerializeField] private Transform playerList_transform;
    private TurnManager _turnManager;

    private void Awake()
    {
        _turnManager = FindAnyObjectByType<TurnManager>();
    }

    public void InstantiatePlayer(in GamePlayerInfoData playerInfo, bool isMine = false)
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

        PlayerManager temp = _turnManager.GetPlayerManagerWithPlayerInfoDataOrNull(in playerInfo);
        _turnManager.PlayerExit(temp);
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
                break;
            case EServerToClientListPacketType.UsedWord:
                Debug.Log($"[{socketType}] 이미 사용한 단어!");
                break;
            case EServerToClientListPacketType.DifferentFirstLetter:
                Debug.Log($"[{socketType}] 앞 글자가 다릅니다!");
                break;
            case EServerToClientListPacketType.GoodWord:
                Debug.Log($"[{socketType}] 성공!!!");
                break;
            case EServerToClientListPacketType.AddPoint:
                int point = BitConverter.ToInt32(data);
                Debug.Log($"{socketType}가 {point}점을 획득하였습니다.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serverToClientListPacketType), serverToClientListPacketType, null);
        }
    }

    public bool IsAllReady()
    {
        return _turnManager.IsAllReady();
    }

    public void StartGame()
    {
        _turnManager.StartGame();
    }
}
