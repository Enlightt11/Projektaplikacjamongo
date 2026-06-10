using System.Windows.Controls;

namespace projektaplikacjamongo.Models
{
    /// <summary>
    /// Runtime game object representing a word falling on the Canvas.
    /// Not persisted in MongoDB — only used during gameplay.
    /// </summary>
    public class FallingWord
    {
        public string Text { get; set; } = string.Empty;
        public Border UIElement { get; set; } = null!;
        public double Speed { get; set; }
        public bool IsDestroyed { get; set; }
    }
}
