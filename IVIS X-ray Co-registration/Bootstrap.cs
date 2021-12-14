using System;
using Greylock.Presentation.Core.ReadModels;
using Greylock.Presentation.Core.Services;
using Greylock.Presentation.Core.Utilities;
using IVIS_X_ray_Co_registration.ImageProcessing;
using IVIS_X_ray_Co_registration.Presentation;
using ReactiveDomain.Bus;
using ReactiveUI;
using Splat;
using Telerik.Windows.Controls;

namespace IVIS_X_ray_Co_registration
{
    public class Bootstrap
    {
        private IGeneralBus _mainBus;
        private ImageProcessingSvc _imgProcSvc;
        private UserNotificationService _userNotificationService;

        public void Run(string[] args)
        {
            Configure(new CommandBus(
                            "Main Bus",
                            false));

            _userNotificationService = new UserNotificationService(_mainBus);
            _imgProcSvc = new ImageProcessingSvc(_mainBus);

            // This needs to be set before the first window is created.
            StyleManager.ApplicationTheme = new Windows8Theme();

            var mainWindow = new MainWindow { ViewModel = new MainWindowVM(_mainBus) };
            mainWindow.Show();
        }

        private void Configure(IGeneralBus bus)
        {
            _mainBus = bus;
            RegisterViews();
        }

        private static void RegisterViews()
        {
            Locator.CurrentMutable.Register(() => new CoReg(), typeof(IViewFor<CoRegVM>));
        }
    }
}
