using Core.Utility;
using UnityEngine;

namespace Core.Singletons
{
    public class GameManager : IndestructibleSingletonBehaviour<GameManager>
    {
        [SerializeField] private VFXPrefabContainer vfxPrefabContainer;
        [HideInInspector]
        public bool playingAgainstAI;
        [HideInInspector]
        public bool blindModeActive;
        [HideInInspector]
        public bool isOnline;
        public int  numberOfRounds = 6;
        public float timer = 10;
        private DoTweenSimpleAnimations _textAnimations;
        public DoTweenSimpleAnimations TextAnimations { get => _textAnimations; set => _textAnimations = value; }

        private VFX _vfx;
        public VFX VFX => _vfx;
        
        
        protected override void Awake()
        {
            base.Awake();
            _textAnimations = new DoTweenSimpleAnimations();
            _vfx = new VFX(vfxPrefabContainer);
        }
    }
}
