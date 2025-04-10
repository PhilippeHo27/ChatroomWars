using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Singletons;
using TMPro;
using DG.Tweening;
using Core.WebSocket;
using MessagePack;
using ColorUtility = UnityEngine.ColorUtility;
using Debug = UnityEngine.Debug;
using static Core.VinceGame.GridGame;

namespace Core.VinceGame
{
    public class GamePrototype : MonoBehaviour
    {
        #region Serialized Fields
        
        [SerializeField] private PowerUps powerUps;
        [SerializeField] private GameGUI gameGUI;
        
        // UI References
        // [SerializeField] private CanvasGroup setupCanvasGroup;
        // [SerializeField] private CanvasGroup battleCanvasGroup;
        // [SerializeField] private CanvasGroup endGameCanvasGroup;

        // Setup Canvas
        // [SerializeField] private TMP_Text countdownText;
        // [SerializeField] private CanvasGroup mainPage;
        // [SerializeField] private CanvasGroup readyPage;
        [SerializeField] private Button readyButton;

        // Main Canvas
        // [SerializeField] private TMP_Text playerNameText;
        // [SerializeField] private TMP_Text opponentNameText;
        // [SerializeField] private Image timerFill;
        [SerializeField] private Button[] gridButtons = new Button[9];
        [SerializeField] private Image[] gridButtonImages = new Image[9];
        [SerializeField] private Image[] otherBoard = new Image[9];
        // [SerializeField] private GameObject playerBoard;
        // [SerializeField] private GameObject opponentBoard;
        // [SerializeField] private CanvasGroup playerBoardCanvasGroup;
        // [SerializeField] private CanvasGroup opponentBoardCanvasGroup;
        [SerializeField] private Button[] colorChoosingButtons = new Button[3];
        // [SerializeField] private TMP_Text playerTurnText;
        // [SerializeField] private TMP_Text opponentTurnText;
        // [SerializeField] private TMP_Text currentRoundText;
        // [SerializeField] private TMP_Text announcementText;

        // End game Canvas
        [SerializeField] private Button replayButton;
        // [SerializeField] private TMP_Text gameOverText;
        // [SerializeField] private TMP_Text playerScoreText;
        // [SerializeField] private TMP_Text opponentScoreText;
        // [SerializeField] private TMP_Text playAgainText;
        // [SerializeField] private Image[] myGrid= new Image[9];
        // [SerializeField] private Image[] opponentGrid = new Image[9];
        
        // Cursor Data
        // [SerializeField] private Texture2D[] cursorTextures = new Texture2D[2];
        #endregion
        
        #region Member Variables

        // Core variables
        private WebSocketNetworkHandler _wsHandler;
        private GameManager _gameManager;
        private Automated _ai;

        private GameState _gameState;
        private bool _playerIsReady;
        private float _turnTimer;
        private int _countdownTimer;
        private Coroutine _timerCoroutine;
        private int _numberOfRounds;
        private bool _playAgainstAI;
        private bool _testBlind;
        private bool _isOnline;


        // Grid data
        private GridData _playerGrid;
        private GridData _opponentGrid;
        public string CurrentPaintColor { get; set; }
        private bool _isMyTurn = true;
        private int _currentRound;
        private int _totalTurns;
        private int _maxTurns;
        public bool shieldSelectionMode = false;
        
        // extra turns
        
        private List<int> _extraTurnMoves = new List<int>();
        private List<string> _extraTurnColors = new List<string>();
        private bool _isInExtraTurn = false;


        // Cursor
        private Texture2D _cursorTexture;
        private Vector2 _cursorHotspot;

        #endregion
        
        #region Core Game Functions
        
        private void Awake()
        {
            _ai = new Automated(this);
            _gameManager = GameManager.Instance;
            _wsHandler = WebSocketNetworkHandler.Instance;
            _wsHandler.VinceGame = this;
        }

        private void Start()
        {
            _playAgainstAI = _gameManager.playingAgainstAI;
            _testBlind = _gameManager.blindModeActive;
            _isOnline = _gameManager.isOnline;
            _numberOfRounds = _gameManager.numberOfRounds;
            _wsHandler.OnGameStartConfirmation += ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound += MatchFound;
            InitializeGameState();
            InitButtons();
            
            _gameState = gameGUI.StateChange(_playAgainstAI ? GameState.Battle : GameState.Setup, false);
            
            if (_playAgainstAI) StartGameAI();
            //if(_testBlind == false) SetPermanentSideBySideView();
        }
        
        private void InitializeGameState()
        {
            _currentRound = 1;
            _totalTurns = 0;
            _maxTurns = _numberOfRounds * 2;
            _turnTimer = _gameManager.timer;
            CurrentPaintColor = "";
            InitializeGrids();
            powerUps.InitializePowerups();
        }

        private void StartGame(bool isMyTurn)
        {
            _isMyTurn = isMyTurn;
            DOTween.KillAll();

            // if (_isMyTurn) _gameManager.TextAnimations.PopText(playerTurnText, " You start! ");
            // else _gameManager.TextAnimations.Typewriter(playerTurnText, " Waiting...");
            
            if (_isMyTurn) _gameManager.TextAnimations.PopText(gameGUI.GetText(GridGame.TextType.PlayerTurn), "You start!");
            else _gameManager.TextAnimations.Typewriter(gameGUI.GetText(GridGame.TextType.PlayerTurn), "Waiting...");
            
            _gameState = gameGUI.StateChange(GameState.Battle);
            if (_isMyTurn) _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        private void StartGameAI()
        {
            _isMyTurn = true;
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }
        
        private void ResetGame()
        {
            InitializeGameState();
            gameGUI.ResetGUI();
            // InitializeGrids();
            // ResetGridColors();
            //InitializeGUIPositions();
            //ResetCursor();
            //powerUps.InitializePowerups();
            
            //countdownText.alpha = 1f;
            //_turnTimer = _gameManager.timer;;
            //_currentRound = 1;
            //_totalTurns = 0;
            //_maxTurns = _numberOfRounds * 2;
            //currentRoundText.text = $"Round: {_currentRound}";
            readyButton.gameObject.SetActive(true);

            //UpdateTurnIndicators();

            _gameState = gameGUI.StateChange(GameState.Setup);
            //_gameManager.TextAnimations.Typewriter(countdownText, "Press Ready to go again");
            _gameManager.TextAnimations.Typewriter(gameGUI.GetText(GridGame.TextType.Countdown), "Press Ready to go again");
            
            if (_playAgainstAI) StartGame(true);
        }
        

        
        // private void ResetGridColors()
        // {
        //     for (int i = 0; i < gridButtons.Length; i++)
        //     {
        //         ChangeButtonColor(i, "#FFFFFF");
        //         otherBoard[i].color = Color.white;
        //     }
        // }
        public void QuitGame()
        {
            CleanUp();
            SceneLoader.Instance.LoadScene("Intro");
        }
        private void CleanUp()
        {
            // Unsubscribe from WebSocket events
            _wsHandler.OnGameStartConfirmation -= ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound -= MatchFound;
            DOTween.KillAll();
        }
        
        private void EndGame()
        {
            // Wait for any active conflict animations to finish first
            StartCoroutine(DelayedEndGame());
        }
        private IEnumerator DelayedEndGame()
        {
            // Wait a short time to ensure all conflicts are resolved
            yield return new WaitForSeconds(1.1f);

            int playerScore = _playerGrid.Marks.Count(mark => mark);
            int opponentScore = _opponentGrid.Marks.Count(mark => mark);

            // Set game state
            _gameState = gameGUI.StateChange(GameState.EndGame);
    
            // Populate end game grids and display results
            gameGUI.PopulateEndGameGrids(_playerGrid, _opponentGrid);
            gameGUI.DisplayGameResults(playerScore, opponentScore);
        }


        // private IEnumerator DelayedEndGame()
        // {
        //     // Wait a short time to ensure all conflicts are resolved
        //     // 1 second matches the animation duration in ResolveConflict
        //     yield return new WaitForSeconds(1.1f);
        //
        //     int playerScore = _playerGrid.Marks.Count(mark => mark);
        //     int opponentScore = _opponentGrid.Marks.Count(mark => mark);
        //
        //     //Debug.Log($"Final Scores - Player: {playerScore}, Opponent: {opponentScore}");
        //
        //     // Set game state
        //     _gameState = gameGUI.StateChange(GameState.EndGame);
        //     //PopulateEndGameGrids();
        //     gameGUI.PopulateEndGameGrids(_playerGrid, _opponentGrid);
        //
        //     // Set all text contents
        //     string resultMessage = playerScore > opponentScore 
        //         ? "YOU WIN!" 
        //         : playerScore < opponentScore 
        //             ? "YOU LOSE!" 
        //             : "IT'S A TIE!";
        //
        //     // gameOverText.text = $"GAME OVER\n{resultMessage}";
        //     // playerScoreText.text = $"Your Score: {playerScore}";
        //     // opponentScoreText.text = $"Opponent Score: {opponentScore}";
        //     
        //     gameGUI.GetText(TextType.GameOver).text = $"GAME OVER\n{resultMessage}";
        //     gameGUI.GetText(TextType.PlayerScore).text = $"GAME OVER\n{resultMessage}";
        //     gameGUI.GetText(TextType.OpponentScore).text = $"GAME OVER\n{resultMessage}";
        //
        //     //_gameManager.TextAnimations.CreateBreathingAnimation(gameOverText.transform);
        //     _gameManager.TextAnimations.CreateBreathingAnimation(gameGUI.GetText(TextType.GameOver).transform);
        // }
        
        // private void PopulateEndGameGrids()
        // {
        //     // Populate both grids in one loop
        //     for (int i = 0; i < 9; i++)
        //     {
        //         // Player grid
        //         ColorUtility.TryParseHtmlString(_playerGrid.Color[i], out Color playerColor);
        //         //myGrid[i].color = playerColor;
        //         gameGUI.MyGridImages[i].color = playerColor;
        //
        //         // Opponent grid
        //         ColorUtility.TryParseHtmlString(_opponentGrid.Color[i], out Color opponentColor);
        //         //opponentGrid[i].color = opponentColor;
        //         gameGUI.OpponentGridImages[i].color = opponentColor;
        //
        //     }
        // }


        #endregion
        
        #region Initializers

        private void InitializeGrids()
        {
            _playerGrid.Marks = new bool[9];
            _playerGrid.Color = new string[9];
            _playerGrid.Immune = new bool[9];

            _opponentGrid.Marks = new bool[9];
            _opponentGrid.Color = new string[9];
            _opponentGrid.Immune = new bool[9];
        }
        // private void InitializeCursor()
        // {
        //     _cursorHotspot = new Vector2(16, 16);
        //     //ResetCursor();
        // }
        private void InitButtons()
        {
            readyButton.onClick.AddListener(SendReady);
            replayButton.onClick.AddListener(ResetGame);

            for (int i = 0; i < colorChoosingButtons.Length; i++)
            {
                int buttonIndex = i;
                colorChoosingButtons[i].onClick.AddListener(() => OnColorButtonClick(buttonIndex));
            }

            for (int i = 0; i < gridButtons.Length; i++)
            {
                int buttonIndex = i;
                gridButtons[i].onClick.AddListener(() => OnGridButtonClick(gridButtons[buttonIndex]));
            }
        }
        // private void InitializeGUIPositions()
        // {
        //     if (!_playAgainstAI || _testBlind)
        //     {
        //         playerBoard.transform.localPosition = CenterPosition;
        //         opponentBoard.transform.localPosition = OffscreenPosition;
        //         opponentBoardCanvasGroup.alpha = 0f;
        //     }
        //     else
        //     {
        //         SetSideBySideView(false);
        //     }
        // }
        
        #endregion

        #region Game Visuals
        // private void StateChanger(GameState state, bool shouldLerp = true)
        // {
        //     SetAllCanvasesNonInteractable();
        //
        //     switch(state)
        //     {
        //         case GameState.Setup:
        //             TransitionCanvas(setupCanvasGroup, true, shouldLerp);
        //             TransitionCanvas(battleCanvasGroup, false,false);
        //             TransitionCanvas(endGameCanvasGroup, false, false);
        //             break;
        //
        //         case GameState.Battle:
        //             TransitionCanvas(battleCanvasGroup, true, shouldLerp);
        //             TransitionCanvas(setupCanvasGroup, false, false);
        //             TransitionCanvas(endGameCanvasGroup, false, false);
        //
        //             playerBoardCanvasGroup.alpha = 1f;
        //             playerBoardCanvasGroup.interactable = true;
        //             playerBoardCanvasGroup.blocksRaycasts = true;
        //             
        //             opponentBoardCanvasGroup.alpha = 1f;
        //             opponentBoardCanvasGroup.interactable = true;
        //             opponentBoardCanvasGroup.blocksRaycasts = true;
        //             
        //             break;
        //
        //         case GameState.EndGame:
        //             TransitionCanvas(endGameCanvasGroup, true, shouldLerp);
        //             TransitionCanvas(setupCanvasGroup, false, false);
        //             TransitionCanvas(battleCanvasGroup, false, false);
        //             LerpCanvasGroup(playerBoardCanvasGroup, 0f);
        //             LerpCanvasGroup(opponentBoardCanvasGroup, 0f);
        //             _gameManager.TextAnimations.Typewriter(playAgainText, " Again?");
        //             break;
        //     }
        //
        //     _gameState = state;
        // }
        // private void SetAllCanvasesNonInteractable()
        // {
        //     setupCanvasGroup.interactable = false;
        //     battleCanvasGroup.interactable = false;
        //     endGameCanvasGroup.interactable = false;
        //     setupCanvasGroup.blocksRaycasts = false;
        //     battleCanvasGroup.blocksRaycasts = false;
        //     endGameCanvasGroup.blocksRaycasts = false;
        // }       
        // private void TransitionCanvas(CanvasGroup group, bool active, bool shouldLerp = true)
        // {
        //     float targetAlpha = active ? 1f : 0f;
        //
        //     if (shouldLerp)
        //     {
        //         group.DOFade(targetAlpha, FadeDuration)
        //             .OnComplete(() => {
        //                 group.interactable = active;
        //                 group.blocksRaycasts = active;
        //             });
        //     }
        //     else
        //     {
        //         group.alpha = targetAlpha;
        //         group.interactable = active;
        //         group.blocksRaycasts = active;
        //     }
        // }
        // private void LerpCanvasGroup(CanvasGroup group, float targetAlpha, bool shouldLerp = true)
        // {
        //     group.interactable = targetAlpha > 0;
        //     group.blocksRaycasts = targetAlpha > 0;
        //     if (shouldLerp) group.DOFade(targetAlpha, FadeDuration);
        // }
        // public void SetSideBySideView(bool animate)
        // {
        //     if (animate)
        //     {
        //         // Animate player board to left
        //         playerBoard.transform.DOLocalMove(LeftPosition, SlideDuration);
        //     
        //         // Slide in and fade in opponent board
        //         opponentBoard.transform.DOLocalMove(RightPosition, SlideDuration);
        //         opponentBoardCanvasGroup.DOFade(1f, FadeDuration);
        //     }
        //     else
        //     {
        //         // Instant positioning
        //         playerBoard.transform.localPosition = LeftPosition;
        //         opponentBoard.transform.localPosition = RightPosition;
        //         opponentBoardCanvasGroup.alpha = 1f;
        //     }
        // }
        
        // private void SetPermanentSideBySideView()
        // {
        //     if (playerBoard == null || opponentBoard == null || opponentBoardCanvasGroup == null)
        //     {
        //         Debug.LogError("Player Board, Opponent Board, or Opponent Board Canvas Group is not assigned!");
        //         return;
        //     }
        //
        //     playerBoard.transform.localPosition = LeftPosition;
        //     opponentBoard.transform.localPosition = RightPosition;
        //
        //     opponentBoardCanvasGroup.alpha = 1f;
        //
        //     opponentBoard.SetActive(true);
        // }
        // public void SetCenteredView(bool animate)
        // {
        //     if (animate)
        //     {
        //         // Animate player board to center
        //         playerBoard.transform.DOLocalMove(CenterPosition, SlideDuration);
        //     
        //         // Slide out and fade out opponent board
        //         opponentBoard.transform.DOLocalMove(OffscreenPosition, SlideDuration);
        //         opponentBoardCanvasGroup.DOFade(0f, FadeDuration);
        //     }
        //     else
        //     {
        //         // Instant positioning
        //         playerBoard.transform.localPosition = CenterPosition;
        //         opponentBoard.transform.localPosition = OffscreenPosition;
        //         opponentBoardCanvasGroup.alpha = 0f;
        //     }
        // }
        private void OnColorButtonClick(int colorIndex)
        {
            string selectedColor = "";
            switch (colorIndex)
            {
                case 0:
                    selectedColor = ColorGreenSelect;
                    gameGUI.UpdateCursor(0);
                    break;
                case 1:
                    selectedColor = ColorBlueSelect;
                    gameGUI.UpdateCursor(1);
                    break;
                case 2:
                    selectedColor = ColorRedSelect;
                    gameGUI.UpdateCursor(2);
                    break;
            }
            CurrentPaintColor = selectedColor;
        }
        // private void UpdateCursor(int colorIndex)
        // {
        //     if (colorIndex >= 0 && colorIndex < cursorTextures.Length)
        //     {
        //         Cursor.SetCursor(cursorTextures[colorIndex], _cursorHotspot, CursorMode.Auto);
        //     }
        // }
        // private void ResetCursor()
        // {
        //     Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        // }
        // private void ChangeButtonColor(int buttonIndex, string hexColor)
        // {
        //     if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
        //     {
        //         gridButtonImages[buttonIndex].color = color;
        //     }
        // }

        // private void UpdateTurnIndicators()
        // {
        //     if (playerTurnText != null)
        //     {
        //         playerTurnText.text = _isMyTurn ? "Your Turn" : "Waiting...";
        //         _gameManager.TextAnimations.PopThenBreathe(playerTurnText.transform);
        //     }
        //
        //     if (opponentTurnText != null)
        //     {
        //         opponentTurnText.text = _isMyTurn ? "Waiting..." :  "Opponent's Turn";
        //         _gameManager.TextAnimations.PopThenBreathe(opponentTurnText.transform);
        //     }
        // }

        #endregion
        
        #region Core Mechanics
        public void OnGridButtonClick(Button clickedButton)
        {
            int buttonIndex = Array.IndexOf(gridButtons, clickedButton);

            // Handle shield selection mode
            if (shieldSelectionMode)
            {
                ApplyShield(buttonIndex);
                return;
            }

            // Regular move logic
            if (_playerGrid.Marks[buttonIndex])
            {
                //_gameManager.TextAnimations.PopText(announcementText, "Spot taken!", 0.15f, 0.1f, 1.15f);
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TextType.Announcement), "Spot taken!", 0.15f, 0.1f, 1.15f);
                return;
            }

            if (!string.IsNullOrEmpty(CurrentPaintColor) && _isMyTurn)
            {
                ApplyMove(buttonIndex, CurrentPaintColor);
            }
        }

        public void ApplyMove(int position, string color)
        {
            if (_playerGrid.Marks[position] || string.IsNullOrEmpty(color) || !_isMyTurn)
                return;
        
            _playerGrid.Marks[position] = true;
            _playerGrid.Color[position] = color;
    
            //ChangeButtonColor(position, color);
            gameGUI.ChangeButtonColor(position, color);
            CurrentPaintColor = "";
            gameGUI.ResetCursor();
            powerUps.ResetBluePowerUp();
    
            StartCoroutine(ProcessTurn(position, true));
        }

        public void ApplyShield(int position)
        {
            if (!_playerGrid.Marks[position] || string.IsNullOrEmpty(_playerGrid.Color[position]))
            {
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TextType.Announcement), "Can only shield your pieces!", 0.15f, 0.1f, 1.15f);
                return;
            }
        
            _playerGrid.Immune[position] = true;
            if (_isOnline) SendImmuneStatus((byte)position);
            _gameManager.TextAnimations.PopText(gameGUI.GetText(TextType.Announcement), "Piece shielded!", 0.15f, 0.1f, 1.15f);
    
            shieldSelectionMode = false;
            powerUps.OnShieldApplied();
        }

        
        private IEnumerator ProcessTurn(int buttonIndex, bool isLocalPlayerMove)
        {
            string originalColor = _playerGrid.Color[buttonIndex];
            
            // Handle conflict resolution
            if ((isLocalPlayerMove && _opponentGrid.Marks[buttonIndex]) || (!isLocalPlayerMove && _playerGrid.Marks[buttonIndex]))
            {
                yield return StartCoroutine(ResolveConflict(buttonIndex));
            }
            
            // Track moves for potential extra turn
            if (isLocalPlayerMove)
            {
                if (_isInExtraTurn)
                {
                    // Add second move to tracking lists
                    _extraTurnMoves.Add(buttonIndex);
                    _extraTurnColors.Add(originalColor);
                }
                else
                {
                    // Initialize tracking for first move
                    _extraTurnMoves.Clear();
                    _extraTurnColors.Clear();
                    _extraTurnMoves.Add(buttonIndex);
                    _extraTurnColors.Add(originalColor);
                }
            }
            
            yield return StartCoroutine(CheckPatternsCoroutine());
            
            // Check for extra turn
            bool extraTurn = powerUps.redPowerActivated && isLocalPlayerMove;
            
            if (extraTurn)
            {
                powerUps.ResetRedPowerup();
                powerUps.redPowerActivated = false;
                _isInExtraTurn = true;
            }
            else
            {
                // Send moves if needed
                if (isLocalPlayerMove && _isOnline)
                {
                    if (_isInExtraTurn)
                    {
                        SendMoves(_extraTurnMoves, _extraTurnColors);
                        _isInExtraTurn = false; 
                    }
                    else
                    {
                        SendMove(buttonIndex, originalColor);
                    }
                }
                
                // Increment turn counter
                _totalTurns++;
                
                // Check for end game AFTER all processing is complete
                if (_totalTurns >= _maxTurns)
                {
                    EndGame();
                    yield break;  // Exit early if game is over
                }
                
                // Otherwise continue with turn swap
                _isMyTurn = !_isMyTurn;
                _turnTimer = _gameManager.timer;;
                //UpdateTurnIndicators();
                gameGUI.UpdateTurnIndicators(_isMyTurn);
                HandleTimerBasedOnGameState();

                if (_isMyTurn)
                {
                    _currentRound = (_totalTurns + (_isMyTurn ? 2 : 1)) / 2;
                    //currentRoundText.text = $"Current Round {_currentRound}";
                    gameGUI.GetText(TextType.CurrentRound).text = $"Current Round {_currentRound}";

                }
                
                // Trigger AI move if needed
                if (isLocalPlayerMove && _playAgainstAI)
                {
                    StartCoroutine(ArtificialOpponent());
                }
            }
        }

        
        private void ResolveReceivedMove(int position, string opponentColor)
        {
            _opponentGrid.Marks[position] = true;
            _opponentGrid.Color[position] = opponentColor;
            otherBoard[position].color = GetColorFromHex(opponentColor);

            if (_playerGrid.Marks[position])
            {
                StartCoroutine(ResolveConflict(position));
            }
        }


        private IEnumerator ResolveConflict(int position)
        {
            string player1Color = _playerGrid.Color[position];
            string player2Color = _opponentGrid.Color[position];

            if (string.IsNullOrEmpty(player1Color) || string.IsNullOrEmpty(player2Color))
            {
                yield break;
            }
            
            (bool shouldClearPlayer1, bool shouldClearPlayer2) = ResolvingComparison(player1Color, player2Color);

            // Check immunity before proceeding
            if (shouldClearPlayer1 && _playerGrid.Immune[position])
            {
                shouldClearPlayer1 = false;
                _playerGrid.Immune[position] = false;
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TextType.Announcement), "Your piece was protected!", 0.15f, 0.1f, 1.15f);
                ClearSquare(position, false);
            }
            
            if (shouldClearPlayer2 && _opponentGrid.Immune[position])
            {
                shouldClearPlayer2 = false; 
                _opponentGrid.Immune[position] = false;
                _gameManager.TextAnimations.PopText(gameGUI.GetText(TextType.Announcement), "Opponent's piece was protected!", 0.15f, 0.1f, 1.15f);
                ClearSquare(position, true);
            }

            // If neither should be cleared now, exit early
            if (!shouldClearPlayer1 && !shouldClearPlayer2)
            {
                yield break;
            }

            float elapsedTime = 0f;
            float duration = 1f;

            // Get the initial colors from the Image components
            Color startColor1 = gridButtonImages[position].color;
            Color startColor2 = otherBoard[position].color;
            Color targetColor = Color.white;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                if (shouldClearPlayer1)
                {
                    gridButtonImages[position].color = Color.Lerp(startColor1, targetColor, progress);
                }

                if (shouldClearPlayer2)
                {
                    otherBoard[position].color = Color.Lerp(startColor2, targetColor, progress);
                }

                yield return null;
            }

            if (shouldClearPlayer1)
            {
                ClearSquare(position, true);
            }

            if (shouldClearPlayer2)
            {
                ClearSquare(position, false);
            }
        }

        private (bool shouldClearPlayer1, bool shouldClearPlayer2) ResolvingComparison(string player1Color, string player2Color)
        {
            if (player1Color == player2Color)
            {
                return (true, true);
            }

            switch (player1Color)
            {
                case ColorGreenSelect:
                    return (player2Color == ColorBlueSelect, player2Color == ColorRedSelect);
                case ColorBlueSelect:
                    return (player2Color == ColorRedSelect, player2Color == ColorGreenSelect);
                case ColorRedSelect:
                    return (player2Color == ColorGreenSelect, player2Color == ColorBlueSelect);
                default:
                    return (false, false);
            }
        }
        
        private void ClearSquare(int position, bool isPlayerSquare)
        {
            Color whiteColor = Color.white;

            if (isPlayerSquare)
            {
                gridButtonImages[position].color = whiteColor;
                _playerGrid.Marks[position] = false;
                _playerGrid.Color[position] = "";
            }
            else
            {
                otherBoard[position].color = whiteColor;
                _opponentGrid.Marks[position] = false;
                _opponentGrid.Color[position] = "";
            }
        }
        private IEnumerator CheckPatternsCoroutine()
        {
            // We always check the active player's grid
            GridData activeGrid = _isMyTurn ? _playerGrid : _opponentGrid;
            CheckForPatterns(activeGrid);
            yield return null;
        }
        private void CheckForPatterns(GridData grid)
        {
            // Check rows
            for (int i = 0; i < 9; i += 3)
            {
                if (CheckLine(grid, i, i + 1, i + 2)) return;
            }

            // Check columns
            for (int i = 0; i < 3; i++)
            {
                if (CheckLine(grid, i, i + 3, i + 6)) return;
            }
    
            // Check diagonals
            CheckLine(grid, 0, 4, 8);
            CheckLine(grid, 2, 4, 6);
        }
        private bool CheckLine(GridData grid, int a, int b, int c)
        {
            if (!grid.Marks[a] || !grid.Marks[b] || !grid.Marks[c]) return false;
            if (grid.Color[a] != grid.Color[b] || grid.Color[b] != grid.Color[c]) return false;

            //Debug.Log($"Found match with color: {grid.Color[a]}");
            if (!_isMyTurn) return false;

            return powerUps.ProcessPowerup(grid.Color[a]);
        }
        private void SwapTurns()
        {
            _isMyTurn = !_isMyTurn;
            _turnTimer = _gameManager.timer;;
            //UpdateTurnIndicators();
            gameGUI.UpdateTurnIndicators(_isMyTurn);
            HandleTimerBasedOnGameState();

            if (_isMyTurn)
            {
                _currentRound = (_totalTurns + (_isMyTurn ? 1 : 0)) / 2;
                //currentRoundText.text = $"Current Round {_currentRound}";
                gameGUI.GetText(TextType.CurrentRound).text = $"Current Round {_currentRound}";

            }
        }


        private IEnumerator ArtificialOpponent()
        {
            yield return new WaitForSeconds(0.5f);
            int movePosition = _ai.SimulateOpponentMove(_opponentGrid, otherBoard);
    
            if (movePosition >= 0)
            {
                StartCoroutine(ProcessTurn(movePosition, false));
            }
        }
        
        private void HandleTimerBasedOnGameState()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }

            if (_gameState == GameState.Battle && _isMyTurn)
            {
                _timerCoroutine = StartCoroutine(TimerCoroutine());
            }
        }
        private IEnumerator TimerCoroutine()
        {
            _turnTimer = _gameManager.timer;

            while (_turnTimer > 0)
            {
                _turnTimer -= Time.deltaTime;
                _gameManager.TextAnimations.UpdateTimerWithEffects(gameGUI.TimerFill, _turnTimer, _gameManager.timer);
                _gameManager.TextAnimations.UpdateTimerWithEffects(gameGUI.TimerFill, _turnTimer, _gameManager.timer);
                yield return null;
            }
    
            _gameManager.TextAnimations.ResetTimerVisuals(gameGUI.TimerFill);
            _turnTimer = _gameManager.timer;
    
            // Handle shield selection first if active
            if (shieldSelectionMode)
            {
                _ai.ForceShieldSelection(_playerGrid);
            }
            else
            {
                _ai.ForcePlayerMove(_playerGrid);
            }
        }

        
        #endregion
        
        #region Network
        private void SendReady()
        {
            _playerIsReady = true;
            readyButton.gameObject.SetActive(false);
            if (!_isOnline || _playAgainstAI)
            {
                StartGame(true);
            }
            else
            {
                var isReady = new BooleanPacket
                {
                    Type = PacketType.VinceGameConfirmStart,
                    Response = _playerIsReady
                };
                WebSocketNetworkHandler.Instance.SendWebSocketPackage(isReady);
            }
            //_gameManager.TextAnimations.Typewriter(countdownText, "Ready and waiting...");
            _gameManager.TextAnimations.Typewriter(gameGUI.GetText(TextType.Countdown), "Ready and waiting...");
        }
        // private void MatchFound(string roomId)
        // {
        //     LerpCanvasGroup(mainPage, 0);
        //     LerpCanvasGroup(readyPage, 1);
        //     //_gameManager.TextAnimations.Typewriter(countdownText, "Player Found! Ready?");
        //     _gameManager.TextAnimations.Typewriter(gameGUI.GetText(TextType.Countdown), "Player Found! Ready?");
        //     Debug.Log("Found match and we're in the room called" + roomId);
        // }
        
        private void MatchFound(string roomId)
        {
            gameGUI.ShowMatchFoundScreen("Player Found! Ready?");
            Debug.Log("Found match and we're in the room called" + roomId);
        }
        private void ReceiveReadyState(bool isMyTurn)
        {
            DOTween.KillAll();
            string[] countdownStrings = { "3", "2", "1", "GO!" };
            StartCoroutine(GameStartCountdownWrapper(countdownStrings, isMyTurn));
        }

        private IEnumerator GameStartCountdownWrapper(string[] countdownStrings, bool isMyTurn)
        {
            yield return StartCoroutine(gameGUI.GameStartCountdown(countdownStrings));
            StartGame(isMyTurn);
        }

        private IEnumerator GameStartCountdown(bool isMyTurn)
        {
            // Set up the countdown strings
            string[] countdownStrings = { "3", "2", "1", "GO!" };
    
            // Calculate total animation time
            float totalAnimationTime = countdownStrings.Length * (0.3f + 0.4f + 0.3f);
    
            // Start the countdown animation without waiting for callback
            _gameManager.TextAnimations.Countdown(
                gameGUI.GetText(TextType.Countdown), 
                countdownStrings,
                fadeInDuration: 0.3f, 
                displayDuration: 0.4f, 
                fadeOutDuration: 0.3f,
                popScale: 1.2f
            );
    
            // Wait for the exact animation duration
            yield return new WaitForSecondsRealtime(totalAnimationTime);
    
            // Start the game
            StartGame(isMyTurn);
        }
        private void SendMove(int index, string colorBeforeResolution)
        {
            var createData = new VinceGameData
            {
                Type = PacketType.VinceGamePacket,
                Index = (byte)index,
                SquareColor = colorBeforeResolution
            };
            
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
        }
        
        private void SendMoves(List<int> indices, List<string> colors)
        {
            byte[] indicesArray = new byte[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                indicesArray[i] = (byte)indices[i];
            }
    
            var createData = new ExtraTurnPacket
            {
                Type = PacketType.ExtraTurnPacket,
                Indices = indicesArray,
                Colors = colors.ToArray()
            };
    
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
        }
        
        public void ReceiveMove(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<VinceGameData>(messagePackData);
            if (receivedData.SenderId != WebSocketNetworkHandler.Instance.ClientId)
            {
                int index = receivedData.Index;
                ResolveReceivedMove(index, receivedData.SquareColor);
        
                // Increment turn counter for online mode only
                _totalTurns++;
        
                // Check if this was the final turn
                if (_totalTurns >= _maxTurns)
                {
                    EndGame();
                    return;  // Skip SwapTurns if game is over
                }
        
                SwapTurns();
            }
        }

        public void ReceiveMoves(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<ExtraTurnPacket>(messagePackData);
            if (receivedData.SenderId != WebSocketNetworkHandler.Instance.ClientId)
            {
                for (int i = 0; i < receivedData.Indices.Length; i++)
                {
                    ResolveReceivedMove(receivedData.Indices[i], receivedData.Colors[i]);
                }
        
                // Increment turn counter for online mode only
                _totalTurns++;
        
                // Check if this was the final turn
                if (_totalTurns >= _maxTurns)
                {
                    EndGame();
                    return;  // Skip SwapTurns if game is over
                }
        
                SwapTurns();
            }
        }

        public void SendImmuneStatus(byte[] indexes)
        {
            var immunePacket = new GridGameIndices 
            {
                Type = PacketType.VinceGameImmune,
                Index = indexes
            };
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(immunePacket);
        }

        public void SendImmuneStatus(byte index)
        {
            // Convert single index to array with one element
            SendImmuneStatus(new byte[] { index });
        }

        public void UpdateClientImmunePieces(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<GridGameIndices>(messagePackData);
            if (receivedData.SenderId != WebSocketNetworkHandler.Instance.ClientId)
            {
                powerUps.ShieldOpponentPieces(_opponentGrid, receivedData.Index);
            }
        }

        #endregion
    }
}
