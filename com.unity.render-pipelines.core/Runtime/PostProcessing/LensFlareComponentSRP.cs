#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Data-Driven Lens Flare can be added on any gameobeject
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Rendering/Lens Flare (SRP)")]
    public sealed class LensFlareComponentSRP : MonoBehaviour
    {
        [SerializeField]
        private LensFlareDataSRP m_LensFlareData = null;

        /// <summary>
        /// Lens flare asset used on this component
        /// </summary>
        public LensFlareDataSRP lensFlareData
        {
            get
            {
                return m_LensFlareData;
            }
            set
            {
                m_LensFlareData = value;
                OnValidate();
            }
        }
        /// <summary>
        /// Intensity
        /// </summary>
        [Min(0.0f)]
        public float intensity = 1.0f;
        /// <summary>
        /// Distance used to scale the Distance Attenuation Curve
        /// </summary>
        [Min(1e-5f)]
        public float maxAttenuationDistance = 50.0f;
        /// <summary>
        /// Distance used to scale the Scale Attenuation Curve
        /// </summary>
        [Min(1e-5f)]
        public float maxAttenuationScale = 50.0f;
        /// <summary>
        /// Attenuation by distance
        /// </summary>
        public AnimationCurve distanceAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));
        /// <summary>
        /// Scale by distance, use the same distance as distanceAttenuationCurve
        /// </summary>
        public AnimationCurve scaleByDistanceCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));
        /// <summary>
        /// If component attached to a light, attenuation the lens flare per light type
        /// </summary>
        public bool attenuationByLightShape = true;
        /// <summary>
        /// Attenuation used radially, which allow for instance to enable flare only on the edge of the screen
        /// </summary>
        public AnimationCurve radialScreenAttenuationCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 1.0f));

        /// <summary>
        /// Enable Occlusion feature
        /// </summary>
        public bool useOcclusion = true;
        /// <summary>
        /// Radius around the light used to occlude the flare (value in world space)
        /// </summary>
        [Min(0.0f)]
        public float occlusionRadius = 0.1f;
        /// <summary>
        /// Random Samples Count used inside the disk with 'occlusionRadius'
        /// </summary>
        [Range(1, 64)]
        public uint sampleCount = 32;
        /// <summary>
        /// Z Occlusion Offset allow us to offset the plane where the disc of occlusion is place (closer to camera), value on world space.
        /// Useful for instance to sample occlusion outside a light bulb if we place a flare inside the light bulb
        /// </summary>
        public float occlusionOffset = 0.05f;
        /// <summary>
        /// Global Scale
        /// </summary>
        [Min(0.0f)]
        public float scale = 1.0f;
        /// <summary>
        /// If allowOffScreen is true then If the lens flare is outside the screen we still emit the flare on screen
        /// </summary>
        public bool allowOffScreen = false;

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
        void OnEnable()
        {
            if (lensFlareData)
                LensFlareCommonSRP.Instance.AddData(this);
            else
                LensFlareCommonSRP.Instance.RemoveData(this);
        }

        /// <summary>
        /// Remove the lens flare to the queue of PostProcess
        /// </summary>
        void OnDisable()
        {
            LensFlareCommonSRP.Instance.RemoveData(this);
        }

        /// <summary>
        /// Add or remove the lens flare to the queue of PostProcess
        /// </summary>
        void OnValidate()
        {
            if (lensFlareData != null)
            {
                LensFlareCommonSRP.Instance.AddData(this);
            }
            else
            {
                LensFlareCommonSRP.Instance.RemoveData(this);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Camera mainCam = Camera.current;
            if (mainCam != null && useOcclusion)
            {
                Vector3 positionWS;
                Light light = GetComponent<Light>();
                if (light != null && light.type == LightType.Directional)
                {
                    positionWS = -transform.forward * mainCam.farClipPlane + mainCam.transform.position;
                }
                else
                {
                    positionWS = transform.position;
                }

                Color previousH = Handles.color;
                Color previousG = Gizmos.color;
                Handles.color = Color.red;
                Gizmos.color = Color.red;
                Vector3 dir = (mainCam.transform.position - positionWS).normalized;
                Handles.DrawWireDisc(positionWS + dir * occlusionOffset, dir, occlusionRadius, 1.0f);
                Gizmos.DrawWireSphere(positionWS, occlusionOffset);
                Gizmos.color = previousG;
                Handles.color = previousH;
            }
        }

#endif
    }
}
