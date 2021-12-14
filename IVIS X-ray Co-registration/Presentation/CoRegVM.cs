using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Greylock.Common.Messages;
using Greylock.Common.ViewModels;
using Greylock.StudyManagement.Utilities;
using IVIS_X_ray_Co_registration.Messages;
using IVIS_X_ray_Co_registration.Utilities;
using ReactiveDomain.Bus;
using ReactiveDomain.Util;
using ReactiveDomain.ViewObjects;
using ReactiveUI;

namespace IVIS_X_ray_Co_registration.Presentation
{
    public class CoRegVM : ViewModel
    {
        #region Fields

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private CoRegRM _rm;

        #endregion

        #region ReactiveCommands

        public ReactiveCommand<Unit, Unit> PickSequenceFolder { get; }
        public ReactiveCommand<Unit, Unit> ComputeCoregistration { get; }
        public ReactiveCommand<Unit, Unit> CancelCoregistration { get; }

        #endregion

        public CoRegVM(IGeneralBus bus)
            : base(bus)
        {
            _rm = new CoRegRM(bus);

            OpticalThreshold = -1;
            XRayThreshold = -2;
            Scale = 0.0;

            CalibrationTypes = new ReactiveList<CoRegCalibrationInfo.CalibrationType>();
            Calibrations = new ReactiveList<CoRegCalibrationInfo>();

            SelectedCalibrationInfo = new CoRegCalibrationInfo(
                                            CoRegCalibrationInfo.CalibrationType.None,
                                            10M,
                                            0.892M,
                                            -1.0M,
                                            0M);

            #region commands

            CancellationTokenSource _tokenSource = new CancellationTokenSource();

            ComputeCoregistration = bus.BuildFireCommand(
                () =>

                    new XrayCoregMsgs.CoregisterImages(
                        ClickFolder,
                        UseClickInfo ? SelectedCalibrationInfo.Scale : (decimal) Scale,
                        UseClickInfo ? SelectedCalibrationInfo.XOffset : (decimal) XOffset,
                        UseClickInfo ? SelectedCalibrationInfo.YOffset : (decimal) YOffset,
                        XRayThreshold,
                        OpticalThreshold,
                        Guid.NewGuid(),
                        null,
                        _tokenSource.Token
                    ),
                    userErrorMsg : "help!",
                    responseTimeout:  Consts.DefaultUiTimeout);


            CancelCoregistration = ReactiveCommand.Create(
                () =>
                {
                    _tokenSource.Cancel();
                    _tokenSource = new CancellationTokenSource();
                });


            PickSequenceFolder = bus.BuildFireCommand(
                () =>
                    new UserMsgs.ShowPickFolderDialog(
                        "Select Sequence Folder",
                        (path, c, s) =>
                        {
                            try
                            {
                                var calibrations = GetCoRegCalibrationFromClickInfo(new DirectoryInfo(path))
                                    .ToList();
                                Threading.RunOnUiThread(() =>
                                {
                                    Calibrations.Clear();
                                    CalibrationTypes.Clear();
                                    Calibrations.AddRange(calibrations);

                                    foreach (var calibration in calibrations)
                                        CalibrationTypes.Add(calibration.CalType);

                                    SelectedCalibrationType = CalibrationTypes[0];
                                    SelectedCalibrationInfo = Calibrations[0];
                                    ClickFolder = new DirectoryInfo(path);

                                    bus.Publish(new XrayCoregMsgs.CoregistrationDataLoaded(Guid.NewGuid(), Guid.Empty));
                                });
                            }
                            catch
                            {
                                Threading.RunOnUiThread(() =>
                                {
                                    Calibrations.Clear();
                                    if (CalibrationTypes.Any(type =>
                                        type != CoRegCalibrationInfo.CalibrationType.None))
                                    {
                                        CalibrationTypes.Clear();
                                        CalibrationTypes.Add(CoRegCalibrationInfo.CalibrationType.None);
                                        SelectedCalibrationType = CalibrationTypes[0];
                                    }

                                    // TODO: default folder?
                                    ClickFolder = new DirectoryInfo("C:\\");
                                });
                            }
                        },
                        Guid.NewGuid(),
                        null),
                responseTimeout:
                Consts.DefaultUiTimeout);

            #endregion commands

            #region RM observations

            _rm.ComputationState
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case CoRegRM.CoregComputationState.Computing:
                            IsComputing = true;
                            ComputationDetails = "Computing...";
                            break;
                        case CoRegRM.CoregComputationState.Cancelled:
                            IsComputing = false;
                            ComputationDetails = "ComputationCancelled. Any results displayed may not be the best fit.";
                            break;
                        case CoRegRM.CoregComputationState.Complete:
                            IsComputing = false;
                            ComputationDetails = "Computation Complete.";
                            break;
                        case CoRegRM.CoregComputationState.NotStarted:
                            IsComputing = false;
                            ComputationDetails = "Idle";
                            break;
                    }
                });

            _rm.IterationsComplete
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.IterationsComplete, out _iterationsComplete);

            _rm.MaxIterations
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.MaxIterations, out _maxIterations);

            _rm.FitScale
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BestFitScale, out _bestFitScale);

            _rm.FitXOffset
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BestFitXOffset, out _bestFitXOffset);

            _rm.FitYOffset
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BestFitYOffset, out _bestFitYOffset);

            _rm.FitMeanDiff
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BestFitMeanDifference, out _bestFitMeanDifference);

            _rm.FitMaxDiff
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.BestFitMaxDifference, out _bestFitMaxDifference);

            _rm.HaveResultsToDisplay
                .ObserveOn(RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .Subscribe(x => HaveResultsToDisplay = x);

            _rm.PhotoDisplayImage
                .ObserveOn(RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .Subscribe(x => PhotoDisplayImage = x);

            _rm.XRayDisplayImage
                .ObserveOn(RxApp.MainThreadScheduler)
                .DistinctUntilChanged()
                .Subscribe(x => XRayDisplayImage = x);

            #endregion RM observations

            #region subscriptions

            this.WhenAnyValue(vm => vm.SelectedCalibrationType)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(vm => SelectedCalibrationInfo = Calibrations.FirstOrDefault(
                    c => c.CalType == SelectedCalibrationType));

            this.WhenAnyValue(vm => vm.ClickFolder)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(vm => UseClickInfo = ClickFolder != null && SelectedCalibrationInfo != null);

            #endregion subscriptions

        }

        #region Private Methods

        private CoRegCalibrationInfo[] GetCoRegCalibrationFromClickInfo(DirectoryInfo clickFolder)
        {
            var calList = new List<CoRegCalibrationInfo>();
            var files = Directory.GetFiles(clickFolder.FullName, "ClickInfo.txt");
            if (files.Any())
            {
                try
                {
                    var lines = File.ReadLines(files[0]);
                    var safeLines = lines as string[] ?? lines.ToArray();

                    var fovLine = safeLines.First(l => l.Contains("Field of View:"));
                    var fov = decimal.Parse(fovLine.Split(':')[1].Trim());

                    var imagingModeLine = safeLines.First(l => l.Contains("Imaging Mode"));
                    var imagingMode = imagingModeLine.Split(':')[1].Trim();

                    var calLines = safeLines.Where(l => l.Contains($"FOV {fov}") && l.StartsWith("Xray"));
                    foreach (var calLine in calLines)
                    {
                        var calTypeName = calLine.Split(' ')[0];
                        var calParams = calLine.Split(':')[1].Trim().Split(',');
                        var scale = decimal.Parse(calParams[0].Trim());
                        var xOffset = decimal.Parse(calParams[1].Trim());
                        var yOffset = decimal.Parse(calParams[2].Trim());
                        CoRegCalibrationInfo.CalibrationType calType;
                        switch (calTypeName)
                        {
                            case "Xray":
                                if (imagingMode != "Mouse") continue;
                                calType = CoRegCalibrationInfo.CalibrationType.Mouse;
                                break;
                            case "Xray2":
                                if (imagingMode != "Large Animal") continue;
                                calType = CoRegCalibrationInfo.CalibrationType.LargeAnimal;
                                break;
                            case "Xray3":
                                if (imagingMode != "Large Animal") continue;
                                calType = CoRegCalibrationInfo.CalibrationType.MVI2;
                                break;
                            case "Xray4":
                                if (imagingMode != "High Resolution") continue;
                                calType = CoRegCalibrationInfo.CalibrationType.HighRes;
                                break;
                            default:
                                throw new Exception($"Unexpected X-ray resizing coefficient type: {calTypeName}.");
                        }
                        calList.Add(new CoRegCalibrationInfo(
                                            calType,
                                            fov,
                                            scale,
                                            xOffset,
                                            yOffset));
                    }
                }
                catch (Exception e)
                {
                    Task.Run(() =>
                        Bus.Fire(new UserMsgs.WarnUser(
                                        "Error Reading ClickInfo",
                                        $"An error occurred reading the ClickInfo file. The file may be missing X-ray calibration info.\n\n{e.Message}",
                                        Guid.NewGuid(),
                                        null),
                                 responseTimeout: Consts.DefaultUiTimeout));
                    throw;
                }
            }
            else
            {
                Task.Run(() =>
                    Bus.Fire(new UserMsgs.WarnUser(
                                    "Not a Click Folder",
                                    "The selected folder does not contain a ClickInfo file. Please select a different folder.",
                                    Guid.NewGuid(),
                                    null),
                             responseTimeout: Consts.DefaultUiTimeout));
                throw new Exception("The selected folder does not contain a ClickInfo file. Please select a different folder.");
            }

            return calList.ToArray();
        }

        #endregion

        #region Reactive Properties, Reactive Lists, and Output Properties

        public ReactiveList<CoRegCalibrationInfo.CalibrationType> CalibrationTypes { get; }

        public DirectoryInfo ClickFolder
        {
            get => _clickFolder;
            set => this.RaiseAndSetIfChanged(ref _clickFolder, value);
        }
        private DirectoryInfo _clickFolder;

        public bool UseClickInfo
        {
            get => _useClickInfo;
            set => this.RaiseAndSetIfChanged(ref _useClickInfo, value);
        }
        private bool _useClickInfo;

        private ReactiveList<CoRegCalibrationInfo> Calibrations { get; }

        public CoRegCalibrationInfo.CalibrationType SelectedCalibrationType
        {
            get => _selectedCalibrationType;
            set => this.RaiseAndSetIfChanged(ref _selectedCalibrationType, value);
        }
        private CoRegCalibrationInfo.CalibrationType _selectedCalibrationType;

        public CoRegCalibrationInfo SelectedCalibrationInfo
        {
            get => _selectedCalibrationInfo;
            set => this.RaiseAndSetIfChanged(ref _selectedCalibrationInfo, value);
        }
        private CoRegCalibrationInfo _selectedCalibrationInfo;

        public WriteableBitmap XRayDisplayImage
        {
            get => _xRayDisplayImage;
            set
            {
                this.RaiseAndSetIfChanged(ref _xRayDisplayImage, value);
                if (_xRayDisplayImage != null)
                {
                    Threading.RunOnUiThread(
                        () =>
                        {
                            _xRayDisplayImage.Lock();
                            Memory.CopyMemory(
                                    _xRayDisplayImage.BackBuffer, 
                                    value.BackBuffer,
                                    (uint) (_xRayDisplayImage.Width * XRayDisplayImage.Height));

                            _xRayDisplayImage.AddDirtyRect( new Int32Rect(0, 0, _xRayDisplayImage.PixelWidth, _xRayDisplayImage.PixelHeight));
                            _xRayDisplayImage.Unlock();
                            Bitmap.RedrawBitmap(_xRayDisplayImage);
                        });
                }
            }
        }
        private WriteableBitmap _xRayDisplayImage;

        public WriteableBitmap PhotoDisplayImage
        {
            get => _photoDisplayImage;
            set
            {
                this.RaiseAndSetIfChanged(ref _photoDisplayImage, value);
                if (_photoDisplayImage != null)
                {
                    Threading.RunOnUiThread(
                        () =>
                        {
                            _photoDisplayImage.Lock();
                            Memory.CopyMemory(
                                _photoDisplayImage.BackBuffer,
                                value.BackBuffer,
                                (uint) (_photoDisplayImage.Width * _photoDisplayImage.Height));

                            _photoDisplayImage.AddDirtyRect(new Int32Rect(0, 0, _photoDisplayImage.PixelWidth,
                                _photoDisplayImage.PixelHeight));
                            _photoDisplayImage.Unlock();
                            Bitmap.RedrawBitmap(_photoDisplayImage);
                        });
                }
            }
        }
        private WriteableBitmap _photoDisplayImage;

        public double Scale
        {
            get => _scale;
            set => this.RaiseAndSetIfChanged(ref _scale, value);
        }
        private double _scale;

        public double XOffset
        {
            get => _xOffset;
            set => this.RaiseAndSetIfChanged(ref _xOffset, value);
        }
        private double _xOffset;

        public double YOffset
        {
            get { return _yOffset; }
            set { this.RaiseAndSetIfChanged(ref _yOffset, value); }
        }
        private double _yOffset;

        public int XRayThreshold
        {
            get => _xRayThreshold;
            set => this.RaiseAndSetIfChanged(ref _xRayThreshold, value);
        }
        private int _xRayThreshold;

        public int OpticalThreshold
        {
            get => _opticalThreshold;
            set => this.RaiseAndSetIfChanged(ref _opticalThreshold, value);
        }
        private int _opticalThreshold;

        public double BestFitScale => _bestFitScale.Value;
        private readonly ObservableAsPropertyHelper<double> _bestFitScale;

        public double BestFitXOffset => _bestFitXOffset.Value;
        private readonly ObservableAsPropertyHelper<double> _bestFitXOffset;

        public double BestFitYOffset => _bestFitYOffset.Value;
        private readonly ObservableAsPropertyHelper<double> _bestFitYOffset;

        public double BestFitMeanDifference => _bestFitMeanDifference.Value;
        private readonly ObservableAsPropertyHelper<double> _bestFitMeanDifference;

        public double BestFitMaxDifference => _bestFitMaxDifference.Value;
        private readonly ObservableAsPropertyHelper<double> _bestFitMaxDifference;

        public CoRegRM.CoregComputationState State
        {
            get { return _state; }
            set { this.RaiseAndSetIfChanged(ref _state, value); }
        }
        private CoRegRM.CoregComputationState _state;

        public bool IsComputing
        {
            get => _isComputing;
            set => this.RaiseAndSetIfChanged(ref _isComputing, value);
        }
        private bool _isComputing;

        public bool HaveResultsToDisplay
        {
            get => _haveResultsToDisplay;
            set => this.RaiseAndSetIfChanged(ref _haveResultsToDisplay, value);
        }
        private bool _haveResultsToDisplay;

        public string ComputationDetails
        {
            get => _computationDetails;
            set => this.RaiseAndSetIfChanged(ref _computationDetails, value);
        }
        private string _computationDetails;

        public int IterationsComplete => _iterationsComplete.Value;
        private readonly ObservableAsPropertyHelper<int> _iterationsComplete;

        public int MaxIterations => _maxIterations.Value;
        private readonly ObservableAsPropertyHelper<int> _maxIterations;

        public bool ShowXRayOverlay
        {
            get => _showXRayOverlay;
            set => this.RaiseAndSetIfChanged(ref _showXRayOverlay, value);
        }
        private bool _showXRayOverlay;

        #endregion
    }
}
