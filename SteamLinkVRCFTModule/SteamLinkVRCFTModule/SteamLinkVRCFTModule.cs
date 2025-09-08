
using VRCFaceTracking;
using VRCFaceTracking.Core.Library;
using VRCFaceTracking.Core.Params.Expressions;
using static VRCFaceTracking.Core.Params.Expressions.UnifiedExpressions;


namespace SteamLinkVRCFTModule
{
    // ReSharper disable once UnusedType.Global
    public class SteamLinkVRCFTModule : ExtTrackingModule
    {
        private OSCHandler? OSCHandler;
        private const int DEFAULT_PORT = 9015;

        public override (bool SupportsEye, bool SupportsExpression) Supported => (true, true);

        public override (bool eyeSuccess, bool expressionSuccess) Initialize(bool eyeAvailable, bool expressionAvailable)
        {
            ModuleInformation.Name = "SteamLink Module";

            var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("SteamLinkVRCFTModule.Assets.steamlink.png");
            ModuleInformation.StaticImages = stream != null ? new List<Stream> { stream } : ModuleInformation.StaticImages;

            // TODO better error handling on fail? isInit for OSC Handler?
            OSCHandler = new OSCHandler(Logger, DEFAULT_PORT);

            return (true, true);
        }

        private static float CalculateEyeOpenness(float fEyeClosedWeight, float fEyeTightener)
        {
            return 1.0f - Math.Clamp(fEyeClosedWeight + fEyeClosedWeight * fEyeTightener, 0.0f, 1.0f);
        }

        private void UpdateEyeTracking()
        {
            if (OSCHandler == null) { return; }

            float fAngleX = MathF.Atan2(OSCHandler.eyeTrackData[0], -OSCHandler.eyeTrackData[2]);
            float fAngleY = MathF.Atan2(OSCHandler.eyeTrackData[1], -OSCHandler.eyeTrackData[2]);

            UnifiedTracking.Data.Eye.Left.Gaze.x = fAngleX;
            UnifiedTracking.Data.Eye.Left.Gaze.y = fAngleY;

            UnifiedTracking.Data.Eye.Right.Gaze.x = fAngleX;
            UnifiedTracking.Data.Eye.Right.Gaze.y = fAngleY;

            // Pupil Dilation, This is not supported, but if we don't set it can cause issues
            UnifiedTracking.Data.Eye.Left.PupilDiameter_MM = 5f;
            UnifiedTracking.Data.Eye.Right.PupilDiameter_MM = 5f;
            UnifiedTracking.Data.Eye._maxDilation = 10;
            UnifiedTracking.Data.Eye._minDilation = 0;

            UnifiedTracking.Data.Eye.Left.Openness = CalculateEyeOpenness(OSCHandler.eyelids[0], OSCHandler.ueData[EyeSquintLeft]);
            UnifiedTracking.Data.Eye.Right.Openness = CalculateEyeOpenness(OSCHandler.eyelids[1], OSCHandler.ueData[EyeSquintRight]);
        }

        private void UpdateFaceTracking()
        {
            foreach (KeyValuePair<UnifiedExpressions, float> entry in OSCHandler.ueData)
            {
                UnifiedTracking.Data.Shapes[(int)entry.Key].Weight = entry.Value;
            }

            UnifiedTracking.Data.Shapes[(int)LipSuckUpperLeft].Weight = Math.Min(1.0f - (float)Math.Pow(UnifiedTracking.Data.Shapes[(int)MouthUpperLeft].Weight, 1f / 6f), UnifiedTracking.Data.Shapes[(int)LipSuckUpperLeft].Weight);
            UnifiedTracking.Data.Shapes[(int)LipSuckUpperRight].Weight = Math.Min(1.0f - (float)Math.Pow(UnifiedTracking.Data.Shapes[(int)MouthUpperRight].Weight, 1f / 6f), UnifiedTracking.Data.Shapes[(int)LipSuckUpperRight].Weight);
        }

        public override void Update()
        {
            Thread.Sleep(10);

            if (Status == ModuleState.Active)
            {
                if (ModuleInformation.UsingEye) { UpdateEyeTracking(); }

                if (ModuleInformation.UsingExpression) { UpdateFaceTracking(); }
            }
        }

        public override void Teardown()
        {
            OSCHandler?.Teardown();
        }
    }
}
