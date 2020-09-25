using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace DiscordWOTMRPS {
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer {
        public ProjectInstaller() {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e) {
            ServiceInstaller serviceInstaller = (ServiceInstaller)sender;
            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName)) {
                sc.Start();
            }
        }
    }
}
