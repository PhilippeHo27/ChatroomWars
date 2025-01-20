using System;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;

namespace Core
{
    public class WordDisplayer : MonoBehaviour
    {
        private const string COLOR_YELLOW = "#C8C676";
        private const string COLOR_GREY = "#37393C";
        private const string COLOR_RED = "#D7444C";
        private const string COLOR_GREEN = "#4CAE54";

        [SerializeField] private WordListScriptableObject wordList;
        [SerializeField] private TextMeshProUGUI tmpText;
        [SerializeField] private RectTransform cursorRect;
        [SerializeField] private Image cursorImage;
        [SerializeField] private CanvasGroup cursorCanvasGroup;

        private List<string> words;
        private int currentWordIndex = 0;
        private string currentWord;
        private List<char> typedChars = new List<char>();
        
        private Sequence blinkSequence;
        private bool isFirstWord = true;
        private Vector3 lastCursorPosition;

        private void Start()
        {
            if (wordList == null || tmpText == null || cursorRect == null)
            {
                Debug.LogError("WordList, TMP_Text component, or cursor is not assigned!");
                return;
            }

            words = wordList.words.ToList();
            currentWord = words[currentWordIndex];
            DisplayWords();
            InitializeCursorBlink();
            
            // Delay the initial cursor positioning to ensure text has been rendered
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
        }

        private void DisplayWords()
        {
            string displayText = "";
    
            for (int i = 0; i < words.Count; i++)
            {
                if (i > 0) displayText += " ";
        
                if (i < currentWordIndex)
                    displayText += $"<color={COLOR_GREEN}>{words[i]}</color>";
                else if (i == currentWordIndex)
                    displayText += GetColoredCurrentWord();
                else
                    displayText += $"<color={COLOR_GREY}>{words[i]}</color>";
            }
    
            tmpText.text = displayText;
        }

        private bool IsCurrentWordComplete()
        {
            if (typedChars.Count != currentWord.Length) return false;
            
            for (int i = 0; i < currentWord.Length; i++)
            {
                if (typedChars[i] != currentWord[i]) return false;
            }
            return true;
        }
        
        private string GetColoredCurrentWord()
        {
            string coloredWord = "";
            for (int i = 0; i < currentWord.Length; i++)
            {
                if (i < typedChars.Count)
                {
                    if (typedChars[i] == currentWord[i])
                        coloredWord += $"<color={COLOR_YELLOW}>{currentWord[i]}</color>";
                    else
                        coloredWord += $"<color={COLOR_RED}>{currentWord[i]}</color>";
                }
                else
                {
                    coloredWord += $"<color={COLOR_GREY}>{currentWord[i]}</color>";
                }
            }
            return coloredWord;
        }

        private void InitializeCursorBlink()
        {
            blinkSequence?.Kill();
    
            blinkSequence = DOTween.Sequence();
            blinkSequence.Append(cursorCanvasGroup.DOFade(0f, 0.53f))
                .Append(cursorCanvasGroup.DOFade(1f, 0.53f))
                .SetLoops(-1, LoopType.Yoyo);
        }
        
        private void UpdateCursorPosition(bool immediate = false)
        {
            TMP_TextInfo textInfo = tmpText.textInfo;
            if (textInfo.characterCount == 0) return;

            int charIndex = currentWordIndex > 0 
                ? words.Take(currentWordIndex).Sum(w => w.Length + 1) + typedChars.Count
                : typedChars.Count;

            if (charIndex >= textInfo.characterCount)
                charIndex = textInfo.characterCount - 1;

            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];

            // Get the line index of the current character
            int lineIndex = charInfo.lineNumber;

            // Calculate the target position
            Vector3 targetPos = tmpText.transform.TransformPoint(
                new Vector3(charInfo.topLeft.x, textInfo.lineInfo[lineIndex].baseline, 0));

            // If it's the first update or immediate update is requested, set position directly
            if (immediate || lastCursorPosition == Vector3.zero)
            {
                cursorRect.position = targetPos;
                lastCursorPosition = targetPos;
            }
            else
            {
                // Kill any ongoing tweens
                DOTween.Kill(cursorRect);

                // Lerp to the new position
                cursorRect.DOMove(targetPos, 0.1f).SetEase(Ease.OutQuad)
                    .OnComplete(() => lastCursorPosition = cursorRect.position);
            }
        }


        private void ProcessTypedChar(char typedChar)
        {
            if (isFirstWord && typedChars.Count == 0)
            {
                StopBlinking();
                isFirstWord = false;
            }

            if (typedChars.Count < currentWord.Length)
            {
                typedChars.Add(typedChar);
                DisplayWords();
                UpdateCursorPosition();
            } 
        }

        private void MoveToNextWord()
        {
            if (currentWordIndex < words.Count - 1)
            {
                currentWordIndex++;
                currentWord = words[currentWordIndex];
                typedChars.Clear();
                DisplayWords();
                UpdateCursorPosition();
            }
        }

        private void HandleBackspace()
        {
            if (typedChars.Count > 0)
            {
                typedChars.RemoveAt(typedChars.Count - 1);
                DisplayWords();
                UpdateCursorPosition();
            
                if (typedChars.Count == 0 && isFirstWord)
                {
                    InitializeCursorBlink();
                }
            }
        }
        
        private void StopBlinking()
        {
            blinkSequence?.Kill();
            cursorCanvasGroup.alpha = 1f;
        }
    }
}
