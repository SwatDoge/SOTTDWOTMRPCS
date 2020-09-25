using System.ServiceProcess;
using System;
using DiscordRPC;
using System.Timers;
using System.Diagnostics;
using DiscordWOTMRPS.Properties;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DiscordWOTMRPS {
    public partial class DiscordWOTMRPS : ServiceBase {

        private static int discord_pipe = -1;
        private readonly Timer update_cooldown;
        private readonly Timer update_totaltime;
        private readonly DiscordRpcClient client;
        private static List<string> word_list = JsonConvert.DeserializeObject<List<string>>(Settings.Default.word_list);
        static bool started = false;

        public DiscordWOTMRPS() {
            update_cooldown = new Timer(60000) { AutoReset = true };
            update_cooldown.Elapsed += update_presence;

            update_totaltime = new Timer(1000) { AutoReset = true };
            update_totaltime.Elapsed += appendTime;

            client = new DiscordRpcClient("747167926242640003", pipe: discord_pipe);
        }

        void appendTime(Object source, ElapsedEventArgs e) {
            Settings.Default.total_time = Settings.Default.total_time.Add(TimeSpan.FromSeconds(1));
            Settings.Default.Save();
            if (Settings.Default.total_time.Seconds == 0 && !started) {
                update_cooldown.Start();
                if (has_connection_to_internet() && has_discord_running()) set_presence();
                started = true;
            }
        }

        private void update_presence(Object source, ElapsedEventArgs e) {
            if (has_connection_to_internet() && has_discord_running()) set_presence();
        }

        private static string get_random_word() {
            if (word_list.Count <= 0) {
                System.Net.WebClient wc = new System.Net.WebClient();
                Settings.Default.word_list = wc.DownloadString("https://random-word-api.herokuapp.com/word?number=300&swear=1");
                Settings.Default.Save();
                word_list = JsonConvert.DeserializeObject<List<string>>(Settings.Default.word_list);
            }
            string word = word_list[0];
            Settings.Default.word_list = JsonConvert.SerializeObject(word_list);
            Settings.Default.Save();
            word_list.RemoveAt(0);
            return word;
        }

        private long get_word_count() {
            return Settings.Default.word_count;
        }

        private void upcount() {
            Settings.Default.word_count++;
            Settings.Default.Save();
        }

        private void set_presence() {
            Timestamps timestamp = new Timestamps() { Start = DateTime.UtcNow.Subtract(Settings.Default.total_time) };
            upcount();
            client.SetPresence(new RichPresence()
            {
                Details = get_random_word(),
                State = "total: " + get_word_count().ToString(),
                Timestamps = timestamp
            });
        }

        private static bool has_discord_running() {
            if (Process.GetProcessesByName("discord").Length > 0) return true;
            return false;
        }

        private static bool has_connection_to_internet() {
            try {
                using (var wc = new System.Net.WebClient())
                using (wc.OpenRead("https://www.discord.com/"))
                    return true;
            }
            catch {
                return false;
            }
        }

        protected override void OnStart(string[] args) {
            if (has_connection_to_internet() && has_discord_running()) {
                for (int i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-pipe":
                            discord_pipe = int.Parse(args[++i]);
                            break;
                        default: break;
                    }
                }
                set_presence();
                update_totaltime.Start();
                client.Initialize();
            }
        }

        protected override void OnStop() {
            update_cooldown.Stop();
            update_totaltime.Stop();
            client.Dispose();
        }
    }
}
