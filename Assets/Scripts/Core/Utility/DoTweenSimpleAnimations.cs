using UnityEngine;
using DG.Tweening;
using TMPro;
using System;
using UnityEngine.UI;

namespace Core.Utility
{
    public class DoTweenSimpleAnimations
    {
        private bool _isWarningAnimationPlaying;

        public void PopText(object textElement, string message, float scaleDuration = 0.2f, float scaleDownDuration = 0.1f, float scaleAmount = 1.2f, bool clearAfterDelay = false, float clearDelay = 1.5f)
        {
            if (textElement == null)
                return;

            if (textElement is TMP_Text singleText)
            {
                singleText.text = message;
                singleText.transform.localScale = Vector3.one;

                Sequence sequence = DOTween.Sequence();
                sequence.Append(singleText.transform.DOScale(scaleAmount, scaleDuration))
                    .Append(singleText.transform.DOScale(1f, scaleDownDuration))
                    .SetEase(Ease.OutBack);
        
                if (clearAfterDelay)
                {
                    DOVirtual.DelayedCall(clearDelay, () => singleText.text = "");
                }
            }
            else if (textElement is TMP_Text[] textArray)
            {
                foreach (var txt in textArray)
                {
                    if (txt != null)
                    {
                        txt.text = message;
                        txt.transform.localScale = Vector3.one;

                        Sequence sequence = DOTween.Sequence();
                        sequence.Append(txt.transform.DOScale(scaleAmount, scaleDuration))
                            .Append(txt.transform.DOScale(1f, scaleDownDuration))
                            .SetEase(Ease.OutBack);
                
                        if (clearAfterDelay)
                        {
                            DOVirtual.DelayedCall(clearDelay, () => txt.text = "");
                        }
                    }
                }
            }
        }
        
        public void ShowFadingText(string message, object textElement, float fadeInDuration = 0.5f, float displayDuration = 1f, float fadeOutDuration = 0.5f)
        {
            if (textElement == null)
                return;
                
            if (textElement is TMP_Text singleText)
            {
                AnimateSingleText(message, singleText, fadeInDuration, displayDuration, fadeOutDuration);
            }
            else if (textElement is TMP_Text[] textArray)
            {
                foreach (var txt in textArray)
                {
                    if (txt != null)
                        AnimateSingleText(message, txt, fadeInDuration, displayDuration, fadeOutDuration);
                }
            }
        }
        
        private void AnimateSingleText(string message, TMP_Text textElement, float fadeInDuration, float displayDuration, float fadeOutDuration)
        {
            textElement.alpha = 0f;
            textElement.text = message;

            Sequence fadeSequence = DOTween.Sequence();

            fadeSequence.Append(textElement.DOFade(1f, fadeInDuration))
                .AppendInterval(displayDuration)
                .Append(textElement.DOFade(0f, fadeOutDuration));
        }
        
        public void CreateBreathingAnimation(Transform target, float scalePulse = 1.05f, float duration = 1.75f)
        {
            DOTween.Kill(target);
            target.localScale = Vector3.one;
            Sequence breathingSequence = DOTween.Sequence();
    
            // More natural breathing with smoother easing and timing
            breathingSequence.Append(target.DOScale(scalePulse, duration * 0.6f)
                    .SetEase(Ease.OutQuad))
                    .Append(target.DOScale(1f, duration * 0.4f)
                    .SetEase(Ease.InOutQuad));
    
            breathingSequence.SetLoops(-1);
        }

        
        // New countdown animation with pop effect
        public void Countdown(TMP_Text textElement, string[] countdownTexts, float fadeInDuration = 0.3f, float displayDuration = 0.7f, float fadeOutDuration = 0.3f, float popScale = 1.3f, Action onComplete = null)
        {
            if (textElement == null || countdownTexts == null || countdownTexts.Length == 0)
                return;
                
            DOTween.Kill(textElement.transform);
            textElement.alpha = 0f;
            
            Sequence countdownSequence = DOTween.Sequence();
            
            for (int i = 0; i < countdownTexts.Length; i++)
            {
                int index = i;
                
                // Add a callback to update the text at the beginning of each number
                countdownSequence.AppendCallback(() => textElement.text = countdownTexts[index]);
                
                // Create a parallel sequence for each number
                Sequence numberSequence = DOTween.Sequence();
                countdownSequence.SetTarget(textElement); // This is the key line

                // Fade in while scaling up
                numberSequence.Append(textElement.DOFade(1f, fadeInDuration))
                    .Join(textElement.transform.DOScale(popScale, fadeInDuration).SetEase(Ease.OutBack));
                    
                // Hold at full opacity
                numberSequence.AppendInterval(displayDuration);
                
                // Fade out while scaling back down
                numberSequence.Append(textElement.DOFade(0f, fadeOutDuration))
                    .Join(textElement.transform.DOScale(1f, fadeOutDuration).SetEase(Ease.InBack));
                
                countdownSequence.Append(numberSequence);
            }
            
            if (onComplete != null)
                countdownSequence.OnComplete(() => onComplete.Invoke());
        }
        
        // Typewriter effect for text
        public void Typewriter(TMP_Text textElement, string fullText, float typingSpeed = 0.05f, float initialDelay = 0.1f, Action onComplete = null)
        {
            if (textElement == null)
                return;
                
            DOTween.Kill(textElement);
            textElement.text = "";
            
            Sequence typeSequence = DOTween.Sequence();
            
            if (initialDelay > 0)
                typeSequence.AppendInterval(initialDelay);
                
            typeSequence.AppendCallback(() => textElement.maxVisibleCharacters = 0);
            typeSequence.Append(DOTween.To(() => textElement.maxVisibleCharacters, 
                x => textElement.maxVisibleCharacters = x, fullText.Length, fullText.Length * typingSpeed)
                .OnUpdate(() => {
                    if (textElement.text != fullText)
                        textElement.text = fullText;
                }));
                
            if (onComplete != null)
                typeSequence.OnComplete(() => onComplete.Invoke());
        }
        
        // Shake animation for any transform
        public void ShakeTransform(Transform target, float duration = 0.5f, float strength = 1f, int vibrato = 10, 
            float randomness = 90f, bool snapping = false, bool fadeOut = true, 
            ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Full)
        {
            if (target == null)
                return;
    
            DOTween.Kill(target);
            target.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut, randomnessMode);
        }

        // Shake animation using directional strength (Vector3)
        public void ShakeTransformDirectional(Transform target, Vector3 strength, float duration = 0.5f, int vibrato = 10, 
            float randomness = 90f, bool snapping = false, bool fadeOut = true, 
            ShakeRandomnessMode randomnessMode = ShakeRandomnessMode.Full)
        {
            if (target == null)
                return;
    
            DOTween.Kill(target);
            target.DOShakePosition(duration, strength, vibrato, randomness, snapping, fadeOut, randomnessMode);
        }
        
        // Floating animation (good for pickups or UI elements that need attention)
        public void FloatingAnimation(Transform target, float yOffset = 0.5f, float duration = 1f, bool loop = true)
        {
            if (target == null)
                return;
                
            DOTween.Kill(target);
            Vector3 startPos = target.position;
            
            Sequence floatSequence = DOTween.Sequence();
            
            floatSequence.Append(target.DOMoveY(startPos.y + yOffset, duration/2).SetEase(Ease.InOutSine))
                .Append(target.DOMoveY(startPos.y, duration/2).SetEase(Ease.InOutSine));
                
            if (loop)
                floatSequence.SetLoops(-1);
        }
        
        public void PopThenBreathe(Transform target, float popScale = 1.3f, float popDuration = 0.2f, 
            float returnDuration = 0.1f, float breathScale = 1.1f, float breathDuration = 0.8f)
        {
            if (target == null)
                return;
        
            DOTween.Kill(target);
            target.localScale = Vector3.one;
    
            Sequence sequence = DOTween.Sequence();
    
            // Initial pop animation
            sequence.Append(target.DOScale(popScale, popDuration).SetEase(Ease.OutBack))
                .Append(target.DOScale(1f, returnDuration))
                .AppendCallback(() => {
                    // Start breathing animation after the pop
                    CreateBreathingAnimation(target, breathScale, breathDuration);
                });
        }
        
        public void RainbowText(string message, object textElement, float fadeInDuration = 0.5f, float displayDuration = 2f, float fadeOutDuration = 0.5f)
        {
            if (textElement == null)
                return;

            // Handle both single text and arrays
            if (textElement is TMP_Text singleText)
            {
                AnimateRainbowText(message, singleText, fadeInDuration, displayDuration, fadeOutDuration);
            }
            else if (textElement is TMP_Text[] textArray)
            {
                foreach (var txt in textArray)
                {
                    if (txt != null)
                        AnimateRainbowText(message, txt, fadeInDuration, displayDuration, fadeOutDuration);
                }
            }
        }

        private void AnimateRainbowText(string message, TMP_Text textElement, float fadeInDuration, float displayDuration, float fadeOutDuration)
        {
            // Store original color to restore later
            Color originalColor = textElement.color;
            
            // Set up text
            textElement.alpha = 0f;
            textElement.text = message;
            
            // Create a sequence
            Sequence rainbowSequence = DOTween.Sequence();
            
            // Fade in
            rainbowSequence.Append(textElement.DOFade(1f, fadeInDuration));
            
            // Define a smoother, less intense rainbow with fewer colors
            Color[] rainbowColors = new Color[] {
                new Color(1, 0.3f, 0.3f),    // Soft Red
                new Color(1, 0.6f, 0.2f),    // Soft Orange
                new Color(1, 1, 0.3f),       // Soft Yellow
                new Color(0.3f, 1, 0.3f),    // Soft Green
                new Color(0.3f, 0.7f, 1),    // Soft Blue
                new Color(0.7f, 0.3f, 1)     // Soft Purple
            };
            
            // Just do one full cycle through the colors during display duration
            float transitionTime = displayDuration / rainbowColors.Length;
            
            // Add color transitions - just one smooth cycle
            foreach (Color color in rainbowColors)
            {
                rainbowSequence.Append(textElement.DOColor(color, transitionTime));
            }
            
            // Fade out and restore original color
            rainbowSequence.Append(textElement.DOFade(0f, fadeOutDuration))
                           .AppendCallback(() => {
                                textElement.color = originalColor;
                                textElement.text = "";
                           });
        }
        
        private Sequence _blinkSequence;

        public void UpdateTimerWithEffects(Image timerFill, float currentTime, float maxTime, float warningThreshold = 2f)
        {
            // Always update the fill amount (this handles shrinking)
            timerFill.fillAmount = Mathf.Clamp01(currentTime / maxTime);

            // Timer is resetting or finished
            if (currentTime >= maxTime || currentTime <= 0)
            {
                ResetTimerVisuals(timerFill);
                return;
            }

            // If we're entering warning territory and warning animation isn't already playing
            if (currentTime <= warningThreshold && !_isWarningAnimationPlaying)
            {
                // Set to red immediately
                timerFill.color = Color.red;

                // Kill any existing tweens on this object first
                DOTween.Kill(timerFill);
        
                // If there's an existing sequence, kill it too
                if (_blinkSequence != null)
                {
                    _blinkSequence.Kill();
                    _blinkSequence = null;
                }

                // Create blinking effect
                _blinkSequence = DOTween.Sequence();
                _blinkSequence.Append(timerFill.DOFade(0.5f, 0.25f));
                _blinkSequence.Append(timerFill.DOFade(1f, 0.25f));
                _blinkSequence.SetLoops(-1);

                _isWarningAnimationPlaying = true;
            }
        }
        
        public void ResetTimerVisuals(Image timerFill)
        {
            DOTween.Kill(timerFill);
    
            // Kill the sequence if it exists
            if (_blinkSequence != null)
            {
                _blinkSequence.Kill();
                _blinkSequence = null;
            }
    
            // Explicitly reset ALL properties to initial state
            timerFill.color = Color.white;
            timerFill.fillAmount = 1f;  // Reset fill amount
            timerFill.DOFade(1f, 0f);   // Reset alpha immediately
            _isWarningAnimationPlaying = false;
        }


    }
}
