using System.Net.Mime;
using System.Windows;
using System.Windows.Media;
using ReactiveUI;

namespace IVIS_X_ray_Co_registration.Presentation
{
    /// <summary>
    /// Interaction logic for CoReg.xaml
    /// </summary>
    public partial class CoReg : IViewFor<CoRegVM>
    {
        public CoReg()
        {
            InitializeComponent();
            FolderPath.Visibility = Visibility.Hidden;

            this.WhenActivated(d =>
            {
                d(this.BindCommand(ViewModel, vm => vm.PickSequenceFolder, v => v.GetFolder));
                d(this.BindCommand(ViewModel, vm => vm.ComputeCoregistration, v => v.Compute));
                d(this.BindCommand(ViewModel, vm => vm.CancelCoregistration, v => v.Cancel));

                d(this.OneWayBind(
                            ViewModel, 
                            vm => vm.ClickFolder.FullName, 
                            v => v.FolderPath.Visibility, 
                            s => string.IsNullOrWhiteSpace(s) ? Visibility.Hidden : Visibility.Visible));

                d(this.OneWayBind(ViewModel, vm => vm.ClickFolder, v => v.UseClickInfo.IsEnabled, cf => cf != null));
                d(this.OneWayBind(ViewModel, vm => vm.ClickFolder, v => v.UseManualParams.IsEnabled, cf => cf != null));
                d(this.OneWayBind(ViewModel, vm => vm.ClickFolder, v => v.XRayThreshold.IsEnabled, cf => cf != null));
                d(this.OneWayBind(ViewModel, vm => vm.ClickFolder, v => v.OpticalThreshold.IsEnabled, cf => cf != null));
                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.ClickFolder.FullName,
                            v => v.FolderPathInstructions.Visibility,
                            s => string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Hidden));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.ClickFolder.FullName,
                            v => v.ShowXRayOverlay.IsChecked,
                            s => !string.IsNullOrWhiteSpace(s)));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.UseClickInfo,
                            v => v.ManualParams.Visibility,
                            s => s ? Visibility.Collapsed : Visibility.Visible));

                d(this.OneWayBind(
                            ViewModel, 
                            vm => vm.SelectedCalibrationInfo, 
                            v => v.Compute.IsEnabled, 
                            s => s != null));
                d(this.OneWayBind(ViewModel, vm => vm.SelectedCalibrationInfo, v => v.ClickInfoParamDetails.Text));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.IsComputing,
                            v => v.Cancel.Visibility,
                            s => s ? Visibility.Visible : Visibility.Hidden));

                d(this.OneWayBind(ViewModel, vm => vm.CalibrationTypes, v => v.CalibrationType.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.ComputationDetails, v => v.ComputationDetails.Text));
                d(this.OneWayBind(ViewModel, vm => vm.XRayThreshold, v => v.XRayThresholdNumber.Text));
                d(this.OneWayBind(ViewModel, vm => vm.OpticalThreshold, v => v.OpticalThresholdNumber.Text));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.HaveResultsToDisplay,
                            v => v.ShowXRayOverlay.IsEnabled));


                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.HaveResultsToDisplay,
                            v => v.Results.Visibility,
                            s => s ? Visibility.Visible : Visibility.Hidden));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.BestFitScale,
                            v => v.BestFitScale.Text,
                            s => $"Scale: {s:F3}  "));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.BestFitXOffset,
                            v => v.BestFitXOffset.Text,
                            xo => $"X offset: {xo:F2}   "));

                d(this.OneWayBind(
                            ViewModel, 
                            vm => vm.BestFitYOffset, 
                            v => v.BestFitYOffset.Text, 
                            yo => $"Y offset: {yo:F2}   "));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.BestFitMeanDifference,
                            v => v.MeanDifference.Text,
                            m => $"Average error: {m:F2}   "));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.BestFitMaxDifference,
                            v => v.MaxDifference.Text,
                            m => $"Y offset: {m:F2}   "));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.HaveResultsToDisplay,
                            v => v.OutputPhotoImage.Visibility,
                            r => r ? Visibility.Visible : Visibility.Hidden));

                d(this.OneWayBind(
                            ViewModel,
                            vm => vm.HaveResultsToDisplay,
                            v => v.OutputXrayImage.Visibility,
                            r => r ? Visibility.Visible : Visibility.Hidden));

                d(this.OneWayBind(
                            ViewModel,
                            vm => (ImageSource)vm.PhotoDisplayImage,
                            v => v.OutputPhotoImage.Source));

                d(this.OneWayBind(
                            ViewModel,
                            vm => (ImageSource)vm.XRayDisplayImage,
                            v => v.OutputXrayImage.Source));

                d(this.OneWayBind(
                            ViewModel,
                            vm => (ImageSource) vm.XRayDisplayImage,
                            v => v.XRayOverlayImage.ImageSource));

                d(this.OneWayBind(
                            ViewModel, 
                            vm => vm.ShowXRayOverlay,
                            v => v.XRayOverlayMask.Visibility,
                            s => s ? Visibility.Visible : Visibility.Hidden));

                d(this.Bind(ViewModel, vm => vm.ClickFolder.FullName, v => v.FolderPath.Text));
                d(this.Bind(ViewModel, vm => vm.UseClickInfo, v => v.UseClickInfo.IsChecked));
                d(this.Bind(ViewModel, vm => vm.SelectedCalibrationType, v => v.CalibrationType.SelectedItem));
                d(this.Bind(ViewModel, vm => vm.Scale, v => v.Scale.Value));
                d(this.Bind(ViewModel, vm => vm.XOffset, v => v.XOffset.Value));
                d(this.Bind(ViewModel, vm => vm.YOffset, v => v.YOffset.Value));
                d(this.Bind(ViewModel, vm => vm.XRayThreshold, v => v.XRayThreshold.Value));
                d(this.Bind(ViewModel, vm => vm.OpticalThreshold, v => v.OpticalThreshold.Value));

                d(this.Bind(ViewModel, vm => vm.IterationsComplete, v => v.ComputationProgress.Value));
                d(this.Bind(ViewModel, vm => vm.MaxIterations, v => v.ComputationProgress.Maximum));

                d(this.Bind(ViewModel, vm => vm.ShowXRayOverlay, v => v.ShowXRayOverlay.IsChecked));

            });
        }

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
                                                                        "ViewModel",
                                                                        typeof(CoRegVM),
                                                                        typeof(CoReg),
                                                                        new PropertyMetadata(default(CoRegVM)));

        public CoRegVM ViewModel
        {
            get => (CoRegVM)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (CoRegVM)value;
        }
    }
}
