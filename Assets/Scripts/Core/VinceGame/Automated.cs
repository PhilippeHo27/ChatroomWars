using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Core.VinceGame.GridGame;
using Random = UnityEngine.Random;

namespace Core.VinceGame
{
    // No MonoBehaviour needed
    public class Automated 
    {
        private readonly GamePrototype _gameRef;
    
        public Automated(GamePrototype gameReference) 
        {
            _gameRef = gameReference;
        }
        
        
        // Returns the position that was played, or -1 if no move was made
        public int SimulateOpponentMove(GridData opponentGrid, Image[] otherBoard)
        {
            List<int> availablePositions = new List<int>();
            for (int i = 0; i < opponentGrid.Marks.Length; i++)
            {
                if (!opponentGrid.Marks[i])
                {
                    availablePositions.Add(i);
                }
            }

            if (availablePositions.Count > 0)
            {
                int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];
                string[] colors = new string[] { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                string randomColor = colors[Random.Range(0, colors.Length)];

                opponentGrid.Marks[randomPosition] = true;
                opponentGrid.Color[randomPosition] = randomColor;
                
                otherBoard[randomPosition].color = GetColorFromHex(randomColor);
                
                return randomPosition;
            }
            
            return -1;
        }
        
        public void ForcePlayerMove(GridData playerGrid, Button[] playerGridButtons)
        {
            List<int> availablePositions = new List<int>();
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                if (!playerGrid.Marks[i])
                {
                    availablePositions.Add(i);
                }
            }

            if (availablePositions.Count > 0)
            {
                int randomPosition = availablePositions[Random.Range(0, availablePositions.Count)];
                string[] colors = { ColorGreenSelect, ColorBlueSelect, ColorRedSelect };
                string randomColor = colors[Random.Range(0, colors.Length)];
                // Call ApplyMove directly instead of going through OnGridButtonClick
                _gameRef.ApplyMove(randomPosition, randomColor);
            }
        }

        public void ForceShieldSelection(GridData playerGrid, Button[] playerGridButtons)
        {
            List<int> markedPositions = new List<int>();
            for (int i = 0; i < playerGrid.Marks.Length; i++)
            {
                // Only include positions that have the player's pieces
                if (playerGrid.Marks[i] && !string.IsNullOrEmpty(playerGrid.Color[i]))
                {
                    markedPositions.Add(i);
                }
            }

            if (markedPositions.Count > 0)
            {
                // Pick a random piece to shield
                int randomPosition = markedPositions[Random.Range(0, markedPositions.Count)];
                // Call ApplyShield directly instead of going through OnGridButtonClick
                _gameRef.ApplyShield(randomPosition);
            }
            else
            {
                // If no pieces to shield, just cancel shield mode
                _gameRef.shieldSelectionMode = false;
            }
        }


    }
}