using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Core.Hidden.HiddenGameGlobals;
using Core.Singletons;

namespace Core.Hidden
{
    public class GameGUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Canvas Groups")] [SerializeField]
        private CanvasGroup introCanvasOfflineGroup;

        [SerializeField] private CanvasGroup setupCanvasGroup;
        [SerializeField] private CanvasGroup battleCanvasGroup;
        [SerializeField] private CanvasGroup endGameCanvasGroup;
        [SerializeField] private CanvasGroup playerBoardCanvasGroup;
        [SerializeField] private CanvasGroup opponentBoardCanvasGroup;
        [SerializeField] private CanvasGroup mainPage;
        [SerializeField] private CanvasGroup readyPage;

        [Header("Game Boards")] [SerializeField]
        private GameObject playerBoard;

        [SerializeField] private GameObject opponentBoard;
        [SerializeField] private Image[] gridButtonImages = new Image[9];
        [SerializeField] private Image[] otherBoard = new Image[9];

        [Header("UI Text Elements")]
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text playerTurnText;
        [SerializeField] private TMP_Text opponentTurnText;
        [SerializeField] private TMP_Text currentRoundText;
        [SerializeField] private TMP_Text announcementText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text opponentNameText;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private TMP_Text playerScoreText;
        [SerializeField] private TMP_Text opponentScoreText;
        [SerializeField] private TMP_Text playAgainText;
        private Dictionary<TMPTextType, TMP_Text> _textElements;

        [Header("Game End UI")] [SerializeField]
        private Image[] myGridImages = new Image[9];

        [SerializeField] private Image[] opponentGridImages = new Image[9];

        [Header("Timer")] [SerializeField] private Image timerFill;
        public Image TimerFill => timerFill;

        [Header("Cursor")] [SerializeField] private Texture2D[] cursorTextures = new Texture2D[2];

        [Header("Animation Settings")] [SerializeField]
        private float fadeDuration = 0.5f;

        [SerializeField] private float slideDuration = 0.5f;

        #endregion

        #region Properties and References

        private GameManager _gameManager;
        private Vector2 _cursorHotspot;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _gameManager = GameManager.Instance;
            InitializeCursor();

            _textElements = new Dictionary<TMPTextType, TMP_Text>
            {
                { TMPTextType.PlayerName, playerNameText },
                { TMPTextType.PlayerTurn, playerTurnText },
                { TMPTextType.OpponentTurn, opponentTurnText },
                { TMPTextType.CurrentRound, currentRoundText },
                { TMPTextType.Announcement, announcementText },
                { TMPTextType.GameOver, gameOverText },
                { TMPTextType.PlayerScore, playerScoreText },
                { TMPTextType.OpponentScore, opponentScoreText },
                { TMPTextType.PlayAgain, playAgainText },
                { TMPTextType.Countdown, countdownText }
            };
        }

        private void Start()
        {
            playerNameText.text = PlayerPrefs.GetString("Username", "");
            InitializeCursor();
            InitializeGUIPositions();

            //Debug.Log(_gameManager.blindModeActive);
            if (_gameManager.blindModeActive == false) SetSideBySideView(false, true);
        }

        #endregion

        #region Public Methods

        public TMP_Text GetText(TMPTextType tmpTextType)
        {
            return _textElements.GetValueOrDefault(tmpTextType);
        }

        public void ChangeButtonColor(int buttonIndex, string hexColor)
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                gridButtonImages[buttonIndex].color = color;
            }
        }

        private void ChangeOpponentButtonColor(int buttonIndex, string hexColor)
        {
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                otherBoard[buttonIndex].color = color;
            }
        }

        public void UpdateTurnIndicators(bool isMyTurn)
        {
            playerTurnText.text = isMyTurn ? "Your Turn" : "Waiting...";
            _gameManager.TextAnimations.PopThenBreathe(playerTurnText.transform);

            opponentTurnText.text = isMyTurn ? "Waiting..." : "Their Turn";
            _gameManager.TextAnimations.PopThenBreathe(opponentTurnText.transform);
        }

        public void UpdateRoundText(int currentRound)
        {
            currentRoundText.text = $"Current Round {currentRound}";
        }

        public void ShowAnnouncement(string message, float scaleAmount = 1.15f)
        {
            _gameManager.TextAnimations.PopText(announcementText, message, 0.15f, 0.1f, scaleAmount);
        }

        public void UpdateCursor(int colorIndex)
        {
            if (colorIndex >= 0 && colorIndex < cursorTextures.Length)
            {
                Cursor.SetCursor(cursorTextures[colorIndex], _cursorHotspot, CursorMode.Auto);
            }
        }

        public void UpdateTimer(float currentTime, float totalTime)
        {
            _gameManager.TextAnimations.UpdateTimerWithEffects(timerFill, currentTime, totalTime);
        }

        public void ResetTimerVisuals()
        {
            _gameManager.TextAnimations.ResetTimerVisuals(timerFill);
        }

        public GameState StateChange(GameState state, bool shouldLerp = true)
        {
            SetAllCanvasesNonInteractable();

            switch(state)
            {
                case GameState.SetupAI:
                    ToggleOfflineCanvas(true);
                    break;

                case GameState.Setup:
                    TransitionCanvas(setupCanvasGroup, true, shouldLerp);
                    TransitionCanvas(battleCanvasGroup, false, false);
                    TransitionCanvas(endGameCanvasGroup, false, false);
                    break;

                case GameState.Battle:
                    TransitionCanvas(battleCanvasGroup, true, shouldLerp);
                    TransitionCanvas(introCanvasOfflineGroup, false, false);
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
                    _gameManager.TextAnimations.Typewriter(playAgainText, " Again?");
                    break;
            }
            
            return state;

        }

        public void SetSideBySideView(bool animate = false, bool permanent = false)
        {
            // Safety checks
            if (playerBoard == null || opponentBoard == null || opponentBoardCanvasGroup == null)
            {
                Debug.LogError("Player Board, Opponent Board, or Opponent Board Canvas Group is not assigned!");
                return;
            }

            // Always ensure opponent board is active if permanent
            if (permanent)
            {
                opponentBoard.SetActive(true);
            }

            if (animate)
            {
                // Animate player board to left
                playerBoard.transform.DOLocalMove(LeftPosition, slideDuration);
        
                // Slide in and fade in opponent board
                opponentBoard.transform.DOLocalMove(RightPosition, slideDuration);
                opponentBoardCanvasGroup.DOFade(1f, fadeDuration);
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
                playerBoard.transform.DOLocalMove(CenterPosition, slideDuration);
            
                // Slide out and fade out opponent board
                opponentBoard.transform.DOLocalMove(OffscreenPosition, slideDuration);
                opponentBoardCanvasGroup.DOFade(0f, fadeDuration);
            }
            else
            {
                // Instant positioning
                playerBoard.transform.localPosition = CenterPosition;
                opponentBoard.transform.localPosition = OffscreenPosition;
                opponentBoardCanvasGroup.alpha = 0f;
            }
        }
        
        public void PopulateEndGameGrids(GridData playerGrid, GridData opponentGrid)
        {
            for (int i = 0; i < 9; i++)
            {
                // Player grid
                ColorUtility.TryParseHtmlString(playerGrid.Color[i], out Color playerColor);
                myGridImages[i].color = playerColor;
        
                // Opponent grid
                ColorUtility.TryParseHtmlString(opponentGrid.Color[i], out Color opponentColor);
                opponentGridImages[i].color = opponentColor;
            }
        }
        
        public void DisplayGameResults(int playerScore, int opponentScore)
        {
            string resultMessage = playerScore > opponentScore 
                ? "YOU WIN!" 
                : playerScore < opponentScore 
                    ? "YOU LOSE!" 
                    : "IT'S A TIE!";

            gameOverText.text = $"GAME OVER\n{resultMessage}";
            playerScoreText.text = $"Your Score: {playerScore}";
            opponentScoreText.text = $"Opponent Score: {opponentScore}";

            _gameManager.TextAnimations.CreateBreathingAnimation(gameOverText.transform);
        }
        
        public void ShowReadyScreen()
        {
            LerpCanvasGroup(mainPage, 0);
            LerpCanvasGroup(readyPage, 1);
        }
        
        public void UpdateReadyText(string message)
        {
            _gameManager.TextAnimations.Typewriter(countdownText, message);
        }
        
        public void ShowShieldVisual(int position, bool isOpponent = false)
        {
            // Choose the correct image array based on the isOpponent parameter
            Image targetImage = isOpponent ? otherBoard[position] : gridButtonImages[position];
    
            if (targetImage.transform.childCount > 0)
            {
                Image shieldImage = targetImage.transform.GetChild(0).GetComponent<Image>();
                if (shieldImage != null)
                {
                    shieldImage.gameObject.SetActive(true);
                    AnimateShieldAppearance(shieldImage);
                }
            }
        }

        public void RemoveShieldVisual(int position, bool isOpponent = false)
        {
            // Choose the correct image array based on the isOpponent parameter
            Image targetImage = isOpponent ? otherBoard[position] : gridButtonImages[position];
    
            if (targetImage.transform.childCount > 0)
            {
                Image shieldImage = targetImage.transform.GetChild(0).GetComponent<Image>();
                if (shieldImage != null && shieldImage.gameObject.activeSelf)
                {
                    AnimateShieldDisappearance(shieldImage);
                }
            }
        }
        public IEnumerator GameStartCountdown(string[] countdownStrings)
        {
            float totalAnimationTime = countdownStrings.Length * (0.3f + 0.4f + 0.3f);

            _gameManager.TextAnimations.Countdown(
                countdownText, 
                countdownStrings,
                fadeInDuration: 0.3f, 
                displayDuration: 0.4f, 
                fadeOutDuration: 0.3f,
                popScale: 1.2f
            );

            yield return new WaitForSecondsRealtime(totalAnimationTime);
        }
        
        public void ResetGUI()
        {
            ResetGridColors();
            InitializeGUIPositions();
            ResetCursor();
            countdownText.alpha = 1f; // why would the alpha not be 1f?...
        }
        
        private void ResetGridColors()
        {
            for (int i = 0; i < gridButtonImages.Length; i++)
            {
                ChangeButtonColor(i, "#FFFFFF");
                ChangeOpponentButtonColor(i, "#FFFFFF");
            }
        }
        
        public void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        public void ShowMatchFoundScreen(string message)
        {
            LerpCanvasGroup(mainPage, 0);
            LerpCanvasGroup(readyPage, 1);
            _gameManager.TextAnimations.Typewriter(GetText(TMPTextType.Countdown), message);
        }
        #endregion

        #region Private Methods
        
        private void InitializeGUIPositions()
        {
            playerBoard.transform.localPosition = CenterPosition;
            opponentBoard.transform.localPosition = OffscreenPosition;
            opponentBoardCanvasGroup.alpha = 0f;
        }
        private void InitializeCursor()
        {
            _cursorHotspot = new Vector2(16, 16);
            ResetCursor();
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

        public void ToggleOfflineCanvas(bool toggle)
        {
            float targetAlpha = toggle ? 1f : 0f;
            introCanvasOfflineGroup.DOFade(targetAlpha, fadeDuration)
                .OnComplete(() => {
                    introCanvasOfflineGroup.interactable = toggle;
                    introCanvasOfflineGroup.blocksRaycasts = toggle;
                    Canvas.ForceUpdateCanvases(); // Force update AFTER animation completes
                });
        }
        
        private void TransitionCanvas(CanvasGroup group, bool active, bool shouldLerp = true)
        {
            float targetAlpha = active ? 1f : 0f;

            if (shouldLerp)
            {
                group.DOFade(targetAlpha, fadeDuration)
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
            group.interactable = targetAlpha > 0;
            group.blocksRaycasts = targetAlpha > 0;
            group.DOFade(targetAlpha, fadeDuration);
        }

        
        private void AnimateShieldAppearance(Image shieldImage)
        {
            if (shieldImage == null) return;
        
            // Call the animation from the animation manager
            _gameManager.TextAnimations.ShieldBouncyAppearance(shieldImage.transform);
        }
        
        private void AnimateShieldDisappearance(Image shieldImage)
        {
            if (shieldImage == null)
                return;
        
            _gameManager.TextAnimations.ShieldBouncyDisappearance(shieldImage.transform);
        }
        
        #endregion
    }
}
