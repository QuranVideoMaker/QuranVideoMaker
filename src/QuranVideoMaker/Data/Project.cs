using FFMpegCore;
using FFMpegCore.Enums;
using NAudio.Wave;
using QuranImageMaker;
using QuranVideoMaker.ClipboardData;
using QuranVideoMaker.CustomControls;
using QuranVideoMaker.Dialogs;
using QuranVideoMaker.Extensions;
using QuranVideoMaker.Serialization;
using QuranVideoMaker.Undo;
using QuranVideoMaker.Undo.UndoUnits;
using QuranVideoMaker.Utilities;
using SkiaSharp;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Timers;
using System.Windows;

namespace QuranVideoMaker.Data
{
    /// <summary>
    /// Project
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    [Description("Project")]
    [DisplayName("Project")]
    [DebuggerDisplay("Project")]
    public class Project : INotifyPropertyChanged, IJsonOnDeserialized
    {
        private string _id = Guid.NewGuid().ToString().Replace("-", string.Empty).ToLower();
        private ConcurrentDictionary<string, SKBitmap> _renderedVerses = new ConcurrentDictionary<string, SKBitmap>();

        private int _previewWidth;
        private int _previewHeight;
        private int _chapter;
        private int _verseFrom;
        private int _verseTo;
        private double _fps = 25;
        private int _timelineZoom = 8;
        private int _trackHeadersWidth = 100;
        private TimeCode _needlePositionTime = new TimeCode(0, 25);
        private ObservableCollection<TimelineTrack> _tracks;
        private ObservableCollection<IProjectClip> _clips = new ObservableCollection<IProjectClip>();
        private string _exportDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        private string _exportFormat = "mp4";
        private bool _exportIncludeAlphaChannel;
        private int _exportThreads = -1;
        private HardwareAccelerationDevice _hardwareAcceleration = HardwareAccelerationDevice.Auto;
        private Speed _encodingSpeed = Speed.Medium;
        private QuranRenderSettings _quranSettings = new QuranRenderSettings();
        private byte[] _currentPreviewFrame;
        private bool _isPlaying;
        private TimelineSelectedTool _selectedTool;

        private System.Timers.Timer _playTimer = new System.Timers.Timer() { AutoReset = true, Enabled = false };

        private int _framesPerBatch = 10;
        private Guid _previewRenderedTranslation = Guid.Empty;
        private Guid _exportRenderedTranslation;

        public event EventHandler<double> ExportProgress;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when needle position time changed (usually from UI).
        /// </summary>
        public event EventHandler<TimeCode> NeedlePositionTimeChanged;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public string Id
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
        /// Gets or sets the width of the track headers.
        /// </summary>
        public int TrackHeadersWidth
        {
            get { return _trackHeadersWidth; }
            set
            {
                if (_trackHeadersWidth != value)
                {
                    _trackHeadersWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Chapter
        /// </summary>
        public int Chapter
        {
            get { return _chapter; }
            set
            {
                if (_chapter != value)
                {
                    _chapter = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// From verse number
        /// </summary>
        public int VerseFrom
        {
            get { return _verseFrom; }
            set
            {
                if (_verseFrom != value)
                {
                    _verseFrom = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// To verse number
        /// </summary>
        public int VerseTo
        {
            get { return _verseTo; }
            set
            {
                if (_verseTo != value)
                {
                    _verseTo = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the FPS.
        /// </summary>
        public double FPS
        {
            get { return _fps; }
            set
            {
                if (_fps != value)
                {
                    _fps = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the zoom.
        /// </summary>
        public int TimelineZoom
        {
            get { return _timelineZoom; }
            set
            {
                if (_timelineZoom != value)
                {
                    _timelineZoom = Math.Clamp(value, 1, Constants.TimelineZoomMax);
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the needle position.
        /// </summary>
        public TimeCode NeedlePositionTime
        {
            get { return _needlePositionTime; }
            set
            {
                if (_needlePositionTime != value)
                {
                    _needlePositionTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the tracks.
        /// </summary>
        public ObservableCollection<TimelineTrack> Tracks
        {
            get { return _tracks ??= new ObservableCollection<TimelineTrack>(); }
            set
            {
                if (_tracks != value)
                {
                    _tracks = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the clips.
        /// </summary>
        public ObservableCollection<IProjectClip> Clips
        {
            get { return _clips; }
            set
            {
                if (_clips != value)
                {
                    _clips = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        public int Width
        {
            get { return QuranSettings.ImageWidth; }
            set
            {
                QuranSettings.ImageWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        public int Height
        {
            get { return QuranSettings.ImageHeight; }
            set
            {
                QuranSettings.ImageHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the width of the preview.
        /// </summary>
        public int PreviewWidth
        {
            get { return _previewWidth; }
            set
            {
                if (_previewWidth != value)
                {
                    _previewWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preview.
        /// </summary>
        public int PreviewHeight
        {
            get { return _previewHeight; }
            set
            {
                if (_previewHeight != value)
                {
                    _previewHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the export directory.
        /// </summary>
        public string ExportDirectory
        {
            get { return _exportDirectory; }
            set
            {
                if (_exportDirectory != value)
                {
                    _exportDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the export format.
        /// </summary>
        public string ExportFormat
        {
            get { return _exportFormat; }
            set
            {
                if (_exportFormat != value)
                {
                    _exportFormat = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether export includes alpha channel.
        /// </summary>
        public bool ExportIncludeAlphaChannel
        {
            get { return _exportIncludeAlphaChannel; }
            set
            {
                if (_exportIncludeAlphaChannel != value)
                {
                    _exportIncludeAlphaChannel = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the export threads, i.e how many threads to use for exporting.
        public int ExportThreads
        {
            get { return _exportThreads; }
            set
            {
                if (_exportThreads != value)
                {
                    // cannot be less than -1
                    if (value < -1)
                    {
                        value = -1;
                    }

                    if (value == 0)
                    {
                        value = -1;
                    }

                    _exportThreads = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the hardware acceleration.
        /// </summary>
        public HardwareAccelerationDevice HardwareAcceleration
        {
            get { return _hardwareAcceleration; }
            set
            {
                if (_hardwareAcceleration != value)
                {
                    _hardwareAcceleration = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the encoding speed.
        /// </summary>
        public Speed EncodingSpeed
        {
            get { return _encodingSpeed; }
            set
            {
                if (_encodingSpeed != value)
                {
                    _encodingSpeed = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Quran settings.
        /// </summary>
        public QuranRenderSettings QuranSettings
        {
            get { return _quranSettings ??= new QuranRenderSettings(); }
            set
            {
                if (_quranSettings != value)
                {
                    _quranSettings = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets how many frames to render per batch.
        /// </summary>
        public int FramesPerBatch
        {
            get { return _framesPerBatch; }
            set
            {
                if (_framesPerBatch != value)
                {
                    _framesPerBatch = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the custom colors.
        /// </summary>
        public int[] CustomColors { get; set; }

        public TimelineTrack QuranTrack { get { return Tracks.FirstOrDefault(x => x.Type == TimelineTrackType.Quran); } }

        public IEnumerable<QuranTrackItem> OrderedVerses { get { return Tracks.FirstOrDefault(x => x.Type == TimelineTrackType.Quran)?.Items.Cast<QuranTrackItem>().OrderBy(x => x.Verse.VerseNumber); } }

        /// <summary>
        /// Gets or sets the preview rendered translation.
        /// </summary>
        public Guid PreviewRenderedTranslation
        {
            get { return _previewRenderedTranslation; }
            set
            {
                if (_previewRenderedTranslation != value)
                {
                    _previewRenderedTranslation = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the export rendered translation.
        /// </summary>
        public Guid ExportRenderedTranslation
        {
            get { return _exportRenderedTranslation; }
            set
            {
                if (_exportRenderedTranslation != value)
                {
                    _exportRenderedTranslation = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current image.
        /// </summary>
        [JsonIgnore]
        public byte[] CurrentPreviewFrame
        {
            get { return _currentPreviewFrame; }
            set
            {
                if (_currentPreviewFrame != value)
                {
                    _currentPreviewFrame = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public TimelineSelectedTool SelectedTool
        {
            get { return _selectedTool; }
            set
            {
                if (_selectedTool != value)
                {
                    _selectedTool = value;
                    OnPropertyChanged();
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<RenderedTranslation> PreviewRenderedTranslations
        {
            get
            {
                var items = QuranSettings.TranslationRenderSettings.Select(x => new RenderedTranslation() { Guid = x.Id, Name = $"{Quran.GetTranslation(x.Id).Language} - {Quran.GetTranslation(x.Id).Name}" });

                // add 'All' option
                return new RenderedTranslation[] { new RenderedTranslation() { Guid = QuranIds.AllTranslations, Name = "All Translations" } }.Concat(items);
            }
        }

        [JsonIgnore]
        public IEnumerable<RenderedTranslation> ExportRenderedTranslations
        {
            get
            {
                var items = PreviewRenderedTranslations;

                // add 'Separate Videos' as the second option
                return new RenderedTranslation[] { items.First(), new RenderedTranslation() { Guid = QuranIds.SeparateTranslations, Name = "Separate Videos" } }.Concat(items.Skip(1));
            }
        }

        [JsonIgnore]
        public TimeSpan ExportProgressTime { get; set; } = TimeSpan.Zero;

        [JsonIgnore]
        public string ExportProgressMessage { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Project"/> class.
        /// </summary>
        public Project()
        {
            _playTimer.Elapsed += PlayTimer_Elapsed;
            HookEvents();
        }

        /// <summary>
        /// Cuts the item at the specified frame.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="frame">The frame.</param>
        public void CutItem(TrackItem item, double frame)
        {
            var undoData = new MultipleUndoUnits("Cut Item");

            var resizeUndoUnit = new TrackItemResizeUndoUnit();
            undoData.UndoUnits.Add(resizeUndoUnit);
            var firstItemData = new TrackItemResizeData(item, item.Position, item.Start, item.End);

            resizeUndoUnit.Items.Add(firstItemData);

            var cutFrame = frame - item.Position.TotalFrames;
            var oldEnd = item.End;
            item.End = new TimeCode(item.Start.TotalFrames + cutFrame, FPS);
            var track = Tracks.First(x => x.Items.Contains(item));

            firstItemData.NewEnd = item.End;

            var newItemPosition = new TimeCode(frame, FPS);
            var newItemStart = item.End;

            if (item.Type == TrackItemType.Quran)
            {
                var newItem = new QuranTrackItem
                {
                    ClipId = item.ClipId,
                    Name = item.Name,
                    Thumbnail = item.Thumbnail,
                    Position = newItemPosition,
                    Start = new TimeCode(),
                    End = oldEnd - item.End,
                    Verse = (item as QuranTrackItem).Verse.Clone(),
                    FadeInFrame = item.FadeInFrame,
                    FadeOutFrame = item.FadeOutFrame,
                };

                track.Items.Add(newItem);

                var secondItemData = new TrackItemAddUndoUnit(track, newItem);

                undoData.UndoUnits.Add(secondItemData);
            }
            else if (item.Type == TrackItemType.Audio)
            {
                var clip = Clips.FirstOrDefault(x => x.Id == item.ClipId);
                var newItem = new AudioTrackItem(clip, newItemPosition, newItemStart, oldEnd);
                track.Items.Add(newItem);
                var secondItemData = new TrackItemAddUndoUnit(track, newItem);

                undoData.UndoUnits.Add(secondItemData);
            }
            else
            {
                var newItem = new TrackItem(item.Type, item.UnlimitedSourceLength, item.ClipId, item.Name, item.Thumbnail, newItemPosition, item.SourceLength, newItemStart, oldEnd);

                track.Items.Add(newItem);

                var secondItemData = new TrackItemAddUndoUnit(track, newItem);

                undoData.UndoUnits.Add(secondItemData);
            }

            UndoEngine.Instance.AddUndoUnit(undoData);
        }

        /// <summary>
        /// Cuts the item at the specified frame.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="frame">The frame.</param>
        public void ResizeQuranItem(ITrackItem item, double frame)
        {
            if (item == null)
            {
                return;
            }

            if (item.Type == TrackItemType.Quran)
            {
                var undoData = new TrackItemResizeUndoUnit();
                var firstItemData = new TrackItemResizeData(item, item.Position, item.Start, item.End);

                undoData.Items.Add(firstItemData);

                var cutFrame = frame - item.Position.TotalFrames;
                var oldEnd = item.End;
                item.End = new TimeCode(item.Start.TotalFrames + cutFrame, FPS);
                var track = Tracks.First(x => x.Items.Contains(item));

                firstItemData.NewEnd = item.End;

                var newItemPosition = new TimeCode(frame, FPS);
                var newItemStart = item.End;

                if (Tracks.First(x => x.Type == TimelineTrackType.Quran).Items.Cast<QuranTrackItem>().FirstOrDefault(x => x.Verse.VerseNumber == (item as QuranTrackItem).Verse.VerseNumber + 1) is QuranTrackItem right)
                {
                    var secondItemData = new TrackItemResizeData(right, right.Position, right.Start, right.End);
                    undoData.Items.Add(secondItemData);

                    var oldRight = right.GetRightTime().TotalFrames;
                    right.Position = newItemPosition;
                    right.End = new TimeCode(oldRight - newItemPosition.TotalFrames, FPS);

                    secondItemData.NewPosition = right.Position;
                    secondItemData.NewEnd = right.End;
                }

                UndoEngine.Instance.AddUndoUnit(undoData);
            }

            ClearVerseRenderCache();
        }

        public void AutoVerse()
        {
            var quranTrack = Tracks.First(x => x.Type == TimelineTrackType.Quran);
            var items = quranTrack.Items.Cast<QuranTrackItem>();
            var last = items.OrderByDescending(x => x.Verse.VerseNumber).First();

            last.End = new TimeCode(NeedlePositionTime.TotalFrames - last.Position.TotalFrames, FPS);

            var verse = Quran.QuranScript.FirstOrDefault(x => x.ChapterNumber == last.Verse.ChapterNumber && x.VerseNumber == last.Verse.VerseNumber + 1);

            //if there is no next verse, we are done
            if (verse.ChapterNumber == 0 && verse.VerseNumber == 0)
            {
                return;
            }

            var verseInfo = new VerseInfo(QuranIds.Quran, verse.ChapterNumber, verse.VerseNumber, verse.VerseText);

            if (QuranSettings.IncludeVerseNumbers && verseInfo.VerseNumber != 0)
            {
                verseInfo.VerseText = $"{verseInfo.VerseText} {Quran.ToArabicNumbers(verseInfo.VerseNumber)}";
            }

            var fadeInFrame = 0d;
            var fadeOutFrame = 0d;

            // fade frame based on the last item
            if (quranTrack.Items.LastOrDefault() is ITrackItem prevItem)
            {
                fadeInFrame = prevItem.FadeInFrame;
                fadeOutFrame = prevItem.FadeOutFrame;
            }

            var newItem = new QuranTrackItem()
            {
                UnlimitedSourceLength = true,
                Name = $"{verseInfo.ChapterNumber}:{verseInfo.VerseNumber}.{verseInfo.VersePart}",
                Verse = verseInfo,
                //SourceLength = TimeCode.FromSeconds(30, FPS),
                Start = new TimeCode(0, FPS),
                End = TimeCode.FromSeconds(30, FPS),
                Position = last.GetRightTime(),
                FadeInFrame = fadeInFrame,
                FadeOutFrame = fadeOutFrame,
            };

            foreach (var translation in QuranSettings.TranslationRenderSettings)
            {
                var translationInfo = Quran.Translations.First(x => x.Id == translation.Id);
                var translatedVerse = translationInfo.GetVerse(verse.ChapterNumber, verse.VerseNumber);
                var translatedVerseInfo = new VerseInfo(translationInfo.Id, translatedVerse.ChapterNumber, translatedVerse.ChapterNumber, translatedVerse.VerseText);
                verseInfo.Translations.Add(translatedVerseInfo);

                if (!QuranSettings.TranslationRenderSettings.Any(x => x.Id == translationInfo.Id))
                {
                    QuranSettings.TranslationRenderSettings.Add(translationInfo.GetVerseRenderSettings());
                }
            }

            quranTrack.AddTrackItem(newItem);
        }

        /// <summary>
        /// Gets the item render order.
        /// </summary>
        /// <param name="trackItem">The track item.</param>
        /// <returns></returns>
        public int GetItemRenderOrder(ITrackItem trackItem)
        {
            var track = Tracks.First(x => x.Items.Contains(trackItem));

            if (track.Type == TimelineTrackType.Quran)
            {
                return Tracks.Count + 1;
            }

            return Tracks.IndexOf(track) + 1;
        }

        /// <summary>
        /// Gets the total frames.
        /// </summary>
        /// <returns></returns>
        public double GetTotalFrames()
        {
            if (!Tracks.SelectMany(x => x.Items).Any())
            {
                return 0;
            }

            return Tracks.SelectMany(x => x.Items).Max(x => x.GetRightTime().TotalFrames);
        }

        /// <summary>
        /// Gets the visual track items at frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <returns></returns>
        public List<ITrackItem> GetVisualTrackItemsAtFrame(int frame)
        {
            return this.Tracks.SelectMany(x => x.Items).Where(x => x.Type != TrackItemType.Audio)
                .Where(x => x.Position.TotalFrames <= frame && x.GetRightTime().TotalFrames >= frame).ToList();
        }

        public ITrackItem GetTrackItemAtFrame(int frame, TimelineTrackType type)
        {
            return Tracks.FirstOrDefault(x => x.Type == type)?.Items.FirstOrDefault(x => x.Position.TotalFrames <= frame && x.GetRightTime().TotalFrames >= frame);
        }

        public List<AudioTrackItem> GetAudioTrackItemsAtFrame(int frame)
        {
            return GetAudioTrackItems().Where(x => x.Position.TotalFrames <= frame && x.GetRightTime().TotalFrames >= frame).ToList();
        }

        /// <summary>
        /// Gets the audio track items.
        /// </summary>
        /// <returns></returns>
        public List<AudioTrackItem> GetAudioTrackItems()
        {
            return this.Tracks.SelectMany(x => x.Items).Where(x => x.Type == TrackItemType.Audio).Cast<AudioTrackItem>().OrderBy(x => x.Position.TotalFrames).ToList();
        }

        /// <summary>
        /// Opens the project.
        /// </summary>
        /// <param name="projectFile">The project file.</param>
        /// <returns></returns>
        public static OperationResult<Project> OpenProject(string projectFile)
        {
            try
            {
                var project = ProjectSerializer.Deserialize<Project>(System.IO.File.ReadAllText(projectFile));
                MainWindowViewModel.Instance.CurrentProject = project;
                project.Initialize();
                return new OperationResult<Project>(true, string.Empty, project);
            }
            catch (Exception ex)
            {
                return new OperationResult<Project>(false, ex.Message, null);
            }
        }

        public OperationResult MoveProjectFiles(string directory, string oldProjectFilePath)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return new OperationResult(false, "Invalid directory specified.");
            }

            try
            {
                // move clips
                foreach (var clip in Clips)
                {
                    if (clip is ProjectClip projectClip && !string.IsNullOrEmpty(projectClip.FilePath) && File.Exists(projectClip.FilePath))
                    {
                        var newFilePath = Path.Combine(directory, Path.GetFileName(projectClip.FilePath));
                        File.Copy(projectClip.FilePath, newFilePath);
                        projectClip.FilePath = newFilePath;
                    }
                }

                // move the project file itself
                var projectContent = ProjectSerializer.Serialize(this);

                var newProjectFilePath = Path.Combine(directory, Path.GetFileName(oldProjectFilePath));

                System.IO.File.WriteAllText(newProjectFilePath, projectContent);

                return new OperationResult(true, "Project files moved successfully.");
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        /// <summary>
        /// Cut current selected items.
        /// </summary>
        public void Cut()
        {
            CutCopy(true);
        }

        /// <summary>
        /// Copy current selected items.
        /// </summary>
        public void Copy()
        {
            CutCopy(false);
        }

        /// <summary>
        /// Paste the copied items.
        /// </summary>
        public void Paste()
        {
            // contains track items?
            if (Clipboard.ContainsData(nameof(ClipboardDataType.QVM_TrackItems)))
            {
                var items = ProjectSerializer.Deserialize<TrackItemClipboardData[]>(Clipboard.GetData(nameof(ClipboardDataType.QVM_TrackItems)).ToString());

                var undoData = new TrackItemRemoveUndoUnit();

                foreach (var item in items)
                {
                    var track = Tracks.First(x => x.Id == item.SourceTrackId);

                    // paste at the needle position
                    var trackItem = item.TrackItem;
                    trackItem.Position = NeedlePositionTime;

                    track.Items.Add(trackItem);

                    undoData.Items.Add(new TrackAndItemData(track, trackItem));
                }

                if (undoData.Items.Count > 0)
                {
                    UndoEngine.Instance.AddUndoUnit(undoData);
                }
            }
        }

        /// <summary>
        /// Cuts or copies the selected items.
        /// </summary>
        /// <param name="cut"></param>
        private void CutCopy(bool cut)
        {
            // get selected items
            var selectedItems = Tracks.SelectMany(x => TrackItemClipboardData.TrackItems(x.Id, x.Items)).Where(x => x.TrackItem.IsSelected).ToArray();

            // if no items are selected, return
            if (selectedItems.Length == 0)
            {
                return;
            }

            var serialized = ProjectSerializer.Serialize(selectedItems);

            if (cut)
            {
                var undoData = new TrackItemRemoveUndoUnit();

                // remove the selected items
                foreach (var item in selectedItems)
                {
                    var track = Tracks.First(x => x.Items.Contains(item.TrackItem));
                    track.Items.Remove(item.TrackItem);

                    undoData.Items.Add(new TrackAndItemData(track, item.TrackItem));
                }

                if (undoData.Items.Count > 0)
                {
                    UndoEngine.Instance.AddUndoUnit(undoData);
                }
            }
            else
            {
                // set new Ids for the copied items
                foreach (var item in selectedItems)
                {
                    item.TrackItem.GenerateNewId();
                }
            }

            // clear any previous clipboard data
            Clipboard.Clear();

            // copy the serialized items to clipboard
            Clipboard.SetData(nameof(ClipboardDataType.QVM_TrackItems), serialized);
        }

        /// <summary>
        /// Selects all items.
        /// </summary>
        public void SelectAll()
        {
            foreach (var track in Tracks)
            {
                foreach (var item in track.Items)
                {
                    item.IsSelected = true;
                }
            }
        }

        public void DeleteSelectedItems()
        {
            var undoData = new TrackItemRemoveUndoUnit();

            foreach (var track in Tracks)
            {
                foreach (var item in track.Items.Where(x => x.IsSelected).ToArray())
                {
                    track.Items.Remove(item);
                    undoData.Items.Add(new TrackAndItemData(track, item));
                }
            }

            UndoEngine.Instance.AddUndoUnit(undoData);
        }

        public void RemoveAndAddTrackItem(ITimelineTrack fromTrack, ITimelineTrack toTrack, ITrackItem item)
        {
            //var undoData = new MultipleUndoUnits("Move Track Item");
            //var removeUndoUnit = new TrackItemRemoveUndoUnit();

            //removeUndoUnit.Items.Add(new TrackAndItemData(fromTrack, item));
            //undoData.UndoUnits.Add(removeUndoUnit);
            fromTrack.Items.Remove(item);

            //var addUndoUnit = new TrackItemAddUndoUnit(toTrack, item);
            //undoData.UndoUnits.Add(addUndoUnit);
            toTrack.Items.Add(item);

            //UndoEngine.Instance.AddUndoUnit(undoData);
        }

        public void AddClips(string[] files)
        {
            var undoData = new ClipAddUndoUnit(this);

            foreach (var file in files)
            {
                var clip = new ProjectClip(file);
                Clips.Add(clip);
                undoData.Clips.Add(clip);

                QuranVideoMakerUI.ShowDialog(DialogType.ClipImport, clip);
            }

            UndoEngine.Instance.AddUndoUnit(undoData);
        }

        public void DeleteSelectedClips()
        {
            DeleteClips(Clips.Where(x => x.IsSelected));
        }

        public void DeleteClips(IEnumerable<IProjectClip> clips)
        {
            var undoData = new MultipleUndoUnits("Delete Clips");

            var removeUndoUnit = new ClipRemoveUndoUnit(this);

            foreach (var clip in clips.ToArray())
            {
                removeUndoUnit.Clips.Add(clip);

                foreach (var track in Tracks)
                {
                    var items = track.Items.Where(x => x.ClipId == clip.Id).ToArray();

                    foreach (var item in items)
                    {
                        track.Items.Remove(item);

                        var trackItemRemoveUndoUnit = new TrackItemRemoveUndoUnit();
                        trackItemRemoveUndoUnit.Items.Add(new TrackAndItemData(track, item));
                        undoData.UndoUnits.Add(trackItemRemoveUndoUnit);
                    }
                }

                undoData.UndoUnits.Add(removeUndoUnit);
                Clips.Remove(clip);
            }

            UndoEngine.Instance.AddUndoUnit(undoData);
        }

        /// <summary>
        /// Saves the project.
        /// </summary>
        /// <param name="projectFile">The project file.</param>
        /// <returns></returns>
        public OperationResult Save(string projectFile)
        {
            try
            {
                System.IO.File.WriteAllText(projectFile, ProjectSerializer.Serialize(this));

                return new OperationResult(true, string.Empty);
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        public void AddTrack(TimelineTrackType type)
        {
            var count = Tracks.Count(x => x.Type == type);

            var track = new TimelineTrack(type, $"{type} {count + 1}");

            var indexOfTheLastTrackOfTheSameType = Tracks.IndexOf(Tracks.Last(x => x.Type == type));

            Tracks.Insert(indexOfTheLastTrackOfTheSameType + 1, track);

            var undoUnit = new TrackAddUndoUnit(this, track, Tracks.IndexOf(track));

            UndoEngine.Instance.AddUndoUnit(undoUnit);
        }

        public void RemoveTrack(TimelineTrack track)
        {
            Tracks.Remove(track);

            var undoUnit = new TrackRemoveUndoUnit(this, track, Tracks.IndexOf(track));

            UndoEngine.Instance.AddUndoUnit(undoUnit);
        }

        public async Task<OperationResult> ExportAsync(string exportDirectory)
        {
            var verses = OrderedVerses;

            var fileName = $"Quran.{ExportFormat}";

            if (verses.Any())
            {
                fileName = $"Quran {verses.First().Verse.ChapterNumber} {verses.First().Verse.VerseNumber}-{verses.Last().Verse.VerseNumber}.{ExportFormat}";
            }

            // if all translations, or a single translation is selected
            if (ExportRenderedTranslation == QuranIds.AllTranslations)
            {
                var filePath = Path.Combine(ExportDirectory, fileName);

                return await ExportCoreAsync(filePath, ExportRenderedTranslation);
            }
            else if (ExportRenderedTranslation == QuranIds.SeparateTranslations)
            {
                foreach (var translation in QuranSettings.TranslationRenderSettings)
                {
                    var translationInfo = Quran.GetTranslation(translation.Id);
                    var translationPath = Path.Combine(exportDirectory, $"Quran {verses.First().Verse.ChapterNumber} {verses.First().Verse.VerseNumber}-{verses.Last().Verse.VerseNumber} ({translationInfo.Language} - {translationInfo.Name}).{ExportFormat}");

                    var result = await ExportCoreAsync(translationPath, translation.Id);

                    if (!result.Success)
                    {
                        return result;
                    }
                }
            }
            else
            {
                return await ExportCoreAsync(Path.Combine(exportDirectory, fileName), ExportRenderedTranslation);
            }

            return new OperationResult(true, string.Empty);
        }

        public async Task<OperationResult> ExportCoreAsync(string exportPath, Guid translationId)
        {
            try
            {
                ClearVerseRenderCache();

                var sw = Stopwatch.StartNew();

                var videoExportPath = Path.Combine(Path.GetTempPath(), "QuranVideoMaker", $"project_{Id}_video.{ExportFormat}");
                var audioExportPath = Path.Combine(Path.GetTempPath(), "QuranVideoMaker", $"project_{Id}_audio.mp3");

                var totalFrames = Convert.ToInt32(GetTotalFrames());
                var totalSeconds = totalFrames / FPS;

                var pipe = new MultithreadedFramePipeSource(FPS, totalFrames);

                var parallelOptions = new ParallelOptions()
                {
                    MaxDegreeOfParallelism = ExportThreads
                };

                ExportProgressMessage = "Exporting video...";

                var audioTrackItems = GetAudioTrackItems();

                await ExportAudioAsync(audioExportPath, audioTrackItems);

                var ffmpegArguments = FFMpegArguments.FromPipeInput(pipe, options =>
                {
                    options.WithHardwareAcceleration(HardwareAcceleration);
                });

                if (audioTrackItems.Count > 0)
                {
                    ffmpegArguments.AddFileInput(audioExportPath, true);
                }

                var output = ffmpegArguments.OutputToFile(videoExportPath, true, options =>
                {
                    options.WithFramerate(FPS);
                    options.WithFastStart();
                    options.WithSpeedPreset(EncodingSpeed);

                    if (ExportIncludeAlphaChannel)
                    {
                        if (ExportFormat == "mov")
                        {
                            options.WithVideoCodec("qtrle");
                            options.ForcePixelFormat("argb");
                        }
                        else if (ExportFormat == "webm")
                        {
                            options.WithVideoCodec("libvpx-vp9");
                            options.ForcePixelFormat("yuva420p");
                        }
                    }
                    else
                    {
                        options.WithVideoCodec(VideoCodec.LibX264);
                    }

                    options.ForceFormat(ExportFormat);
                });

                _ = Task.Run(() =>
                {
                    var count = 0;

                    var range = Enumerable.Range(1, totalFrames).ToArray();

                    var chunkSize = FramesPerBatch;
                    var chunkQueueLimit = chunkSize * 3;

                    var split = range.Split(chunkSize);

                    foreach (var chunk in split)
                    {
                        Parallel.ForEach(chunk.AsParallel().AsOrdered(), parallelOptions, frameNumber =>
                        {
                            // if pipe is has more than chunkQueueLimit, wait for it to process
                            while (pipe.GetUnprocessedFramesCount() > chunkQueueLimit)
                            {
                                Task.Delay(500).Wait();
                            }

                            var frame = RenderFrame(frameNumber, translationId, false);
                            pipe.AddFrame(frame);

                            count++;

                            var progress = Math.Round(((double)count / (double)totalFrames) * 100d, 2);

                            // deal with NaN and Infinity
                            if (double.IsNaN(progress) || double.IsInfinity(progress))
                            {
                                progress = 0;
                            }

                            ExportProgressMessage = $"Frames rendered: {count} of {totalFrames}...";
                            ExportProgress?.Invoke(null, progress);
                            ExportProgressTime = sw.Elapsed;
                            Debug.WriteLine($"Progress: {progress}%");
                        });
                    }
                });

                await output.ProcessAsynchronously();

                File.Copy(videoExportPath, exportPath, true);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                sw.Stop();
                Debug.WriteLine($"Elapsed: {sw.Elapsed}");

                return new OperationResult(true, string.Empty);
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        public async Task<OperationResult> ExportAudioAsync(string audioExportPath)
        {
            return await ExportAudioAsync(audioExportPath, GetAudioTrackItems());
        }

        public async Task<OperationResult> ExportAudioAsync(string audioExportPath, IEnumerable<AudioTrackItem> audioTrackItems)
        {
            try
            {
                if (!audioTrackItems.Any())
                {
                    return new OperationResult(true, string.Empty);
                }

                var waveFormat = new WaveFormat();

                using (var writer = new WaveFileWriter(audioExportPath, waveFormat))
                {
                    var lastPosition = TimeCode.FromSeconds(0, FPS);

                    foreach (var audioItem in audioTrackItems)
                    {
                        if (audioItem.Position > lastPosition)
                        {
                            // add silence
                            var silence = audioItem.Position - lastPosition;
                            var silenceSeconds = silence.TotalSeconds;

                            var silenceData = new byte[(int)(silenceSeconds * waveFormat.AverageBytesPerSecond)];

                            await writer.WriteAsync(silenceData, 0, silenceData.Length);
                        }

                        lastPosition = audioItem.Position + audioItem.Duration;

                        var itemWaveStream = audioItem.GetWaveStream();

                        await itemWaveStream.CopyToAsync(writer);

                        //var clip = Clips.FirstOrDefault(x => x.Id == audioItem.ClipId);

                        //using (var reader = new AudioFileReader(clip.FilePath))
                        //{
                        //    // seek to the start of the audio item
                        //    reader.CurrentTime = audioItem.Start.ToTimeSpan();

                        //    // resample the audio
                        //    using (var resampler = new MediaFoundationResampler(reader, waveFormat))
                        //    {
                        //        var itemLength = audioItem.End - audioItem.Start;
                        //        var itemLengthSeconds = itemLength.TotalSeconds;

                        //        var buffer = new byte[(int)(itemLengthSeconds * waveFormat.AverageBytesPerSecond)];

                        //        resampler.Read(buffer, 0, buffer.Length);

                        //        await writer.WriteAsync(buffer, 0, buffer.Length);
                        //    }
                        //}
                    }
                }

                return new OperationResult(true, string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new OperationResult(false, ex.Message);
            }
        }

        public async Task<OperationResult> ExportVersesAsync(string exportPath)
        {
            try
            {
                ClearVerseRenderCache();

                await Task.Run(() =>
                {
                    var verses = Tracks.SelectMany(x => x.Items).Where(x => x.Type == TrackItemType.Quran).Cast<QuranTrackItem>().ToArray();

                    var verseInfo = verses.Select(x => x.Verse).ToArray();
                    var settings = QuranSettings.Clone();
                    settings.OutputDirectory = exportPath;
                    settings.OutputType = OutputType.File;

                    VerseRenderer.RenderVerses(verseInfo, settings);
                });

                return new OperationResult(true, string.Empty);
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        public FrameContainer RenderFrame(int frameNumber, Guid translationId, bool preview)
        {
            try
            {
                var frameContainer = new FrameContainer(frameNumber);

                var width = Width;
                var height = Height;

                var quranSettings = QuranSettings.Clone();
                quranSettings.RenderedTranslation = translationId;

                if (preview)
                {
                    width /= 4;
                    height /= 4;
                }

                SKBitmap background = null;

                // create the background once if full screen text background is enabled
                if (quranSettings.TextBackground && quranSettings.FullScreenTextBackground)
                {
                    background = new SKBitmap(width, height);
                    background.Erase(new SKColor(quranSettings.TextBackgroundColor.Red, quranSettings.TextBackgroundColor.Green, quranSettings.TextBackgroundColor.Blue, Convert.ToByte(quranSettings.TextBackgroundOpacity * 255)));
                }

                var visualItems = GetVisualTrackItemsAtFrame(frameNumber);

                var currentFrames = new List<FrameData>();

                foreach (var trackItem in visualItems)
                {
                    var itemFrame = trackItem.GetLocalFrame(frameNumber);
                    var itemOrder = GetItemRenderOrder(trackItem);

                    if (itemFrame > 0 && itemFrame >= trackItem.Start.TotalFrames && itemFrame <= trackItem.End.TotalFrames)
                    {
                        if (trackItem.Type == TrackItemType.Video)
                        {
                            var clip = Clips.First(x => x.Id == trackItem.ClipId);
                            currentFrames.Add(new FrameData(clip.FramesCache.First(x => x.Frame == itemFrame).Data, trackItem.GetOpacity(itemFrame), itemOrder));
                        }
                        else if (trackItem.Type == TrackItemType.Image)
                        {
                            var clip = Clips.First(x => x.Id == trackItem.ClipId);
                            currentFrames.Add(new FrameData(clip.FramesCache.First().Data, trackItem.GetOpacity(itemFrame), itemOrder));
                        }
                        else if (trackItem.Type == TrackItemType.Quran)
                        {
                            var verseItem = trackItem as QuranTrackItem;

                            if (!_renderedVerses.ContainsKey(trackItem.Id))
                            {
                                var rv = VerseRenderer.RenderVerses(new[] { verseItem.Verse }, quranSettings);
                                _renderedVerses[trackItem.Id] = rv.FirstOrDefault().Bitmap;
                            }

                            var opacity = quranSettings.TextBackgroundTransition ? trackItem.GetOpacity(itemFrame) : 1;

                            if (quranSettings.FullScreenTextBackground)
                            {
                                currentFrames.Add(new FrameData(background, opacity, itemOrder));
                            }

                            currentFrames.Add(new FrameData(_renderedVerses[trackItem.Id], trackItem.GetOpacity(itemFrame), itemOrder));
                        }
                    }
                }

                using (var frameBitmap = new SKBitmap(width, height))
                {
                    using (var frameCanvas = new SKCanvas(frameBitmap))
                    {
                        frameCanvas.Clear(SKColors.Transparent);

                        foreach (var cf in currentFrames.OrderBy(x => x.Order))
                        {
                            var cfBitmap = cf.Bitmap ?? SKBitmap.Decode(cf.Data);
                            var left = 0;
                            var top = 0;
                            var right = cfBitmap.Width;
                            var bottom = cfBitmap.Height;
                            if (cfBitmap.Width != width || cfBitmap.Height != height)
                            {
                                double ratio = Math.Max((double)cfBitmap.Width / (double)width, (double)cfBitmap.Height / (double)height);
                                var newWidth = (int)(cfBitmap.Width / ratio);
                                var newHeight = (int)(cfBitmap.Height / ratio);
                                left = (width - newWidth) / 2;
                                top = (height - newHeight) / 2;
                                right = left + newWidth;
                                bottom = top + newHeight;
                                cfBitmap = cfBitmap.Resize(new SKSizeI(newWidth, newHeight), SKFilterQuality.High);
                            }
                            if (cf.Opacity != 1)
                            {
                                var alpha = (byte)(255 * cf.Opacity);
                                var textPaint = new SKPaint()
                                {
                                    Color = new SKColor(255, 255, 255, alpha)
                                };
                                frameCanvas.DrawBitmap(cfBitmap, new SKRect(left, top, right, bottom), textPaint);
                            }
                            else
                            {
                                frameCanvas.DrawBitmap(cfBitmap, new SKRect(left, top, right, bottom));
                            }
                        }

                        using (var pixMap = frameBitmap.PeekPixels())
                        {
                            if (ExportIncludeAlphaChannel && !preview)
                            {
                                using (var data = pixMap.Encode(new SKPngEncoderOptions() { ZLibLevel = 0, FilterFlags = SKPngEncoderFilterFlags.NoFilters }))
                                {
                                    frameContainer.Data = data.ToArray();
                                }
                            }
                            else
                            {
                                using (var data = pixMap.Encode(new SKJpegEncoderOptions() { Quality = 100, AlphaOption = SKJpegEncoderAlphaOption.BlendOnBlack }))
                                {
                                    frameContainer.Data = data.ToArray();
                                }
                            }
                        }
                    }
                }

                return frameContainer;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return null;
        }

        public IEnumerable<TrackItem> GetQuranTrackItems()
        {
            return Tracks.SelectMany(x => x.Items).Where(x => x.Type == TrackItemType.Quran).Cast<TrackItem>();
        }

        public IEnumerable<VerseInfo> GetVerses()
        {
            return GetQuranTrackItems().Cast<QuranTrackItem>().Select(x => x.Verse);
        }

        #region [Play]

        public void Play()
        {
            ClearVerseRenderCache();

            if (IsPlaying)
            {
                Stop();
                return;
            }

            _playTimer.Interval = 1000d / (double)FPS;

            var audioItems = GetAudioTrackItemsAtFrame(Convert.ToInt32(NeedlePositionTime.TotalFrames));
            var firstAudio = audioItems.FirstOrDefault();

            if (firstAudio != null)
            {
                var localTime = firstAudio.GetLocalTime(NeedlePositionTime);
                firstAudio.Play(localTime);
            }

            _playTimer.Start();

            IsPlaying = true;
        }

        public void FastForward()
        {
            var newTime = new TimeCode(NeedlePositionTime.TotalFrames + (3 * FPS), FPS);
            NeedlePositionTime = newTime;
            RaiseNeedlePositionTimeChanged(newTime);
        }

        public void Rewind()
        {
            var newTime = new TimeCode(NeedlePositionTime.TotalFrames - (3 * FPS), FPS);
            NeedlePositionTime = newTime;
            RaiseNeedlePositionTimeChanged(newTime);
        }

        public void Stop()
        {
            var audioItems = GetAudioTrackItems();

            _playTimer?.Stop();

            foreach (var audioItem in audioItems)
            {
                audioItem.Stop();
            }

            IsPlaying = false;
            ClearVerseRenderCache();
        }

        private void PlayTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (NeedlePositionTime.TotalFrames >= GetTotalFrames())
            {
                Stop();
            }

            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                NeedlePositionTime = NeedlePositionTime.AddFrames(1);
            });

            if (GetAudioTrackItemsAtFrame(Convert.ToInt32(NeedlePositionTime.TotalFrames)).FirstOrDefault() is AudioTrackItem trackItem)
            {
                if (!trackItem.IsPlaying())
                {
                    // if there is no audio at the current position, stop all audio
                    foreach (var audioItem in GetAudioTrackItems())
                    {
                        audioItem.Stop();
                    }

                    var localTime = trackItem.GetLocalTime(NeedlePositionTime);
                    trackItem.Play(localTime);
                }
            }
            else
            {
                // if there is no audio at the current position, stop all audio
                foreach (var audioItem in GetAudioTrackItems())
                {
                    audioItem.Stop();
                }
            }

            PreviewCurrentFrame();
        }

        public void PreviewCurrentFrame()
        {
            var currentFrame = Convert.ToInt32(NeedlePositionTime.TotalFrames);

            var render = RenderFrame(currentFrame, PreviewRenderedTranslation, true);

            if (render != null)
            {
                CurrentPreviewFrame = render.Data;
            }
        }

        #endregion

        public void CacheClipFrames()
        {
            foreach (var clip in Clips)
            {
                clip.CacheFrames();
            }
        }

        /// <summary>
        /// Raises the needle position time changed.
        /// </summary>
        /// <param name="time">The time.</param>
        public void RaiseNeedlePositionTimeChanged(TimeCode time)
        {
            var audioItems = GetAudioTrackItemsAtFrame(Convert.ToInt32(NeedlePositionTime.TotalFrames));
            var firstAudio = audioItems.FirstOrDefault();

            if (firstAudio != null)
            {
                var localTime = firstAudio.GetLocalTime(NeedlePositionTime);
                firstAudio.Seek(localTime);
            }
            else
            {
                // if there is no audio at the current position, stop all audio
                foreach (var audioItem in GetAudioTrackItems())
                {
                    audioItem.Stop();
                }
            }

            PreviewCurrentFrame();

            NeedlePositionTimeChanged?.Invoke(this, time);
        }

        public void FixVerseNumbers()
        {
            var quranTrack = Tracks.First(x => x.Type == TimelineTrackType.Quran);
            quranTrack.FixVerseNumbers();
        }

        public void ClearVerseRenderCache()
        {
            Debug.WriteLine("Clearing verse render cache...");
            _renderedVerses.Clear();
        }

        public void Initialize()
        {
            foreach (var track in Tracks)
            {
                foreach (var item in track.Items)
                {
                    item.Initialize();
                }
            }
        }

        /// <summary>
        /// Called when public properties changed.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            if (name != nameof(NeedlePositionTime) && name != nameof(CurrentPreviewFrame))
            {
                ClearVerseRenderCache();
            }
        }

        public void OnDeserialized()
        {
            HookEvents();
        }

        private void HookEvents()
        {
            Tracks.CollectionChanged -= TracksChangedEventHandler;
            Tracks.CollectionChanged += TracksChangedEventHandler;

            HookTrackChangedEvents();
        }

        private void TracksChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            HookTrackChangedEvents();
        }

        private void HookTrackChangedEvents()
        {
            foreach (var track in Tracks)
            {
                track.Changed -= Track_Changed;
                track.Changed += Track_Changed;
            }
        }

        private void Track_Changed(object sender, EventArgs e)
        {
            ClearVerseRenderCache();
        }
    }
}
