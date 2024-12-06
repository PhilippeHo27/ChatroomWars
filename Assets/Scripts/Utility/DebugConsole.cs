using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugConsole
{
    private static readonly Queue<DebugMessage> messageQueue = new Queue<DebugMessage>();
    private static int maxMessages = 5;
    private static float messageDuration = 5f;

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

    public static void Print(string message)
    {
        messageQueue.Enqueue(new DebugMessage(message));
        if (messageQueue.Count > maxMessages)
            messageQueue.Dequeue();
    }

    public static void OnGUIUpdate()
    {
        GUI.skin.box.fontSize = 16;
        float yPos = 10f;

        // Create a copy of the queue to avoid modification during enumeration
        var currentMessages = messageQueue.ToList();
        foreach (var message in currentMessages)
        {
            if (Time.time - message.TimeStamp > messageDuration)
            {
                messageQueue.Dequeue();
                continue;
            }

            GUI.Box(new Rect(10, yPos, Screen.width / 3, 25), message.Text);
            yPos += 30f;
        }
    }
}
