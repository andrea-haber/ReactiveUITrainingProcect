using System;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using ReactiveDomain.Annotations;
using ReactiveDomain.Messaging;

namespace IVIS_X_ray_Co_registration.Messages
{
    public class XrayCoregMsgs
    {
        public class CoregistrationDataLoaded : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public CoregistrationDataLoaded(
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
            }
        }

        public class CoregisterImages : TokenCancellableCommand
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly DirectoryInfo SequenceFolder;
            public readonly decimal InitialScale;
            public readonly decimal InitialXOffset;
            public readonly decimal InitialYOffset;
            public readonly int XRayThreshold;
            public readonly int OpticalThreshold;

            public CoregisterImages(
                DirectoryInfo sequenceFolder,
                decimal initialScale,
                decimal initialXOffset,
                decimal initialYOffset,
                int xRayThreshold,
                int opticalThreshold,
                Guid correlationId,
                Guid? sourceId,
                CancellationToken token)
                : base(correlationId, sourceId, token)
            {
                SequenceFolder = sequenceFolder;
                InitialScale = initialScale;
                InitialXOffset = initialXOffset;
                InitialYOffset = initialYOffset;
                XRayThreshold = xRayThreshold;
                OpticalThreshold = opticalThreshold;
            }
        }

        public class ImageCoregistrationCancelled : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public ImageCoregistrationCancelled(
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
            }
        }

        public class ComputeCoregistration : TokenCancellableCommand
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly string XRayFile;
            public readonly string PhotoFile;
            public readonly decimal InitialScale;
            public readonly decimal InitialXOffset;
            public readonly decimal InitialYOffset;
            public readonly int XRayThreshold;
            public readonly int OpticalThreshold;

            public ComputeCoregistration(
                string xRayFile,
                string photoFile,
                decimal initialScale,
                decimal initialXOffset,
                decimal initialYOffset,
                int xRayThreshold,
                int opticalThreshold,
                Guid correlationId,
                Guid? sourceId,
                CancellationToken token)
                : base(correlationId, sourceId, token)
            {
                XRayFile = xRayFile;
                PhotoFile = photoFile;
                InitialScale = initialScale;
                InitialXOffset = initialXOffset;
                InitialYOffset = initialYOffset;
                XRayThreshold = xRayThreshold;
                OpticalThreshold = opticalThreshold;
            }
        }

        public class ImageCoregistrationStarted : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public ImageCoregistrationStarted(
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
            }
        }

        public class ImageCoregistrationIterationComplete : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly int IterationsComplete;
            public readonly int MaxIterations;

            public ImageCoregistrationIterationComplete(
                int iterationsComplete,
                int maxIterations,
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
                IterationsComplete = iterationsComplete;
                MaxIterations = maxIterations;
            }
        }

        public class ImagesCoregistered : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;


            public ImagesCoregistered(
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
            }
        }

        public class XRayImageThresholded : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly WriteableBitmap ThresholdedImage;

            public XRayImageThresholded(
                WriteableBitmap thresholdedImage,
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
                ThresholdedImage = thresholdedImage;
            }
        }

        public class PhotoImageThresholded : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly WriteableBitmap ThresholdedImage;

            public PhotoImageThresholded(
                WriteableBitmap thresholdedImage,
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
                ThresholdedImage = thresholdedImage;
            }
        }

        public class BetterFitFound : DomainEvent
        {
            private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
            public override int MsgTypeId => TypeId;

            public readonly decimal Scale;
            public readonly decimal XOffset;
            public readonly decimal YOffset;
            public readonly double MeanDiff;
            public readonly double MaxDiff;

            public BetterFitFound(
                decimal scale,
                decimal xOffset,
                decimal yOffset,
                double meanDiff,
                double maxDiff,
                Guid correlationId,
                Guid sourceId)
                : base(correlationId, sourceId)
            {
                Scale = scale;
                XOffset = xOffset;
                YOffset = yOffset;
                MeanDiff = meanDiff;
                MaxDiff = maxDiff;
            }
        }
    }
}
