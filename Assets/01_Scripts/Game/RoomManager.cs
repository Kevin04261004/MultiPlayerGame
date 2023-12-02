using System;
using System.Collections;
using System.Collections.Generic;
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
        if (_turnManager.IsPlayerAlreadyEnter(playerInfo))
        {
            return;
        }
        GameObject temp = Instantiate(Player_prefab, Vector3.zero, Quaternion.identity);
        temp.transform.parent = playerList_transform;
        temp.TryGetComponent(out PlayerManager playerManager);
        playerManager.isMine = isMine;
        _turnManager.TryAddPlayer(playerManager);
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
}
