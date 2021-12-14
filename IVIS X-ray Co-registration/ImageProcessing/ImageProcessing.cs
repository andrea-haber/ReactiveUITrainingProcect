using System.Collections.Generic;
using System.IO;
using EventStore.Core.Services.Transport.Tcp;
using OpenCvSharp;

namespace IVIS_X_ray_Co_registration.ImageProcessing
{
    public static class ImageProcessing
    {
        public static Mat AdaptiveGaussianThreshold(FileInfo file, double dcOffset = 0, bool isForegroundBlack = false)
        {
            var sourceImage = Cv2.ImRead(file.FullName, ImreadModes.Unchanged);

            if (isForegroundBlack)
                sourceImage = ushort.MaxValue - sourceImage;

            // median filter the image to get rid of noise.
            var smoothedImage = new Mat(
                                        sourceImage.Size(),
                                        sourceImage.Type());
            Cv2.MedianBlur(sourceImage, smoothedImage, 3);

            // convert to 8-bit since that's what AdaptiveThreshold requires.
            var eightBitSourceImage = new MatOfByte();
            smoothedImage.ConvertTo(
                            eightBitSourceImage,
                            eightBitSourceImage.Type(),
                            1.0 / 256);
            var thresholdedImage = new Mat(
                                        eightBitSourceImage.Size(),
                                        eightBitSourceImage.Type());

            // threshold the image!
            Cv2.AdaptiveThreshold(
                            eightBitSourceImage,
                            thresholdedImage,
                            byte.MaxValue,
                            AdaptiveThresholdTypes.MeanC,
                            ThresholdTypes.Binary,
                            2 * (eightBitSourceImage.Cols / 16) + 1, // use ~1/8 of the image as the neighborhood, to match the behavior of MATLAB's adaptive thresholding
                            dcOffset);

            Cv2.MedianBlur(thresholdedImage, smoothedImage, 5);

            return smoothedImage;
        }

        public static IList<Point2d> ComputeCentroids(Mat binaryImage, int edgeBufferSize)
        {
            var centroids = new Mat();
            var labels = new Mat();
            var stats = new Mat();
            Cv2.ConnectedComponentsWithStats(binaryImage, labels, stats, centroids);

            var xMax = binaryImage.Width - edgeBufferSize;
            var yMax = binaryImage.Height - edgeBufferSize;
            var filteredCentroids = new List<Point2d>();
            // Remove centroids too close to the edges
            for (int i = 0; i < centroids.Rows; i++)
            {
                var x = centroids.At<double>(i, 0);
                var y = centroids.At<double>(i, 1);
                if (x > edgeBufferSize && x < xMax && y > edgeBufferSize && y < yMax)
                    filteredCentroids.Add(new Point2d(x, y));
            }

            return filteredCentroids;
        }
    }
}
