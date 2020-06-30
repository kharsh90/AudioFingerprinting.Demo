using ATL;
using NAudio.Wave;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SoundFingerprinting.AddictedCS.Demo
{
    class Program
    {
        static readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
        static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        //static WaveInEvent waveSource = null;
        //static WaveFileWriter waveFile = null;

        static async Task Main(string[] args)
        {
            await StoreForLaterRetrieval("./Generic PBX IVR -Customers.wav", "Customers IVR");
            await StoreForLaterRetrieval("./Emma-EmailSupport.wav", "email");
            await StoreForLaterRetrieval("./Emma-FTP.wav", "ftp");
            await StoreForLaterRetrieval("./Emma-Hardware-Support.wav", "hardware");

            
            string audioPath = Path.GetFullPath("./Generic PBX IVR -Customers.wav");
            var audiobytes = ConvertAudioToByteArray(audioPath);

            TrimWavFile(audioPath, "TrimmedAudio1.wav", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));

            //Full audio is of 29 seconds. below trying to create sample of full audio and trying to match it with full audio
            var sampleAudioDuration1 = audiobytes.Length /5; // this audio is of 5 seconds
            var sampleAudioBytes1 = audiobytes.Take(sampleAudioDuration1).ToArray();
            ConvertByteArrayToAudio("SampledAudio1.wav", sampleAudioBytes1);

            var sampleAudioDuration2 = audiobytes.Length /10; // this audio is of 2 seconds
            var sampleAudioBytes2 = audiobytes.Take(sampleAudioDuration2).ToArray();
            ConvertByteArrayToAudio("SampledAudio2.wav", sampleAudioBytes2);

            var sampleAudioDuration3 = audiobytes.Length / 100; // this audio is of 0.29 seconds
            var sampleAudioBytes3 = audiobytes.Take(sampleAudioDuration3).ToArray();
            ConvertByteArrayToAudio("SampledAudio3.wav", sampleAudioBytes3);

            var foundTrack1 = await GetBestMatchForSong("./SampledAudio1.wav");
            Console.WriteLine("Sample audio 1 :" + foundTrack1?.Id);
            var foundTrack2 = await GetBestMatchForSong("./SampledAudio2.wav");
            Console.WriteLine("Sample audio 2 :" + foundTrack2?.Id);
            var foundTrack3 = await GetBestMatchForSong("./SampledAudio3.wav");
            Console.WriteLine("Sample audio 3 :" + foundTrack3?.Id);
            var foundTrack4 = await GetBestMatchForSong("./TrimmedAudio1.wav");
            Console.WriteLine("Sample audio 4 :" + foundTrack4?.Id);


            var audioTrack = new Track("./Emma-EmailSupport.wav");

            ReadMetadata(audioTrack);
            WriteMetadata(audioTrack);

            //audio to bytearray and bytearray to audio conversion
            string audioFilePath1 = Path.GetFullPath("./Emma-EmailSupport.wav");
            var audiobytes1 = ConvertAudioToByteArray(audioFilePath1);
            ConvertByteArrayToAudio("byteArrayToAudio.wav",audiobytes1);

            var audioFilePath2 = Path.GetFullPath("byteArrayToAudio.wav");
            PlayAudio(audioFilePath2);

            

            //record voice, read and write it's metadata. 
            //Console.WriteLine("Press 1 to record your audio. Any other key to exit");
            //RecordAudio("MyAudio1");            
            //var path3 = Path.GetFullPath("MyAudio1.wav");
            //var recordedAudioTrack = new Track(path3);
            //ReadMetadata(recordedAudioTrack);
            //WriteMetadata(recordedAudioTrack);
            //PlayAudio(path3);
            
            //fingerprinting of recorded voice.
            //await StoreForLaterRetrieval(path3, "MyVoice");
            //var recordedAudioMatch = await GetBestMatchForSong(path3);

            //Console.WriteLine("Press 1 to record your audio. Any other key to exit");
            //RecordAudio("MyAudio2");
            //var path4 = Path.GetFullPath("MyAudio2.wav");
            //PlayAudio(path4);



            //Console.WriteLine(recordedAudioMatch?.Id);
            Console.ReadLine();
        
        }

        public static void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos -= startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes -= endBytes % reader.WaveFormat.BlockAlign;
                    int endPos = (int)reader.Length - endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }
        }

        private static void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = System.Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
        
        private static byte[] ConvertAudioToByteArray(string audioFilePath)
        {
            return File.ReadAllBytes(audioFilePath);
        }

        private static void ConvertByteArrayToAudio(string audioFileName,byte[] audioBytes)
        {
            // generate audio file from given byte array
            File.WriteAllBytes(audioFileName, audioBytes); 
        }
        private static void ReadMetadata(Track audioTrack)
        {
            Console.WriteLine("Title of audio " + audioTrack.Title);
            Console.WriteLine("Bitrate of audio " + audioTrack.Bitrate);
            Console.WriteLine("Sample rate of audio" + audioTrack.SampleRate);
            foreach (var field in audioTrack.AdditionalFields)
            {
                Console.WriteLine("Key is " + field.Key);
                Console.WriteLine("value is " + field.Value);
            }

        }

        private static void WriteMetadata(Track audioTrack)
        {
            audioTrack.Album = "Test Album";
            Console.WriteLine("Album name is " + audioTrack.Album);

        }
        //private static void RecordAudio(string fileName)
        //{
            
        //    var input = Console.ReadLine();
        //    while (input == "1")
        //    {
        //        StartRecording(fileName);
        //        input = Console.ReadLine();
        //    }
        //    StopRecording();
        //}
        private static void PlayAudio(string filePath)
        {
            //var bytes = File.ReadAllBytes(filePath); // as sample
            WaveStream mainOutputStream = new WaveFileReader(filePath);
            WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);

            WaveOutEvent player = new WaveOutEvent();

            player.Init(volumeStream);

            player.Play();
        }
        //private static void StartRecording(string fileName)
        //{
        //    waveSource = new WaveInEvent
        //    {
        //        WaveFormat = new NAudio.Wave.WaveFormat(44100, 1)
        //    };

        //    waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(WaveSource_DataAvailable);
        //    waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(WaveSource_RecordingStopped);

        //    waveFile = new WaveFileWriter(fileName+".wav", waveSource.WaveFormat);

        //    waveSource.StartRecording();
        //}
        //private static void StopRecording()
        //{
        //    waveSource.StopRecording();
        //}

        //static void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        //{
        //    if (waveFile != null)
        //    {
        //        waveFile.Write(e.Buffer, 0, e.BytesRecorded);
        //        waveFile.Flush();
        //    }
        //}

        //static void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        //{
        //    if (waveSource != null)
        //    {
        //        waveSource.Dispose();
        //        waveSource = null;
        //    }

        //    if (waveFile != null)
        //    {
        //        waveFile.Dispose();
        //        waveFile = null;
        //    }
        //}
        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = 3; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .WithQueryConfig(new HighPrecisionQueryConfiguration())
                                                 .UsingServices(modelService, audioService)
                                                 .Query();
            var trackData = queryResult.BestMatch?.Track;
            
            //emyModelService.RegisterMatches(queryResult.ResultEntries,trackData.MetaFields);
            return trackData;
        }

        public static async Task StoreForLaterRetrieval(string pathToAudioFile, string trackInfo)
        {
            var track = new TrackInfo(trackInfo, trackInfo, trackInfo);

            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .WithFingerprintConfig(new HighPrecisionFingerprintConfiguration())
                                        .UsingServices(audioService)
                                        .Hash();
            Console.WriteLine("Count of hashed fingerprints " + hashedFingerprints.Count);
            Console.WriteLine("DUration in seconds " + hashedFingerprints.DurationInSeconds);


            // store hashes in the database for later retrieval
            modelService.Insert(track, hashedFingerprints);
        }
    }
}
