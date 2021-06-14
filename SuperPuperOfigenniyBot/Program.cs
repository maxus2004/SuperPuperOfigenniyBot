using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;

namespace SuperPuperOfigenniyBot
{
    class Program
    {
        private AudioStream discordAudioStream;
        private bool busy = false;
        private DiscordSocketClient discord;
        private IAudioClient audioClient;
        private bool speaking = false;

        static void Main()
        {
            Console.WriteLine("starting bot...");
            new Program().MainAsync().GetAwaiter().GetResult();
        }
        async Task MainAsync()
        {
            discord = new DiscordSocketClient();
            discord.Log += DiscordDotNetLog;
            await discord.LoginAsync(TokenType.Bot, "ODUzNjEwNjk3NTY0NDg3Njgy.YMX46Q.pcvYAHzuPvUu665w2tTlrSD7Nz4");
            await discord.StartAsync();
            discord.MessageReceived += MessageReceivedHandler;
            await Task.Delay(-1);
        }

        private async Task MessageReceivedHandler(SocketMessage arg)
        {
            if (arg.Content == "/глаголь")
            {
                if (speaking) return;
                new Thread(() =>
                {
                    var voiceChannel = (arg.Author as IGuildUser).VoiceChannel;
                    if (voiceChannel == null) return;
                    audioClient = voiceChannel.ConnectAsync().Result;
                    discordAudioStream = audioClient.CreatePCMStream(AudioApplication.Voice);
                    speaking = true;
                }).Start();
            }
            else if (arg.Content == "/завались")
            {
                if (!speaking) return;

                while (busy)
                {
                    Thread.Sleep(50);
                }
                busy = true;
                Stream voice = TextToSpeech("ну ладно, заваливаюсь");
                await voice.CopyToAsync(discordAudioStream);
                voice.Close();
                busy = false;

                await Task.Delay(1000);

                speaking = false;

                await audioClient.StopAsync();
            }
            else if (speaking)
            {

                while (busy)
                {
                    Thread.Sleep(50);
                }
                busy = true;
                Stream voice = TextToSpeech(arg.Content);
                await voice.CopyToAsync(discordAudioStream);
                voice.Close();
                busy = false;
            }
        }
        private Task DiscordDotNetLog(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        Stream TextToSpeech(string text)
        {
            string pathOrUrl = "https://translate.google.com/translate_tts?ie=UTF-8&tl=Ru-ru&client=tw-ob&q=" + WebUtility.UrlEncode(text);
            var process = Process.Start(new ProcessStartInfo
            { 
                FileName = "ffmpeg",
                Arguments = $"-i {pathOrUrl} " + 
                            "-f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            return process.StandardOutput.BaseStream;
        }
    }
}
