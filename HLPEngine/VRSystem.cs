using GLOOP.Rendering;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using Valve.VR;

namespace HLPEngine
{
    public static class VRSystem
    {
        private static CVRSystem System;
        private static Matrix4[] ProjectionMatricies = new Matrix4[2];
        private static Matrix4[] ViewMatricies = new Matrix4[2];
        private static Matrix4 HMDPose;
        public static Matrix4 InverseOriginlHMDPose { get; private set; } = Matrix4.Identity;

        public static void SetUpOpenVR()
        {
            // Set up OpenVR
            var error = EVRInitError.None;
            System = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None)
            {
                Console.WriteLine("Failed to initilize OpenVR");
                return;
            }

            UpdateEyes();
        }

        public static void GetFramebufferSize(out uint width, out uint height)
        {
            width = 0;
            height = 0;
            System.GetRecommendedRenderTargetSize(ref width, ref height);
        }

        public static void SetOriginHeadTransform() => InverseOriginlHMDPose = HMDPose.Inverted();

        public static void UpdateEyes()
        {
            for (int i = 0; i < 2; i++)
            {
                GetHMDProjectionMatrixForEye((EVREye)i, out ProjectionMatricies[i]);
                GetHMDPoseMatrixForEye((EVREye)i, out ViewMatricies[i]);
            }
        }

        private static void GetHMDProjectionMatrixForEye(EVREye eye, out Matrix4 output)
        {
            const float nearZ = 0.05f;
            const float farZ = 100f;
            var m = System.GetProjectionMatrix(eye, nearZ, farZ);
            m.ToOpenTK(out output);
        }

        private static void GetHMDPoseMatrixForEye(EVREye eye, out Matrix4 output)
        {
            var mat = System.GetEyeToHeadTransform(eye);
            mat.ToOpenTK(out output);
            output.Invert();
        }

        public static void UpdatePoses()
        {
            var renderPoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            var gamePoses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);

            if (renderPoses[OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
            {
                gamePoses[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking.ToOpenTK(out HMDPose);
                HMDPose.Invert();
            }
        }

        public static Matrix4 GetEyeViewMatrix(EVREye eye) => InverseOriginlHMDPose * HMDPose * ViewMatricies[(int)eye];
        public static Matrix4 GetEyeProjectionMatrix(EVREye eye) => ProjectionMatricies[(int)eye];

        public static void SubmitEye(Texture backbuffer, EVREye eye)
        {
            VRTextureBounds_t bounds;
            bounds.uMin = bounds.vMin = 0;
            bounds.uMax = bounds.vMax = 1;

            Texture_t eyeTexture;
            eyeTexture.eType = ETextureType.OpenGL;
            eyeTexture.eColorSpace = EColorSpace.Gamma;

            EVRCompositorError error;
            eyeTexture.handle = new IntPtr(backbuffer.Handle);
            error = OpenVR.Compositor.Submit(
                eye,
                ref eyeTexture,
                ref bounds,
                EVRSubmitFlags.Submit_Default
            );
            global::System.Diagnostics.Debug.Assert(error == EVRCompositorError.None);
        }

        private static void ToOpenTK(this HmdMatrix34_t m, out Matrix4 output)
        {
            output.Row0 = new Vector4(m.m0, m.m4, m.m8, 0);
            output.Row1 = new Vector4(m.m1, m.m5, m.m9, 0);
            output.Row2 = new Vector4(m.m2, m.m6, m.m10, 0);
            output.Row3 = new Vector4(m.m3, m.m7, m.m11, 1);
        }

        private static void ToOpenTK(this HmdMatrix44_t m, out Matrix4 output)
        {
            output.Row0 = new Vector4(m.m0, m.m4, m.m8, m.m12);
            output.Row1 = new Vector4(m.m1, m.m5, m.m9, m.m13);
            output.Row2 = new Vector4(m.m2, m.m6, m.m10, m.m14);
            output.Row3 = new Vector4(m.m3, m.m7, m.m11, m.m15);
        }
    }
}
