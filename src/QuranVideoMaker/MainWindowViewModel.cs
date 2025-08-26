using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using QuranVideoMaker.CustomControls;
using QuranVideoMaker.Data;
using QuranVideoMaker.Dialogs;
using QuranVideoMaker.Undo;
using QuranVideoMaker.Utilities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace QuranVideoMaker
{
    /// <summary>
    /// MainWindowViewModel
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    [Description("MainWindowViewModel")]
    [DisplayName("MainWindowViewModel")]
    [DebuggerDisplay("MainWindowViewModel")]
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private Project _currentProject = new DefaultNewProject();
        private string _projectFilePath;
        private List<RecentProject> _recentProjects;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current project.
        /// </summary>
        public Project CurrentProject
        {
            get { return _currentProject; }
            set
            {
                if (_currentProject != value)
                {
                    _currentProject = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the project file path.
        /// </summary>
        public string ProjectFilePath
        {
            get { return _projectFilePath; }
            set
            {
                if (_projectFilePath != value)
                {
                    _projectFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the application title.
        /// </summary>
        public string Title
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectFilePath))
                {
                    return "Quran Video Maker";
                }
                else
                {
                    return $"Quran Video Maker - {ProjectFilePath}";
                }
            }
        }

        /// <summary>
        /// Gets or sets the recent projects.
        /// </summary>
        public List<RecentProject> RecentProjects
        {
            get { return _recentProjects; }
            set
            {
                if (_recentProjects != value)
                {
                    _recentProjects = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the main window view model instance.
        /// </summary>
        public static MainWindowViewModel Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        public MainWindowViewModel()
        {
            Instance = this;

#if !DEBUG
            Task.Delay(3000).ContinueWith((task) =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    CheckForUpdates();
                }
            });
#endif
        }

        [RelayCommand]
        private void OnNewProject()
        {
            var project = new DefaultNewProject();

            var result = QuranVideoMakerUI.ShowDialog(DialogType.NewProject, project);

            if (result.Result.Value)
            {
                CurrentProject = project;
            }

            UndoEngine.Instance.ClearAll();
        }

        [RelayCommand]
        private void OnOpenProject()
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Quran Video Maker Project|*.qv"
            };

            if (dlg.ShowDialog() == true)
            {
                OpenProject(dlg.FileName);
            }
        }

        [RelayCommand]
        private void OnOpenRecentProject(RecentProject recentProject)
        {
            OpenProject(recentProject.Path);
        }

        [RelayCommand]
        private void OnSaveProject()
        {
            if (string.IsNullOrWhiteSpace(ProjectFilePath))
            {
                OnSaveProjectAs();
            }
            else
            {
                var result = CurrentProject.Save(ProjectFilePath);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message);
                }
            }
        }

        [RelayCommand]
        private void OnSaveProjectAs()
        {
            var dlg = new SaveFileDialog()
            {
                Filter = "Quran Video Maker Project|*.qv"
            };

            if (dlg.ShowDialog() == true)
            {
                var result = CurrentProject.Save(dlg.FileName);

                if (!result.Success)
                {
                    MessageBox.Show(result.Message);
                    return;
                }

                ProjectFilePath = dlg.FileName;

                AddRecentProject(dlg.FileName);

                OnPropertyChanged(nameof(Title));
            }
        }

        [RelayCommand]
        private void OnUndo()
        {
            UndoEngine.Instance.Undo();
        }

        [RelayCommand]
        private void OnRedo()
        {
            UndoEngine.Instance.Redo();
        }

        [RelayCommand]
        private void OnCut()
        {
            CurrentProject.Cut();
        }

        [RelayCommand]
        private void OnCopy()
        {
            CurrentProject.Copy();
        }

        [RelayCommand]
        private void OnPaste()
        {
            CurrentProject.Paste();
        }

        [RelayCommand]
        private void OnSelectAll()
        {
            CurrentProject.SelectAll();
        }

        [RelayCommand]
        private void AddVideoTrack()
        {
            CurrentProject.AddTrack(TimelineTrackType.Video);
        }

        [RelayCommand]
        private void OnExportProject()
        {
            QuranVideoMakerUI.ShowDialog(DialogType.ExportProject, CurrentProject);
        }

        [RelayCommand]
        private async Task ExportAudio()
        {
            var dlg = new SaveFileDialog()
            {
                Filter = "Audio File|*.wav"
            };

            if (dlg.ShowDialog() == true)
            {
                var result = await CurrentProject.ExportAudioAsync(dlg.FileName);

                if (result.Success)
                {
                    System.Windows.MessageBox.Show("Export Complete", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Export Failed.\r\n{result.Message}", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ExportVerses()
        {
            var dlg = new System.Windows.Forms.FolderBrowserDialog();

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var result = await CurrentProject.ExportVersesAsync(dlg.SelectedPath);

                if (result.Success)
                {
                    System.Windows.MessageBox.Show("Export Complete", "Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Windows.MessageBox.Show($"Export Failed.\r\n{result.Message}", "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void OnQuranAdd()
        {
            QuranVideoMakerUI.ShowDialog(DialogType.AddQuran, CurrentProject, false);
        }

        [RelayCommand]
        private void OnFixVerseNumbers()
        {
            CurrentProject.FixVerseNumbers();
        }

        [RelayCommand]
        private void OnRemoveAllVerses()
        {
            var quranTrack = CurrentProject.Tracks.First(x => x.Type == TimelineTrackType.Quran);

            quranTrack.Items.Clear();
        }

        [RelayCommand]
        private void OnQuranSettings()
        {
            QuranVideoMakerUI.ShowDialog(DialogType.QuranSettings, CurrentProject.QuranSettings);
        }

        [RelayCommand]
        private void OnMoveProjectFiles()
        {
            var ofd = new OpenFolderDialog
            {
                Title = "Select the folder to move the project files to",
                Multiselect = false
            };

            if (ofd.ShowDialog() != true)
            {
                return;
            }

            var result = CurrentProject.MoveProjectFiles(ofd.FolderName, ProjectFilePath);

            if (result.Success)
            {
                ProjectFilePath = Path.Combine(ofd.FolderName, Path.GetFileName(ProjectFilePath));
                AddRecentProject(ProjectFilePath);
                OnPropertyChanged(nameof(Title));

                MessageBox.Show("Project files copied to the new location successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(result.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OnProjectSettings()
        {
            QuranVideoMakerUI.ShowDialog(DialogType.ProjectSettings, CurrentProject);
        }

        [RelayCommand]
        private void OnExitApplication()
        {
            Environment.Exit(0);
        }

        [RelayCommand]
        private void OnProjectPlay()
        {
            CurrentProject.Play();
        }

        [RelayCommand]
        private void OnProjectFastForward()
        {
            CurrentProject.FastForward();
        }

        [RelayCommand]
        private void OnProjectRewind()
        {
            CurrentProject.Rewind();
        }

        [RelayCommand]
        private void OnProjectStop()
        {
            CurrentProject.Stop();
        }

        [RelayCommand]
        private void AutoVerse()
        {
            if (CurrentProject.SelectedTool == TimelineSelectedTool.AutoVerse)
            {
                CurrentProject.AutoVerse();
            }
        }

        [RelayCommand]
        private void ResizeVerse()
        {
            if (CurrentProject.SelectedTool == TimelineSelectedTool.VerseResizer)
            {
                var needlePosition = CurrentProject.NeedlePositionTime;

                var item = CurrentProject.GetTrackItemAtFrame(Convert.ToInt32(needlePosition.TotalFrames), TimelineTrackType.Quran);

                CurrentProject.ResizeQuranItem(item, needlePosition.TotalFrames);
            }
        }

        [RelayCommand]
        private void SelectTool(TimelineSelectedTool tool)
        {
            CurrentProject.SelectedTool = tool;
        }

        [RelayCommand]
        private void OnAddMedia()
        {
            var dlg = new OpenFileDialog()
            {
                Multiselect = true,
                Filter = $"Media files|{FileFormats.AllSupportedFormatsOpenFileExtensions}"
            };

            if (dlg.ShowDialog() == true)
            {
                CurrentProject.AddClips(dlg.FileNames);
            }
        }

        [RelayCommand]
        private void OnRemoveMedia()
        {
            // confirm as this will remove the track items in the timeline as well
            if (MessageBox.Show("Are you sure you want to remove the selected media?\r\nThis will also remove the track items in the timeline.", "Remove Media", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            CurrentProject.DeleteSelectedClips();
        }

        [RelayCommand]
        private void OpenQuranImageMaker()
        {
            try
            {
                Process.Start(new ProcessStartInfo("QuranImageMaker.App.exe") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ReportIssue()
        {
            Process.Start(new ProcessStartInfo("https://github.com/rebinf/QuranVideoMaker/issues/new") { UseShellExecute = true });
        }

        [RelayCommand]
        private void GoToWebsite()
        {
            Process.Start(new ProcessStartInfo("https://github.com/rebinf/QuranVideoMaker") { UseShellExecute = true });
        }

        [RelayCommand]
        private void About()
        {
            QuranVideoMakerUI.ShowDialog(DialogType.About);
        }

        private void CheckForUpdates()
        {
            UpdateChecker.CheckForUpdates().ContinueWith((task) =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var result = task.Result;

                    if (result.LatestVersion > UpdateChecker.GetCurrentVersion())
                    {
                        MainWindow.Instance.Dispatcher.Invoke(() =>
                        {
                            if (MessageBox.Show(MainWindow.Instance, "A new version is available. Do you want to go to the download page?", "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo(result.ReleaseInfo.HtmlUrl) { UseShellExecute = true });
                            }
                        });
                    }
                }
            });
        }

        private void OpenProject(string projectFile)
        {
            var result = Project.OpenProject(projectFile);

            if (result.Success)
            {
                CurrentProject = result.Data;
                ProjectFilePath = projectFile;
                OnPropertyChanged(nameof(Title));

                foreach (var clip in CurrentProject.Clips)
                {
                    QuranVideoMakerUI.ShowDialog(DialogType.ClipImport, clip);
                }

                CurrentProject.PreviewCurrentFrame();

                AddRecentProject(projectFile);
            }
            else
            {
                MessageBox.Show(result.Message);
            }

            UndoEngine.Instance.ClearAll();
        }

        public string GetProjectFileHash()
        {
            if (!string.IsNullOrWhiteSpace(ProjectFilePath))
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(ProjectFilePath))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty).ToLower();
                    }
                }
            }

            return string.Empty;
        }

        public void OnLoaded()
        {
            LoadRecentProjects();
        }

        private void AddRecentProject(string projectFile)
        {
            if (RecentProjects.FirstOrDefault(x => x.Path.Equals(projectFile, StringComparison.OrdinalIgnoreCase)) is RecentProject recentProject)
            {
                RecentProjects.Remove(recentProject);
            }

            RecentProjects.Insert(0, new RecentProject(1, projectFile));

            // update the numbers

            for (int i = 0; i < RecentProjects.Count; i++)
            {
                RecentProjects[i].Number = i + 1;
                RecentProjects[i].Name = $"{i + 1} - {RecentProjects[i].Path}";
            }

            SaveRecentProjects();

            OnPropertyChanged(nameof(RecentProjects));
        }

        private void LoadRecentProjects()
        {
            try
            {
                var recentProjectsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuranVideoMaker", "RecentProjects.json");

                if (File.Exists(recentProjectsFilePath))
                {
                    var json = File.ReadAllText(recentProjectsFilePath);
                    var recent = JsonSerializer.Deserialize<List<string>>(json);

                    // remove any non-existing files
                    recent = recent.Where(x => File.Exists(x)).ToList();

                    var recentProjects = new List<RecentProject>();

                    for (int i = 0; i < recent.Count; i++)
                    {
                        recentProjects.Add(new RecentProject(i + 1, recent[i]));
                    }

                    RecentProjects = recentProjects;
                }
                else
                {
                    RecentProjects = new List<RecentProject>();
                }
            }
            catch (Exception ex)
            {
                RecentProjects = new List<RecentProject>();

                // should not bother user with this, but log it for debugging purposes
                Debug.WriteLine($"Error loading recent projects: {ex.Message}");
            }
        }

        private void SaveRecentProjects()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuranVideoMaker");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var recentProjectsFilePath = Path.Combine(dir, "RecentProjects.json");

            var lastTenProjects = RecentProjects.Take(10);

            var lastTenProjectsPaths = lastTenProjects.Select(x => x.Path).ToList();

            var json = JsonSerializer.Serialize(lastTenProjectsPaths);
            File.WriteAllText(recentProjectsFilePath, json);

            RecentProjects = lastTenProjects.ToList();

            OnPropertyChanged(nameof(RecentProjects));
        }

        /// <summary>
        /// Called when public properties changed.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
