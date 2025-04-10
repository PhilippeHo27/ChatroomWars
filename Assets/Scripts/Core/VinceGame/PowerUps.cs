using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Core.VinceGame.GridGame;

namespace Core.VinceGame
{
    public class PowerUps : MonoBehaviour
    {
        [SerializeField] private GamePrototype gamePrototype;
        [SerializeField] private GameGUI gameGUI;
        [SerializeField] private TMP_Text specialUsedText;
        [SerializeField] private CanvasGroup[] specialsGroups;
        [SerializeField] private Button shieldButton;
        [SerializeField] private Button revealButton;
        [SerializeField] private Button extraButton;
        
        private bool _redPowerupUsed;
        private bool _greenPowerupUsed;
        private bool _bluePowerupUsed;
        
        private bool _redPowerupObtained;
        private bool _greenPowerupObtained;
        private bool _bluePowerupObtained;

        public bool redPowerActivated;

        private GameManager _gameManager;

        private void Start()
        {
            _gameManager = GameManager.Instance;
            shieldButton.onClick.AddListener(ShieldSelectedPiece);
            revealButton.onClick.AddListener(RevealBoard);
            extraButton.onClick.AddListener(EnableExtraTurn);
        }

        public bool ProcessPowerup(string color)
        {
            switch (color)
            {
                case ColorGreenSelect when !_greenPowerupObtained:
                    _greenPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(0));
                    return true;

                case ColorBlueSelect when !_bluePowerupObtained:
                    _bluePowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(1));
                    return true;

                case ColorRedSelect when !_redPowerupObtained:
                    _redPowerupObtained = true;
                    StartCoroutine(ActivatePowerupUI(2));
                    return true;
            }
            return false;
        }
        
        public void InitializePowerups()
        {
            ResetValues();
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

        private void ShieldSelectedPiece()
        {
            if (_greenPowerupUsed) return;

            gamePrototype.shieldSelectionMode = true;
            _greenPowerupUsed = true;

            _gameManager.TextAnimations.RainbowText("Shield used", specialUsedText);
        }

        public void ShieldPieces(bool isOnline, GridData playerGrid)
        {
            if (_greenPowerupUsed) return;

            _gameManager.TextAnimations.RainbowText("Shield used", specialUsedText);

            StartCoroutine(DeactivatePowerupUI(0));
            _greenPowerupUsed = true;
            List<int> immuneSquares = new List<int>(); 
    
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                if (playerGrid.Marks[i])
                {
                    Debug.Log("Shielding these indexes: " + i);
                    playerGrid.Immune[i] = true;
                    immuneSquares.Add(i);
                }
            }
    
            //if (isOnline) gamePrototype.SendImmuneStatus(immuneSquares.Select(x => (byte)x).ToArray());
            if (isOnline) gamePrototype.NetworkHandler.SendImmuneStatus(immuneSquares.Select(x => (byte)x).ToArray());
        }
        
        public void ShieldOpponentPieces(GridData opponentGrid, byte[] indexes)
        {
            if (_greenPowerupUsed) return;
            _gameManager.TextAnimations.RainbowText("Opponent used shield!!", specialUsedText);
            StartCoroutine(DeactivatePowerupUI(0));
            _greenPowerupUsed = true;
    
            foreach (byte index in indexes)
            {
                Debug.Log("Shielding index on opponent grid: " + index);
                opponentGrid.Immune[index] = true;
            }
        }

        
        public void ResetGreenPowerUp(GridData playerGrid)
        {
            if (_greenPowerupUsed)
            {
                for (int i = 0; i < playerGrid.Marks.Length; i++)
                {
                    playerGrid.Immune[i] = false;
                }
                _greenPowerupUsed = false;

                specialUsedText.text = "";
            }
        }
        
        public void OnShieldApplied()
        {
            StartCoroutine(DeactivatePowerupUI(0));
        }
        
        public void RevealBoard()
        {
            if (_bluePowerupUsed) return;
            
            _gameManager.TextAnimations.RainbowText("Reveal board used", specialUsedText);
            StartCoroutine(DeactivatePowerupUI(1));
            //gamePrototype.SetSideBySideView(true);
            gameGUI.SetSideBySideView(true);
            _bluePowerupUsed = true;
        }
        
        public void ResetBluePowerUp()
        {
            if (_bluePowerupUsed)
            {
                //gamePrototype.SetCenteredView(true);
                gameGUI.SetCenteredView(true);
                _bluePowerupUsed = false;
            }
        }
        
        public void EnableExtraTurn()
        {
            if (_redPowerupUsed) return;
            redPowerActivated = true;
            _gameManager.TextAnimations.RainbowText("Extra turn activated!", specialUsedText);
            StartCoroutine(DeactivatePowerupUI(2));
            _redPowerupUsed = true;
        }
        
        public void ResetRedPowerup()
        {
            if (_redPowerupUsed)
            {
                _gameManager.TextAnimations.RainbowText("Extra turn applied!", specialUsedText);
                _redPowerupUsed = false;
            }
        }

        private void ResetValues()
        {
            _redPowerupUsed = false;
            _greenPowerupUsed = false;
            _bluePowerupUsed = false;
            _redPowerupObtained = false;
            _greenPowerupObtained = false;
            _bluePowerupObtained = false;
        }
    }
}
