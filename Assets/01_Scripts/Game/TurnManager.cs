using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private List<PlayerManager> _playerList = new List<PlayerManager>();
    private PlayerManager myPlayer;
    [SerializeField] private int _round = 0;
    [SerializeField] private int _turn = 0;
    [SerializeField] private float _time = 0;
    private float _maxTime = 10;
    private float _oneRoundSpeedUp = 0.98f;
    private bool _isGameOn = false;

    public void StartGame()
    {
        if (_playerList.Count == 0)
        {
            Debug.Assert(true, "버그");
            return;
        }
        _round = 1;
        _turn = 0;
        _maxTime = 10;
        _time = _maxTime;
        for (int i = 0; i < _playerList.Count; ++i)
        {
            _playerList[i].MyTurnEnd();
        }
        _playerList[_turn].MyTurnStart();

        _isGameOn = true;
    }

    private void NextTurn()
    {
        _playerList[_turn].MyTurnEnd();
        _turn++;
        if (_turn >= _playerList.Count)
        {
            _turn = 0;
            RoundUp();
        }
        _playerList[_turn].MyTurnStart();
    }

    private void RoundUp()
    {
        _round++;
        _maxTime *= _oneRoundSpeedUp;
    }
    private void Update()
    {
        if (!_isGameOn)
        {
            return;
        }
        
        if (_time > 0)
        {
            _time -= Time.deltaTime;
        }
        else
        {
            NextTurn();
            _time = _maxTime;
        }
    }

    public bool IsPlayerAlreadyEnter(GamePlayerInfoData data)
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (_playerList[i].PlayerInfoData == data)
            {
                return true;
            }
        }

        return false;
    }
    public bool TryAddPlayer(PlayerManager playerManager)
    {
        if (_playerList.Contains(playerManager))
        {
            return false;
        }
        _playerList.Add(playerManager);
        return true;
    }

    public PlayerManager GetMyPlayerManagerOrNull()
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (_playerList[i].isMine)
            {
                return _playerList[i];
            }
        }

        return null;
    }

    public PlayerManager GetPlayerManagerWithPlayerInfoDataOrNull(in GamePlayerInfoData data)
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (_playerList[i].PlayerInfoData == data)
            {
                return _playerList[i];
            }
        }

        return null;
    }
    public void PlayerExit(PlayerManager playerManager)
    {
        _playerList.Remove(playerManager);
        Destroy(playerManager.gameObject);
    }
}
