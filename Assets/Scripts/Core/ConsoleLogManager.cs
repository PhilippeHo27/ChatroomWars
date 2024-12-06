using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Core
{
    public class ConsoleLogManager : IndestructibleSingletonBehaviour<ConsoleLogManager>
    {
        private bool isConsoleEnabled = true;
        private KeyCode toggleKey = KeyCode.BackQuote;
        
        private readonly Queue<DebugMessage> messageQueue = new Queue<DebugMessage>();
        private int maxMessages = 5;
        private float messageDuration = 5f;

        // GUI Position control
        private Rect windowRect = new Rect(10, 10, Screen.width / 3, Screen.height / 3);
        private Vector2 scrollPosition;
        private bool isDraggable = true;
        private GUIStyle windowStyle;
        private bool isStyleInitialized = false;

        private bool isResizing = false;
        private Rect resizeHandle = new Rect(0, 0, 15, 15); // Size of resize handle
        private Vector2 minWindowSize = new Vector2(200, 100); // Minimum window size
        private float handleSize = 15f;
        private class DebugMessage
        {
            public string Text;
            public float TimeStamp;

            public DebugMessage(string text)
            {
                Text = text;
                TimeStamp = Time.time;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            LoadWindowPosition();
        }
        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleConsole();
            }

            // Debug test message
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Log("Test message at " + Time.time);
            }
        }
        private void OnGUI()
                {
                    if (!isConsoleEnabled) return;
        
                    if (!isStyleInitialized)
                    {
                        InitializeGUIStyle();
                    }
        
                    ClampWindowToBounds();
                    windowRect = GUI.Window(0, windowRect, DrawConsoleWindow, "Console", windowStyle);
                }
        
        // Console functions
        public void ToggleConsole()
        {
            isConsoleEnabled = !isConsoleEnabled;
        }
        
        private void LogMessage(string message)
        {
            messageQueue.Enqueue(new DebugMessage(message));
            if (messageQueue.Count > maxMessages)
                messageQueue.Dequeue();
        }
        
        public static void Log(string message)
        {
            if (Instance != null)
            {
                Instance.LogMessage(message);
            }
        }
        
        
        // GUI functions
        private void InitializeGUIStyle()
        {
            if (isStyleInitialized) return;
        
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.fontSize = 12;
            windowStyle.normal.textColor = Color.white;
            windowStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.8f));
        
            isStyleInitialized = true;
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private void ClampWindowToBounds()
        {
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }
        
        private void DrawConsoleWindow(int windowID)
        {
            // Handle scrolling
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            var currentMessages = messageQueue.ToList();
            foreach (var message in currentMessages)
            {
                if (Time.time - message.TimeStamp > messageDuration)
                {
                    messageQueue.Dequeue();
                    continue;
                }

                GUILayout.Box(message.Text);
            }

            GUILayout.EndScrollView();

            // Draw resize handle in bottom right corner
            resizeHandle.x = windowRect.width - handleSize;
            resizeHandle.y = windowRect.height - handleSize;
            GUI.Box(resizeHandle, "↘");

            // Handle resizing
            HandleResize();

            // Make the window draggable (but not when resizing)
            if (isDraggable && !isResizing)
            {
                GUI.DragWindow();
                SaveWindowPosition();
            }
        }
        
        private void HandleResize()
        {
            Event e = Event.current;
    
            // Only handle mouse events, not repaint events
            if (e.type != EventType.Repaint)
            {
                if (e.type == EventType.MouseDown && resizeHandle.Contains(e.mousePosition))
                {
                    isResizing = true;
                    e.Use(); // Now safe to use
                }
                else if (e.type == EventType.MouseUp)
                {
                    isResizing = false;
                    e.Use(); // Now safe to use
                }

                if (isResizing && e.type == EventType.MouseDrag)
                {
                    windowRect.width = Mathf.Max(minWindowSize.x, e.mousePosition.x);
                    windowRect.height = Mathf.Max(minWindowSize.y, e.mousePosition.y);
                    e.Use(); // Now safe to use
                }
            }
        }
        
        // Window management
        
        private void SaveWindowPosition()
        {
            PlayerPrefs.SetFloat("ConsoleX", windowRect.x);
            PlayerPrefs.SetFloat("ConsoleY", windowRect.y);
            PlayerPrefs.SetFloat("ConsoleWidth", windowRect.width);
            PlayerPrefs.SetFloat("ConsoleHeight", windowRect.height);
            PlayerPrefs.Save();
        }
        
        private void LoadWindowPosition()
        {
            if (PlayerPrefs.HasKey("ConsoleX"))
            {
                windowRect.x = PlayerPrefs.GetFloat("ConsoleX");
                windowRect.y = PlayerPrefs.GetFloat("ConsoleY");
                windowRect.width = PlayerPrefs.GetFloat("ConsoleWidth", Screen.width / 3);
                windowRect.height = PlayerPrefs.GetFloat("ConsoleHeight", Screen.height / 3);
            }
        }
        
        
        // Configuration methods
        public void SetMaxMessages(int max)
        {
            maxMessages = max;
        }

        public void SetMessageDuration(float duration)
        {
            messageDuration = duration;
        }

        public void SetConsolePosition(Vector2 position)
        {
            windowRect.x = position.x;
            windowRect.y = position.y;
            SaveWindowPosition();
        }

        public void SetConsoleSize(Vector2 size)
        {
            windowRect.width = size.x;
            windowRect.height = size.y;
        }

        public void SetDraggable(bool draggable)
        {
            isDraggable = draggable;
        }
    }
}