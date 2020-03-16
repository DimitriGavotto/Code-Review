using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Score : MonoBehaviour
{
    #region Invisible Variables

    public static Score Instance;

    public Dictionary<int, PlayerScore> playerDictionary = new Dictionary<int, PlayerScore>();

    //class being serialized
    public ScoreBoard scoreBoard;

    #endregion


    private void Awake()
    {
        #region Setting Instance

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.Log("Score._instance is not null");

            if (Instance == this)
                return;

            Debug.Log("This Score._instance is not the singleton, destroying");

            Destroy(gameObject);
        }

        #endregion

        scoreBoard = new ScoreBoard();
        LoadAllPlayers();
    }

    //called when the game starts create a list of all the active players
    public void PopulatePlayerList()
    {
        //getting current players
        foreach (var temp in PlayerConnectionSetUp.Instance.players.Select(player =>
            player.Value.myGameObject.GetComponent<PlayerNet>()))
        {
            playerDictionary.Add(temp.playerNumber, new PlayerScore(temp.playerNumber, temp.gameObject));
        }

    }


    //keeps track of when a player is getting hit
    public void AddNewHit(int _playerNumber)
    {
        //if player exist return this player from the list
        if (!CheckIfPlayerExist(_playerNumber, out var player))
            return;

        player.hitAmount++;

        player.playerGameObject.GetComponent<PlayerNet>().UpdateScoreOnPlayer(player.hitAmount,
            player.gotHitAmount);
    }


    public void AddNewGotHit(int _playerNumber)
    {
        //if player exist return this player from the list
        if (!CheckIfPlayerExist(_playerNumber, out var player))
            return;
        
        player.gotHitAmount++;

        player.playerGameObject.GetComponent<PlayerNet>().UpdateScoreOnPlayer(playerDictionary[_playerNumber].hitAmount,
            player.gotHitAmount);
        
    }


    public void PlayerFinishedRace(int _playerNumber)
    {
        if (!CheckIfPlayerExist(_playerNumber, out var player))
            return;
        
        player.time = UIPC.Instance.RaceTime;

        scoreBoard.PushData(player);

        Save.SaveAllPlayers(this);

        scoreBoard.PushData(player);

        UpdateGlobalScoreboard();
    }


    public void UpdateGlobalScoreboard()
    {
        List<PlayerScoreData> tempscoreboardRecorderList = new List<PlayerScoreData>(scoreBoard.cachedPlayerScores);

        tempscoreboardRecorderList.Sort((left, right) => left.time.CompareTo(right.time));

        //create al list of playersScore data for the top 10
        var tempTop10 = new List<PlayerScoreData>();

        //create a class that will hold the list of all the players in top 10
        DataToSendOver tempscoScoreBoardToSend = new DataToSendOver();

        //get the amount of available playerScore
        var count = tempscoreboardRecorderList.Count > 10 ? 10 : tempscoreboardRecorderList.Count;

        for (int i = 0; i < count; i++)
        {
            tempTop10.Add(tempscoreboardRecorderList[i]);
        }

        tempscoScoreBoardToSend.cachedPlayerScores = tempTop10;

        //send top 10 to clients    
        GameManager.Instance.SendScoreBoardToClients(tempscoScoreBoardToSend);
        
        //update scoreBoard on Server
        for (int i = 0; i < tempTop10.Count; i++)
        {
            InGameUI.Instance.top10Text[i].SetText(tempTop10[i]);
        }
    }
    
    #region Tools

    private void LoadAllPlayers()
    {
        //get Json data
        var data = Save.LoadAllPlayers();

        if (data == null)
        {
            print("No data present inside Json AllPlayers");
            return;
        }
        // assign Json list to new class    
        ScoreBoard.cachedPlayerScores = data.cachedPlayerScores;
    }
    
    private bool CheckIfPlayerExist(int _playerNumber, out PlayerScore player)
    {
        var exist = playerDictionary.TryGetValue(_playerNumber, out player);

        if (exist) return true;

        Debug.LogError($"this key: {_playerNumber}, does not exist");

        return false;
    }

    #endregion
}


public class PlayerScore
{
    public int playerNumber;
    public int hitAmount;
    public int gotHitAmount;
    public float time;
    public GameObject playerGameObject;
    public string playerName;

    public PlayerScore(int _playerNumber, GameObject _playerGameObject)
    {
        playerNumber = _playerNumber;
        playerGameObject = _playerGameObject;

        hitAmount = 0;
        gotHitAmount = 0;
        time = 0;
        playerName = "unknown";
    }
}