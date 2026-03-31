using RunCat365.Properties;

namespace RunCat365
{
    internal class CustomRunnerForm : Form
    {
        private readonly CustomRunnerRepository repository;
        private readonly Action<string> applyCustomRunner;
        private readonly Action revertToBuiltInRunner;
        private readonly ListBox runnerListBox;
        private readonly TextBox nameTextBox;
        private readonly FlowLayoutPanel framePanel;
        private readonly Button addFramesButton;
        private readonly Button saveButton;
        private readonly Button deleteButton;
        private readonly Button useButton;
        private readonly Label requirementsLabel;
        private readonly List<Bitmap> pendingFrames = [];

        internal CustomRunnerForm(
            CustomRunnerRepository repository,
            Action<string> applyCustomRunner,
            Action revertToBuiltInRunner
        )
        {
            this.repository = repository;
            this.applyCustomRunner = applyCustomRunner;
            this.revertToBuiltInRunner = revertToBuiltInRunner;

            Text = Strings.Window_CustomRunners;
            Icon = Resources.AppIcon;
            ClientSize = new Size(580, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(45, 45, 45);
            ForeColor = Color.White;

            var listLabel = new Label
            {
                Text = Strings.CustomRunner_AddedRunners,
                Location = new Point(12, 12),
                Size = new Size(160, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            runnerListBox = new ListBox
            {
                Location = new Point(12, 35),
                Size = new Size(160, 310),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle
            };
            runnerListBox.SelectedIndexChanged += RunnerListBoxSelectedIndexChanged;

            var nameLabel = new Label
            {
                Text = Strings.CustomRunner_Name,
                Location = new Point(190, 12),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 9F)
            };

            nameTextBox = new TextBox
            {
                Location = new Point(290, 10),
                Size = new Size(270, 24),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 30
            };

            requirementsLabel = new Label
            {
                Text = Strings.CustomRunner_Requirements,
                Location = new Point(190, 40),
                Size = new Size(370, 18),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 7.5F)
            };

            addFramesButton = new Button
            {
                Text = Strings.CustomRunner_AddFrames,
                Location = new Point(190, 64),
                Size = new Size(110, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            addFramesButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            addFramesButton.Click += AddFramesButtonClick;

            var clearFramesButton = new Button
            {
                Text = Strings.CustomRunner_ClearFrames,
                Location = new Point(310, 64),
                Size = new Size(90, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand
            };
            clearFramesButton.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
            clearFramesButton.Click += ClearFramesButtonClick;

            framePanel = new FlowLayoutPanel
            {
                Location = new Point(190, 100),
                Size = new Size(370, 245),
                AutoScroll = true,
                BackColor = Color.FromArgb(55, 55, 55),
                BorderStyle = BorderStyle.FixedSingle,
                WrapContents = true
            };

            deleteButton = new Button
            {
                Text = Strings.CustomRunner_Delete,
                Location = new Point(12, 358),
                Size = new Size(75, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(120, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            deleteButton.FlatAppearance.BorderColor = Color.FromArgb(150, 60, 60);
            deleteButton.Click += DeleteButtonClick;

            useButton = new Button
            {
                Text = Strings.CustomRunner_Use,
                Location = new Point(97, 358),
                Size = new Size(75, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 100, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5F),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            useButton.FlatAppearance.BorderColor = Color.FromArgb(60, 130, 80);
            useButton.Click += UseButtonClick;

            saveButton = new Button
            {
                Text = Strings.CustomRunner_Save,
                Location = new Point(475, 358),
                Size = new Size(85, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 90, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderColor = Color.FromArgb(70, 120, 200);
            saveButton.Click += SaveButtonClick;

            Controls.AddRange([
                listLabel,
                runnerListBox,
                nameLabel,
                nameTextBox,
                requirementsLabel,
                addFramesButton,
                clearFramesButton,
                framePanel,
                deleteButton,
                useButton,
                saveButton
            ]);

            RefreshRunnerList();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ClearPendingFrames();
        }

        private void RefreshRunnerList()
        {
            runnerListBox.Items.Clear();
            foreach (var profile in repository.GetAll())
            {
                runnerListBox.Items.Add(profile.Name);
            }
        }

        private void RunnerListBoxSelectedIndexChanged(object? sender, EventArgs e)
        {
            var hasSelection = runnerListBox.SelectedIndex >= 0;
            deleteButton.Enabled = hasSelection;
            useButton.Enabled = hasSelection;

            if (hasSelection && runnerListBox.SelectedItem is string selectedName)
            {
                nameTextBox.Text = selectedName;
                ClearPendingFrames();
                var frames = repository.LoadFrames(selectedName);
                pendingFrames.AddRange(frames);
                RefreshFramePanel();
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
                if (pendingFrames.Count >= 30) break;
                try
                {
                    pendingFrames.Add(new Bitmap(file));
                }
                catch { }
            }
            RefreshFramePanel();
        }

        private void ClearFramesButtonClick(object? sender, EventArgs e)
        {
            ClearPendingFrames();
            RefreshFramePanel();
        }

        private void RefreshFramePanel()
        {
            framePanel.SuspendLayout();
            framePanel.Controls.Clear();

            for (int i = 0; i < pendingFrames.Count; i++)
            {
                var container = new Panel
                {
                    Size = new Size(64, 70),
                    Margin = new Padding(4)
                };

                var pictureBox = new PictureBox
                {
                    Size = new Size(48, 48),
                    Location = new Point(8, 0),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = pendingFrames[i],
                    BackColor = Color.FromArgb(40, 40, 40)
                };

                var indexLabel = new Label
                {
                    Text = $"{i}",
                    Size = new Size(64, 18),
                    Location = new Point(0, 50),
                    TextAlign = ContentAlignment.TopCenter,
                    ForeColor = Color.FromArgb(170, 170, 170),
                    Font = new Font("Segoe UI", 7.5F)
                };

                container.Controls.Add(pictureBox);
                container.Controls.Add(indexLabel);
                framePanel.Controls.Add(container);
            }

            framePanel.ResumeLayout();
        }

        private void SaveButtonClick(object? sender, EventArgs e)
        {
            var name = nameTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                ShowWarning(Strings.CustomRunner_ErrorEmptyName);
                return;
            }

            if (pendingFrames.Count < 2)
            {
                ShowWarning(Strings.CustomRunner_ErrorMinFrames);
                return;
            }

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
            revertToBuiltInRunner();
            ClearPendingFrames();
            RefreshFramePanel();
            nameTextBox.Clear();
            RefreshRunnerList();
        }

        private void UseButtonClick(object? sender, EventArgs e)
        {
            if (runnerListBox.SelectedItem is not string selectedName) return;
            applyCustomRunner(selectedName);
        }

        private void ClearPendingFrames()
        {
            foreach (var frame in pendingFrames)
            {
                frame.Dispose();
            }
            pendingFrames.Clear();
        }

        private static void ShowWarning(string message)
        {
            MessageBox.Show(message, Strings.Message_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
