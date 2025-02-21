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
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace Core.VinceGame
{
    public class GamePrototype : MonoBehaviour
    {
        //Data Structures
        private struct GridData
        {
            public bool[] Marks;
            public string[] Color;
            public bool[] Immune;
        }

        private enum GameState
        {
            Setup,
            Battle,
            EndGame
        }
        
        // Parameters
        [SerializeField] private bool playAgainstAI = true;
        [SerializeField] private bool testBlind = true;
        [SerializeField] private bool isOnline = false;
        [SerializeField] private byte numberOfRounds = 6;
        [SerializeField] private float timer = 10f;
        
        // Constants
        private const string ColorGreenSelect = "#A6E22E";
        private const string ColorBlueSelect = "#4591DB";
        private const string ColorRedSelect = "#CC3941";
        
        private readonly Vector3 _centerPosition = new Vector3(0, 0, 0);
        private readonly Vector3 _leftPosition = new Vector3(-450, 0, 0);
        private readonly Vector3 _rightPosition = new Vector3(450, 0, 0);
        private readonly Vector3 _offscreenPosition = new Vector3(1500, 0, 0);
        private const float SlideDuration = 0.5f;
        private const float FadeDuration = 0.3f;
        
        // Variables
        private WebSocketNetworkHandler _wsHandler;
        private GameState _gameState;
        private bool _playerIsReady;
        private float _turnTimer;
        
        //Grid Data
        private GridData _playerGrid;
        private GridData _opponentGrid;
        private string _currentPaintColor = "";
        private bool _isMyTurn = true;
        private byte _currentRound ;

        //Powerups 
        private bool _redPowerupUsed;
        private bool _greenPowerupUsed;
        private bool _bluePowerupUsed;

        private bool _redPowerupObtained;
        private bool _greenPowerupObtained;
        private bool _bluePowerupObtained;
        
        // UI References
        [SerializeField] private CanvasGroup setupCanvasGroup;
        [SerializeField] private CanvasGroup battleCanvasGroup;
        [SerializeField] private CanvasGroup endGameCanvasGroup;
            
        // Setup Canvas
        [SerializeField] private Button readyButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_InputField[] userInputParameter;
        [SerializeField] private Toggle[] someWinConditionToggle;
        
        // Main Canvas
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text timerText;
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
        [SerializeField] private TMP_Text[] specialUsedText;
        [SerializeField] private CanvasGroup[] specialsGroups;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private Button extraButton;
        
        // End game Canvas
        [SerializeField] private Button replayButton;
        [SerializeField] private Button quitButtonTwo;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private TMP_Text playerScoreText;
        [SerializeField] private TMP_Text opponentScoreText;

        // Cursor Data
        private Texture2D _cursorTexture;
        private Vector2 _cursorHotspot;
        [SerializeField] private Texture2D[] cursorTextures = new Texture2D[2];

        private void Start()
        {
            playAgainstAI = GameManager.Instance.playingAgainstAI;
            testBlind = GameManager.Instance.blindModeActive;
            isOnline = GameManager.Instance.isOnline;
            
            _wsHandler = WebSocketNetworkHandler.Instance;
            WebSocketNetworkHandler.Instance.VinceGame = this;

            StateChanger(GameState.Setup, false);
            
            _wsHandler.OnServerResponse += ReceiveWhoStartsFirst;
            _wsHandler.OnGameReadyResponse += ReceiveReadyState;
            
            InitializeGrids();
            InitButtons();
            InitializeCursor();
            InitializePowerupUI();
            InitializeGUIPositions();
            
            if (playAgainstAI)
            {
                opponentNameText.text = "AI player";
            }
            
            for (int i = 0; i < userInputParameter.Length; i++)
            {
                int index = i;
                userInputParameter[i].contentType = TMP_InputField.ContentType.IntegerNumber;
                userInputParameter[i].onEndEdit.AddListener(value => OnParameterChanged(value, index));
                Debug.Log($"Input field {i} name: {userInputParameter[i].name}");
            }
            
            // userInputParameter[0].onEndEdit.AddListener(value => OnParameterChanged(value, 0));
            // userInputParameter[1].onEndEdit.AddListener(value => OnParameterChanged(value, 1));
            //
            _turnTimer = timer;
        }
        
        private void Update()
        {
            if (_gameState == GameState.Battle && _isMyTurn)
            {
                UpdateTimer();
            }
        }
        
        private void UpdateTimer()
        {
            _turnTimer -= Time.deltaTime;

            timerText.text = _turnTimer.ToString("F2");
    
            timerFill.fillAmount = _turnTimer / timer;

            if (_turnTimer <= 0)
            {
                _turnTimer = timer;
                ForcePlayerMove();
            }
        }

        private void ForcePlayerMove()
        {
            List<int> availablePositions = new List<int>();
            for (int i = 0; i < _playerGrid.Marks.Length; i++)
            {
                if (!_playerGrid.Marks[i])
                {
                    availablePositions.Add(i);
                }
            }

            if (availablePositions.Count > 0)
            {
                int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];

                // Select a random color
                string[] colors = new string[] { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                string randomColor = colors[Random.Range(0, colors.Length)];
                SetPaintColor(randomColor);
                UpdateCursor(Array.IndexOf(colors, randomColor));

                // Simulate a click on the random grid button
                OnGridButtonClick(gridButtons[randomPosition]);
            }
        }
        
        // Init functions
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
            quitButton.onClick.AddListener(QuitGame);
            replayButton.onClick.AddListener(ResetGame);
            quitButtonTwo.onClick.AddListener(QuitGame);

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

            shieldButton.onClick.AddListener(ShieldPieces);
            revealButton.onClick.AddListener(RevealBoard);
            extraButton.onClick.AddListener(EnableExtraTurn);
        }
        private void InitializeGUIPositions()
        {
            if (!playAgainstAI || testBlind)
            {
                // Center the board
                playerBoard.transform.localPosition = _centerPosition;
                opponentBoard.transform.localPosition = _offscreenPosition;
                opponentBoardCanvasGroup.alpha = 0f;
            }
            else
            {
                SetSideBySideView(false);
            }
        }
        
        private void OnParameterChanged(string value, int index)
        {
            if (string.IsNullOrEmpty(value)) return;
    
            if (float.TryParse(value, out float inputValue))
            {
                switch (index)
                {
                    case 0:
                        numberOfRounds = (byte)inputValue;
                        break;
                    case 1:
                        timer = inputValue;
                        _turnTimer = timer;
                        timerText.text = timer.ToString("F1");
                        break;

                }
            }
        }

        
        // Setup
        private void StartGame()
        {
            _isMyTurn = true;
            StateChanger(GameState.Battle);
        }

        private void StateChanger(GameState state, bool shouldLerp = true)
        {
            SetAllCanvasesNonInteractable();

            switch(state)
            {
                case GameState.Setup:
                    TransitionCanvas(setupCanvasGroup, true, shouldLerp);
                    TransitionCanvas(battleCanvasGroup, false, shouldLerp);
                    TransitionCanvas(endGameCanvasGroup, false, shouldLerp);
                    break;

                case GameState.Battle:
                    TransitionCanvas(setupCanvasGroup, false, shouldLerp);
                    TransitionCanvas(battleCanvasGroup, true, shouldLerp);
                    TransitionCanvas(endGameCanvasGroup, false, shouldLerp);
                    break;

                case GameState.EndGame:
                    TransitionCanvas(setupCanvasGroup, false, shouldLerp);
                    TransitionCanvas(battleCanvasGroup, false, shouldLerp);
                    TransitionCanvas(endGameCanvasGroup, true, shouldLerp);
                    LerpCanvasGroup(playerBoardCanvasGroup, 0f);
                    LerpCanvasGroup(opponentBoardCanvasGroup, 0f);
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

        private void LerpCanvasGroup(CanvasGroup group, float targetAlpha)
        {
            // Immediately set interactability based on target
            group.interactable = targetAlpha > 0;
            group.blocksRaycasts = targetAlpha > 0;

            // Using DOTween (since you have it)
            group.DOFade(targetAlpha, FadeDuration);
        }
        // Visuals
        private void SetSideBySideView(bool animate)
        {
            if (animate)
            {
                // Animate player board to left
                playerBoard.transform.DOLocalMove(_leftPosition, SlideDuration);
            
                // Slide in and fade in opponent board
                opponentBoard.transform.DOLocalMove(_rightPosition, SlideDuration);
                opponentBoardCanvasGroup.DOFade(1f, FadeDuration);
            }
            else
            {
                // Instant positioning
                playerBoard.transform.localPosition = _leftPosition;
                opponentBoard.transform.localPosition = _rightPosition;
                opponentBoardCanvasGroup.alpha = 1f;
            }
        }
        private void SetCenteredView(bool animate)
        {
            if (animate)
            {
                // Animate player board to center
                playerBoard.transform.DOLocalMove(_centerPosition, SlideDuration);
            
                // Slide out and fade out opponent board
                opponentBoard.transform.DOLocalMove(_offscreenPosition, SlideDuration);
                opponentBoardCanvasGroup.DOFade(0f, FadeDuration);
            }
            else
            {
                // Instant positioning
                playerBoard.transform.localPosition = _centerPosition;
                opponentBoard.transform.localPosition = _offscreenPosition;
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
            SetPaintColor(selectedColor);
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
        private void SetPaintColor(string hexColor)
        {
            _currentPaintColor = hexColor;
        }
        
        
        // Core mechanics
        private void OnGridButtonClick(Button clickedButton)
        {
            if (!string.IsNullOrEmpty(_currentPaintColor) && _isMyTurn)
            {
                int buttonIndex = System.Array.IndexOf(gridButtons, clickedButton);

                _playerGrid.Marks[buttonIndex] = true;
                _playerGrid.Color[buttonIndex] = _currentPaintColor;
                //Debug.Log($"Player placed color: {_currentPaintColor} at {buttonIndex}");

                ChangeButtonColor(buttonIndex, _currentPaintColor);

                _currentPaintColor = "";
                ResetCursor();

                StartCoroutine(EndOfTurnSequence(buttonIndex));
            }
        }

        private IEnumerator EndOfTurnSequence(int buttonIndex)
        {
            if (_opponentGrid.Marks[buttonIndex])
            {
                yield return StartCoroutine(ResolveConflict(buttonIndex));
            }

            yield return StartCoroutine(CheckPatternsCoroutine());

            yield return StartCoroutine(EndTurn(playAgainstAI, buttonIndex));
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

            Debug.Log($"Found match with color: {grid.Color[a]}");
    
            // Only process powerups if it's the player's turn
            if (!_isMyTurn) return false;

            return ProcessPowerup(grid.Color[a]);
        }
        private bool ProcessPowerup(string color)
        {
            switch (color)
            {
                case ColorGreenSelect when !_redPowerupObtained:
                    _redPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(0));
                    return true;
            
                case ColorBlueSelect when !_greenPowerupObtained:
                    _greenPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(1));
                    return true;
            
                case ColorRedSelect when !_bluePowerupObtained:
                    _bluePowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(2));
                    return true;
            }
            return false;
        }
        private IEnumerator EndTurn(bool againstAI, int index)
        {
            // originaly implemented a slow-down effect to signify end of turn, can redo later
            SwapTurns();

            if (againstAI)
            {
                StartCoroutine(DelayedOpponentMove());
            }
            else if(isOnline)
            {
                SendMove(index);
            }
            yield return null;
        }
        private void SwapTurns()
        {
            if (_redPowerupUsed && _isMyTurn)
            {
                _redPowerupUsed = false;
                ShowFadingText("Extra turn activated!");
                return;
            }

            _isMyTurn = !_isMyTurn;
            _turnTimer = timer;
            UpdateTurnIndicators();

            if (_isMyTurn)
            {
                _currentRound++;
                
                Debug.Log(_currentRound);
                if (CheckGameOver())
                {
                    EndGame();
                    return;
                }
        
                currentRoundText.text = $"Current Round {_currentRound}";
                ResetPowerups();
            }
        }

        private bool CheckGameOver()
        {
            return _currentRound > numberOfRounds;
        }

        private void EndGame()
        {
            // Count occupied squares for each player
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

            gameOverText.text = "GAME OVER";
            currentRoundText.text = resultMessage;
            playerScoreText.text = $"Your Score: {playerScore}";
            opponentScoreText.text = $"Opponent Score: {opponentScore}";

            // Animate game over text
            CreateBreathingAnimation(gameOverText.transform);

            Debug.Log($"Game ended - {resultMessage} (Player: {playerScore} vs Opponent: {opponentScore})");
        }

        private void ResetPowerups()
        {
            // Reset Shield (Green)
            if (_greenPowerupUsed)
            {
                for (int i = 0; i < _playerGrid.Marks.Length; i++)
                {
                    _playerGrid.Immune[i] = false;
                }
                _greenPowerupUsed = false;
            }

            // Reset Reveal (Blue)
            if (_bluePowerupUsed)
            {
                SetCenteredView(true);
                _bluePowerupUsed = false;
            }

            // Extra Turn (Red) is handled at the start of SwapTurns
        }

        private IEnumerator DelayedOpponentMove()
        {
            yield return new WaitForSeconds(0.5f);
            SimulateOpponentMove();
        }
        private void SimulateOpponentMove()
        {
            if (!_isMyTurn)
            {
                List<int> availablePositions = new List<int>();
                for (int i = 0; i < _opponentGrid.Marks.Length; i++)
                {
                    if (!_opponentGrid.Marks[i])
                    {
                        availablePositions.Add(i);
                    }
                }

                if (availablePositions.Count > 0)
                {
                    int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];
                    string[] colors = new string[] { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                    string randomColor = colors[Random.Range(0, colors.Length)];

                    _opponentGrid.Marks[randomPosition] = true;
                    _opponentGrid.Color[randomPosition] = randomColor;
                    otherBoard[randomPosition].color = GetColorFromHex(randomColor);

                    StartCoroutine(EndOfTurnSequence(randomPosition));
                }
            }
        }
        private Color GetColorFromHex(string hexColor)
        {
            ColorUtility.TryParseHtmlString(hexColor, out Color color);
            return color;
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
        private void UpdateTurnIndicators()
        {
            if (playerTurnText != null)
                playerTurnText.text = _isMyTurn ? "Your Turn" : "Waiting...";
            if (opponentTurnText != null)
                opponentTurnText.text = _isMyTurn ? "Waiting..." : "AI Turn";
        }
        private void InitializePowerupUI()
        {
            foreach (CanvasGroup group in specialsGroups)
            {
                group.alpha = 0.1f;
                group.interactable = false;
            }
        }
        private IEnumerator ActivatePowerupUI(int index)
        {
            float duration = 0.5f;
            float startAlpha = specialsGroups[index].alpha;
            float targetAlpha = 1.0f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                specialsGroups[index].alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }

            specialsGroups[index].alpha = targetAlpha;
            specialsGroups[index].interactable = true;
        }
        private IEnumerator DeactivatePowerupUI(int index)
        {
            float duration = 0.5f;
            float startAlpha = specialsGroups[index].alpha;
            float targetAlpha = 0f;
            float elapsed = 0;

            specialsGroups[index].interactable = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / duration;
                specialsGroups[index].alpha = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                yield return null;
            }

            specialsGroups[index].alpha = targetAlpha;
        }
        private void ShieldPieces()
        {
            if (_greenPowerupUsed) return;

            ShowFadingText("Shield used");
            StartCoroutine(DeactivatePowerupUI(0));
            _greenPowerupUsed = true;
            List<int> immuneSquares = new List<int>(); 
    
            for (int i = 0; i < _playerGrid.Marks.Length; i++)
            {
                if (_playerGrid.Marks[i])
                {
                    Debug.Log("Shielding these indexes: " + i);
                    _playerGrid.Immune[i] = true;
                    immuneSquares.Add(i);
                }
            }
    
            if (isOnline)
            {
                SendImmuneStatus(immuneSquares.Select(x => (byte)x).ToArray());
            }
        }
        private void RevealBoard()
        {
            if (_bluePowerupUsed) return;
            ShowFadingText("Reveal board used");
            StartCoroutine(DeactivatePowerupUI(1));
        
            SetSideBySideView(true); // Animate the transition
        
            _bluePowerupUsed = true;
        }
        private void HideOpponentBoard()
        {
            SetCenteredView(true); // Animate back to centered view
        }
        private void EnableExtraTurn()
        {
            if (_redPowerupUsed) return;
    
            ShowFadingText("Extra turn used");
            StartCoroutine(DeactivatePowerupUI(2));
            _redPowerupUsed = true;
        }
        private void ShowFadingText(string message)
        {
            foreach(var txt in specialUsedText)
            {
                txt.alpha = 0f;
                txt.text = message;
        
                Sequence fadeSequence = DOTween.Sequence();
        
                fadeSequence.Append(txt.DOFade(1f, 0.5f))
                    .AppendInterval(1f)
                    .Append(txt.DOFade(0f, 0.5f));
            }
        }
        
        private void CreateBreathingAnimation(Transform target, float scalePulse = 1.1f, float duration = 1f)
        {
            DOTween.Kill(target);
            target.localScale = Vector3.one;
            Sequence breathingSequence = DOTween.Sequence();
    
            breathingSequence.Append(target.DOScale(scalePulse, duration)
                    .SetEase(Ease.InOutSine))
                .Append(target.DOScale(1f, duration)
                    .SetEase(Ease.InOutSine));

            breathingSequence.SetLoops(-1);
        }
        
        //Network
        private void SendReady()
        {
            _playerIsReady = true;

            if (!isOnline || playAgainstAI)
            {
                StartGame();
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
        }

        private void ReceiveWhoStartsFirst(bool isStarting)
        {
            if (isStarting)
            {
                _isMyTurn = true;
            }
            else
            {
                _isMyTurn = false;
            }
        }

        private void ReceiveReadyState()
        {
            StartGame();
        }
        
        private void SendMove(int index)
        {
            var createData = new VinceGameData
            {
                Type = PacketType.VinceGamePacket,
                Index = (byte)index,
                SquareColor = _playerGrid.Color[index]
            };
            
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
        }
        
        public void ReceiveMove(byte[] messagePackData)
        {
            SwapTurns();
            var vinceGameData = MessagePackSerializer.Deserialize<VinceGameData>(messagePackData);
            if (vinceGameData.SenderId != _wsHandler.ClientId)
            {
                int index = vinceGameData.Index;
                _opponentGrid.Marks[index] = true;
                _opponentGrid.Color[index] = vinceGameData.SquareColor;
            }
        }
        
        private void SendImmuneStatus(byte[] indexes)
        {
            var immunePacket = new VinceGameImmune 
            {
                Type = PacketType.VinceGameImmune,
                Index = indexes
            };
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(immunePacket);

        }
        
        private void ResetGame()
        {
            DOTween.KillAll();

            // Reset grid data
            InitializeGrids();
            InitializeGUIPositions();
            
            // Reset powerup states
            _redPowerupUsed = false;
            _greenPowerupUsed = false;
            _bluePowerupUsed = false;
            _redPowerupObtained = false;
            _greenPowerupObtained = false;
            _bluePowerupObtained = false;
            InitializePowerupUI();

            // Reset game state
            _isMyTurn = true; // todo not really accurate
            _currentRound = 0;
            currentRoundText.text = $"Current Round {_currentRound}";
            UpdateTurnIndicators();

            // Reset visual elements
            ResetCursor();
            _currentPaintColor = "";
            
            playerBoardCanvasGroup.alpha = 1f;
            playerBoardCanvasGroup.interactable = true;
            playerBoardCanvasGroup.blocksRaycasts = true;

            opponentBoardCanvasGroup.alpha = 1f;
            opponentBoardCanvasGroup.interactable = true;
            opponentBoardCanvasGroup.blocksRaycasts = true;

            // Reset all buttons
            for (int i = 0; i < gridButtons.Length; i++)
            {
                ChangeButtonColor(i, "#FFFFFF");
                otherBoard[i].color = Color.white;
                gridButtons[i].interactable = true;
            }

            // Reset color choosing buttons
            foreach (var button in colorChoosingButtons)
            {
                button.interactable = true;
            }

            // Explicitly set setup canvas as interactive
            setupCanvasGroup.alpha = 1f;
            setupCanvasGroup.interactable = true;
            setupCanvasGroup.blocksRaycasts = true;

            // Reset other canvases
            battleCanvasGroup.alpha = 0f;
            battleCanvasGroup.interactable = false;
            battleCanvasGroup.blocksRaycasts = false;

            endGameCanvasGroup.alpha = 0f;
            endGameCanvasGroup.interactable = false;
            endGameCanvasGroup.blocksRaycasts = false;

            _gameState = GameState.Setup;
        }



        private void QuitGame()
        {
            CleanUp();
            //todo: any other quitting logic
        }

        private void CleanUp()
        {
            // Unsubscribe from WebSocket events
            _wsHandler.OnServerResponse -= ReceiveWhoStartsFirst;
            _wsHandler.OnGameReadyResponse -= ReceiveReadyState;

            // Reset the game state
            ResetGame();

            // Additional cleanup (if needed)
            ResetCursor();
        }
    }
}
