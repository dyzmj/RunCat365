// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace RunCat365
{
    internal sealed class FlatTrackBar : Control
    {
        private int minimum = 1;
        private int maximum = 10;
        private int currentValue = 5;
        private bool dragging;

        internal event EventHandler? ValueChanged;

        [DefaultValue(1)]
        internal int Minimum
        {
            get => minimum;
            set
            {
                minimum = value;
                if (currentValue < minimum) Value = minimum;
                Invalidate();
            }
        }

        [DefaultValue(10)]
        internal int Maximum
        {
            get => maximum;
            set
            {
                maximum = value;
                if (currentValue > maximum) Value = maximum;
                Invalidate();
            }
        }

        [DefaultValue(5)]
        internal int Value
        {
            get => currentValue;
            set
            {
                var clamped = Math.Clamp(value, minimum, maximum);
                if (currentValue == clamped) return;
                currentValue = clamped;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal Color TrackColor { get; set; } = Color.FromArgb(110, 110, 110);
        internal Color TrackFilledColor { get; set; } = Color.FromArgb(180, 180, 180);
        internal Color ThumbColor { get; set; } = Color.FromArgb(70, 120, 200);
        internal Color ThumbBorderColor { get; set; } = Color.FromArgb(140, 170, 220);

        internal int TrackThickness { get; set; } = 3;
        internal int ThumbWidth { get; set; } = 12;
        internal int ThumbHeight { get; set; } = 18;

        public FlatTrackBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.ResizeRedraw,
                true);
            BackColor = Color.Transparent;
            Height = 24;
            Cursor = Cursors.Hand;
        }

        private Rectangle GetTrackRectangle()
        {
            var halfThumb = ThumbWidth / 2;
            var trackY = (Height - TrackThickness) / 2;
            return new Rectangle(halfThumb, trackY, Math.Max(0, Width - ThumbWidth), TrackThickness);
        }

        private int GetThumbCenterX()
        {
            var range = Math.Max(1, maximum - minimum);
            var ratio = (float)(currentValue - minimum) / range;
            var trackWidth = Math.Max(0, Width - ThumbWidth);
            return ThumbWidth / 2 + (int)(trackWidth * ratio);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var track = GetTrackRectangle();
            var thumbCenterX = GetThumbCenterX();

            using var trackBrush = new SolidBrush(TrackColor);
            e.Graphics.FillRectangle(trackBrush, track);

            var filledRect = new Rectangle(track.X, track.Y, thumbCenterX - track.X, track.Height);
            if (filledRect.Width > 0)
            {
                using var filledBrush = new SolidBrush(TrackFilledColor);
                e.Graphics.FillRectangle(filledBrush, filledRect);
            }

            var thumbRect = new Rectangle(
                thumbCenterX - ThumbWidth / 2,
                (Height - ThumbHeight) / 2,
                ThumbWidth,
                ThumbHeight);
            using var thumbBrush = new SolidBrush(ThumbColor);
            using var thumbPen = new Pen(ThumbBorderColor);
            e.Graphics.FillRectangle(thumbBrush, thumbRect);
            e.Graphics.DrawRectangle(thumbPen, thumbRect.X, thumbRect.Y, thumbRect.Width - 1, thumbRect.Height - 1);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            dragging = true;
            UpdateValueFromMouseX(e.X);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            UpdateValueFromMouseX(e.X);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down)
            {
                Value = currentValue - 1;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up)
            {
                Value = currentValue + 1;
                e.Handled = true;
            }
        }

        private void UpdateValueFromMouseX(int x)
        {
            var trackWidth = Math.Max(1, Width - ThumbWidth);
            var relativeX = Math.Clamp(x - ThumbWidth / 2, 0, trackWidth);
            var ratio = (float)relativeX / trackWidth;
            var newValue = minimum + (int)Math.Round(ratio * (maximum - minimum));
            Value = newValue;
        }
    }
}
