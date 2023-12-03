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

    public bool IsPlayerAlreadyEnter(in GamePlayerInfoData data)
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (CompareGamePlayerInfoData(data,_playerList[i].PlayerInfoData))
            {
                return true;
            }
        }

        return false;
    }
    public bool TryAddPlayer(in PlayerManager playerManager)
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

    public PlayerManager GetPlayerManagerOrNullWithPlayerInfoData(in GamePlayerInfoData data)
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (CompareGamePlayerInfoData(data,_playerList[i].PlayerInfoData))
            {
                return _playerList[i];
            }
        }

        return null;
    }
    public PlayerManager GetPlayerManagerOrNullWithESocketType(in ESocketType socketType)
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (_playerList[i].PlayerInfoData.socketType == socketType)
            {
                return _playerList[i];
            }
        }

        return null;
    }
    private bool CompareGamePlayerInfoData(in GamePlayerInfoData first,in GamePlayerInfoData second)
    {
        if (first.playerName == second.playerName &&first.socketType == second.socketType)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public void PlayerExit(PlayerManager playerManager)
    {
        _playerList.Remove(playerManager);
        Destroy(playerManager.gameObject);
    }

    public void ReadyGame(ESocketType socketType)
    {
        PlayerManager temp = GetPlayerManagerOrNullWithESocketType(socketType);
        temp.ReadyTrigger();
    }
    public bool IsAllReady()
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            if (!_playerList[i].IsReady())
            {
                return false;
            }
        }

        return true;
    }
}
