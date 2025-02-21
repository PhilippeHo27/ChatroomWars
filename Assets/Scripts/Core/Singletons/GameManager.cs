using UnityEngine;

namespace Core.Singletons
{
    public class GameManager : IndestructibleSingletonBehaviour<GameManager>
    {
        [HideInInspector]
        public bool playingAgainstAI = true;
        [HideInInspector]
        public bool blindModeActive = true;
        [HideInInspector]
        public bool isOnline = false;
    }
}
