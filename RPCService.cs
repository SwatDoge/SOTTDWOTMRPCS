using System;
using DiscordRPC;
using System.Timers;
using System.Diagnostics;

namespace DiscordWOTM {
    public class RPCService {

        private string random_word = "";
        private static int discord_pipe = -1;
        private readonly Timer update_cooldown;
        private readonly DiscordRpcClient client;

        public RPCService() { //Setup one time requirements.
            update_cooldown = new Timer(60000) { AutoReset = true };
            update_cooldown.Elapsed += update_presence;
            client = new DiscordRpcClient("747167926242640003", pipe: discord_pipe) {/*Logger = new DiscordRPC.Logging.ConsoleLogger(DiscordRPC.Logging.LogLevel.Info, true)*/};
        }

        private static string get_random_word() { //Get a random word thru random-word-api
            System.Net.WebClient wc = new System.Net.WebClient();
            return wc.DownloadString("https://random-word-api.herokuapp.com/word?number=1&swear=0").Trim('[', ']', '\"');
        }

        private void update_presence(Object source, ElapsedEventArgs e) { //Updates presence every 60 seconds.
            if (has_connection_to_internet() && has_discord_running()) {
                random_word = get_random_word();
                client.SetPresence(new RichPresence() { State = random_word });
            }
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
                random_word = get_random_word();
                client.SetPresence(new RichPresence() { State = random_word });
                update_cooldown.Start();
                client.Initialize();
            }
        }

        public void Stop() { //Stops and disposes on server close.
            update_cooldown.Stop();
            client.Dispose();
        }
    }
}
