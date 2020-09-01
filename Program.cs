using System;
using Topshelf;

//Created using Topshelf documentation: https://dotnetcoretutorials.com/2019/09/27/creating-windows-services-in-net-core-part-2-the-topshelf-way/
namespace DiscordWOTM {
    class Program {
        static void Main(string[] args) {
            var exitCode = HostFactory.Run(x =>{
                x.Service<RPCService>(s =>{ //Manage when services run.
                    s.ConstructUsing(service => new RPCService());
                    s.WhenStarted(service => service.Start(args));
                    s.WhenStopped(service => service.Stop());
                });
                //Set service settings.
                x.RunAsLocalSystem();

                x.SetDisplayName("Swats WOTM discord RPC Service");
                x.SetServiceName("SwatsWOTMDiscordRPC");
                x.SetDescription("This service will update a discord status at any time.");
                x.StartAutomaticallyDelayed();
            });

            int exitCodeValue = (int)Convert.ChangeType(exitCode, exitCode.GetTypeCode());
            Environment.ExitCode = exitCodeValue;
        }
    }
}
