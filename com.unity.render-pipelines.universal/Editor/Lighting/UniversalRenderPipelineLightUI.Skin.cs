using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineLightUI
    {
        static class Styles
        {
            public static readonly GUIContent generalHeader = new GUIContent("General");
            public static readonly GUIContent shapeHeader = new GUIContent("Shape");
            public static readonly GUIContent renderingHeader = new GUIContent("Rendering");
            public static readonly GUIContent emissionHeader = new GUIContent("Emission");
            public static readonly GUIContent shadowHeader = new GUIContent("Shadows");

            public static readonly GUIContent Type = EditorGUIUtility.TrTextContent("Type", "Specifies the current type of light. Possible types are Directional, Spot, Point, and Area lights.");

            public static readonly GUIContent AreaLightShapeContent = EditorGUIUtility.TrTextContent("Shape", "Specifies the shape of the Area light. Possible types are Rectangle and Disc.");
            public static readonly GUIContent[] LightTypeTitles = { EditorGUIUtility.TrTextContent("Spot"), EditorGUIUtility.TrTextContent("Directional"), EditorGUIUtility.TrTextContent("Point"), EditorGUIUtility.TrTextContent("Area (baked only)") };
            public static readonly int[] LightTypeValues = { (int)LightType.Spot, (int)LightType.Directional, (int)LightType.Point, (int)LightType.Rectangle };

            public static readonly GUIContent[] AreaLightShapeTitles = { EditorGUIUtility.TrTextContent("Rectangle"), EditorGUIUtility.TrTextContent("Disc") };
            public static readonly int[] AreaLightShapeValues = { (int)LightType.Rectangle, (int)LightType.Disc };

            public static readonly GUIContent SpotAngle = EditorGUIUtility.TrTextContent("Spot Angle", "Controls the angle in degrees at the base of a Spot light's cone.");

            public static readonly GUIContent BakingWarning = EditorGUIUtility.TrTextContent("Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");
            public static readonly GUIContent DisabledLightWarning = EditorGUIUtility.TrTextContent("Lighting has been disabled in at least one Scene view. Any changes applied to lights in the Scene will not be updated in these views until Lighting has been enabled again.");
            public static readonly GUIContent SunSourceWarning = EditorGUIUtility.TrTextContent("This light is set as the current Sun Source, which requires a directional light. Go to the Lighting Window's Environment settings to edit the Sun Source.");

            public static readonly GUIContent ShadowRealtimeSettings = EditorGUIUtility.TrTextContent("Realtime Shadows", "Settings for realtime direct shadows.");
            public static readonly GUIContent ShadowStrength = EditorGUIUtility.TrTextContent("Strength", "Controls how dark the shadows cast by the light will be.");
            public static readonly GUIContent ShadowNearPlane = EditorGUIUtility.TrTextContent("Near Plane", "Controls the value for the near clip plane when rendering shadows. Currently clamped to 0.1 units or 1% of the lights range property, whichever is lower.");
            public static readonly GUIContent ShadowNormalBias = EditorGUIUtility.TrTextContent("Normal", "Controls the distance shadow caster vertices are offset along their normals when rendering shadow maps. Currently ignored for Point Lights.");
            public static readonly GUIContent ShadowDepthBias = EditorGUIUtility.TrTextContent("Depth");

            // Resolution (default or custom)
            public static readonly GUIContent ShadowResolution = EditorGUIUtility.TrTextContent("Resolution", $"Sets the rendered resolution of the shadow maps. A higher resolution increases the fidelity of shadows at the cost of GPU performance and memory usage. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.");
            public static readonly int[] ShadowResolutionDefaultValues =
            {
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium,
                UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh
            };
            public static readonly GUIContent[] ShadowResolutionDefaultOptions =
            {
                new GUIContent("Custom"),
                UniversalRenderPipelineAssetEditor.Styles.additionalLightsShadowResolutionTierNames[0],
                UniversalRenderPipelineAssetEditor.Styles.additionalLightsShadowResolutionTierNames[1],
                UniversalRenderPipelineAssetEditor.Styles.additionalLightsShadowResolutionTierNames[2],
            };

            // Bias (default or custom)
            public static GUIContent shadowBias = EditorGUIUtility.TrTextContent("Bias", "Select if the Bias should use the settings from the Pipeline Asset or Custom settings.");
            public static int[] optionDefaultValues = { 0, 1 };
            public static GUIContent[] displayedDefaultOptions =
            {
                new GUIContent("Custom"),
                new GUIContent("Use Pipeline Settings")
            };

            public static readonly GUIContent LightLayer = EditorGUIUtility.TrTextContent("Light Layer", "Specifies the current Light Layers that the Light affects. This Light illuminates corresponding Renderers with the same Light Layer flags.");
            public static readonly GUIContent customShadowLayers = EditorGUIUtility.TrTextContent("Custom Shadow Layers", "When enabled, you can use the Layer property below to specify the layers for shadows seperately to lighting. When disabled, the Light Layer property in the General section specifies the layers for both lighting and for shadows.");
            public static readonly GUIContent ShadowLayer = EditorGUIUtility.TrTextContent("Layer", "Specifies the light layer to use for shadows.");
        }
    }
}
