using DiscordRPC;
using DiscordWOTMRPCS.Properties;
using Newtonsoft.Json;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using System.Windows.Forms;

using Timer = System.Timers.Timer;

namespace DiscordWOTM {
    public class RPCService {

        private static int discord_pipe = -1;
        private readonly Timer update_cooldown;
        private readonly Timer update_totaltime;
        private readonly DiscordRpcClient client;
        private static List<string> word_list = JsonConvert.DeserializeObject<List<string>>(Settings.Default.word_list);
        static bool started = false;

        public RPCService() { //Setup one time requirements.
            update_cooldown = new Timer(60000) { AutoReset = true };
            update_cooldown.Elapsed += update_presence;

            update_totaltime = new Timer(1000) { AutoReset = true };
            update_totaltime.Elapsed += appendTime;

            client = new DiscordRpcClient("747167926242640003", pipe: discord_pipe) {};
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

        private static string get_random_word() { //Get a random word thru random-word-api
            if (word_list.Count <= 0) {
                System.Net.WebClient wc = new System.Net.WebClient();
                Settings.Default.word_list = wc.DownloadString("https://random-word-api.herokuapp.com/word?number=300&swear=1");
                Settings.Default.Save();
                word_list = JsonConvert.DeserializeObject<List<string>>(Settings.Default.word_list);
                CheckForUpdate();
            }
            string word = word_list[0];
            Settings.Default.word_list = JsonConvert.SerializeObject(word_list);
            Settings.Default.Save();
            word_list.RemoveAt(0);
            return word;
        }

        private long get_word_count() {
            Settings.Default.word_count++;
            Settings.Default.Save();
            return Settings.Default.word_count;
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

        private static bool has_discord_running() { //Checks if the discord executable is running.
            if (Process.GetProcessesByName("discord").Length > 0) return true;
            return false;
        }

        private static bool has_connection_to_internet() { //Checks if discord is online.
            try {
                using (var wc = new System.Net.WebClient())
                using (wc.OpenRead("https://discord.com/"))
                    return true;
            }
            catch {
                return false;
            }
        }

        public void Start(string[] args) { //Start function when service starts.
            if (has_connection_to_internet() && has_discord_running()) {
                for (int i = 0; i < args.Length; i++) { //Set pipeline
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
                CheckForUpdate();
            }
        }

        private static async void CheckForUpdate() {
            try {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/SwatDoge/Discord-WOTM-RPC-Service")) {
                    UpdateManager updateManager = mgr;
                    var release = await mgr.UpdateApp();
                }
            }
            catch (Exception ex) {
                string message = ex.Message + Environment.NewLine;
                if (ex.InnerException != null)
                    message += ex.InnerException.Message;
                MessageBox.Show(message);
            }
        }

        public void Stop() { //Stops and disposes on server close.
            update_cooldown.Stop();
            update_totaltime.Stop();
            client.Dispose();
        }
    }
}
