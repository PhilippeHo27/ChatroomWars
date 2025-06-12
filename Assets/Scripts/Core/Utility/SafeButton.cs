using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Core.Utility
{
    public class SafeButton : Button
    {
        [SerializeField] 
        [Tooltip("Time in seconds before button can be clicked again")]
        private float cooldownTime = 0.5f;
        
        private bool _isOnCooldown;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (_isOnCooldown) return;
            
            StartCooldown();
            base.OnPointerClick(eventData);
        }
        
        public override void OnSubmit(BaseEventData eventData)
        {
            if (_isOnCooldown) return;
            
            StartCooldown();
            base.OnSubmit(eventData);
        }
    
        private void StartCooldown()
        {
            if (_isOnCooldown) return;
            
            _isOnCooldown = true;
            StartCoroutine(CooldownCoroutine());
        }
    
        private IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(cooldownTime);
            _isOnCooldown = false;
        }
        
        // Utility method to change cooldown at runtime if needed
        public void SetCooldownTime(float newCooldownTime)
        {
            cooldownTime = Mathf.Max(0f, newCooldownTime);
        }
    }
}