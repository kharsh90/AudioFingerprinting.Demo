using ATL;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NAudio.Wave;
using Newtonsoft.Json;
using SoundFingerprinting.AddictedCS.Demo.EFDatabase;
using SoundFingerprinting.AddictedCS.Demo.Repositories;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SoundFingerprinting.AddictedCS.Demo
{
    class Program
    {
        static readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
        static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        static IAudioFingerprintRepository repo = null;

        static async Task Main(string[] args)
        {
            var container = new WindsorContainer();
            container.Register(Component.For<IAudioFingerprintRepository>().ImplementedBy<AudioFingerprintRepository>());
            // Resolving
            repo = container.Resolve<IAudioFingerprintRepository>();

            await StoreForLaterRetrieval("./Generic PBX IVR -Customers.wav");
            //await StoreForLaterRetrieval("./Emma-EmailSupport.wav", "email");
            //await StoreForLaterRetrieval("./Emma-FTP.wav", "ftp");
            //await StoreForLaterRetrieval("./Emma-Hardware-Support.wav", "hardware");

            string audioPath = Path.GetFullPath("./Generic PBX IVR -Customers.wav");
            var audiobytes = ConvertAudioToByteArray(audioPath);

            TrimWavFile(audioPath, "TrimmedAudio1.wav", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            TrimWavFile(audioPath, "TrimmedAudio2.wav", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5));
            TrimWavFile(audioPath, "TrimmedAudio3.wav", TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(7));


            //Full audio is of 29 seconds. We can also sample an audio file using following way.
            var sampleAudioDuration1 = audiobytes.Length /5; // this audio is of 5 seconds
            var sampleAudioBytes1 = audiobytes.Take(sampleAudioDuration1).ToArray();
            ConvertByteArrayToAudio("SampledAudio1.wav", sampleAudioBytes1);

            var foundTrack1 = await GetBestMatchForSong("./SampledAudio1.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack1?.Id);
            var foundTrack2 = await GetBestMatchForSong("./TrimmedAudio2.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack2?.Id);
            var foundTrack3 = await GetBestMatchForSong("./TrimmedAudio3.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack3?.Id);
            var foundTrack4 = await GetBestMatchForSong("./TrimmedAudio1.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack4?.Id);
            var foundTrack5 = await GetBestMatchForSong("./Emma-EmailSupport.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack5?.Id);


            var audioTrack = new Track("./Emma-EmailSupport.wav");

            ReadMetadata(audioTrack);
            WriteMetadata(audioTrack);

            //audio to bytearray and bytearray to audio conversion
            string audioFilePath1 = Path.GetFullPath("./Emma-EmailSupport.wav");
            var audiobytes1 = ConvertAudioToByteArray(audioFilePath1);
            ConvertByteArrayToAudio("byteArrayToAudio.wav",audiobytes1);

            var audioFilePath2 = Path.GetFullPath("byteArrayToAudio.wav");
            PlayAudio(audioFilePath2);

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
            Console.Write("*************** Reading metadata of an audio file*******************");
            Console.WriteLine("Title of audio " + audioTrack.Title);
            Console.WriteLine("Bitrate of audio " + audioTrack.Bitrate);
            Console.WriteLine("Sample rate of audio " + audioTrack.SampleRate);
            foreach (var field in audioTrack.AdditionalFields)
            {
                Console.WriteLine("Key is " + field.Key);
                Console.WriteLine("value is " + field.Value);
            }

        }

        private static void WriteMetadata(Track audioTrack)
        {
            Console.Write("*************** Writing to metadata of an audio file*******************");
            audioTrack.Album = "Test Album";
            Console.WriteLine("Album name is " + audioTrack.Album);

        }

        private static void PlayAudio(string filePath)
        {
            //var bytes = File.ReadAllBytes(filePath); // as sample
            WaveStream mainOutputStream = new WaveFileReader(filePath);
            WaveChannel32 volumeStream = new WaveChannel32(mainOutputStream);

            WaveOutEvent player = new WaveOutEvent();

            player.Init(volumeStream);

            player.Play();
        }

        public static async Task<TrackData> GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = 3; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            var hashes = repo.GetAudioFingerprintHashes();
            var track = new TrackInfo("Customers IVR", "Customers IVR", "Customers IVR");
            modelService.Insert(track, hashes);
            
            // query the underlying database for similar audio sub-fingerprints
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .WithQueryConfig(new HighPrecisionQueryConfiguration())
                                                 .UsingServices(modelService, audioService)
                                                 .Query();
            var trackData = queryResult.BestMatch?.Track;
            return trackData;
        }

        public static async Task StoreForLaterRetrieval(string pathToAudioFile)
        {
            // create fingerprints
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(pathToAudioFile)
                                        .WithFingerprintConfig(new HighPrecisionFingerprintConfiguration())
                                        .UsingServices(audioService)
                                        .Hash();
            Console.WriteLine("Count of hashed fingerprints " + hashedFingerprints.Count);
            repo.SaveAudioFingerprints(hashedFingerprints);
        }
    }
}
