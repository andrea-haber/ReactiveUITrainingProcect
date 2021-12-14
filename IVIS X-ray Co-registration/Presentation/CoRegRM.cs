using System;
using System.Drawing;
using System.Windows.Media.Imaging;
using IVIS_X_ray_Co_registration.Messages;
using ReactiveDomain.Bus;
using ReactiveDomain.ReadModel;
using ReactiveUI;

namespace IVIS_X_ray_Co_registration.Presentation
{
    public class CoRegRM :
        TransientSubscriber,
        IHandle<XrayCoregMsgs.XRayImageThresholded>,
        IHandle<XrayCoregMsgs.PhotoImageThresholded>,
        IHandle<XrayCoregMsgs.BetterFitFound>,
        IHandle<XrayCoregMsgs.ImageCoregistrationStarted>,
        IHandle<XrayCoregMsgs.ImageCoregistrationCancelled>,
        IHandle<XrayCoregMsgs.ImagesCoregistered>,
        IHandle<XrayCoregMsgs.CoregistrationDataLoaded>,
        IHandle<XrayCoregMsgs.ImageCoregistrationIterationComplete>
    {
        public CoRegRM(IGeneralBus bus) : base(bus)
        {
            Subscribe<XrayCoregMsgs.XRayImageThresholded>(this);
            Subscribe<XrayCoregMsgs.PhotoImageThresholded>(this);
            Subscribe<XrayCoregMsgs.BetterFitFound>(this);
            Subscribe<XrayCoregMsgs.ImageCoregistrationStarted>(this);
            Subscribe<XrayCoregMsgs.ImageCoregistrationCancelled>(this);
            Subscribe<XrayCoregMsgs.ImagesCoregistered>(this);
            Subscribe<XrayCoregMsgs.CoregistrationDataLoaded>(this);
            Subscribe<XrayCoregMsgs.ImageCoregistrationIterationComplete>(this);
        }

        public IObservable<WriteableBitmap> XRayDisplayImage => _xRayDisplayImage;
        private readonly ReadModelProperty<WriteableBitmap> _xRayDisplayImage = new ReadModelProperty<WriteableBitmap>(null);

        public void Handle(XrayCoregMsgs.XRayImageThresholded message)
        {
            _xRayDisplayImage.Update(message.ThresholdedImage as WriteableBitmap, true);
        }

        public IObservable<WriteableBitmap> PhotoDisplayImage => _photoDisplayImage;
        private readonly ReadModelProperty<WriteableBitmap> _photoDisplayImage = new ReadModelProperty<WriteableBitmap>(null);

        public void Handle(XrayCoregMsgs.PhotoImageThresholded message)
        {
            _photoDisplayImage.Update(message.ThresholdedImage as WriteableBitmap, true);
        }

        public IObservable<bool> HaveResultsToDisplay => _haveResultsToDisplay;
        private readonly ReadModelProperty<bool> _haveResultsToDisplay = new ReadModelProperty<bool>(false);

        public IObservable<double> FitScale => _fitScale;
        private readonly ReadModelProperty<double> _fitScale = new ReadModelProperty<double>(0);

        public IObservable<double> FitXOffset => _fitXOffset;
        private readonly ReadModelProperty<double> _fitXOffset = new ReadModelProperty<double>(0);

        public IObservable<double> FitYOffset => _fitYOffset;
        private readonly ReadModelProperty<double> _fitYOffset = new ReadModelProperty<double>(0);

        public IObservable<double> FitMeanDiff => _fitMeanDiff;
        private readonly ReadModelProperty<double> _fitMeanDiff = new ReadModelProperty<double>(0);

        public IObservable<double> FitMaxDiff => _fitMaxDiff;
        private readonly ReadModelProperty<double> _fitMaxDiff = new ReadModelProperty<double>(0);

        public void Handle(XrayCoregMsgs.BetterFitFound message)
        {
            _fitScale.Update((double)message.Scale);
            _fitXOffset.Update((double)message.XOffset);
            _fitYOffset.Update((double)message.YOffset);
            _fitMeanDiff.Update(message.MeanDiff);
            _fitMaxDiff.Update(message.MaxDiff);
            _haveResultsToDisplay.Update(true);
        }

        public enum CoregComputationState
        {
            NotStarted,
            Computing,
            Complete,
            Cancelled
        }

        public IObservable<CoregComputationState> ComputationState => _computationState;
        private readonly ReadModelProperty<CoregComputationState> _computationState = new ReadModelProperty<CoregComputationState>(CoregComputationState.NotStarted);

        public void Handle(XrayCoregMsgs.ImageCoregistrationStarted message)
        {
            _computationState.Update(CoregComputationState.Computing);
            _iterationsComplete.Update(0, true);
        }

        public void Handle(XrayCoregMsgs.ImagesCoregistered message)
        {
            _computationState.Update(CoregComputationState.Complete);
        }

        public void Handle(XrayCoregMsgs.ImageCoregistrationCancelled message)
        {
            _computationState.Update(CoregComputationState.Cancelled);
            _iterationsComplete.Update(0, true);
        }

        public void Handle(XrayCoregMsgs.CoregistrationDataLoaded message)
        {
            _haveResultsToDisplay.Update(false);
            _computationState.Update(CoregComputationState.NotStarted);
        }

        public IObservable<int> IterationsComplete => _iterationsComplete;
        private readonly ReadModelProperty<int> _iterationsComplete = new ReadModelProperty<int>(0);
        public IObservable<int> MaxIterations => _maxIterations;
        private readonly ReadModelProperty<int> _maxIterations = new ReadModelProperty<int>(100);
        public void Handle(XrayCoregMsgs.ImageCoregistrationIterationComplete message)
        {
            _iterationsComplete.Update(message.IterationsComplete);
            _maxIterations.Update(message.MaxIterations);
        }
    }
}
