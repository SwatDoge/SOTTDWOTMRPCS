using System.ServiceProcess;
using System;
using DiscordRPC;
using System.Timers;
using System.Diagnostics;
using DiscordWOTMRPS.Properties;
using System.Collections.Generic;
using Newtonsoft.Json;


//Settings variables:
//Default.word_list  (string) the current list of words being used.
//Default.total_time (timespan) the total time spend with the service in seconds. (This gets deducted from the current time to get a starting timestamp.)
//Default.word_count (int) a count of the amount of words used.

namespace DiscordWOTMRPS {
    public partial class DiscordWOTMRPS : ServiceBase {

        private static int discord_pipe = -1;           //discord pipe stuff, dont worry too much about this.
        private readonly Timer update_cooldown;         //cooldown of 60 seconds which updates the presence
        private readonly Timer update_totaltime;        //cooldown of 1 second which updates the total seconds the services is used.
        private readonly DiscordRpcClient client;       //reference to the discord client.
        private static List<string> word_list = JsonConvert.DeserializeObject<List<string>>(Settings.Default.word_list);    //unserialized wordlist (a settings variable.)
        static bool started = false;                    //after rebooting, the service will wait for the time to round up to 00 before selecting a new word.

        public DiscordWOTMRPS() { //init
            update_cooldown = new Timer(60000) { AutoReset = true };
            update_cooldown.Elapsed += update_presence;

            update_totaltime = new Timer(1000) { AutoReset = true };
            update_totaltime.Elapsed += appendTime;

            client = new DiscordRpcClient("747167926242640003", pipe: discord_pipe);

            Settings.Default.total_time = TimeSpan.Zero;
            Settings.Default.word_count = 0;
            Settings.Default.word_list = "[]";
            Settings.Default.Save();
        }

        void appendTime(Object source, ElapsedEventArgs e) {
            //if internet and discord client, add a second to time spend on the service.
            if (has_connection_to_internet() && has_discord_running()) {
                Settings.Default.total_time = Settings.Default.total_time.Add(TimeSpan.FromSeconds(1));
                Settings.Default.Save();
                //if seconds is 0 (16:00 for instance), start updating the rpc with new words, if it hasnt already.
                if (!started && Settings.Default.total_time.Seconds == 0) {
                    update_cooldown.Start();
                    up_word_count();
                    set_presence();
                    started = true;
                }
            }
        }

        private void update_presence(Object source, ElapsedEventArgs e) {
            if (has_connection_to_internet() && has_discord_running()) {
                up_word_count();
                set_presence();
            }
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

        private void up_word_count() {
            Settings.Default.word_count++;
            Settings.Default.Save();
        }

        private void set_presence() {
            Timestamps timestamp = new Timestamps() { Start = DateTime.UtcNow.Subtract(Settings.Default.total_time) };
            client.SetPresence(new RichPresence()
            {
                Details = get_random_word(),
                State = "total: " + get_word_count().ToString(),
                Timestamps = timestamp
            });
        }

        private static bool has_discord_running() {
            return Process.GetProcessesByName("discord").Length > 0;
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
