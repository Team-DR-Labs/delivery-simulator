using DeliveryBot.Delivery;

namespace DeliveryBot.UI
{
    /// <summary>Text buffer behind the nickname prompt: length-capped, control characters rejected.</summary>
    public sealed class NicknameBuffer
    {
        public string Text { get; private set; }
        public int MaxLength { get; }

        public NicknameBuffer(int maxLength = Leaderboard.MaxNameLength, string initial = "")
        {
            MaxLength = maxLength;
            Text = "";
            if (!string.IsNullOrEmpty(initial))
                foreach (var c in initial) Append(c);
        }

        /// <summary>Returns false when the character was rejected (control char or buffer full).</summary>
        public bool Append(char c)
        {
            if (c < ' ' || char.IsSurrogate(c)) return false;
            if (Text.Length >= MaxLength) return false;
            Text += c;
            return true;
        }

        public bool Backspace()
        {
            if (Text.Length == 0) return false;
            Text = Text.Substring(0, Text.Length - 1);
            return true;
        }

        public void Clear() => Text = "";

        /// <summary>Final, sanitized nickname (falls back to the default name when empty).</summary>
        public string Commit() => Leaderboard.SanitizeName(Text);
    }
}
