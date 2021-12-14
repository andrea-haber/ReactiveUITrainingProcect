using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Greylock.StudyManagement.Utilities;
using IVIS_X_ray_Co_registration.Messages;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ReactiveDomain.Bus;
using ReactiveDomain.Messaging;
using ReactiveDomain.Util;
using Size = OpenCvSharp.Size;

namespace IVIS_X_ray_Co_registration.ImageProcessing
{
    public class ImageProcessingSvc :
        TransientSubscriber,
        IHandleCommand<XrayCoregMsgs.CoregisterImages>,
        IHandleCommand<XrayCoregMsgs.ComputeCoregistration>
    {
        private readonly IGeneralBus _bus;

        public ImageProcessingSvc(IGeneralBus bus)
            : base(bus)
        {
            _bus = bus;
            Subscribe<XrayCoregMsgs.CoregisterImages>(this);
            Subscribe<XrayCoregMsgs.ComputeCoregistration>(this);
        }

        public CommandResponse Handle(XrayCoregMsgs.CoregisterImages command)
        {
            if (!command.SequenceFolder.Exists)
                return command.Fail(new FileNotFoundException($"Could not find {command.SequenceFolder.FullName}", command.SequenceFolder.FullName));
            var files = Directory.GetFiles(command.SequenceFolder.FullName);
            if (!files.Any())
                throw new Exception("The selected folder does not contain any files.");

            string xRayFile;
            try
            {
                xRayFile = files.First(f => f.Contains("x-ray.TIF"));
            }
            catch (InvalidOperationException)
            {
                throw new Exception("The selected folder does not contain an X-ray.TIF file.");
            }

            string photoFile;
            try
            {
                photoFile = files.First(f => f.Contains("photograph.TIF"));
            }
            catch (InvalidOperationException)
            {
                throw new Exception("The selected folder does not contain a photograph.TIF file.");
            }

            Task.Run(() =>
            {
                _bus.TryFire(new XrayCoregMsgs.ComputeCoregistration(
                                xRayFile,
                                photoFile,
                                command.InitialScale,
                                command.InitialXOffset,
                                command.InitialYOffset,
                                command.XRayThreshold,
                                command.OpticalThreshold,
                                command.CorrelationId,
                                command.MsgId,
                                command.CancellationToken),
                responseTimeout: Consts.DefaultUiTimeout);
             
            });
            return command.Succeed();
        }

        public CommandResponse Handle(XrayCoregMsgs.ComputeCoregistration command)
        {
            _bus.Publish(new XrayCoregMsgs.ImageCoregistrationStarted(
                                command.CorrelationId,
                                command.MsgId));

            const int edgeBuffer = 100;
            const int minDistance = 10;

            var scaleLimit = Math.Round(0.01m * command.InitialScale, 3);
            const decimal scaleResolution = 0.001m;
            var offsetLimit = (int)(3m / command.InitialScale);
            var bestMeanDiff = double.MaxValue;
            var nScaleIterations = (int)(scaleLimit / scaleResolution * 2 + 1);
            var nOffsetIterations = offsetLimit * 2 + 1;
            var nIterations = nOffsetIterations * nOffsetIterations * nScaleIterations;
            var iteration = 0;
            for (var scaleDelta = -scaleLimit; scaleDelta <= scaleLimit; scaleDelta += scaleResolution)
            {
                for (var xDelta = -offsetLimit; xDelta <= offsetLimit; xDelta++)
                {
                    for (var yDelta = -offsetLimit; yDelta <= offsetLimit; yDelta++)
                    {
                        if (command.IsCanceled)
                        {
                            _bus.Publish(new XrayCoregMsgs.ImageCoregistrationCancelled(
                                                command.CorrelationId,
                                                command.MsgId));
                            return command.Canceled();
                        }

                        var xRayImg = ImageProcessing.AdaptiveGaussianThreshold(new FileInfo(command.XRayFile), command.XRayThreshold);
                        var xRayCentroids = ImageProcessing.ComputeCentroids(xRayImg, edgeBuffer);

                        var photoImg = ImageProcessing.AdaptiveGaussianThreshold(new FileInfo(command.PhotoFile), command.OpticalThreshold, true);
                        var photoCentroids = ImageProcessing.ComputeCentroids(photoImg, edgeBuffer);

                        // Find matches between centroids
                        List<double> diffs;
                        List<Point2d> xr2;
                        List<Point2d> p2;
                        GetCentroidMatches(
                            xRayImg.Size(),
                            xRayCentroids,
                            photoCentroids,
                            out xr2,
                            out p2,
                            out diffs,
                            (double)(command.InitialScale + scaleDelta),
                            (double)(command.InitialXOffset + xDelta),
                            (double)(command.InitialYOffset + yDelta),
                            minDistance);

                        // Make sure we have at least 12 matches, which is the bare minimum for high res data
                        if (diffs.Count < 12) continue;

                        var meanDiff = diffs.Sum() / diffs.Count;
                        var maxDiff = diffs.Max();

                        if (meanDiff < bestMeanDiff)
                        {
                            bestMeanDiff = meanDiff;
                            var maxDiffAtBestMean = maxDiff;
                            var bestScale = command.InitialScale + scaleDelta;
                            var bestXOffset = command.InitialXOffset + xDelta;
                            var bestYOffset = command.InitialYOffset + yDelta;
                            _bus.Publish(new XrayCoregMsgs.BetterFitFound(
                                                bestScale,
                                                bestXOffset,
                                                bestYOffset,
                                                bestMeanDiff,
                                                maxDiffAtBestMean,
                                                command.CorrelationId,
                                                command.MsgId));

                            Threading.RunOnUiThread(() =>
                            {
                                var colorXRayMask = new Mat(xRayImg.Size(), MatType.CV_8UC4);
                                Cv2.CvtColor(xRayImg, colorXRayMask, ColorConversionCodes.GRAY2BGRA, 4);
                                var tempBitmap = colorXRayMask.ToBitmap();
                                tempBitmap.MakeTransparent(Color.Black);
                                colorXRayMask = tempBitmap.ToMat();
                                //foreach (var p in xRayCentroids)
                                //{
                                //    colorXRayMask.DrawMarker((int)Math.Round(p.X), (int)Math.Round(p.Y), Scalar.Red, thickness: 3);
                                //}
                                _bus.Publish(new XrayCoregMsgs.XRayImageThresholded(
                                                 colorXRayMask.ToWriteableBitmap(),
                                                 command.CorrelationId,
                                                 command.MsgId));

                                var colorPhotoMask = new Mat(photoImg.Size(), MatType.CV_8UC4);
                                Cv2.CvtColor(photoImg, colorPhotoMask, ColorConversionCodes.GRAY2BGRA);
                                //foreach (var p in p2)
                                //{
                                //    colorPhotoMask.DrawMarker((int)Math.Round(p.X), (int)Math.Round(p.Y), Scalar.Blue, thickness: 3);
                                //}
                                _bus.Publish(new XrayCoregMsgs.PhotoImageThresholded(
                                                 colorPhotoMask.ToWriteableBitmap(),
                                                 command.CorrelationId,
                                                 command.MsgId));
                            });
                        }

                        iteration++;
                        Task.Run(() =>
                            _bus.Publish(new XrayCoregMsgs.ImageCoregistrationIterationComplete(
                                                iteration,
                                                nIterations,
                                                command.CorrelationId,
                                                command.MsgId)));
                    }
                }
            }
            _bus.Publish(new XrayCoregMsgs.ImagesCoregistered(command.CorrelationId, command.MsgId));
            return command.Succeed();
        }

        private static void GetCentroidMatches(
            Size imageSize,
            IEnumerable<Point2d> xRayCentroids,
            IEnumerable<Point2d> photoCentroids,
            out List<Point2d> scaledXRayCentroids,
            out List<Point2d> filteredPhotoCentroids,
            out List<double> centroidDiffs,
            double scale,
            double xOffset,
            double yOffset,
            double minDistance)
        {
            var inverseScale = 1.0 - scale;
            var xCenter = imageSize.Width / 2 - 1;
            var yCenter = imageSize.Height / 2 - 1;
            var xShift = xCenter * inverseScale + xOffset;
            var yShift = yCenter * inverseScale + yOffset;
            scaledXRayCentroids =
                xRayCentroids
                    .Select(centroid => new Point2d(centroid.X * scale + xShift, centroid.Y * scale + yShift))
                    .ToList();

            filteredPhotoCentroids = new List<Point2d>();
            centroidDiffs = new List<double>();
            foreach (var photoCentroid in photoCentroids)
            {
                var match = scaledXRayCentroids.FindIndex(xrc => xrc.DistanceTo(photoCentroid) < minDistance);
                if (match == -1) continue;
                filteredPhotoCentroids.Add(photoCentroid);
                var distance = photoCentroid.DistanceTo(scaledXRayCentroids[match]);
                centroidDiffs.Add(distance);
            }
        }
    }
}
