using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private List<PlayerManager> _playerList = new List<PlayerManager>();
    private GameClient _client;
    private PlayerManager myPlayer;
    private int _maxRound = 20;
    [SerializeField] private int _round = 0;
    [SerializeField] private int _turn = 0;
    [SerializeField] private float _time = 0;
    private UIManager _uiManager;
    private float _realMaxTime = 20;
    private float _maxTime;
    private float _oneRoundSpeedUp = 0.98f;
    private bool _isGameOn = false;
    
    private void Awake()
    {
        _uiManager = FindAnyObjectByType<UIManager>();
        _client = FindAnyObjectByType<GameClient>();
        _maxTime = _realMaxTime;
        _uiManager._roundTMP.text = $"Round: {_round}/{_maxRound}";
    }

    public void StartGame()
    {
        if (_playerList.Count == 0)
        {
            Debug.Assert(true, "버그");
            return;
        }
        SortPlayerList();
        _round = 1;
        _turn = 0;
        _time = _maxTime;
        for (int i = 0; i < _playerList.Count; ++i)
        {
            _playerList[i].MyTurnEnd();
        }
        _playerList[_turn].MyTurnStart();

        _isGameOn = true;
    }

    private void SortPlayerList()
    {
        for (int i = 0; i < _playerList.Count; ++i)
        {
            for (int j = i; j < _playerList.Count; ++j)
            {
                if (_playerList[i].PlayerInfoData.socketType > _playerList[j].PlayerInfoData.socketType)
                {
                    (_playerList[i], _playerList[j]) = (_playerList[j], _playerList[i]);
                }
            }
        }
    }
    public void NextTurn()
    {
        _playerList[_turn].MyTurnEnd();
        _turn++;
        if (_turn >= _playerList.Count)
        {
            _turn = 0;
            RoundUp();
        }
        _playerList[_turn].MyTurnStart();
        _time = _maxTime;
    }

    private void RoundUp()
    {
        _round++;
        _maxTime *= _oneRoundSpeedUp;
        _uiManager._roundTMP.text = $"Round: {_round}/{_maxRound}";
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
            _uiManager.SetSlider(_time,_maxTime);
        }
        else
        {
            // 실패.
            _client.FailInputWord(in _playerList[_turn].PlayerInfoData.socketType);
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

    public void ResetMaxTime()
    {
        _maxTime = _realMaxTime;
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
            if (!_playerList[i].IsReady() && !_playerList[i].isMine)
            {
                return false;
            }
        }

        return true;
    }
}
