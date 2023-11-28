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

    private void InstantiatePlayer()
    {
        GameObject temp = Instantiate(Player_prefab, Vector3.zero, Quaternion.identity);
        temp.transform.parent = playerList_transform;
        temp.TryGetComponent(out PlayerManager playerManager);
        _turnManager.AddPlayer(playerManager);
    }
    
    public void EnterRoom()
    {
        InstantiatePlayer();
        
    }
}
