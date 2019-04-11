﻿#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrainCloud;
using LitJson;
using UnityEngine;
using UnityEngine.UI;

#endregion

public class TicTacToe : GameScene
{
    
    #region Variables
    private readonly int[] _grid = new int[9];

    private readonly Vector3[] _tokenPositions =
    {
        new Vector3(-2.1f, 12, 2.1f),
        new Vector3(0, 12, 2.1f),
        new Vector3(2.1f, 12, 2.1f),
        new Vector3(-2.1f, 12, 0),
        new Vector3(0, 12, 0),
        new Vector3(2.1f, 12, 0),
        new Vector3(-2.1f, 12, -2.1f),
        new Vector3(0, 12, -2.1f),
        new Vector3(2.1f, 12, -2.1f)
    };

    private readonly int[,] _winningCond =
    {
        //List of possible winning conditions
        {0, 1, 2},
        {3, 4, 5},
        {6, 7, 8},
        {0, 3, 6},
        {1, 4, 7},
        {2, 5, 8},
        {0, 4, 8},
        {2, 4, 6}
    };

    public List<GameObject> GridObjList;
    private List<string> _history;
    private bool _isHistoryMatch;

    public GameObject PlayerO;
    public TextMesh PlayerTurnText;

    public GameObject PlayerX;
    private bool _turnPlayed;
    private bool _turnSubmitted;

    private int _winner;

    public PlayerInfo WinnerInfo;
    public PlayerInfo LoserInfo;

    private bool hasNoNewTurn;

    public TextMesh PlayerXName;
    public TextMesh PlayerOName;
    
    #endregion

    private void Start()
    {
        _winner = 0;

        var parent = gameObject.transform.parent.gameObject;

        parent.transform.localPosition = new Vector3(parent.transform.localPosition.x + App.Offset,
            parent.transform.localPosition.y, parent.transform.localPosition.z);

        for (var i = 0; i < _tokenPositions.Length; i++) _tokenPositions[i].x += App.Offset;

        parent.GetComponentInChildren<Camera>().rect = App.ViewportRect;


        // Read the state and assembly the board
        BuildBoardFromState(App.BoardState);

        // Check we if are not seeing a done match
        _winner = CheckForWinner();

        // Setup HUD with player pics and names
        

        PlayerXName.text = App.PlayerInfoX.PlayerName;
        PlayerOName.text = App.PlayerInfoO.PlayerName;
        PlayerTurnText.text = "Your Turn";

        _turnPlayed = false;

        _winner = CheckForWinner();
        if (_winner != 0)
        {
            _isHistoryMatch = true;
            _turnPlayed = true;

            if (_winner == -1)
                PlayerTurnText.text = "Match Tied";
            else
                PlayerTurnText.text = "Match Completed";

            App.Bc.AsyncMatchService
                .ReadMatchHistory(App.OwnerId, App.MatchId, OnReadMatchHistory, null, null);
        }
        // Read match history
        enableRTT();
    }

    // Enable RTT
    private void enableRTT()
    {
        // Only Enable RTT if its not already started
        if (!App.Bc.Client.IsRTTEnabled())
        {
            App.Bc.Client.EnableRTT(eRTTConnectionType.WEBSOCKET, onRTTEnabled, onRTTFailure);
        }
        else
        {
            // its already started, lets call our success delegate 
            onRTTEnabled("", null);
        }
    }

    // rtt enabled, ensure we now request the updated match state
    private void onRTTEnabled(string responseData, object cbPostObject)
    {
        // LISTEN TO THE ASYNC CALLS, when we get one of these calls, lets just refresh 
        // match state
        queryMatchState();

        App.Bc.Client.RegisterRTTAsyncMatchCallback(queryMatchStateRTT);
    }

    // the listener, can parse the json and request just the updated match 
    // in this example, just re-request it all
    private void queryMatchStateRTT(string in_json)
    {
        queryMatchState();
    }

    private void queryMatchState()
    {
        // Read match history
        // Query more detail state about the match
        App.Bc.AsyncMatchService
            .ReadMatch(App.OwnerId, App.MatchId, (response, cbObject) =>
            {
                var match = App.CurrentMatch;
                var data = JsonMapper.ToObject(response)["data"];


                int newVersion = int.Parse(data["version"].ToString());

                if (App.MatchVersion + 1 >= (ulong)newVersion)
                {
                    hasNoNewTurn = true;
                }
                else
                {
                    App.MatchVersion = (ulong)newVersion;

                            // Setup a couple stuff into our TicTacToe scene
                            App.BoardState = (string)data["matchState"]["board"];
                    App.PlayerInfoX = match.playerXInfo;
                    App.PlayerInfoO = match.playerOInfo;
                    App.WhosTurn = match.yourToken == "X" ? App.PlayerInfoX : match.playerOInfo;
                    App.OwnerId = match.ownerId;
                    App.MatchId = match.matchId;

                            // Load the Tic Tac Toe scene

                            App.GotoTicTacToeScene(gameObject);

                }
            });
    }

    private void onRTTFailure(int status, int reasonCode, string responseData, object cbPostObject)
    {
        // TODO! Bring up a user dialog to inform of poor connection
        // for now, try to auto connect 
        Invoke("enableRTT", 5.0f);
    }

    private void OnReadMatchHistory(string responseData, object cbPostObject)
    {
        var turns = JsonMapper.ToObject(responseData)["data"]["turns"];

        _history = new List<string>();
        for (var i = 0; i < turns.Count; ++i)
        {
            var turn = turns[i];
            var turnState = (string) turn["matchState"]["board"];
            _history.Add(turnState);
        }
    }

    private void AddToken(int index, string token)
    {
        GridObjList.Add(Instantiate(token == "X" ? PlayerX : PlayerO, _tokenPositions[index],
            Quaternion.Euler(Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f), Random.Range(-7.0f, 7.0f))));
        GridObjList.Last().transform.parent = gameObject.transform;
        _grid[index] = token == "X" ? 1 : 2;
    }

    public void PlayTurn(int index, PlayerInfo player)
    {
        var token = player == App.PlayerInfoX ? "X" : "O";
        AddToken(index, token);
        // Modify the boardState
        var boardStateBuilder = new StringBuilder(App.BoardState);
        boardStateBuilder[index] = token[0];
        App.BoardState = boardStateBuilder.ToString();

        _turnPlayed = true;

        if (App.WhosTurn == App.PlayerInfoX)
            App.WhosTurn = App.PlayerInfoO;
        else
            App.WhosTurn = App.PlayerInfoX;

        _winner = CheckForWinner();

        if (_winner < 0)
        {
            PlayerTurnText.text = "Game Tied!";
        }
        else if (_winner > 0)
        {
            if (_winner == 1)
            {
                PlayerTurnText.text = App.PlayerInfoX.PlayerName + " Wins!";
                WinnerInfo = App.PlayerInfoX;
                LoserInfo = App.PlayerInfoO;
            }
            else
            {
                PlayerTurnText.text = App.PlayerInfoO.PlayerName + " Wins!";
                WinnerInfo = App.PlayerInfoO;
                LoserInfo = App.PlayerInfoX;
            }
        }
        else
        {
            PlayerTurnText.text = App.WhosTurn.PlayerName + " Turn";
        }
    }

    private void ClearTokens()
    {
        //Clear logical grid
        for (var i = 0; i < _grid.Length; i++) _grid[i] = 0;

        //Clear instanciated game objects
        foreach (var obj in GridObjList) Destroy(obj);
        GridObjList.Clear();
    }

    public bool AvailableSlot(int index)
    {
        if (_turnPlayed) return false;
        if (_grid[index] == 0) return true;
        return false;
    }

    // Checks if we have a winner yet.
    // Returns -1 = Game Tied, 0 = No winner yet, 1 = Player1 won, 2 = Player2 won
    private int CheckForWinner()
    {
        var ourWinner = 0;
        var gameEnded = true;

        for (var i = 0; i < 8; i++)
        {
            int a = _winningCond[i, 0], b = _winningCond[i, 1], c = _winningCond[i, 2];
            int b1 = _grid[a], b2 = _grid[b], b3 = _grid[c];

            if (b1 == 0 || b2 == 0 || b3 == 0)
            {
                gameEnded = false;
                continue;
            }

            if (b1 == b2 && b2 == b3)
            {
                ourWinner = b1;
                break;
            }
        }

        if (gameEnded && ourWinner == 0) ourWinner = -1;

        return ourWinner;
    }

    private void OnGUI()
    {
        // Display History HUD
        OnHistoryGUI();

        if (!_turnPlayed) return;

        var btnText = "Submit Turn";
        if (_winner != 0) btnText = "Complete Game";

        if (_isHistoryMatch)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), "Leave"))
                App.GotoMatchSelectScene(gameObject);
        }

        if (_turnSubmitted)
        {
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), "Leave"))
            {
                App.GotoMatchSelectScene(gameObject);
            }
            if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60 - 45, 140, 30), "Refresh"))
            {
                queryMatchState();
            }

            if (hasNoNewTurn)
            {
                GUI.Label(new Rect(Screen.width / 2 - 70 + App.Offset, 60 + 45, 140, 30), "Has no new turn");
            }
            
        }
        else if (GUI.Button(new Rect(Screen.width / 2 - 70 + App.Offset, 60, 140, 30), btnText))
        {
            // Ask the user to submit their turn
            var boardStateJson = new JsonData();
            boardStateJson["board"] = App.BoardState;

            App.Bc.AsyncMatchService.SubmitTurn(
                App.OwnerId,
                App.MatchId,
                App.MatchVersion,
                boardStateJson.ToJson(),
                "A turn has been played",
                null,
                null,
                null,
                OnTurnSubmitted, (status, code, error, cbObject) => { Debug.Log(status); Debug.Log(code); Debug.Log(error.ToString()); });
        }
    }

    private void BuildBoardFromState(string boardState)
    {
        ClearTokens();
        var j = 0;
        foreach (var c in boardState)
        {
            if (c != '#') AddToken(j, c.ToString());
            ++j;
        }
    }

    private void OnHistoryGUI()
    {
        if (_history == null) return;

        var i = 0;
        GUI.Label(new Rect(Screen.width / 2 + App.Offset, 130, 70, 30), "History:");
        foreach (var turnState in _history)
        {
            if (GUI.Button(new Rect(Screen.width / 2 + App.Offset, 150 + i * 40, 70, 30), "Turn " + i))
                BuildBoardFromState(turnState);

            ++i;
        }
    }

    private void OnTurnSubmitted(string responseData, object cbPostObject)
    {
        
        if (_winner == 0)
        {
            _turnSubmitted = true;
            return;
        }

        // Otherwise, the game was done. Can send complete turn
        /*
         App.Bc.AsyncMatchService.CompleteMatch(
             App.OwnerId,
             App.MatchId,
             OnMatchCompleted);
          */

        // However, we are using a custom FINISH_RANK_MATCH script which is set up on brainCloud. View the commented Cloud Code script below
        var matchResults = new JsonData();

        matchResults["ownerId"] = App.OwnerId;
        matchResults["matchId"] = App.MatchId;

        if (_winner < 0)
        {
            matchResults["isTie"] = true;
        }
        else
        {
            matchResults["isTie"] = false;
            matchResults["winnerId"] = WinnerInfo.ProfileId;
            matchResults["loserId"] = LoserInfo.ProfileId;
            matchResults["winnerRating"] = int.Parse(WinnerInfo.PlayerRating);
            matchResults["loserRating"] = int.Parse(LoserInfo.PlayerRating);
        }


        App.Bc.ScriptService.RunScript("RankGame_FinishMatch", matchResults.ToJson(), OnMatchCompleted,
            (status, code, error, cbObject) => { });
    }

    private void OnMatchCompleted(string responseData, object cbPostObject)
    {
        // Get the new PlayerRating
        App.PlayerRating = JsonMapper.ToObject(responseData)["data"]["response"]["data"]["playerRating"].ToString();

        
        // Go back to game select scene
        App.GotoMatchSelectScene(gameObject);
    }


    private enum MatchState
    {
        YOUR_TURN,
        TURN_PLAYED,
        WAIT_FOR_TURN,
        MATCH_HISTORY,
        COMPLETED
    }
}