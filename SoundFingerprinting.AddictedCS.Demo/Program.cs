using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NAudio.Wave;
using Newtonsoft.Json;
using SoundFingerprinting.AddictedCS.Demo.Repositories;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Configuration;
using SoundFingerprinting.Data;
using SoundFingerprinting.Extensions.LMDB;
using SoundFingerprinting.InMemory;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SoundFingerprinting.AddictedCS.Demo
{
    public class Program
    {
        static readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
        static readonly IModelService lmdbModelService = new LMDBModelService(Path.GetFullPath("DB"));
        static readonly IAudioService audioService = new SoundFingerprintingAudioService(); // default audio library
        static IAudioFingerprintRepository repo = null;
        private static Dictionary<string, Hashes> _audioFingerprints = new Dictionary<string, Hashes>();
        private static int _dbStorageChoice = 3;
        private static readonly string _filePath = @"C:\Users\harshadk.MUMBAI1\source\repos\AudioFingerprinting.Demo\SoundFingerprinting.AddictedCS.Demo\Fingerprints.txt";

        static async Task Main(string[] args)
        {
            RegisterComponents();

            Console.WriteLine("---***** Welcome to ABC Organization. Choose your option from below Menu *****---");
            Console.WriteLine("Press 1 - To generate Audiofingerprints of audio files");
            Console.WriteLine("Press 2 - To save Audio fingerprints in File system");
            Console.WriteLine("Press 3 - To save Audio fingerprints in PostgrSQL database");
            Console.WriteLine("Press 4 - To save Audio fingerprints in LMDB database");
            Console.WriteLine("Press 5 - To match an audio with generated audio fingerprints");
            Console.WriteLine("Press 6 - To explore more audio samples with generated audio fingerprints");
            Console.WriteLine("Press any other digit to exit");

            var userInput = int.Parse(Console.ReadLine());
            while(userInput > 0 && userInput <= 6)
            {
                switch(userInput)
                {
                    case 1: Console.WriteLine("Please mention name of an audio file to generate its audio fingerprints");
                        var audioFileName = Console.ReadLine();
                        try
                        {
                            await GenerateAudioFingerprints(audioFileName);
                            Console.WriteLine("Audio fingerprints of " + audioFileName + " generated successfully");
                        }
                        catch
                        {
                            Console.WriteLine("Some error occured while generating audio fingerprints");
                        }
                        break;
                    case 2: _dbStorageChoice = userInput;
                        SaveAudioFingerprintsInFile();
                        
                        break;
                    case 3:
                        _dbStorageChoice = userInput;
                        SaveAudioFingerprintsInPostgreSql();
                        break;
                    case 4:
                        _dbStorageChoice = userInput; 
                        SaveAudioFingerprintsInLMDB();
                        break;
                    case 5: Console.WriteLine("Please mention sample file name to match audio");
                        var sampleAudioFileName = Console.ReadLine();
                        var resultEntry = await GetBestMatchForSong(sampleAudioFileName);
                        Console.WriteLine("Matching audio is " + resultEntry?.Track.Id);
                        Console.WriteLine("Confidence is " + resultEntry?.Confidence);
                        break;
                    case 6: await ExploreMoreAudioSamples();
                        break;
                    default: Console.WriteLine("Please select valid menu option");
                        break;
                }
                Console.WriteLine("Please select next operation to perform");
                userInput = int.Parse(Console.ReadLine());
            }

            Console.ReadLine();
        
        }

        private static async Task ExploreMoreAudioSamples()
        {
            Console.WriteLine("Testing audio sample with background noise");
            var foundTrack = await GetBestMatchForSong("./TrimmedAudio-TwiceRecorded.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample with faded audio");
            foundTrack = await GetBestMatchForSong("./WelcomeABC_Alice_Faded_Audio.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample in Alice's missing voice");
            foundTrack = await GetBestMatchForSong("./WelcomeABC_With_Missing_Audio_2.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample with missing audio. Trimmed one");
            foundTrack = await GetBestMatchForSong("./Trimmed_WelcomeABC_WIth_Missing_Audio.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample with missing audio. To missing");
            foundTrack = await GetBestMatchForSong("./Trimmed_WelcomeABC_WIth_Missing_Audio.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample in Alice's voice");
            foundTrack = await GetBestMatchForSong("./TrimmedAudio-Alice.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample in George's voice");
            foundTrack = await GetBestMatchForSong("./TrimmedAudio-George.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);

            Console.WriteLine("Testing audio sample in Jenna's voice");
            foundTrack = await GetBestMatchForSong("./TrimmedAudio-Jenna.wav");
            Console.WriteLine("Best matching audio track is :" + foundTrack?.Track.Id);
            Console.WriteLine("Confidence level of audio matching is :" + foundTrack?.Confidence);


        }
        private static void SaveAudioFingerprintsInFile()
        {
            File.WriteAllText(_filePath,JsonConvert.SerializeObject(_audioFingerprints));
            Console.WriteLine("Audio fingerprints saved successfully in a file.");
        }

        private static void SaveAudioFingerprintsInPostgreSql()
        {
            foreach(var audiofingerprint in _audioFingerprints)
            {
                repo.SaveAudioFingerprints(audiofingerprint.Value, audiofingerprint.Key);
            }
            Console.WriteLine("Audio fingerprints saved successfully in PostgrSql database");
        }

        private static void SaveAudioFingerprintsInLMDB()
        {
            foreach (var audiofingerprint in _audioFingerprints)
            {
                var key = audiofingerprint.Key;
                var value = audiofingerprint.Value;
                lmdbModelService.Insert(new TrackInfo(key,key,key), value);
            }
            Console.WriteLine("Audio fingerprints saved successfully in LMDB database");
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
        
        public static async Task<ResultEntry> GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = 3; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining
            var queryAudioFilePath = "./" + queryAudioFile;
            if(_dbStorageChoice == 2)
            {
                
                var audioFingerprints = JsonConvert.DeserializeObject<Dictionary<string,List<HashedFingerprint>>>(
                    File.ReadAllText(_filePath));
                foreach (var audioFingerprint in audioFingerprints)
                {
                    modelService.Insert(new TrackInfo(audioFingerprint.Key, audioFingerprint.Key, audioFingerprint.Key), new Hashes(audioFingerprint.Value,1));
                }
            }
            else if(_dbStorageChoice == 3)
            {
                var audioFingerprints = repo.GetAudioFingerprintHashes();
                foreach(var audioFingerprint in audioFingerprints )
                {
                    modelService.Insert(new TrackInfo(audioFingerprint.Key,audioFingerprint.Key,audioFingerprint.Key), new Hashes(audioFingerprint.Value,1));
                }
            }
            else if(_dbStorageChoice == 4)
            {
                var lmdbQueryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                         .From(queryAudioFilePath, secondsToAnalyze, startAtSecond)
                                         .WithQueryConfig(new HighPrecisionQueryConfiguration())
                                         .UsingServices(lmdbModelService, audioService)
                                         .Query();
                return lmdbQueryResult.BestMatch;
            }
            else
            {
                throw new NotImplementedException();
            }
            var queryResult = await QueryCommandBuilder.Instance.BuildQueryCommand()
                                                .From(queryAudioFilePath, secondsToAnalyze, startAtSecond)
                                                .WithQueryConfig(new HighPrecisionQueryConfiguration())
                                                .UsingServices(modelService, audioService)
                                                .Query();
            return queryResult.BestMatch;
        }

        public static async Task GenerateAudioFingerprints(string audioFileName)
        {
            var trackName = GetTrackName(audioFileName);
            var hashedFingerprints = await FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From("./"+audioFileName)
                                        .WithFingerprintConfig(new HighPrecisionFingerprintConfiguration())
                                        .UsingServices(audioService)
                                        .Hash();
            Console.WriteLine("Total audio fingerprints generated of " + audioFileName + " are " +hashedFingerprints.Count);
            if(!_audioFingerprints.ContainsKey(trackName))
            {
                _audioFingerprints.Add(trackName, hashedFingerprints);
            }
            
        }

        private static string GetTrackName(string audioFileName)
        {
            string trackName;
            switch (audioFileName)
            {
                case "WelcomeABC_Alice.wav": trackName = "Welcome Audio by Alice"; break;
                case "WelcomeABC_George.wav": trackName = "Welcome Audio by George"; break;
                case "WelcomeABC_Jenna.wav": trackName = "Welcome Audio by Jenna"; break;
                case "AppointmentScheduling_AliceVoice.wav": trackName = "Appointment scheduling audio by Alice"; break;
                case "AppointmentTypes_AliceVoice.wav": trackName = "Appointment types audio by Alice"; break;
                default: throw new NotImplementedException();
            }
            return trackName;
        }

        private static void RegisterComponents()
        {
            var container = new WindsorContainer();
            container.Register(Component.For<IAudioFingerprintRepository>().ImplementedBy<AudioFingerprintRepository>());
            // Resolving
            repo = container.Resolve<IAudioFingerprintRepository>();
        }
    }
}
