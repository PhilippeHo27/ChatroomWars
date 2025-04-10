using Core.Utility;
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
        public int  numberOfRounds = 6;
        public float timer = 10;
        private DoTweenSimpleAnimations _textAnimations;
        public DoTweenSimpleAnimations TextAnimations { get => _textAnimations; set => _textAnimations = value; }

        protected override void Awake()
        {
            base.Awake();
            _textAnimations = new DoTweenSimpleAnimations();
        }
    }
}
