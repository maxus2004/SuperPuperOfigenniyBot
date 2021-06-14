using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.WebSocket;
using NAudio.Wave;

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
            await discord.LoginAsync(TokenType.Bot, "O DUzNjEwNjk3NTY0NDg3Njgy.YMX46Q.pcvYAHzuPvUu665w2tTlrSD7Nz4");
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
                await audioClient.StopAsync();
                speaking = false;
            }
            else if (speaking)
            {

                while (busy)
                {
                    Thread.Sleep(50);
                }
                busy = true;
                TextToSpeech(arg.Content);
                Mp3FileReader fr = new Mp3FileReader("audio.mp3");
                WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(fr);
                WaveFormatConversionStream resampler = new WaveFormatConversionStream(new WaveFormat(96000, 1), pcm);
                resampler.CopyTo(discordAudioStream);
                fr.Close();
                busy = false;
            }
        }
        private Task DiscordDotNetLog(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        void TextToSpeech(string text)
        {
            WebClient web = new WebClient();
            web.Headers.Add(HttpRequestHeader.Referer, "http://translate.google.com/");
            web.Headers.Add(HttpRequestHeader.UserAgent, "stagefright/1.2 (Linux;Android 5.0)");
            web.DownloadFile("https://translate.google.com/translate_tts?ie=UTF-8&tl=Ru-ru&client=tw-ob&q=" + text, "audio.mp3");
        }
    }
}
