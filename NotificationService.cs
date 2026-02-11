using System.Collections.Concurrent;

namespace SecureTPMVault;

/// <summary>
/// Defines the severity or category of a notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Represents a neutral, informational message.
    /// </summary>
    Info,

    /// <summary>
    /// Represents a successful operation message.
    /// </summary>
    Success,

    /// <summary>
    /// Represents a warning message that requires user attention.
    /// </summary>
    Warning,

    /// <summary>
    /// Represents a critical error message.
    /// </summary>
    Error,

    /// <summary>
    /// Represents a helpful tip or hint.
    /// </summary>
    Tip
}

/// <summary>
/// Defines the lifecycle behavior of a notification in the display queue.
/// </summary>
public enum DisplayPolicy
{
    /// <summary>
    /// The notification is displayed exactly once and then removed.
    /// </summary>
    Once,

    /// <summary>
    /// The notification remains in the queue indefinitely.
    /// </summary>
    Always,

    /// <summary>
    /// The notification is displayed for a specific number of render cycles.
    /// </summary>
    CountBased,

    /// <summary>
    /// The notification is selected based on a weighted probability algorithm.
    /// </summary>
    Weighted
}

/// <summary>
/// Represents a single notification object containing the message, type, and display rules.
/// </summary>
internal class Notification
{
    /// <summary>
    /// The text content of the notification.
    /// </summary>
    public readonly Func<string> Message;

    /// <summary>
    /// The category type of the notification.
    /// </summary>
    public readonly NotificationType Type;

    /// <summary>
    /// The display policy governing the notification's lifecycle.
    /// </summary>
    public readonly DisplayPolicy Policy;

    /// <summary>
    /// The probability weight for selection. Used only if Policy is Weighted.
    /// </summary>
    public readonly int Weight;

    /// <summary>
    /// Tracks how many times the notification can still be displayed. Used only if Policy is CountBased.
    /// This field is mutable because it decrements on every render.
    /// </summary>
    public int RemainingViews;

    /// <summary>
    /// Initializes a new instance of the Notification class.
    /// </summary>
    /// <param name="message">The notification text.</param>
    /// <param name="type">The type of notification.</param>
    /// <param name="policy">The lifecycle policy.</param>
    /// <param name="value">
    /// Represents 'Weight' if policy is Weighted, or 'RemainingViews' if policy is CountBased. 
    /// Default is 1.
    /// </param>
    public Notification(Func<string> message, NotificationType type, DisplayPolicy policy, int value = 1)
    {
        Message = message;
        Type = type;
        Policy = policy;

        if (policy == DisplayPolicy.Weighted)
        {
            Weight = value < 1 ? 1 : value;
            RemainingViews = 0;
        }
        else if (policy == DisplayPolicy.CountBased)
        {
            Weight = 0;
            RemainingViews = value < 1 ? 1 : value;
        }
        else
        {
            Weight = 1;
            RemainingViews = 1;
        }
    }

    /// <summary>
    /// Determines the console color associated with the notification type.
    /// </summary>
    /// <returns>The <see cref="ConsoleColor"/> corresponding to the notification type.</returns>
    public ConsoleColor GetColor() => Type switch
    {
        NotificationType.Error => Theme.ErrorColor,
        NotificationType.Warning => Theme.WarningColor,
        NotificationType.Tip => Theme.TipColor,
        NotificationType.Success => Theme.SuccessColor,
        _ => Theme.TextColor
    };
}

/// <summary>
/// Manages the creation, storage, and rendering of UI notifications.
/// Implements a unified interface for adding alerts and tips.
/// </summary>
internal static class NotificationService
{
    private static readonly ConcurrentQueue<Notification> _notifications = new();
    private static readonly List<Notification> _tipPool = [];

    /// <summary>
    /// Initializes the service and populates the tip pool with educational and functional hints.
    /// </summary>
    static NotificationService()
    {
        // 1. CRITICAL CONCEPTS (High Weight: 8-10)
        Add("CONCEPT: You don't need a password because YOU are the key. Windows DPAPI verifies your identity.",
            NotificationType.Tip, DisplayPolicy.Weighted, 10);

        Add("WARNING: Since your key is bound to this PC, copying " + AppConstants.FileExtension + " files to another PC will not work.",
            NotificationType.Tip, DisplayPolicy.Weighted, 9);

        Add("DATA LOSS WARNING: Reinstalling Windows deletes your User Profile Key. Decryption will become impossible.",
            NotificationType.Tip, DisplayPolicy.Weighted, 9);

        // 2. DEVELOPER MESSAGES (Medium Weight: 5-7)
        Add("Developer Message: I hope you like my program ^_^",
            NotificationType.Tip, DisplayPolicy.Weighted, 7);

        Add("Developer Message: Please contribute my program in github :)",
            NotificationType.Tip, DisplayPolicy.Weighted, 6);

        Add("Behind the Scenes: We use 'AES-256' with a unique 32-byte salt for EVERY single file.",
            NotificationType.Tip, DisplayPolicy.Weighted, 5);

        // 3. FEATURES & TRIVIA (Low Weight: 1-4)
        Add(() => "Did you know? The clipboard auto-clear feature runs after " + Settings.ClipboardClearSeconds.Value,
            NotificationType.Tip, DisplayPolicy.Weighted, 4);

        Add("Pro Tip: You can drag & drop a .tpmPassword file onto the .exe to open it instantly.",
            NotificationType.Tip, DisplayPolicy.Weighted, 4);

        Add("Automation: Use arguments like '--set' and '--get' to use this tool in batch scripts.",
            NotificationType.Tip, DisplayPolicy.Weighted, 4);

        Add("Fun Fact: Your Master Key is re-encrypted every time you save, ensuring fresh entropy.",
            NotificationType.Tip, DisplayPolicy.Weighted, 4);

        Add("Privacy: If you have Windows Defender, the program scans parent processes via to ensure no malware is trying to automate this vault.",
            NotificationType.Tip, DisplayPolicy.Weighted, 4);
    }

    public static void Add(string message,
                       NotificationType type = NotificationType.Info,
                       DisplayPolicy policy = DisplayPolicy.Once,
                       int value = 1)
    {
        Add(() => message, type, policy, value);
    }
    /// <summary>
    /// Adds a new notification to the system.
    /// Automatically routes the notification to the priority queue or the tip pool based on its type and policy.
    /// </summary>
    /// <param name="message">The message text to display.</param>
    /// <param name="type">The category of the notification. Defaults to Info.</param>
    /// <param name="policy">The display policy. Defaults to Once.</param>
    /// <param name="value">
    /// An optional integer value that acts as 'Weight' for Weighted policy or 'Count' for CountBased policy. 
    /// Defaults to 1.
    /// </param>
    public static void Add(Func<string> message,
                           NotificationType type = NotificationType.Info,
                           DisplayPolicy policy = DisplayPolicy.Once,
                           int value = 1)
    {
        var note = new Notification(message, type, policy, value);

        if (type == NotificationType.Tip || policy == DisplayPolicy.Weighted)
        {
            _tipPool.Add(note);
        }
        else
        {
            _notifications.Enqueue(note);
            Log(note, PrintToConsole: false);
        }
    }

    /// <summary>
    /// Iterates through active notifications and renders them to the console.
    /// Handles lifecycle management (removal of expired notifications).
    /// </summary>
    public static void RenderNotifications()
    {
        int queueCount = _notifications.Count;
        for (int i = 0; i < queueCount; i++)
        {
            if (_notifications.TryDequeue(out var note))
            {
                DrawNotificationBox(note);

                if (ShouldKeep(note))
                {
                    _notifications.Enqueue(note);
                }
            }
        }

        if (_tipPool.Count > 0)
        {
            Notification selectedTip = SelectWeightedTip();
            DrawNotificationBox(selectedTip);
        }
    }

    private static Notification SelectWeightedTip()
    {
        int totalWeight = 0;
        foreach (var tip in _tipPool) totalWeight += tip.Weight;

        int randomValue = Random.Shared.Next(1, totalWeight + 1);
        int currentSum = 0;

        foreach (var tip in _tipPool)
        {
            currentSum += tip.Weight;
            if (randomValue <= currentSum) return tip;
        }

        return _tipPool[0];
    }

    private static bool ShouldKeep(Notification note)
    {
        if (note.Policy == DisplayPolicy.Once) return false;

        if (note.Policy == DisplayPolicy.CountBased)
        {
            note.RemainingViews--;
            return note.RemainingViews > 0;
        }

        return true;
    }

    private static void DrawNotificationBox(Notification note)
    {
        string prefix = note.Type switch
        {
            NotificationType.Error => "[!]",
            NotificationType.Warning => "[?]",
            NotificationType.Tip => "[*]",
            NotificationType.Success => "[V]",
            _ => "[i]"
        };

        string content = $" {prefix} {note.Message()} ";
        string topBorder = "┌" + new string('─', content.Length) + "┐";
        string botBorder = "└" + new string('─', content.Length) + "┘";
        string middle = $"│{content}│";

        Console.ForegroundColor = note.GetColor();
        Console.WriteLine(topBorder);
        Console.WriteLine(middle);
        Console.WriteLine(botBorder);
        Console.ResetColor();
    }
}