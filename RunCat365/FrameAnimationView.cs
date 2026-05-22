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
    internal sealed class FrameAnimationView : Control
    {
        private readonly System.Threading.Timer timer;
        private IReadOnlyList<Bitmap> frames = [];
        private int currentFrame = 0;
        private int intervalMs = 100;
        private volatile bool running = false;

        internal FrameAnimationView()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw,
                true);
            timer = new System.Threading.Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        internal int FrameIntervalMs
        {
            get => intervalMs;
            set => intervalMs = Math.Max(1, value);
        }

        internal void SetFrames(IReadOnlyList<Bitmap> newFrames)
        {
            frames = newFrames;
            currentFrame = 0;
            Invalidate();
            if (frames.Count > 1)
            {
                running = true;
                timer.Change(intervalMs, Timeout.Infinite);
            }
            else
            {
                running = false;
                timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        internal void Clear()
        {
            running = false;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
            frames = [];
            currentFrame = 0;
            Invalidate();
        }

        private void OnTimerTick(object? state)
        {
            if (!running) return;
            if (IsDisposed || !IsHandleCreated) return;
            try
            {
                BeginInvoke(AdvanceFrame);
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
                return;
            }
            if (running) timer.Change(intervalMs, Timeout.Infinite);
        }

        private void AdvanceFrame()
        {
            if (!running || frames.Count < 2) return;
            currentFrame = (currentFrame + 1) % frames.Count;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            if (frames.Count == 0 || currentFrame >= frames.Count) return;

            var frame = frames[currentFrame];
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

            var scale = Math.Min(
                (float)ClientSize.Width / frame.Width,
                (float)ClientSize.Height / frame.Height);
            var width = (int)(frame.Width * scale);
            var height = (int)(frame.Height * scale);
            var x = (ClientSize.Width - width) / 2;
            var y = (ClientSize.Height - height) / 2;
            e.Graphics.DrawImage(frame, x, y, width, height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                running = false;
                timer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
