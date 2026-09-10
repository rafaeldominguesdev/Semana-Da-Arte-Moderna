using UnityEngine;

namespace MuseumModerna
{
    /// <summary>Tokens semânticos compartilhados pela interface nativa uGUI.</summary>
    public static class MuseumGuideTheme
    {
        public static readonly Color Surface = new Color(.035f, .075f, .08f, .96f);
        public static readonly Color Text = new Color(.97f, .94f, .86f);
        public static readonly Color Accent = new Color(.82f, .69f, .45f);
        public static readonly Color Control = new Color(.10f, .17f, .18f);
        public static readonly Color Focus = new Color(.28f, .37f, .36f);
        public const int Body = 18, Title = 28, Label = 16, Padding = 20, Gap = 12;
        public const float Width = 440, ButtonHeight = 44, Fade = .25f, Radius = 8;
        public const string FontResource = "LegacyRuntime.ttf";
    }
}
