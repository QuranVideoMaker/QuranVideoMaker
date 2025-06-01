using NAudio.Wave;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuranVideoMaker.Data
{
    /// <summary>
    /// AudioTrackItem
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    [Description("AudioTrackItem")]
    [DisplayName("AudioTrackItem")]
    [DebuggerDisplay("AudioTrackItem")]
    public class AudioTrackItem : TrackItem
    {
        private AudioFileReader _audioReader;
        private WaveOutEvent _outputDevice;

        private string _filePath;
        private bool _isPlaying;
        private bool _isInitializing;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioTrackItem"/> class.
        /// </summary>
        public AudioTrackItem()
        {
            Type = TrackItemType.Audio;
        }

        public AudioTrackItem(IProjectClip clipInfo, TimeCode position, TimeCode start, TimeCode end) : base(clipInfo, position, start, end)
        {
            _filePath = clipInfo.FilePath;
        }

        public void Play(TimeCode time)
        {
            while (_isInitializing)
            {
                //wait for initialization to complete
            }

            InitIfNeeded();

            var playTime = time.ToTimeSpan();

            _audioReader.CurrentTime = playTime;

            _outputDevice.Play();

            _isPlaying = true;
        }

        public void Stop()
        {
            _outputDevice?.Stop();

            _isPlaying = false;
        }

        public void Seek(TimeCode time)
        {
            while (_isInitializing)
            {
                //wait for initialization to complete
            }

            InitIfNeeded();

            var playTime = time.ToTimeSpan();

            _audioReader.CurrentTime = playTime;
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public WaveStream GetWaveStream()
        {
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                return null;
            }

            using (var reader = new AudioFileReader(_filePath))
            {
                var waveFormat = new WaveFormat();

                // set position to Start
                reader.CurrentTime = Start.ToTimeSpan();

                // resample the audio
                using (var resampler = new MediaFoundationResampler(reader, waveFormat))
                {
                    var itemLength = End - Start;
                    var itemLengthSeconds = itemLength.TotalSeconds;

                    var buffer = new byte[(int)(itemLengthSeconds * waveFormat.AverageBytesPerSecond)];

                    resampler.Read(buffer, 0, buffer.Length);

                    using (var waveStream = new RawSourceWaveStream(buffer, 0, buffer.Length, waveFormat))
                    {
                        return waveStream;
                    }
                }
            }
        }

        public override void Initialize()
        {
            InitIfNeeded();
            base.Initialize();
        }

        public override void OnPropertyChanged([CallerMemberName] string name = null)
        {
            base.OnPropertyChanged(name);
        }

        private void InitIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitializing = true;

            if (string.IsNullOrWhiteSpace(_filePath))
            {
                _filePath = MainWindowViewModel.Instance.CurrentProject.Clips.FirstOrDefault(c => c.Id == this.ClipId)?.FilePath;
            }

            _outputDevice ??= new WaveOutEvent();

            _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;

            if (_audioReader == null)
            {
                _audioReader = new AudioFileReader(_filePath);
                _outputDevice.Init(_audioReader);
            }

            _isInitializing = false;
            _isInitialized = true;
        }

        private void OutputDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            _isPlaying = false;
        }
    }
}
