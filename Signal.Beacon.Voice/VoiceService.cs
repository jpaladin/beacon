using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DeepSpeechClient;
using Microsoft.Extensions.Logging;
using OpenTK.Audio.OpenAL;
using Pv;
using Signal.Beacon.Core.Workers;

namespace Signal.Beacon.Voice
{
    public class SpeechScene
    {
        public string Name { get; init; }

        public Dictionary<string, float> HotWords { get; } = new Dictionary<string, float>();
    }

    public class VoiceService : IWorkerService, IDisposable
    {
        private readonly ILogger<VoiceService> logger;

        private Porcupine? porcupine;
        private short[]? porcupineRecordingBuffer;
        private const float PorcupineSensitivity = 0.7f;
        private const string PorcupineModelFilePath = @"lib\common\porcupine_params.pv";
        private const string? PorcupineKeywordFilePath = @"Profiles\signal_windows_2020-12-23_v1.8.0.ppn";

        private readonly List<SpeechScene> speechScenes = new List<SpeechScene>();
        private DeepSpeech? deepSpeechClient;
        private int? deepSpeechFrameLength;
        private short[]? deepSpeechRecordingBuffer;
        private const int DeepSpeechSilenceDuration = 1800;
        private const int DeepSpeechDurationLimit = 30000;
        private const int DeepSpeechIntermediatePeriod = 100;
        private const string DeepSpeechModelFileName = @"Profiles\deepspeech-0.9.1-models.pbmm";
        private const string DeepSpeechScorerFileName = @"Profiles\deepspeech-0.9.1-models.scorer";
        private const float DeepSpeechScorerAlpha = 0.931289039105002f;
        private const float DeepSpeechScorerBeta = 1.1834137581510284f;
        
        private ALCaptureDevice? captureDevice;

        private readonly List<AlSound> sounds = new List<AlSound>();
        private ALContext? alContext;
        private int? alSource;

        public VoiceService(ILogger<VoiceService> logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void InitializePorcupine()
        {
            var executionLocation = ExecutingLocation();
            this.porcupine = new Porcupine(
                Path.Combine(executionLocation, PorcupineModelFilePath),
                new[] { Path.Combine(executionLocation, PorcupineKeywordFilePath) },
                new[] { PorcupineSensitivity });
            this.porcupineRecordingBuffer = new short[this.porcupine.FrameLength];
        }

        private void WaitForWakeWord(CancellationToken cancellationToken)
        {
            if (this.porcupine == null)
                throw new NullReferenceException("Porcupine is null. Initialize first.");
            if (this.porcupineRecordingBuffer == null)
                throw new NullReferenceException("PorcupineRecordingBuffer is null. Initialize first.");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (this.GetNextFrame(this.porcupine.FrameLength, ref this.porcupineRecordingBuffer))
                {
                    var result = this.porcupine.Process(this.porcupineRecordingBuffer);
                    if (result >= 0)
                        return;
                }

                Thread.Yield();
            }
        }

        private void RegisterSpeechScene(SpeechScene scene)
        {
            var existingScene = this.speechScenes.FirstOrDefault(ss => ss.Name == scene.Name);
            if (existingScene != null)
                this.speechScenes.Remove(existingScene);
                
            this.speechScenes.Add(scene);
        }

        private void SetSpeechScene(string name)
        {
            var scene = this.speechScenes.FirstOrDefault(ss => ss.Name == name);
            if (scene == null)
            {
                this.logger.LogWarning("Can't switch to scene {SceneName} - not registered", name);
                return;
            }

            if (this.deepSpeechClient == null)
                throw new NullReferenceException("DeepSpeech is null. Can't change scene now. Initialize first");

            this.deepSpeechClient.ClearHotWords();
            foreach (var (word, boost) in scene.HotWords) 
                this.deepSpeechClient.AddHotWord(word, boost);

            this.logger.LogDebug("Speech scene {SceneName} set", name);
        }

        private void InitializeDeepSpeech()
        {
            var executingLocation = ExecutingLocation();
            this.deepSpeechClient = new DeepSpeech(
                Path.Combine(
                    executingLocation,
                    DeepSpeechModelFileName));
            this.deepSpeechClient.EnableExternalScorer(
                Path.Combine(
                    executingLocation,
                    DeepSpeechScorerFileName));
            this.deepSpeechClient.SetScorerAlphaBeta(DeepSpeechScorerAlpha, DeepSpeechScorerBeta);
            this.deepSpeechFrameLength = this.deepSpeechClient.GetModelSampleRate();
            this.deepSpeechRecordingBuffer = new short[this.deepSpeechFrameLength.Value];
            var beamWidth = this.deepSpeechClient.GetModelBeamWidth();
            this.deepSpeechClient.SetModelBeamWidth(beamWidth * 2);

            var generalScene = new SpeechScene
            {
                Name = "General"
            };
            generalScene.HotWords.Add("light", 0.1f);
            generalScene.HotWords.Add("bed", 0.1f);
            generalScene.HotWords.Add("sleep", 0.1f);
            generalScene.HotWords.Add("leaving", 0.1f);
            generalScene.HotWords.Add("off", 0.1f);
            generalScene.HotWords.Add("on", 0.1f);
            this.RegisterSpeechScene(generalScene);
            this.SetSpeechScene(generalScene.Name);
        }

        private static string ExecutingLocation()
        {
            var executingLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (executingLocation == null)
                throw new Exception("Couldn't determine application executing location.");
            return executingLocation;
        }

        private IEnumerable<string> GetAvailableCaptureDevices()
        {
            var devices = ALC.GetStringList(GetEnumerationStringList.CaptureDeviceSpecifier).ToList();
            this.AlHasError();

            return devices;
        }

        private void InitializeCaptureDevice(string deviceName, int maxFrameLength)
        {
            if (string.IsNullOrWhiteSpace(deviceName))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(deviceName));
            if (maxFrameLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxFrameLength));

            this.captureDevice = ALC.CaptureOpenDevice(deviceName, 16000, ALFormat.Mono16, maxFrameLength * 3);
            this.AlHasError();
        }

        private void StartCaptureDevice()
        {
            if (this.captureDevice == null)
                throw new NullReferenceException("Capture device not initialized.");

            ALC.CaptureStart(this.captureDevice.Value);
            this.AlHasError();
        }

        private void StopCaptureDevice()
        {
            if (this.captureDevice == null) 
                return;

            ALC.CaptureStop(this.captureDevice.Value);
            this.AlHasError();
        }

        private void DisposeCaptureDevice()
        {
            this.logger.LogDebug("Disposing capture devices...");

            this.StopCaptureDevice();

            if (this.captureDevice == null) 
                return;

            ALC.CaptureCloseDevice(this.captureDevice.Value);
            this.AlHasError();

            this.captureDevice = null;
        }

        private bool GetNextFrame(int frameLength, ref short[] buffer)
        {
            if (this.captureDevice == null)
                throw new NullReferenceException("Capture device not initialized.");
            if (frameLength <= 0) throw new ArgumentOutOfRangeException(nameof(frameLength));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length < frameLength)
                throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer smaller than frame length.");

            if (ALC.GetAvailableSamples(this.captureDevice.Value) < frameLength) 
                return false;

            ALC.CaptureSamples(this.captureDevice.Value, ref buffer[0], frameLength);
            return true;
        }

        private string? ProcessDeepSpeech(CancellationToken cancellationToken)
        {
            if (this.deepSpeechClient == null)
                throw new NullReferenceException("DeepSpeechClient is null. Initialize DeepSpeech first.");
            if (this.deepSpeechFrameLength == null)
                throw new NullReferenceException("DeepSpeech FrameLength is null. Initialize DeepSpeech first.");
            if (this.deepSpeechRecordingBuffer == null)
                throw new NullReferenceException("DeepSpeech RecordingBuffer is null. Initialize DeepSpeech first.");

            var swTotalTranscript = new Stopwatch();
            var swLastIntermediateResult = new Stopwatch();
            var swCurrentIntermediateResult = new Stopwatch();
            swCurrentIntermediateResult.Start();
            swTotalTranscript.Start();

            try
            {
                var frameLength = this.deepSpeechFrameLength.Value;
                var lastIntermediateResult = string.Empty;

                using var dss = this.deepSpeechClient.CreateStream();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (this.GetNextFrame(frameLength, ref this.deepSpeechRecordingBuffer))
                    {
                        this.deepSpeechClient.FeedAudioContent(dss,
                            this.deepSpeechRecordingBuffer,
                            Convert.ToUInt32(this.deepSpeechFrameLength));
                    }

                    if (swCurrentIntermediateResult.ElapsedMilliseconds > DeepSpeechIntermediatePeriod)
                    {
                        var intermediateResult = this.deepSpeechClient.IntermediateDecode(dss).ToLowerInvariant().Trim();

                        // If intermediate result changed
                        if (lastIntermediateResult != intermediateResult)
                        {
                            lastIntermediateResult = intermediateResult;
                            swLastIntermediateResult.Restart();

                            this.logger.LogInformation("Human: {Result}...", intermediateResult);
                        }

                        swCurrentIntermediateResult.Restart();
                    }

                    // Silence detection (or duration limit exceeded)
                    // TODO: Increase silence if no conducts available
                    if (swTotalTranscript.Elapsed > TimeSpan.FromMilliseconds(DeepSpeechDurationLimit) ||
                        swLastIntermediateResult.Elapsed > TimeSpan.FromMilliseconds(DeepSpeechSilenceDuration))
                    {
                        var result = this.deepSpeechClient.FinishStream(dss).ToLowerInvariant().Trim();
                        this.logger.LogInformation(
                            "Human: {Result}. (in {Elapsed}s)",
                            result,
                            swTotalTranscript.Elapsed);

                        return result;
                    }

                    Thread.Yield();
                }

                return null;
            }
            catch(Exception ex)
            {
                this.logger.LogWarning(ex, "Processing failed.");
                return null;
            }
        }

        // Loads a wave/riff audio file.
        public static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using BinaryReader reader = new BinaryReader(stream);

            // RIFF header
            string signature = new string(reader.ReadChars(4));
            if (signature != "RIFF")
                throw new NotSupportedException("Specified stream is not a wave file.");

            _ = reader.ReadInt32();      //riff_chunk_size

            string format = new string(reader.ReadChars(4));
            if (format != "WAVE")
                throw new NotSupportedException("Specified stream is not a wave file.");

            // WAVE header
            string formatSignature = new string(reader.ReadChars(4));
            if (formatSignature != "fmt ")
                throw new NotSupportedException("Specified wave file is not supported.");

            _ = reader.ReadInt32();                     // format_chunk_size
            _ = reader.ReadInt16();                     // audio_format
            int numChannels = reader.ReadInt16();      // num_channels
            var sampleRate = reader.ReadInt32();       // sample_rate
            _ = reader.ReadInt32();                     // byte_rate
            _ = reader.ReadInt16();                     // block_align
            int bitsPerSample = reader.ReadInt16();   // bits_per_sample

            string dataSignature = new string(reader.ReadChars(4));
            if (dataSignature != "data")
                throw new NotSupportedException("Specified wave file is not supported.");

            var dataLength = reader.ReadInt32();

            channels = numChannels;
            bits = bitsPerSample;
            rate = sampleRate;

            return reader.ReadBytes(dataLength);
        }

        public static ALFormat GetSoundFormat(int channels, int bits)
        {
            return channels switch
            {
                1 => bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16,
                2 => bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16,
                _ => throw new NotSupportedException("The specified sound format is not supported.")
            };
        }

        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    this.InitializePorcupine();
                    this.InitializeDeepSpeech();
                    this.InitializeSounds();

                    var captureDeviceName = this.GetAvailableCaptureDevices().FirstOrDefault();
                    if (captureDeviceName == null)
                        throw new Exception("No capture devices available");

                    this.InitializeCaptureDevice(captureDeviceName, this.deepSpeechFrameLength ?? 1600);
                    this.StartCaptureDevice();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        this.WaitForWakeWord(cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        _ = this.PlaySoundAsync("wake");
                        var result = this.ProcessDeepSpeech(cancellationToken);

                        _ = this.PlaySoundAsync(string.IsNullOrWhiteSpace(result)
                            ? "error"
                            : "accept");
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Worker failed.");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        private void InitializeSounds()
        {
            var device = ALC.OpenDevice(null);
            this.AlHasError();
            this.alContext = ALC.CreateContext(device, new ALContextAttributes());
            this.AlHasError();

            // Set context
            ALC.MakeContextCurrent(this.alContext.Value);
            this.AlHasError();

            // Generate source
            this.alSource = AL.GenSource();
            this.AlHasError();

            var executionLocation = ExecutingLocation();
            
            const string wakePath = @"voice_wake.wav";
            const string acceptPath = @"voice_accept.wav";
            const string errorPath = @"voice_error.wav";
            
            this.sounds.Add(this.WavToAudioSource("accept", Path.Combine(executionLocation, "Sounds", acceptPath)));
            this.sounds.Add(this.WavToAudioSource("error", Path.Combine(executionLocation, "Sounds", errorPath)));
            this.sounds.Add(this.WavToAudioSource("wake", Path.Combine(executionLocation, "Sounds", wakePath)));
        }

        private void DisposeSounds()
        {
            this.logger.LogDebug("Disposing sounds...");

            foreach (var alSound in this.sounds)
            {
                AL.DeleteBuffer(alSound.Buffer);
                this.AlHasError();
            }

            this.sounds.Clear();

            if (this.alSource != null)
            {
                AL.DeleteSource(this.alSource.Value);
                this.alSource = null;
                this.AlHasError();
            }

            if (this.alContext != null)
            {
                var device = ALC.GetContextsDevice(this.alContext.Value);
                ALC.SuspendContext(this.alContext.Value);
                ALC.DestroyContext(this.alContext.Value);
                if (!ALC.CloseDevice(device))
                {
                    this.logger.LogDebug("AL failed to close device. Error code in next log entry.");
                    this.AlHasError();
                }

                this.alContext = null;
            }
        }

        private async Task PlaySoundAsync(string name)
        {
            if (this.alContext == null)
            {
                this.logger.LogWarning("Can't play sound because context is null.");
                return;
            }

            if (!this.alSource.HasValue)
            {
                this.logger.LogWarning("Can't play sound because source is null");
                return;
            }

            var sound = this.sounds.FirstOrDefault(s => s.Name == name);
            if (sound == null)
                throw new Exception("Sound not found \"" + name + "\".");

            await this.WaitSourceToStop();

            ALC.MakeContextCurrent(this.alContext.Value);
            this.AlHasError();

            AL.BindBufferToSource(this.alSource.Value, sound.Buffer);
            this.AlHasError();

            AL.SourcePlay(this.alSource.Value);
            this.AlHasError();

            await this.WaitSourceToStop();
        }

        private Task WaitSourceToStop()
        {
            if (!this.alSource.HasValue)
            {
                this.logger.LogWarning("Can't play sound because source is null");
                return Task.CompletedTask;
            }

            return Task.Run(() =>
            {
                ALSourceState state;
                do
                {
                    state = AL.GetSourceState(this.alSource.Value);
                    if (this.AlHasError()) break;

                    Thread.Yield();
                } while (state == ALSourceState.Playing);
            });
        }

        private bool AlHasError()
        {
            var errorCode = AL.GetError();
            if (errorCode == ALError.NoError)
                return false;

#if DEBUG
            if (Debugger.IsAttached)
                Debugger.Break();
#endif

            this.logger.LogDebug("AL error: {Error} ({ErrorCode})", AL.GetErrorString(errorCode), errorCode);
            return true;
        }

        private AlSound WavToAudioSource(string name, string wavPath)
        {
            var buffer = AL.GenBuffer();
            this.AlHasError();
            
            byte[] soundData = LoadWave(
                File.Open(wavPath, FileMode.Open),
                out var channels, out var bitsPerSample, out var sampleRate);

            AL.BufferData(buffer, GetSoundFormat(channels, bitsPerSample), soundData, sampleRate);
            this.AlHasError();

            return new AlSound
            {
                Name = name,
                Buffer = buffer
            };
        }

        private class AlSound
        {
            public string Name { get; init; }

            public int Buffer { get; init; }
        }

        public void Dispose()
        {
            var sw = Stopwatch.StartNew();
            this.logger.LogInformation("Stopping Voice service...");

            this.DisposePorcupine();
            this.DisposeSpeechClient();
            this.DisposeCaptureDevice();
            this.DisposeSounds();
            
            sw.Stop();
            this.logger.LogInformation("Voice service stopped in {Elapsed}", sw.Elapsed);
        }

        private void DisposePorcupine()
        {
            this.logger.LogDebug("Disposing Porcupine...");

            this.porcupine?.Dispose();
        }

        private void DisposeSpeechClient()
        {
            this.logger.LogDebug("Disposing DeepSpeech...");

            this.deepSpeechClient?.Dispose();
        }
    }
}
