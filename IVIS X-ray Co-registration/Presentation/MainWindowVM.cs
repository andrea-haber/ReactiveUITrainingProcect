using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Greylock.Common.ViewModels;
using ReactiveDomain.Bus;
using ReactiveUI;

namespace IVIS_X_ray_Co_registration.Presentation
{
    public class MainWindowVM : ViewModel
    {
        public MainWindowVM(IGeneralBus bus)
            : base(bus)
        {
            ControlViewModel = new CoRegVM(bus);
        }

        public IViewModel ControlViewModel
        {
            get { return _controlViewModel; }
            set { this.RaiseAndSetIfChanged(ref _controlViewModel, value); }
        }
        private IViewModel _controlViewModel;
    }
}
