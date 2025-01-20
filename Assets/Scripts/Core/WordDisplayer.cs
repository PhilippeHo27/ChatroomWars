using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace Core
{
    public class WordDisplayer : MonoBehaviour
    {
        private const string COLOR_YELLOW = "#C8C676";
        private const string COLOR_GREY = "#37393C";
        private const string COLOR_RED = "#D7444C";
        private const string COLOR_GREEN = "#4CAE54";

        [SerializeField] private WordListScriptableObject wordList;
        [SerializeField] private TextMeshProUGUI sampleText;
        [SerializeField] private TextMeshProUGUI countdownTimerText;
        [SerializeField] private TextMeshProUGUI wpmText;
        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private CanvasGroup cursorCanvasGroup;

        private List<string> _words;
        private int _currentWordIndex = 0;
        private string _currentWord;
        private readonly List<char> _typedChars = new List<char>();
        
        private Sequence _blinkSequence;
        private bool _isFirstWord = true;
        private Vector3 _lastCursorPosition;
        private float _countdownTimer;
        
        private int _correctCharCount = 0;
        private float _lastWpmUpdateTime = 0f;
        private const float WPM_UPDATE_INTERVAL = 0.1f; 
        private int _completedWordsCharCount = 0;
        private int _completedWordsCount = 0;
        
        private float _simulatedTypingSpeed = 16f;
        private float _lastSimulatedTypeTime = 0f;

        private void Start()
        {
            _countdownTimer = 30f;
            if (wordList == null || sampleText == null || cursorRect == null)
            {
                Debug.LogError("WordList, TMP_Text component, or cursor is not assigned!");
                return;
            }

            _words = wordList.words.ToList();
            _currentWord = _words[_currentWordIndex];
            DisplayWords();
            InitializeCursorBlink();
            
            Invoke(nameof(InitialCursorPositioning), 0.5f);
        }
        
        private void InitialCursorPositioning()
        {
            UpdateCursorPosition(true);
        }
        
        private void Update()
        {
            if (Input.anyKeyDown)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (IsCurrentWordComplete())
                    {
                        MoveToNextWord();
                    }
                    return;
                }

                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    HandleBackspace();
                    return;
                }

                foreach (KeyCode kcode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kcode))
                    {
                        string keyString = kcode.ToString();
                        if (keyString.Length == 1 && keyString != " ")
                        {
                            ProcessTypedChar(keyString.ToLower()[0]);
                            break;
                        }
                    }
                }
            }

            CountdownTimerText();
            //CalculateAndDisplayWPM();
            CalculateTypeRacerStyle();
            SimulateTyping();

        }

        private void CountdownTimerText()
        {
            if (_countdownTimer <= 0)
                return;
            _countdownTimer -= Time.deltaTime;
            countdownTimerText.text = _countdownTimer.ToString("0.0");
        }

        private void DisplayWords()
        {
            string displayText = "";
    
            for (int i = 0; i < _words.Count; i++)
            {
                if (i > 0) displayText += " ";
        
                if (i < _currentWordIndex)
                    displayText += $"<color={COLOR_GREEN}>{_words[i]}</color>";
                else if (i == _currentWordIndex)
                    displayText += GetColoredCurrentWord();
                else
                    displayText += $"<color={COLOR_GREY}>{_words[i]}</color>";
            }
    
            sampleText.text = displayText;
        }

        private bool IsCurrentWordComplete()
        {
            if (_typedChars.Count != _currentWord.Length) return false;
            
            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (_typedChars[i] != _currentWord[i]) return false;
            }
            return true;
        }
        
        private string GetColoredCurrentWord()
        {
            string coloredWord = "";
            for (int i = 0; i < _currentWord.Length; i++)
            {
                if (i < _typedChars.Count)
                {
                    if (_typedChars[i] == _currentWord[i])
                        coloredWord += $"<color={COLOR_YELLOW}>{_currentWord[i]}</color>";
                    else
                        coloredWord += $"<color={COLOR_RED}>{_currentWord[i]}</color>";
                }
                else
                {
                    coloredWord += $"<color={COLOR_GREY}>{_currentWord[i]}</color>";
                }
            }
            return coloredWord;
        }

        private void InitializeCursorBlink()
        {
            _blinkSequence?.Kill();
    
            _blinkSequence = DOTween.Sequence();
            _blinkSequence.Append(cursorCanvasGroup.DOFade(0f, 0.53f))
                .Append(cursorCanvasGroup.DOFade(1f, 0.53f))
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        private void UpdateCursorPosition(bool immediate = false)
        {
            TMP_TextInfo textInfo = sampleText.textInfo;
            if (textInfo.characterCount == 0) return;

            int charIndex = _currentWordIndex > 0 
                ? _words.Take(_currentWordIndex).Sum(w => w.Length + 1) + _typedChars.Count
                : _typedChars.Count;

            if (charIndex >= textInfo.characterCount)
                charIndex = textInfo.characterCount - 1;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

            int lineIndex = charInfo.lineNumber;

            Vector3 targetPos = sampleText.transform.TransformPoint(new Vector3(charInfo.topLeft.x, textInfo.lineInfo[lineIndex].baseline, 0));

            if (immediate || _lastCursorPosition == Vector3.zero)
            {
                cursorRect.position = targetPos;
                _lastCursorPosition = targetPos;
            }
            else
            {
                DOTween.Kill(cursorRect);
                cursorRect.DOMove(targetPos, 0.1f).SetEase(Ease.OutQuad).OnComplete(() => _lastCursorPosition = cursorRect.position);
            }
        }


        private void ProcessTypedChar(char typedChar)
        {
            if (_isFirstWord && _typedChars.Count == 0)
            {
                StopBlinking();
                _isFirstWord = false;
            }

            if (_typedChars.Count < _currentWord.Length)
            {
                _typedChars.Add(typedChar);
        
                // Track correct characters
                if (_typedChars.Count <= _currentWord.Length && typedChar == _currentWord[_typedChars.Count - 1])
                {
                    _correctCharCount++;
                }
        
                DisplayWords();
                UpdateCursorPosition();
            }
        }


        private void MoveToNextWord()
        {
            if (IsCurrentWordComplete())
            {
                _completedWordsCharCount += _currentWord.Length;
                _completedWordsCount++;
            }
            
            if (_currentWordIndex < _words.Count - 1)
            {
                _currentWordIndex++;
                _currentWord = _words[_currentWordIndex];
                _typedChars.Clear();
                DisplayWords();
                UpdateCursorPosition();
            }
        }

        private void HandleBackspace()
        {
            if (_typedChars.Count > 0)
            {
                _typedChars.RemoveAt(_typedChars.Count - 1);
                DisplayWords();
                UpdateCursorPosition();
            
                if (_typedChars.Count == 0 && _isFirstWord)
                {
                    InitializeCursorBlink();
                }
            }
        }
        
        private void StopBlinking()
        {
            _blinkSequence?.Kill();
            cursorCanvasGroup.alpha = 1f;
        }
        
        private void CalculateTypeRacerStyle()
        {
            float timeElapsed = (30f - _countdownTimer) / 60f; // Convert to minutes
            if (timeElapsed <= 0 || _completedWordsCount == 0) return;

            float averageWordLength = _completedWordsCharCount / (float)_completedWordsCount;
            float wpm = (_completedWordsCharCount / averageWordLength) / timeElapsed;

            wpmText.text = $"{Mathf.Round(wpm)} WPM";
        }
        
        private void CalculateAndDisplayWPM()
        {
            if (Time.time - _lastWpmUpdateTime < WPM_UPDATE_INTERVAL) return;
            _lastWpmUpdateTime = Time.time;

            // Calculate time elapsed in minutes (30 seconds = 0.5 minutes)
            float timeElapsed = Time.time / 60f;
            if (timeElapsed <= 0) return;

            // Calculate WPM
            float wpm = (_correctCharCount / 5f) / timeElapsed;
    
            // Update WPM display
            wpmText.text = $"{Mathf.Round(wpm)} WPM";
        }
        
        private void SimulateTyping()
        {
            if (_countdownTimer <= 0) return;
    
            if (Time.time - _lastSimulatedTypeTime >= 1f/_simulatedTypingSpeed)
            {
                _lastSimulatedTypeTime = Time.time;

                // If current word is complete, simulate space press
                if (_typedChars.Count == _currentWord.Length)
                {
                    if (Input.GetKeyDown(KeyCode.Space))
                        return;
                
                    if (IsCurrentWordComplete())
                    {
                        MoveToNextWord();
                    }
                }
                // Otherwise type the next character
                else if (_typedChars.Count < _currentWord.Length)
                {
                    ProcessTypedChar(_currentWord[_typedChars.Count]);
                }
            }
        }

    }
}
