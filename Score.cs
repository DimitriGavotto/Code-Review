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
    public List<PlayerScore> EveryPlayerThatCompletedTheRace = new List<PlayerScore>();

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

    //called when the game starts creates of all the active players
    public void PopulatePlayerList()
    {
        //getting current players
        foreach (var temp in PlayerConnectionSetUp.Instance.players.Select(player =>
            player.Value.myGameObject.GetComponent<PlayerNet>()))
        {
            playerDictionary.Add(temp.playerNumber, new PlayerScore(temp.playerNumber, temp.gameObject));
        }

        //add new players to AllPlayers Json
        foreach (var player in EveryPlayerThatCompletedTheRace)
        {
            scoreBoard.PushData(player);
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

        EveryPlayerThatCompletedTheRace.Add(player);

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

   

    #region Debugging

    public void testscorething()
    {
        //create a list of 15 fake players
        for (int i = 0; i < 15; i++)
        {
            EveryPlayerThatCompletedTheRace.Add(new PlayerScore(Random.Range(1, 100), new GameObject()));
        }

        //give all the players a random time
        foreach (var playerScore in EveryPlayerThatCompletedTheRace)
        {
            playerScore.time = Random.Range(100, 200);

            scoreBoard.PushData(playerScore);
        }

        foreach (var test in scoreBoard.cachedPlayerScores)
        {
            print($"Test------{test.time}");
        }


        Save.SaveAllPlayers(this);
        UpdateGlobalScoreboard();
    }

    #endregion

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
        // find all players into the Json and add them to the list of everyPlayerThatCompletedTheRace
        foreach (var tempPlayer in data.cachedPlayerScores.Select(player => new PlayerScore(-999, null) {time = player.time, playerName = player.name}))
        {
            EveryPlayerThatCompletedTheRace.Add(tempPlayer);
        }
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