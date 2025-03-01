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
                _gameRef.CurrentPaintColor = randomColor; // Plays a rando color
                _gameRef.OnGridButtonClick(playerGridButtons[randomPosition]);
            }
        }
    }
}