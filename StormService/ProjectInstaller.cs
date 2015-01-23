using System.ComponentModel;
using System.Configuration.Install;

namespace IoStorm.StormService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
