using System.Numerics;

namespace URP_Extension.Features.CoreFeature
{
    public class ViewConstants
    {
        /// <summary>View matrix.</summary>
        public Matrix4x4 viewMatrix;

        /// <summary>Inverse View matrix.</summary>
        public Matrix4x4 invViewMatrix;

        /// <summary>Projection matrix.</summary>
        public Matrix4x4 projMatrix;

        /// <summary>Non-jittered Projection matrix.</summary>
        public Matrix4x4 nonJitteredProjMatrix;

        /// <summary>Inverse Projection matrix.</summary>
        public Matrix4x4 invProjMatrix;

        /// <summary>View Projection matrix.</summary>
        public Matrix4x4 viewProjMatrix;

        /// <summary>Inverse View Projection matrix.</summary>
        public Matrix4x4 invViewProjMatrix;

        /// <summary>Non-jittered View Projection matrix.</summary>
        public Matrix4x4 nonJitteredViewProjMatrix;

        /// <summary>Non-jittered View Projection matrix.</summary>
        public Matrix4x4 nonJitteredInvViewProjMatrix;

        /// <summary>Previous view matrix from previous frame.</summary>
        public Matrix4x4 prevViewMatrix;

        /// <summary>Non-jittered Projection matrix from previous frame.</summary>
        public Matrix4x4 prevProjMatrix;

        /// <summary>Non-jittered View Projection matrix from previous frame.</summary>
        public Matrix4x4 prevViewProjMatrix;

        /// <summary>Non-jittered Inverse View Projection matrix from previous frame.</summary>
        public Matrix4x4 prevInvViewProjMatrix;

        /// <summary>Non-jittered View Projection matrix from previous frame without translation.</summary>
        public Matrix4x4 prevViewProjMatrixNoCameraTrans;

        /// <summary>Utility matrix (used by sky) to map screen position to WS view direction.</summary>
        public Matrix4x4 pixelCoordToViewDirWS;

        // We need this to track the previous VP matrix with camera translation excluded. Internal since it is used only in its "previous" form
        internal Matrix4x4 viewProjectionNoCameraTrans;

        /// <summary>World Space camera position.</summary>
        public Vector3 worldSpaceCameraPos;

        internal float pad0;

        /// <summary>Offset from the main view position for stereo view constants.</summary>
        public Vector3 worldSpaceCameraPosViewOffset;

        internal float pad1;

        /// <summary>World Space camera position from previous frame.</summary>
        public Vector3 prevWorldSpaceCameraPos;

        internal float pad2;

        /// <summary>View matrix from the frame before the previous frame.</summary>
        public Matrix4x4 prevPrevViewMatrix;

        /// <summary>Non-jittered projection matrix from the frame before the previous frame.</summary>
        public Matrix4x4 prevPrevProjMatrix;

        /// <summary>World Space camera position from the frame before the previous frame.</summary>
        public Vector3 prevPrevWorldSpaceCameraPos;
    }
}