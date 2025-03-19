using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
        
        // Parameters
        [SerializeField] private bool playAgainstAI = true;
        [SerializeField] private bool testBlind = true;
        [SerializeField] private bool isOnline;
        [SerializeField] private int numberOfRounds = 6;
        [SerializeField] private float timer = 10f;

        // UI References
        [SerializeField] private CanvasGroup setupCanvasGroup;
        [SerializeField] private CanvasGroup battleCanvasGroup;
        [SerializeField] private CanvasGroup endGameCanvasGroup;

        // Setup Canvas
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private CanvasGroup mainPage;
        [SerializeField] private CanvasGroup readyPage;
        [SerializeField] private Button readyButton;
        [SerializeField] private Toggle[] someWinConditionToggle;

        // Main Canvas
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private Image timerFill;
        [SerializeField] private Button[] gridButtons = new Button[9];
        [SerializeField] private Image[] gridButtonImages = new Image[9];
        [SerializeField] private Image[] otherBoard = new Image[9];
        [SerializeField] private GameObject playerBoard;
        [SerializeField] private GameObject opponentBoard;
        [SerializeField] private CanvasGroup playerBoardCanvasGroup;
        [SerializeField] private CanvasGroup opponentBoardCanvasGroup;
        [SerializeField] private Button[] colorChoosingButtons = new Button[3];
        [SerializeField] private TMP_Text playerTurnText;
        [SerializeField] private TMP_Text opponentTurnText;
        [SerializeField] private TMP_Text currentRoundText;
        [SerializeField] private TMP_Text announcementText;
        
        // [SerializeField] private Button shieldButton;
        // [SerializeField] private Button revealButton;
        // [SerializeField] private Button extraButton;

        // End game Canvas
        [SerializeField] private Button replayButton;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private TMP_Text playerScoreText;
        [SerializeField] private TMP_Text opponentScoreText;
        [SerializeField] private TMP_Text playAgainText;
        
        // Cursor Data
        [SerializeField] private Texture2D[] cursorTextures = new Texture2D[2];
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
        
        // Grid data
        private GridData _playerGrid;
        private GridData _opponentGrid;
        public string CurrentPaintColor { get; set; }
        private bool _isMyTurn = true;
        private int _currentRound;
        private int _totalTurns;
        private int _maxTurns;
        public bool shieldSelectionMode = false;


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
            playAgainstAI = _gameManager.playingAgainstAI;
            testBlind = _gameManager.blindModeActive;
            isOnline = _gameManager.isOnline;
            _maxTurns = numberOfRounds * 2;
            
            numberOfRounds = _gameManager.numberOfRounds;
            _turnTimer = _gameManager.timer;
            CurrentPaintColor = "";
            
            playerNameText.text = PlayerPrefs.GetString("Username", "");
            
            StateChanger(playAgainstAI ? GameState.Battle : GameState.Setup, false);
            if (playAgainstAI) StartGameAI();
            
            _wsHandler.OnGameStartConfirmation += ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound += MatchFound;
            
            InitializeGrids();
            InitButtons();
            InitializeCursor();
            powerUps.InitializePowerups();
            InitializeGUIPositions();
        }

        private void StartGame(bool isMyTurn)
        {
            _isMyTurn = isMyTurn;
            DOTween.KillAll();

            if (_isMyTurn) _gameManager.TextAnimations.PopText(playerTurnText, " You start! ");
            else _gameManager.TextAnimations.Typewriter(playerTurnText, " Waiting...");
            
            StateChanger(GameState.Battle);
            if (_isMyTurn) _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        private void StartGameAI()
        {
            _isMyTurn = true;
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }
        
        private void ResetGame()
        {
            
            InitializeGrids();
            ResetGridColors();
            InitializeGUIPositions();
            ResetCursor();
            powerUps.InitializePowerups();
            
            countdownText.alpha = 1f;
            _turnTimer = timer;
            _currentRound = 0;
            _totalTurns = 0;
            _maxTurns = numberOfRounds * 2;
            currentRoundText.text = $"Round: {_currentRound}";
            UpdateTurnIndicators();

            StateChanger(GameState.Setup);
            _gameManager.TextAnimations.Typewriter(countdownText, "Press Ready to go again");
            
            if (playAgainstAI) StartGame(true);
        }
        
        private void ResetGridColors()
        {
            for (int i = 0; i < gridButtons.Length; i++)
            {
                ChangeButtonColor(i, "#FFFFFF");
                otherBoard[i].color = Color.white;
            }
        }
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
            int playerScore = _playerGrid.Marks.Count(mark => mark);
            int opponentScore = _opponentGrid.Marks.Count(mark => mark);

            Debug.Log($"Final Scores - Player: {playerScore}, Opponent: {opponentScore}");

            // Set game state
            StateChanger(GameState.EndGame);

            // Set all text contents
            string resultMessage = playerScore > opponentScore 
                ? "YOU WIN!" 
                : playerScore < opponentScore 
                    ? "YOU LOSE!" 
                    : "IT'S A TIE!";

            gameOverText.text = $"GAME OVER\n{resultMessage}";
            playerScoreText.text = $"Your Score: {playerScore}";
            opponentScoreText.text = $"Opponent Score: {opponentScore}";

            _gameManager.TextAnimations.CreateBreathingAnimation(gameOverText.transform);

            //Debug.Log($"Game ended - {resultMessage} (Player: {playerScore} vs Opponent: {opponentScore})");
        }

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
        private void InitializeCursor()
        {
            _cursorHotspot = new Vector2(16, 16);
            ResetCursor();
        }
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

            // shieldButton.onClick.AddListener( () => powerUps.ShieldPieces(isOnline,_playerGrid));
            // revealButton.onClick.AddListener(powerUps.RevealBoard);
            // extraButton.onClick.AddListener(powerUps.EnableExtraTurn);
        }
        private void InitializeGUIPositions()
        {
            if (!playAgainstAI || testBlind)
            {
                playerBoard.transform.localPosition = CenterPosition;
                opponentBoard.transform.localPosition = OffscreenPosition;
                opponentBoardCanvasGroup.alpha = 0f;
            }
            else
            {
                SetSideBySideView(false);
            }
        }
        
        #endregion

        #region Game Visuals
        private void StateChanger(GameState state, bool shouldLerp = true)
        {
            SetAllCanvasesNonInteractable();

            switch(state)
            {
                case GameState.Setup:
                    TransitionCanvas(setupCanvasGroup, true, shouldLerp);
                    TransitionCanvas(battleCanvasGroup, false,false);
                    TransitionCanvas(endGameCanvasGroup, false, false);
                    break;

                case GameState.Battle:
                    TransitionCanvas(battleCanvasGroup, true, shouldLerp);
                    TransitionCanvas(setupCanvasGroup, false, false);
                    TransitionCanvas(endGameCanvasGroup, false, false);

                    playerBoardCanvasGroup.alpha = 1f;
                    playerBoardCanvasGroup.interactable = true;
                    playerBoardCanvasGroup.blocksRaycasts = true;
                    
                    opponentBoardCanvasGroup.alpha = 1f;
                    opponentBoardCanvasGroup.interactable = true;
                    opponentBoardCanvasGroup.blocksRaycasts = true;
                    
                    break;

                case GameState.EndGame:
                    TransitionCanvas(endGameCanvasGroup, true, shouldLerp);
                    TransitionCanvas(setupCanvasGroup, false, false);
                    TransitionCanvas(battleCanvasGroup, false, false);
                    LerpCanvasGroup(playerBoardCanvasGroup, 0f);
                    LerpCanvasGroup(opponentBoardCanvasGroup, 0f);
                    _gameManager.TextAnimations.Typewriter(playAgainText, " Play again?");
                    break;
            }

            _gameState = state;
        }
        private void SetAllCanvasesNonInteractable()
        {
            setupCanvasGroup.interactable = false;
            battleCanvasGroup.interactable = false;
            endGameCanvasGroup.interactable = false;
            setupCanvasGroup.blocksRaycasts = false;
            battleCanvasGroup.blocksRaycasts = false;
            endGameCanvasGroup.blocksRaycasts = false;
        }       
        private void TransitionCanvas(CanvasGroup group, bool active, bool shouldLerp = true)
        {
            float targetAlpha = active ? 1f : 0f;

            if (shouldLerp)
            {
                group.DOFade(targetAlpha, FadeDuration)
                    .OnComplete(() => {
                        group.interactable = active;
                        group.blocksRaycasts = active;
                    });
            }
            else
            {
                group.alpha = targetAlpha;
                group.interactable = active;
                group.blocksRaycasts = active;
            }
        }
        private void LerpCanvasGroup(CanvasGroup group, float targetAlpha, bool shouldLerp = true)
        {
            group.interactable = targetAlpha > 0;
            group.blocksRaycasts = targetAlpha > 0;
            if (shouldLerp) group.DOFade(targetAlpha, FadeDuration);
        }
        public void SetSideBySideView(bool animate)
        {
            if (animate)
            {
                // Animate player board to left
                playerBoard.transform.DOLocalMove(LeftPosition, SlideDuration);
            
                // Slide in and fade in opponent board
                opponentBoard.transform.DOLocalMove(RightPosition, SlideDuration);
                opponentBoardCanvasGroup.DOFade(1f, FadeDuration);
            }
            else
            {
                // Instant positioning
                playerBoard.transform.localPosition = LeftPosition;
                opponentBoard.transform.localPosition = RightPosition;
                opponentBoardCanvasGroup.alpha = 1f;
            }
        }
        public void SetCenteredView(bool animate)
        {
            if (animate)
            {
                // Animate player board to center
                playerBoard.transform.DOLocalMove(CenterPosition, SlideDuration);
            
                // Slide out and fade out opponent board
                opponentBoard.transform.DOLocalMove(OffscreenPosition, SlideDuration);
                opponentBoardCanvasGroup.DOFade(0f, FadeDuration);
            }
            else
            {
                // Instant positioning
                playerBoard.transform.localPosition = CenterPosition;
                opponentBoard.transform.localPosition = OffscreenPosition;
                opponentBoardCanvasGroup.alpha = 0f;
            }
        }
        private void OnColorButtonClick(int colorIndex)
        {
            string selectedColor = "";
            switch (colorIndex)
            {
                case 0:
                    selectedColor = ColorGreenSelect;
                    UpdateCursor(0);
                    break;
                case 1:
                    selectedColor = ColorBlueSelect;
                    UpdateCursor(1);
                    break;
                case 2:
                    selectedColor = ColorRedSelect;
                    UpdateCursor(2);
                    break;
            }
            CurrentPaintColor = selectedColor;
        }
        private void UpdateCursor(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < cursorTextures.Length)
            {
                Cursor.SetCursor(cursorTextures[colorIndex], _cursorHotspot, CursorMode.Auto);
            }
        }
        private void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        private void ChangeButtonColor(int buttonIndex, string hexColor)
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                gridButtonImages[buttonIndex].color = color;
            }
        }

        private void UpdateTurnIndicators()
        {
            if (playerTurnText != null)
            {
                playerTurnText.text = _isMyTurn ? "Your Turn" : "Waiting...";
                _gameManager.TextAnimations.PopThenBreathe(playerTurnText.transform);
            }
    
            if (opponentTurnText != null)
            {
                opponentTurnText.text = _isMyTurn ? "Waiting..." : "AI Turn";
                _gameManager.TextAnimations.PopThenBreathe(opponentTurnText.transform);
            }
        }

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
                _gameManager.TextAnimations.PopText(announcementText, "Spot taken!", 0.15f, 0.1f, 1.15f);
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
    
            ChangeButtonColor(position, color);
            CurrentPaintColor = "";
            ResetCursor();
            powerUps.ResetBluePowerUp();
    
            StartCoroutine(ProcessTurn(position, true));
        }

        public void ApplyShield(int position)
        {
            if (!_playerGrid.Marks[position] || string.IsNullOrEmpty(_playerGrid.Color[position]))
            {
                _gameManager.TextAnimations.PopText(announcementText, "Can only shield your pieces!", 0.15f, 0.1f, 1.15f);
                return;
            }
        
            _playerGrid.Immune[position] = true;
            if (isOnline) SendImmuneStatus((byte)position);
            _gameManager.TextAnimations.PopText(announcementText, "Piece shielded!", 0.15f, 0.1f, 1.15f);
    
            shieldSelectionMode = false;
            powerUps.OnShieldApplied();
        }
        

        
        
        
        // public void OnGridButtonClick(Button clickedButton)
        // {
        //     int buttonIndex = Array.IndexOf(gridButtons, clickedButton);
        //
        //     // First check if position is already marked
        //     if (_playerGrid.Marks[buttonIndex])
        //     {
        //         // Position already taken, show pop-up message
        //         _gameManager.TextAnimations.PopText(announcementText, "Spot taken!", 0.15f, 0.1f, 1.15f);
        //         return;
        //     }
        //
        //     // Then check if we have a color selected and it's our turn
        //     if (!string.IsNullOrEmpty(CurrentPaintColor) && _isMyTurn)
        //     {
        //         _playerGrid.Marks[buttonIndex] = true;
        //         _playerGrid.Color[buttonIndex] = CurrentPaintColor;
        //
        //         ChangeButtonColor(buttonIndex, CurrentPaintColor);
        //         CurrentPaintColor = "";
        //         ResetCursor();
        //         powerUps.ResetBluePowerUp();
        //
        //         StartCoroutine(ProcessTurn(buttonIndex, true));
        //     }
        // }
        //
        // public void OnGrindButtonClickForShield(Button clickedButton)
        // {
        //     
        // }


        private IEnumerator ProcessTurn(int buttonIndex, bool isLocalPlayerMove)
        {
            // Handle conflict resolution
            string originalColor = _playerGrid.Color[buttonIndex];

            if (isLocalPlayerMove)
            {
                if (_opponentGrid.Marks[buttonIndex])
                {
                    yield return StartCoroutine(ResolveConflict(buttonIndex));
                }
            }
            else 
            {
                if (_playerGrid.Marks[buttonIndex])
                {
                    yield return StartCoroutine(ResolveConflict(buttonIndex));
                }
            }

            // Check for patterns todo this can be moved  into islocal player move im pretty sure
            yield return StartCoroutine(CheckPatternsCoroutine());

            if (powerUps.redPowerActivated && isLocalPlayerMove)
            {
                Debug.Log("Extra turn applied!!");
                powerUps.ResetRedPowerup();
                powerUps.redPowerActivated = false;
                yield break; 
            }
            
            if (isLocalPlayerMove)
            {
                SwapTurns();
        
                if (playAgainstAI)
                {
                    StartCoroutine(ArtificialOpponent());
                }
                else if (isOnline)
                {
                    SendMove(buttonIndex, originalColor);
                }
            }
            else
            {
                SwapTurns();
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
                shouldClearPlayer1 = false; // Don't clear if immune
                _playerGrid.Immune[position] = false;
                // Optional: Visual feedback that immunity prevented clearing
                _gameManager.TextAnimations.PopText(announcementText, "Your piece was protected!", 0.15f, 0.1f, 1.15f);
            }
            
            if (shouldClearPlayer2 && _opponentGrid.Immune[position])
            {
                shouldClearPlayer2 = false; // Don't clear if immune
                _opponentGrid.Immune[position] = false;
                // Optional: Visual feedback
                _gameManager.TextAnimations.PopText(announcementText, "Opponent's piece was protected!", 0.15f, 0.1f, 1.15f);
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
            _totalTurns++;
            if (_totalTurns >= _maxTurns)
            {
                EndGame();
                return;
            }

            _isMyTurn = !_isMyTurn;
            _turnTimer = timer;
            UpdateTurnIndicators();
            HandleTimerBasedOnGameState();

            if (_isMyTurn)
            {
                _currentRound = (_totalTurns + (_isMyTurn ? 1 : 0)) / 2;
                currentRoundText.text = $"Current Round {_currentRound}";
                //powerUps.ResetGreenPowerUp(_playerGrid);
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
            _turnTimer = timer;

            while (_turnTimer > 0)
            {
                _turnTimer -= Time.deltaTime;
                _gameManager.TextAnimations.UpdateTimerWithEffects(timerFill, _turnTimer, timer);
                yield return null;
            }
    
            _gameManager.TextAnimations.ResetTimerVisuals(timerFill);
            _turnTimer = timer;
    
            // Handle shield selection first if active
            if (shieldSelectionMode)
            {
                _ai.ForceShieldSelection(_playerGrid, gridButtons);
            }
            // Handle regular move only if it's the player's turn and not in shield mode
            else if (!string.IsNullOrEmpty(CurrentPaintColor) && _isMyTurn)
            {
                _ai.ForcePlayerMove(_playerGrid, gridButtons);
            }
        }

        
        #endregion
        
        #region Network
        private void SendReady()
        {
            _playerIsReady = true;
            
            if (!isOnline || playAgainstAI)
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
            _gameManager.TextAnimations.Typewriter(countdownText, "Ready and waiting...");
        }
        private void MatchFound(string roomId)
        {
            LerpCanvasGroup(mainPage, 0);
            LerpCanvasGroup(readyPage, 1);
            _gameManager.TextAnimations.Typewriter(countdownText, "Player Found! Ready?");
            Debug.Log("Found match and we're in the room called" + roomId);
        }
        private void ReceiveReadyState(bool isMyTurn)
        {
            DOTween.KillAll();
            StartCoroutine(GameStartCountdown(isMyTurn));
        }
        private IEnumerator GameStartCountdown(bool isMyTurn)
        {
            // Set up the countdown strings
            string[] countdownStrings = { "3", "2", "1", "GO!" };
    
            // Calculate total animation time
            float totalAnimationTime = countdownStrings.Length * (0.3f + 0.4f + 0.3f);
    
            // Start the countdown animation without waiting for callback
            _gameManager.TextAnimations.Countdown(
                countdownText, 
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
            Debug.Log(createData.Index + " " + createData.SquareColor);
            
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
        }
        public void ReceiveMove(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<VinceGameData>(messagePackData);
            if (receivedData.SenderId != WebSocketNetworkHandler.Instance.ClientId)
            {
                int index = receivedData.Index;
                _opponentGrid.Marks[index] = true;
                _opponentGrid.Color[index] = receivedData.SquareColor;
                otherBoard[index].color = GetColorFromHex(receivedData.SquareColor);
            
                StartCoroutine(ProcessTurn(index, false));
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
