using System.ComponentModel;

namespace IVIS_X_ray_Co_registration.Presentation
{
    public class CoRegCalibrationInfo
    {
        public enum CalibrationType
        {
            [Description("None")]
            None,
            [Description("Mouse")]
            Mouse,
            [Description("Large Animal")]
            LargeAnimal,
            [Description("MVI-2")]
            MVI2,
            [Description("High Res")]
            HighRes
        }

        public readonly CalibrationType CalType;
        private readonly decimal _fovInCm;
        public readonly decimal Scale;
        public readonly decimal XOffset;
        public readonly decimal YOffset;

        public CoRegCalibrationInfo(
            CalibrationType calType,
            decimal fovInCm,
            decimal scale,
            decimal xOffset,
            decimal yOffset)
        {
            CalType = calType;
            _fovInCm = fovInCm;
            Scale = scale;
            XOffset = xOffset;
            YOffset = yOffset;
        }

        public override string ToString()
        {
            return CalType == CalibrationType.None
                       ? string.Empty
                       : $"Optical FOV: {_fovInCm:F0}cm   Scale: {Scale:F3}   X offset: {XOffset:F1}   Y offset: {YOffset:F1}";
        }
    }
}
