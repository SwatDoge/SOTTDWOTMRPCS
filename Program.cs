using System.ServiceProcess;

namespace DiscordWOTMRPS {
    static class Program {
        static void Main() {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DiscordWOTMRPS()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
