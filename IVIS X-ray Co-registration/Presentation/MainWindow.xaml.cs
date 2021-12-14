using System.Windows;
using ReactiveDomain.ViewObjects;
using ReactiveUI;

namespace IVIS_X_ray_Co_registration.Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : IViewFor<MainWindowVM>
    {
        public MainWindow()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.ControlViewModel, v => v.Host.ViewModel));
            });

            //Default Error popup
            Interactions.Errors.RegisterHandler(err =>
            {
                MessageBox.Show(
                    this,
                    err.Input.ErrorMessage +
                    (err.Input.Ex.InnerException != null && err.Input.Ex.InnerException.Message != err.Input.ErrorMessage ? "\n\n" + err.Input.Ex.InnerException.Message : string.Empty),
                    "IVIS X-ray/Optical Co-Registration Tool",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // This is what the ViewModel should do in response to the user's decision
                err.SetOutput(Interactions.RecoveryOptionResult.CancelOperation);
            });
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
            "ViewModel",
            typeof(MainWindowVM),
            typeof(MainWindow),
            new PropertyMetadata(default(MainWindowVM)));

        public MainWindowVM ViewModel
        {
            get { return (MainWindowVM)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainWindowVM)value; }
        }
    }
}
