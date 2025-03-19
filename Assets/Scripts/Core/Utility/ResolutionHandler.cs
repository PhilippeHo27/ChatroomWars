using UnityEngine;

namespace Core.Utility
{
    public class ResolutionHandler
    {
        private readonly float _mobileZoomFactor = 1.6f;
        private readonly int _defaultWidth = 1920;
        private readonly int _defaultHeight = 1080;
        
        public ResolutionHandler() { }
        
        public ResolutionHandler(float customMobileZoomFactor)
        {
            _mobileZoomFactor = customMobileZoomFactor;
        }
        
        public void AdjustResolution()
        {
            
            Screen.SetResolution(33, 88, Screen.fullScreen);

            
            // if (true)
            // {
            //     ApplyMobileZoom();
            //     Debug.Log($"Mobile browser detected. Applied zoom factor: {_mobileZoomFactor}");
            // }
            // else
            // {
            //     Screen.SetResolution(_defaultWidth, _defaultHeight, Screen.fullScreen);
            // }
        }
        
        private void ApplyMobileZoom()
        {
            int zoomedWidth = Mathf.RoundToInt(_defaultWidth / _mobileZoomFactor);
            Screen.SetResolution(zoomedWidth, _defaultHeight, Screen.fullScreen);
        }

        
        private bool IsMobileBrowser()
        {
            bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;
            if (!isWebGL) return false;
            
            return Input.touchSupported;
        }
    }
}