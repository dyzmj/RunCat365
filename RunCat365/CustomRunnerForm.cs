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

using RunCat365.Properties;
using FormsTimer = System.Windows.Forms.Timer;

namespace RunCat365
{
    internal class CustomRunnerForm : Form
    {
        private const int PREVIEW_INITIAL_INTERVAL_MS = 100;
        private const int PREVIEW_MIN_INTERVAL_MS = 20;
        private const int PREVIEW_BASE_RATE = 500;
        private const int FRAME_NAME_MAX_LENGTH = 30;
        private const int MIN_FRAMES_FOR_SAVE = 2;

        private readonly CustomRunnerRepository repository;
        private readonly Action<string> onCustomRunnerDeleted;
        private readonly string fontFamily;
        private readonly Font tileLabelFont;
        private ListBox runnerListBox = null!;
        private TextBox nameTextBox = null!;
        private Label nameWarningLabel = null!;
        private FlowLayoutPanel framePanel = null!;
        private Button addFramesButton = null!;
        private Button removeFrameButton = null!;
        private Button saveButton = null!;
        private Button deleteButton = null!;
        private FrameAnimationView previewView = null!;
        private TrackBar speedSlider = null!;

        private readonly List<Bitmap> pendingFrames = [];
        private readonly List<Bitmap> recoloredFrames = [];
        private int selectedFrameIndex = -1;
        private Point? dragOriginPoint;
        private int dragSourceIndex = -1;

        internal CustomRunnerForm(
            CustomRunnerRepository repository,
            Action<string> onCustomRunnerDeleted
        )
        {
            this.repository = repository;
            this.onCustomRunnerDeleted = onCustomRunnerDeleted;
            fontFamily = SupportedLanguageExtension.GetCurrentLanguage().GetFontName();
            tileLabelFont = new Font(fontFamily, 7.5F);

            InitializeFormProperties();
            InitializeControls();
            InitializeEventHandlers();

            FitWindowToContent();

            RefreshRunnerList();
            ValidateFormState();
        }

        private void FitWindowToContent()
        {
            if (Controls.Count == 0) return;
            var root = Controls[0];
            root.PerformLayout();
            var preferred = root.PreferredSize;
            ClientSize = new Size(
                preferred.Width + Padding.Horizontal,
                preferred.Height + Padding.Vertical
            );
        }

        private void InitializeFormProperties()
        {
            Text = Strings.Window_CustomRunners;
            Icon = Resources.AppIcon;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Palette.FormBackground;
            ForeColor = Palette.TextPrimary;
            Padding = new Padding(12);
        }

        private void InitializeControls()
        {
            var listSection = BuildListSection();
            var centerSection = BuildCenterSection();
            var previewSection = BuildPreviewSection();
            var bottomBar = BuildBottomBar();

            var topContent = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };
            topContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topContent.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listSection.Margin = new Padding(0, 0, 16, 0);
            centerSection.Margin = new Padding(0, 0, 16, 0);
            previewSection.Margin = new Padding(0);
            topContent.Controls.Add(listSection, 0, 0);
            topContent.Controls.Add(centerSection, 1, 0);
            topContent.Controls.Add(previewSection, 2, 0);

            var root = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Location = new Point(0, 0)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bottomBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            bottomBar.Margin = new Padding(0);
            root.Controls.Add(topContent, 0, 0);
            root.Controls.Add(bottomBar, 0, 1);

            Controls.Add(root);
        }

        private TableLayoutPanel BuildListSection()
        {
            var listLabel = CreateLabel(
                Strings.CustomRunner_AddedRunners,
                9F, FontStyle.Bold, Palette.TextSecondary
            );
            listLabel.Margin = new Padding(0, 0, 0, 4);

            runnerListBox = new ListBox
            {
                Size = new Size(160, 391),
                MinimumSize = new Size(160, 391),
                BackColor = Palette.ControlBackground,
                ForeColor = Palette.TextPrimary,
                Font = new Font(fontFamily, 9F),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                Margin = new Padding(0)
            };

            var section = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            section.Controls.Add(listLabel, 0, 0);
            section.Controls.Add(runnerListBox, 0, 1);
            return section;
        }

        private Label CreateLabel(string text, float fontSize, FontStyle fontStyle, Color foreColor)
        {
            var font = new Font(fontFamily, fontSize, fontStyle);
            var measured = TextRenderer.MeasureText(text, font);
            return new Label
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(measured.Width + 2, 0),
                Font = font,
                ForeColor = foreColor
            };
        }

        private TableLayoutPanel BuildCenterSection()
        {
            var nameLabel = CreateLabel(
                Strings.CustomRunner_Name,
                9F, FontStyle.Regular, Palette.TextSecondary
            );
            nameLabel.Margin = new Padding(0, 6, 8, 0);
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;

            nameTextBox = new TextBox
            {
                Size = new Size(280, 24),
                BackColor = Palette.ControlBackground,
                ForeColor = Palette.TextPrimary,
                Font = new Font(fontFamily, 9F),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = FRAME_NAME_MAX_LENGTH,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0)
            };

            var nameRow = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 0, 8)
            };
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            nameRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            nameRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            nameRow.Controls.Add(nameLabel, 0, 0);
            nameRow.Controls.Add(nameTextBox, 1, 0);

            var requirementsLabel = CreateLabel(
                Strings.CustomRunner_Requirements,
                7.5F, FontStyle.Regular, Palette.TextMuted
            );
            requirementsLabel.Margin = new Padding(0, 0, 0, 4);

            nameWarningLabel = CreateLabel(
                Strings.CustomRunner_OverwriteWarning,
                7.5F, FontStyle.Regular, Palette.WarningOrange
            );
            nameWarningLabel.Anchor = AnchorStyles.Right;
            nameWarningLabel.Visible = false;
            nameWarningLabel.Margin = new Padding(0, 0, 0, 8);

            addFramesButton = CreateActionButton(
                Strings.CustomRunner_AddFrames,
                new Size(110, 28)
            );
            addFramesButton.Margin = new Padding(0, 0, 8, 0);

            removeFrameButton = CreateActionButton(
                Strings.CustomRunner_RemoveFrame,
                new Size(110, 28),
                initiallyEnabled: false
            );
            removeFrameButton.Margin = new Padding(0);

            var actionButtonRow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            actionButtonRow.Controls.Add(addFramesButton);
            actionButtonRow.Controls.Add(removeFrameButton);

            framePanel = new FlowLayoutPanel
            {
                Size = new Size(350, 312),
                AutoScroll = true,
                BackColor = Palette.FramePanelBackground,
                BorderStyle = BorderStyle.FixedSingle,
                WrapContents = true,
                AllowDrop = true,
                Margin = new Padding(0)
            };
            framePanel.DragEnter += FrameDragOver;
            framePanel.DragOver += FrameDragOver;
            framePanel.DragDrop += FramePanelDragDrop;

            var section = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 5; i++)
            {
                section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            section.Controls.Add(nameRow, 0, 0);
            section.Controls.Add(requirementsLabel, 0, 1);
            section.Controls.Add(nameWarningLabel, 0, 2);
            section.Controls.Add(actionButtonRow, 0, 3);
            section.Controls.Add(framePanel, 0, 4);
            return section;
        }

        private TableLayoutPanel BuildPreviewSection()
        {
            var previewLabel = CreateLabel(
                Strings.CustomRunner_Preview,
                8.5F, FontStyle.Regular, Palette.TextSecondary
            );
            previewLabel.Margin = new Padding(0, 0, 0, 4);

            previewView = new FrameAnimationView
            {
                Size = new Size(142, 142),
                BackColor = Palette.FramePanelBackground,
                FrameIntervalMs = PREVIEW_INITIAL_INTERVAL_MS,
                Margin = new Padding(0, 0, 0, 12)
            };

            var speedLabel = CreateLabel(
                Strings.CustomRunner_PreviewSpeed,
                8.5F, FontStyle.Regular, Palette.TextSecondary
            );
            speedLabel.Margin = new Padding(0, 0, 0, 4);

            speedSlider = new TrackBar
            {
                Size = new Size(142, 45),
                Minimum = 1,
                Maximum = 10,
                Value = 5,
                TickStyle = TickStyle.None,
                AutoSize = false,
                Margin = new Padding(0)
            };

            var section = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            section.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int i = 0; i < 4; i++)
            {
                section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            section.Controls.Add(previewLabel, 0, 0);
            section.Controls.Add(previewView, 0, 1);
            section.Controls.Add(speedLabel, 0, 2);
            section.Controls.Add(speedSlider, 0, 3);
            return section;
        }

        private TableLayoutPanel BuildBottomBar()
        {
            deleteButton = CreateColoredButton(
                Strings.CustomRunner_Delete,
                new Size(75, 30),
                Palette.DangerRed,
                Palette.DangerBorder,
                bold: false,
                initiallyEnabled: false
            );
            deleteButton.Margin = new Padding(0);

            saveButton = CreateColoredButton(
                Strings.CustomRunner_Save,
                new Size(85, 30),
                Palette.AccentBlue,
                Palette.AccentBlueBorder,
                bold: true,
                initiallyEnabled: false
            );
            saveButton.Margin = new Padding(0);
            saveButton.Anchor = AnchorStyles.Right;

            var bar = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bar.Controls.Add(deleteButton, 0, 0);
            bar.Controls.Add(saveButton, 2, 0);
            return bar;
        }

        private void InitializeEventHandlers()
        {
            runnerListBox.SelectedIndexChanged += RunnerListBoxSelectedIndexChanged;
            nameTextBox.TextChanged += (sender, e) => ValidateFormState();

            addFramesButton.Click += AddFramesButtonClick;
            removeFrameButton.Click += RemoveFrameButtonClick;

            speedSlider.ValueChanged += (sender, e) =>
            {
                if (speedSlider.Value > 0)
                {
                    previewView.FrameIntervalMs = Math.Max(PREVIEW_MIN_INTERVAL_MS, PREVIEW_BASE_RATE / speedSlider.Value);
                }
            };

            deleteButton.Click += DeleteButtonClick;
            saveButton.Click += SaveButtonClick;
        }

        private ThemedButton CreateActionButton(string text, Size minimumSize, bool initiallyEnabled = true)
        {
            var button = new ThemedButton
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = minimumSize,
                Padding = new Padding(10, 4, 10, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = Palette.ButtonBackground,
                ForeColor = Palette.TextPrimary,
                Font = new Font(fontFamily, 8.5F),
                Cursor = Cursors.Hand,
                Enabled = initiallyEnabled
            };
            button.FlatAppearance.BorderColor = Palette.BorderAccent;
            return button;
        }

        private ThemedButton CreateColoredButton(
            string text,
            Size minimumSize,
            Color backColor,
            Color borderColor,
            bool bold,
            bool initiallyEnabled
        )
        {
            var button = new ThemedButton
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = minimumSize,
                Padding = new Padding(14, 4, 14, 4),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Palette.TextPrimary,
                Font = new Font(fontFamily, bold ? 9F : 8.5F, bold ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand,
                Enabled = initiallyEnabled
            };
            button.FlatAppearance.BorderColor = borderColor;
            return button;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ClearPendingFrames();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var frame in pendingFrames) frame.Dispose();
                pendingFrames.Clear();
                foreach (var frame in recoloredFrames) frame.Dispose();
                recoloredFrames.Clear();
                tileLabelFont.Dispose();
            }
            base.Dispose(disposing);
        }

        private void RefreshRunnerList()
        {
            runnerListBox.Items.Clear();
            foreach (var profile in repository.GetAll())
            {
                runnerListBox.Items.Add(profile.Name);
            }
            UpdateRunnerActionButtons();
        }

        private void RunnerListBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = runnerListBox.SelectedIndex >= 0;
            UpdateRunnerActionButtons();

            if (hasSelection && runnerListBox.SelectedItem is string selectedName)
            {
                nameTextBox.Text = selectedName;
                ClearPendingFrames();
                var frames = repository.LoadFrames(selectedName);
                pendingFrames.AddRange(frames);
                selectedFrameIndex = pendingFrames.Count > 0 ? 0 : -1;
                OnFramesChanged();
            }
        }

        private void AddFramesButtonClick(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "PNG Images|*.png",
                Multiselect = true,
                Title = Strings.CustomRunner_AddFrames
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            var sortedFiles = dialog.FileNames.OrderBy(f => f).ToArray();
            foreach (var file in sortedFiles)
            {
                if (pendingFrames.Count >= CustomRunnerRepository.MAX_FRAME_COUNT) break;
                try
                {
                    pendingFrames.Add(new Bitmap(file));
                }
                catch (Exception ex) when (ex is OutOfMemoryException or ArgumentException or FileNotFoundException)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load frame '{file}': {ex.Message}");
                }
            }
            OnFramesChanged();
        }

        private void RemoveFrameButtonClick(object? sender, EventArgs e)
        {
            if (selectedFrameIndex < 0 || pendingFrames.Count <= selectedFrameIndex) return;

            pendingFrames[selectedFrameIndex].Dispose();
            pendingFrames.RemoveAt(selectedFrameIndex);
            selectedFrameIndex = pendingFrames.Count == 0
                ? -1
                : Math.Min(selectedFrameIndex, pendingFrames.Count - 1);
            OnFramesChanged();
        }

        private void OnFramesChanged()
        {
            RebuildRecoloredFrames();
            RebuildFrameGrid();
            RestartPreview();
            ValidateFormState();
        }

        private void RebuildRecoloredFrames()
        {
            foreach (var frame in recoloredFrames) frame.Dispose();
            recoloredFrames.Clear();
            foreach (var frame in pendingFrames)
            {
                recoloredFrames.Add(frame.Recolor(Color.White));
            }
            if (pendingFrames.Count <= selectedFrameIndex)
            {
                selectedFrameIndex = pendingFrames.Count - 1;
            }
        }

        private void RebuildFrameGrid()
        {
            framePanel.SuspendLayout();
            foreach (Control oldTile in framePanel.Controls)
            {
                oldTile.Dispose();
            }
            framePanel.Controls.Clear();

            for (int i = 0; i < recoloredFrames.Count; i++)
            {
                framePanel.Controls.Add(CreateFrameTile(i));
            }

            framePanel.ResumeLayout();
        }

        private Panel CreateFrameTile(int frameIndex)
        {
            var container = new Panel
            {
                Size = new Size(70, 76),
                Margin = new Padding(4),
                BackColor = frameIndex == selectedFrameIndex ? Palette.SelectionHighlight : Color.Transparent,
                Cursor = Cursors.SizeAll
            };

            var pictureBox = new PictureBox
            {
                Size = new Size(48, 48),
                Location = new Point(11, 0),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = recoloredFrames[frameIndex],
                BackColor = Palette.FrameThumbnailBackground,
                Cursor = Cursors.SizeAll
            };

            var indexLabel = new Label
            {
                Text = $"{frameIndex}",
                Size = new Size(70, 18),
                Location = new Point(0, 52),
                TextAlign = ContentAlignment.TopCenter,
                ForeColor = Palette.TextThumbnailIndex,
                Font = tileLabelFont,
                Cursor = Cursors.SizeAll
            };

            foreach (var control in new Control[] { container, pictureBox, indexLabel })
            {
                control.Click += (sender, e) => SelectFrame(frameIndex);
                WireFrameDragSource(control, frameIndex);
                WireFrameDragReceiver(control, frameIndex);
            }

            container.Controls.Add(pictureBox);
            container.Controls.Add(indexLabel);
            return container;
        }

        private void WireFrameDragSource(Control control, int frameIndex)
        {
            control.MouseDown += (sender, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                dragOriginPoint = e.Location;
                dragSourceIndex = frameIndex;
            };
            control.MouseMove += (sender, e) =>
            {
                if (dragOriginPoint is null || dragSourceIndex < 0) return;
                if (e.Button != MouseButtons.Left) return;
                var dx = Math.Abs(e.X - dragOriginPoint.Value.X);
                var dy = Math.Abs(e.Y - dragOriginPoint.Value.Y);
                if (dx < SystemInformation.DragSize.Width && dy < SystemInformation.DragSize.Height) return;
                var startedIndex = dragSourceIndex;
                dragOriginPoint = null;
                dragSourceIndex = -1;
                control.DoDragDrop(startedIndex, DragDropEffects.Move);
            };
            control.MouseUp += (sender, e) =>
            {
                dragOriginPoint = null;
                dragSourceIndex = -1;
            };
        }

        private void WireFrameDragReceiver(Control control, int frameIndex)
        {
            control.AllowDrop = true;
            control.DragEnter += FrameDragOver;
            control.DragOver += FrameDragOver;
            control.DragDrop += (sender, e) => HandleFrameDrop(e, frameIndex);
        }

        private static void FrameDragOver(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(typeof(int)) == true
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void HandleFrameDrop(DragEventArgs e, int targetIndex)
        {
            if (e.Data?.GetData(typeof(int)) is not int sourceIndex) return;
            ReorderFrame(sourceIndex, targetIndex);
        }

        private void FramePanelDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(typeof(int)) is not int sourceIndex) return;
            ReorderFrame(sourceIndex, pendingFrames.Count - 1);
        }

        private void ReorderFrame(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= pendingFrames.Count) return;
            if (sourceIndex == targetIndex) return;

            var item = pendingFrames[sourceIndex];
            pendingFrames.RemoveAt(sourceIndex);

            var insertAt = sourceIndex < targetIndex ? targetIndex - 1 : targetIndex;
            insertAt = Math.Clamp(insertAt, 0, pendingFrames.Count);
            pendingFrames.Insert(insertAt, item);

            selectedFrameIndex = insertAt;
            OnFramesChanged();
        }

        private void RestartPreview()
        {
            previewView.FrameIntervalMs = Math.Max(PREVIEW_MIN_INTERVAL_MS, PREVIEW_BASE_RATE / speedSlider.Value);
            previewView.SetFrames(recoloredFrames);
        }

        private void SelectFrame(int frameIndex)
        {
            if (frameIndex < 0 || pendingFrames.Count <= frameIndex) return;
            selectedFrameIndex = frameIndex;
            RebuildFrameGrid();
            UpdateFrameActionButtons();
        }

        private void ValidateFormState()
        {
            var name = nameTextBox.Text.Trim();
            bool hasValidName = !string.IsNullOrWhiteSpace(name);
            bool hasValidFrames = pendingFrames.Count >= MIN_FRAMES_FOR_SAVE;

            saveButton.Enabled = hasValidName && hasValidFrames;

            nameWarningLabel.Visible = hasValidName && repository.Exists(name);
            UpdateFrameActionButtons();
        }

        private void UpdateFrameActionButtons()
        {
            var hasSelectedFrame = 0 <= selectedFrameIndex && selectedFrameIndex < pendingFrames.Count;
            removeFrameButton.Enabled = hasSelectedFrame;
        }

        private void UpdateRunnerActionButtons()
        {
            deleteButton.Enabled = runnerListBox.SelectedIndex >= 0;
        }

        private void SaveButtonClick(object? sender, EventArgs e)
        {
            var name = nameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;
            if (pendingFrames.Count < MIN_FRAMES_FOR_SAVE) return;

            if (repository.Exists(name))
            {
                var result = MessageBox.Show(
                    Strings.CustomRunner_ConfirmOverwrite,
                    Strings.Message_Warning,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (result != DialogResult.Yes) return;
            }

            if (repository.Save(name, pendingFrames))
            {
                RefreshRunnerList();
                for (int i = 0; i < runnerListBox.Items.Count; i++)
                {
                    if (runnerListBox.Items[i] is string itemName &&
                        itemName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        runnerListBox.SelectedIndex = i;
                        break;
                    }
                }
                ValidateFormState();
            }
        }

        private void DeleteButtonClick(object? sender, EventArgs e)
        {
            if (runnerListBox.SelectedItem is not string selectedName) return;

            var result = MessageBox.Show(
                string.Format(Strings.CustomRunner_ConfirmDelete, selectedName),
                Strings.Message_Warning,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );
            if (result != DialogResult.Yes) return;

            repository.Delete(selectedName);
            onCustomRunnerDeleted(selectedName);
            ClearPendingFrames();
            OnFramesChanged();
            nameTextBox.Clear();
            RefreshRunnerList();
            UpdateRunnerActionButtons();
        }

        private void ClearPendingFrames()
        {
            previewView.Clear();
            foreach (var frame in pendingFrames) frame.Dispose();
            pendingFrames.Clear();
            foreach (var frame in recoloredFrames) frame.Dispose();
            recoloredFrames.Clear();
            selectedFrameIndex = -1;
            UpdateFrameActionButtons();
        }

        private static class Palette
        {
            internal static readonly Color FormBackground = Color.FromArgb(45, 45, 45);
            internal static readonly Color ControlBackground = Color.FromArgb(60, 60, 60);
            internal static readonly Color ButtonBackground = Color.FromArgb(70, 70, 70);
            internal static readonly Color BorderAccent = Color.FromArgb(100, 100, 100);
            internal static readonly Color FramePanelBackground = Color.FromArgb(55, 55, 55);
            internal static readonly Color FrameThumbnailBackground = Color.FromArgb(40, 40, 40);
            internal static readonly Color SelectionHighlight = Color.FromArgb(75, 95, 125);
            internal static readonly Color TextPrimary = Color.White;
            internal static readonly Color TextSecondary = Color.FromArgb(200, 200, 200);
            internal static readonly Color TextMuted = Color.FromArgb(150, 150, 150);
            internal static readonly Color TextThumbnailIndex = Color.FromArgb(170, 170, 170);
            internal static readonly Color WarningOrange = Color.FromArgb(220, 160, 60);
            internal static readonly Color DangerRed = Color.FromArgb(120, 40, 40);
            internal static readonly Color DangerBorder = Color.FromArgb(150, 60, 60);
            internal static readonly Color AccentBlue = Color.FromArgb(50, 90, 160);
            internal static readonly Color AccentBlueBorder = Color.FromArgb(70, 120, 200);
        }

        private sealed class ThemedButton : Button
        {
            private static readonly Color DisabledBackColor = Color.FromArgb(58, 58, 58);
            private static readonly Color DisabledForeColor = Color.FromArgb(145, 145, 145);
            private static readonly Color DisabledBorderColor = Color.FromArgb(82, 82, 82);

            protected override void OnEnabledChanged(EventArgs e)
            {
                base.OnEnabledChanged(e);
                Cursor = Enabled ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var backColor = Enabled ? BackColor : DisabledBackColor;
                var foreColor = Enabled ? ForeColor : DisabledForeColor;
                var borderColor = Enabled ? FlatAppearance.BorderColor : DisabledBorderColor;

                using var backBrush = new SolidBrush(backColor);
                using var borderPen = new Pen(borderColor);
                e.Graphics.FillRectangle(backBrush, ClientRectangle);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    ClientRectangle,
                    foreColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                );
            }
        }
    }
}
