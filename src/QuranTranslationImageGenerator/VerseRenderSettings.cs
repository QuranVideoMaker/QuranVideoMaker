﻿using SkiaSharp;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuranTranslationImageGenerator
{
    /// <summary>
    /// VerseRenderSettings
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    [Description("VerseRenderSettings")]
    [DisplayName("VerseRenderSettings")]
    [DebuggerDisplay("VerseRenderSettings")]
    public class VerseRenderSettings : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private Guid _id;
        private SKColor _textColor = SKColors.White;
        private SKColor _textShadowColor = SKColors.Black;

        private bool _textShadow = true;
        private SKPoint _textShadowOffset = new SKPoint(-2, 2);
        private int _fontSize = 100;
        private int _gapBetweenLines = 100;
        private bool _isRightToLeft;
        private bool _isNonAscii;
        private string _font = "Arial";
        private bool _boldFont;
        private bool _italicFont;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public Guid Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        public SKColor TextColor
        {
            get { return _textColor; }
            set
            {
                if (_textColor != value)
                {
                    _textColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the color of the shadow.
        /// </summary>
        public SKColor TextShadowColor
        {
            get { return _textShadowColor; }
            set
            {
                if (_textShadowColor != value)
                {
                    _textShadowColor = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether text should have shadows.
        /// </summary>
        public bool TextShadow
        {
            get { return _textShadow; }
            set
            {
                if (_textShadow != value)
                {
                    _textShadow = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text shadow offset.
        /// </summary>
        public SKPoint TextShadowOffset
        {
            get { return _textShadowOffset; }
            set
            {
                if (_textShadowOffset != value)
                {
                    _textShadowOffset = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        public int FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the gap between lines.
        /// </summary>
        public int GapBetweenLines
        {
            get { return _gapBetweenLines; }
            set
            {
                if (_gapBetweenLines != value)
                {
                    _gapBetweenLines = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether text is right to left.
        /// </summary>
        public bool IsRightToLeft
        {
            get { return _isRightToLeft; }
            set
            {
                if (_isRightToLeft != value)
                {
                    _isRightToLeft = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is non ASCII.
        /// </summary>
        public bool IsNonAscii
        {
            get { return _isNonAscii; }
            set
            {
                if (_isNonAscii != value)
                {
                    _isNonAscii = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the font.
        /// </summary>
        public string Font
        {
            get { return _font; }
            set
            {
                if (_font != value)
                {
                    _font = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use bold font.
        /// </summary>
        public bool BoldFont
        {
            get { return _boldFont; }
            set
            {
                if (_boldFont != value)
                {
                    _boldFont = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use italic font.
        /// </summary>
        public bool ItalicFont
        {
            get { return _italicFont; }
            set
            {
                if (_italicFont != value)
                {
                    _italicFont = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VerseRenderSettings"/> class.
        /// </summary>
        public VerseRenderSettings()
        {
        }

        /// <summary>
        /// Called when public properties changed.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public VerseRenderSettings Clone()
        {
            return new VerseRenderSettings()
            {
                Id = Id,
                FontSize = this.FontSize,
                GapBetweenLines = this.GapBetweenLines,
                IsNonAscii = this.IsNonAscii,
                IsRightToLeft = this.IsRightToLeft,
                BoldFont = this.BoldFont,
                ItalicFont = this.ItalicFont,
                TextColor = this.TextColor,
                TextShadow = this.TextShadow,
                TextShadowColor = this.TextShadowColor,
                TextShadowOffset = this.TextShadowOffset,
                Font = this.Font
            };
        }
    }
}
