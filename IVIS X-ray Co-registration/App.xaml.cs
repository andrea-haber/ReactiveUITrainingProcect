using System.Reflection;
using System.Windows;
using NLog;

namespace IVIS_X_ray_Co_registration
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILogger Log = LogManager.GetLogger("Co-reg");
        private Bootstrap _bootstrap;
        public string AssemblyName { get; set; }

        public App()
        {
            var fullName = Assembly.GetExecutingAssembly().FullName;
            Log.Info($"{fullName} Loaded.");
            AssemblyName = fullName.Split(',')[0];
            _bootstrap = new Bootstrap();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _bootstrap.Run(e.Args);
        }
    }
}
