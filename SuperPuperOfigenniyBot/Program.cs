using System;
using System.Collections.Generic;
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
        private DiscordSocketClient discord;
        private Dictionary<ulong, AudioStream> audioStreams = new Dictionary<ulong, AudioStream>();
        private Dictionary<ulong, IAudioClient> audioClients = new Dictionary<ulong, IAudioClient>();
        private Dictionary<ulong, bool> canSpeak = new Dictionary<ulong, bool>();
        static void Main()
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }
        async Task MainAsync()
        {
            discord = new DiscordSocketClient();
            discord.Log += DiscordDotNetLog;
            await discord.LoginAsync(TokenType.Bot, "ODUzNjEwNjk3NTY0NDg3Njgy.YMX" + "46Q.85kBjRY4Or0ic4k4vRjdv_mnWIw");
            await discord.StartAsync();
            discord.MessageReceived += MessageReceivedHandler;
            await Task.Delay(-1);
        }

        private async Task MessageReceivedHandler(SocketMessage arg)
        {
            ulong channelId = arg.Channel.Id;
            bool speaking = audioClients.ContainsKey(channelId);

            if (arg.Content == "/глаголь")
            {
                if (speaking) return;

                var voiceChannel = (arg.Author as IGuildUser).VoiceChannel;
                if (voiceChannel == null)
                {
                    await arg.Channel.SendMessageAsync("Зайдите в голосовой чат чтобы написать /глаголь");
                    return;
                }

                new Thread(() =>
                {
                    audioClients.Add(channelId, voiceChannel.ConnectAsync().Result);
                    audioStreams.Add(channelId, audioClients[channelId].CreatePCMStream(AudioApplication.Voice));
                    canSpeak.Add(channelId,true);
                }).Start();
            }
            else if (arg.Content == "/завались")
            {
                if (!speaking) return;

                await Speak(channelId, "Ну ладно, заваливаюсь");

                await Task.Delay(1000);
                await audioClients[channelId].StopAsync();
                audioClients.Remove(channelId);
                audioStreams.Remove(channelId);
                canSpeak.Remove(channelId);
            }
            else if (speaking)
            {
                _ = Speak(channelId, arg.Content).ConfigureAwait(false);
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
                Arguments = $"-hide_banner -loglevel error -i {pathOrUrl} -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            return process.StandardOutput.BaseStream;
        }

        async Task Speak(ulong channelId, string text)
        {
            Stream voice = TextToSpeech(text);
            while (!canSpeak[channelId])
            {
                await Task.Delay(50);
            }
            canSpeak[channelId] = false;
            await voice.CopyToAsync(audioStreams[channelId]);
            voice.Close();
            canSpeak[channelId] = true;
        }
    }
}
