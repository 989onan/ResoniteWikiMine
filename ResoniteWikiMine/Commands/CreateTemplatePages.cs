using System.Reflection;
using System.Text;
using Dapper;
using Elements.Core;
using FrooxEngine;

namespace ResoniteWikiMine.Commands;

public sealed class CreateTemplatePages : ICommand
{

    public static Dictionary<string, string> CreateTemplates = new Dictionary<string, string>()
    {
        {"{{Template:Material_Iterations}}", "How many ghost images to make in order to incur the blurring effect"},
        {"{{Template:Material_Spread}}", "How much the ghost images spread from the center real image to incur the blurring effect"},
        {"{{Template:Material_SpreadMagnitudeTexture}}", "A texture to use that affects the strength of the blur effect."},
        {"{{Template:Material_UsePoissonDisc}}", "Use the poisson disk blur algorithm"},
        {"{{Template:Material_DepthFadeDivisor}}", "This setting uses the depth of the scene as a scaling factor for how blurry the filter is based on this depth, so further out objects get more blurry."},
        {"{{Template:Material_SpreadTextureScale}}", "The uv scale of the <code>SpreadMagnitudeTexture</code>"},
        {"{{Template:Material_SpreadTextureOffset}}", "The uv offset of the <code>SpreadMagnitudeTexture</code>"},
        {"{{Template:Material_Refract}}", "Whether this material should also do refraction effects."},
        {"{{Template:Material_RefractionStrength}}", "the strength of the refactions"},
        {"{{Template:Material_NormalMap}}", "The normal map used in the refraction effect."},
        {"{{Template:Material_NormalTextureScale}}", "The scale of the <code>NormalMap</code> texture."},
        {"{{Template:Material_NormalTextureOffset}}", "The offset of the <code>NormalMap</code> texture."},
        {"{{Template:Material_PerObject}}", "Whether to do the effect per object rather than to the entire image grab pass done by the shader by default."},
        {"{{Template:Material_RedFromRed}}", "{{Template_Material_ColorFromColor|red|red}}"},
        {"{{Template:Material_RedFromGreen}}", "{{Template_Material_ColorFromColor|red|green}}"},
        {"{{Template:Material_RedFromBlue}}", "{{Template_Material_ColorFromColor|red|blue}}"},
        {"{{Template:Material_RedOffset}}", "{{Template:Material_ColorMatrixOffset|red}}"},
        {"{{Template:Material_GreenFromRed}}", "{{Template_Material_ColorFromColor|green|red}}"},
        {"{{Template:Material_GreenFromGreen}}", "{{Template_Material_ColorFromColor|green|green}}"},
        {"{{Template:Material_GreenFromBlue}}", "{{Template_Material_ColorFromColor|green|blue}}"},
        {"{{Template:Material_GreenOffset}}", "{{Template:Material_ColorMatrixOffset|green}}"},
        {"{{Template:Material_BlueFromRed}}", "{{Template_Material_ColorFromColor|blue|red}}"},
        {"{{Template:Material_BlueFromGreen}}", "{{Template_Material_ColorFromColor|blue|green}}"},
        {"{{Template:Material_BlueFromBlue}}", "{{Template_Material_ColorFromColor|blue|blue}}"},
        {"{{Template:Material_BlueOffset}}", "{{Template:Material_ColorMatrixOffset|blue}}"},
        {"{{Template:Material_ClampRedMin}}", "the minimum resulting red color."},
        {"{{Template:Material_ClampGreenMin}}", "the minimum resulting green color"},
        {"{{Template:Material_ClampBlueMin}}", "the minimum resulting blue color"},
        {"{{Template:Material_ClampRedMax}}", "the maximum resulting red color."},
        {"{{Template:Material_ClampGreenMax}}", "the maximum resulting green color"},
        {"{{Template:Material_ClampBlueMax}}", "the maximum resulting blue color"},
        {"{{Template:Material_Scale}}", "How much to scale the positions of the displayed color data pixels."},
        {"{{Template:Material_Offset}}", "How much to offset the positions of the displayed color data pixels."},
        {"{{Template:Material_Visualize}}", "What kind of data to visualize for the mesh."},
        {"{{Template:Material_Normalize}}", "Enable keeping visualized mesh data within a 0-1 range for all color values."},
        {"{{Template:Material_Multiply}}", "how much to multiply the resulting color."},
        {"{{Template:Material_Clip}}", "whether to clip the depth range."},
        {"{{Template:Material_ClipStart}}", "the depth start value, which is the minimum distance from the camera which will be black."},
        {"{{Template:Material_ClipEnd}}", "the depth end value, which is the maximum distance from the camera, which will be white."},
        {"{{Template:Material_Color}}", "The surface color texture of the material."},
        {"{{Template:Material_Depth}}", "the texture used to displace vertices based on intensity."},
        {"{{Template:Material_DepthEncoding}}", "How the depth data is encoded into <code>Depth</code>"},
        {"{{Template:Material_ColorTextureOffset}}", "the texture uv offset of <code>Color</code>."},
        {"{{Template:Material_ColorTextureScale}}", "the texture uv scale of <code>Color</code>."},
        {"{{Template:Material_DepthTextureOffset}}", "the texture uv offset of <code>Depth</code>."},
        {"{{Template:Material_DepthTextureScale}}", "the texture uv scale of <code>Depth</code>."},
        {"{{Template:Material_DepthFrom}}", "the minimum depth distance."},
        {"{{Template:Material_DepthTo}}", "the maximum depth distance."},
        {"{{Template:Material_FieldOfView}}", "the field of view of the <code>Depth</code> data."},
        {"{{Template:Material_NearClip}}", "the near clip of the <code>Depth</code> data."},
        {"{{Template:Material_FarClip}}", "the far clip of the <code>Depth</code> data."},
        {"{{Template:Material_DiscardThreshold}}", "how strong the depth data can be till its cut off."},
        {"{{Template:Material_DiscardOffset}}", "how much to offset the <code>DiscardThreshold</code>."},
        {"{{Template:Material_MainTexture}}", "The main color map of the material."},
        {"{{Template:Material_ColorMask}}", "A mask used for color modification."},
        {"{{Template:Material_EmissionMap}}", "The glow or emission color map."},
        {"{{Template:Material_MainTextureScale}}", "The UV scaling of the main texture."},
        {"{{Template:Material_MainTextureOffset}}", "The UV offset of the main texture."},
        {"{{Template:Material_ColorMaskScale}}", "The UV scaling of the color mask texture."},
        {"{{Template:Material_ColorMaskOffset}}", "The UV offset of the color mask texture."},
        {"{{Template:Material_EmissionMapScale}}", "The UV scaling of the emission texture."},
        {"{{Template:Material_EmissionMapOffset}}", "The UV offset of the emission texture."},
        {"{{Template:Material_NormalMapScale}}", "The UV scaling of the normal map texture."},
        {"{{Template:Material_NormalMapOffset}}", "The UV offset of the normal texture."},
        {"{{Template:Material_AlphaCutoff}}", "The threshold for whether a pixel with transparency should not render or render fully opaque"},
        {"{{Template:Material_EmissionColor}}", "The tint of the emission color."},
        {"{{Template:Material_Shadow}}", "The strength of shadow on the toon material."},
        {"{{Template:Material_Outline}}", "The outline style to use."},
        {"{{Template:Material_OutlineWidth}}", "The thickness of the outline around the mesh."},
        {"{{Template:Material_OutlineColor}}", "The color of the outline around the mesh."},
        {"{{Template:Material_OutlineTint}}", "How much to tint the outline around the mesh."},
        {"{{Template:Material_BaseColor}}", "Starting color for the fog volume"},
        {"{{Template:Material_AccumulationColor}}", "Color to use for a Constant fog"},
        {"{{Template:Material_AccumulationColorBottom}}", "Color at the end of a Vertical Gradient fog"},
        {"{{Template:Material_AccumulationColorTop}}", "Color at the start of a Vertical Gradient fog"},
        {"{{Template:Material_Lerp}}", "The value to use to transition between the first and second texture and value sets in absence of <code>LerpTexture</code>."},
        {"{{Template:Material_LerpTexture}}", "The texture to use to blend linearly between the first and second texture and value sets."},
        {"{{Template:Material_LerpTextureScale}}", "The UV scale of the lerp texture."},
        {"{{Template:Material_LerpTextureOffset}}", "The UV offset of the lerp texture."},
        {"{{Template:Material_LerpTexturePolarUV}}", "Whether to UV map the lerp texture using polar UVs."},
        {"{{Template:Material_LerpTexturePolarPower}}", "The polar UV power of the lerp texture UV when using polar UVs."},
        {"{{Template:Material_Exponent0}}", "The sharpness of the fresnel effect for the first lerp value set."},
        {"{{Template:Material_Exponent1}}", "The sharpness of the fresnel effect for the second lerp value set."},
        {"{{Template:Material_GammaCurve}}", "The gamma curve of the fresnel."},
        {"{{Template:Material_FarColor0}}", "The far color tint for the first lerp set."},
        {"{{Template:Material_NearColor0}}", "The near color tint for the first lerp set."},
        {"{{Template:Material_FarColor1}}", "The Far color tint for the second lerp set."},
        {"{{Template:Material_NearColor1}}", "The near color tint for the second lerp set."},
        {"{{Template:Material_FarTexture0}}", "The Far texture for the first lerp texture set."},
        {"{{Template:Material_NearTexture0}}", "The near texture for the first lerp texture set."},
        {"{{Template:Material_FarTexture1}}", "The Far texture for the second lerp texture set."},
        {"{{Template:Material_NearTexture1}}", "The near texture for the second lerp texture set."},
        {"{{Template:Material_NormalMap0}}", "The normal map for the first lerp texture set."},
        {"{{Template:Material_NormalMap1}}", "The normal map for the second lerp texture set."},
        {"{{Template:Material_Exponent}}", "The sharpness of the fresnel effect."},
        {"{{Template:Material_FarColor}}", "The color tint of geometry facing away from the camera."},
        {"{{Template:Material_NearColor}}", "The color tint of geometry facing towards the camera."},
        {"{{Template:Material_FarTexture}}", "The texture for the geometry facing away from the camera."},
        {"{{Template:Material_NearTexture}}", "The texture for geometry facing towards the camera."},
        {"{{Template:Material_FarTextureScale}}", "The UV scale of the far texture."},
        {"{{Template:Material_FarTextureOffset}}", "The UV offset of the far texture."},
        {"{{Template:Material_NearTextureScale}}", "The UV scale of the near texture."},
        {"{{Template:Material_NearTextureOffset}}", "The UV offset of the near texture."},
        {"{{Template:Material_NormalScale}}", "The amplification of the normal effect."},
        {"{{Template:Material_UseVertexColors}}", "Whether the material should use vertex colors from the mesh."},
        {"{{Template:Material_VertexColorInterpolationSpace}}", "How to interpolate vertex colors on the mesh."},
        {"{{Template:Material_MaskTexture}}", "The mask texture to use."},
        {"{{Template:Material_MaskScale}}", "The UV scale of the mask texture."},
        {"{{Template:Material_MaskOffset}}", "The UV offset of the mask texture."},
        {"{{Template:Material_MaskMode}}", "How to apply the mask texture to the material."},
        {"{{Template:Material_PolarUVmapping}}", "Whether to use polar UV unwrapping."},
        {"{{Template:Material_PolarPower}}", "The power of the polar UV unwrapping."},
        {"{{Template:Material_SpecularColor}}", "Specular Tint. Behaves like PBS Specular Tinting"},
        {"{{Template:Material_Shininess}}", "Behaves like PBS Smoothness"},
        {"{{Template:Material_Gloss}}", "Reflection Intensity."},
        {"{{Template:Material_RimColor}}", "Rim Lighting Color"},
        {"{{Template:Material_RimPower}}", "Rim Lighting Power"},
        {"{{Template:Material_FurLength}}", "Length of the fur.  Always applied in the force direction."},
        {"{{Template:Material_FurHardness}}", "How Stiff the fur is. Biases fur to point in the mesh normal direction instead of the force direction."},
        {"{{Template:Material_FurThinness}}", "How many times to re-iterate the fur noise map. Behaves similarly to texture scaling."},
        {"{{Template:Material_FurShading}}", "How much fake shading should be applied to the fur."},
        {"{{Template:Material_FurColoring}}", "How much the color of the fur should take precedent over shading."},
        {"{{Template:Material_Base}}", "Albedo Texture. Alpha is Heightmap"},
        {"{{Template:Material_Noise}}", "Noise texture. This should always be on the alpha channel. This determines the pattern of the fur."},
        {"{{Template:Material_TextureScale}}", "Texture Scale"},
        {"{{Template:Material_TextureOffset}}", "Texture Offset"},
        {"{{Template:Material_ForceGlobal}}", "The amount of displacement to apply in the world coordinate system.  The '''W''' value corresponds to a proportional affinity to point towards world origin."},
        {"{{Template:Material_ForceLocal}}", "The amount of displacement to apply in the object's local coordinate system."},
        {"{{Template:Material_Gamma}}", "the gamma effect."},
        {"{{Template:Material__gradients}}", "Applies a list of [[#Gradient|Gradients]] to the sky."},
        {"{{Template:Material_RatioRed}}", "The proportion of red to use for grayscale computation"},
        {"{{Template:Material_RatioGreen}}", "The proportion of green to use for grayscale computation"},
        {"{{Template:Material_RatioBlue}}", "The proportion of blue to use for grayscale computation"},
        {"{{Template:Material_Gradient}}", "A texture which maps black values on its far left and white values on its far right"},
        {"{{Template:Material_HSV_Offset}}", "How much to add to the hue, saturation, and value of colors behind the material"},
        {"{{Template:Material_HSV_Multiply}}", "How much to multiply the hue, saturation, and value of colors behind the material"},
        {"{{Template:Material_LUT}}", "The 3D texture to use as a lookup table"},
        {"{{Template:Material_SecondaryLUT}}", "A secondary 3D texture to use to lerp between"},
        {"{{Template:Material_UseSRGB}}", "Whether to use the SRGB color space when spitting out colors."},
        {"{{Template:Material_Matcap}}", "A texture that looks like a sphere with reflection shading on it. Used to make the fake reflection map."},
        {"{{Template:Material_BehindFarColor}}", "The far color tint when the material is behind things and being rendered on top."},
        {"{{Template:Material_BehindNearColor}}", "The near color tint when the material is behind things and being rendered on top."},
        {"{{Template:Material_FrontFarColor}}", "The far color tint when the material is within view."},
        {"{{Template:Material_FrontNearColor}}", "The near color tint when the material is within view."},
        {"{{Template:Material_BehindFarTexture}}", "The far texture when the material is behind things and being rendered on top."},
        {"{{Template:Material_BehindNearTexture}}", "The near texture when the material is behind things and being rendered on top."},
        {"{{Template:Material_FrontFarTexture}}", "The far texture when the material is within view."},
        {"{{Template:Material_FrontNearTexture}}", "The near texture when the material is within view."},
        {"{{Template:Material_BehindFarTextureScale}}", "The UV texture scale of <code>BehindFarTexture</code>."},
        {"{{Template:Material_BehindFarTextureOffset}}", "The UV texture offset of <code>BehindFarTexture</code>."},
        {"{{Template:Material_BehindNearTextureScale}}", "The UV texture scale of <code>BehindNearTexture</code>."},
        {"{{Template:Material_BehindNearTextureOffset}}", "The UV texture offset of <code>BehindNearTexture</code>."},
        {"{{Template:Material_FrontFarTextureScale}}", "The UV texture scale of <code>FrontFarTexture</code>."},
        {"{{Template:Material_FrontFarTextureOffset}}", "The UV texture offset of <code>FrontFarTexture</code>."},
        {"{{Template:Material_FrontNearTextureScale}}", "The UV texture scale of <code>FrontNearTexture</code>."},
        {"{{Template:Material_FrontNearTextureOffset}}", "The UV texture offset of <code>FrontNearTexture</code>."},
        {"{{Template:Material_BehindTintColor}}", "The color tint when the material is behind an object."},
        {"{{Template:Material_FrontTintColor}}", "The color tint when the material is in front of an object."},
        {"{{Template:Material_BehindTexture}}", "The texture to use when the material is behind an object."},
        {"{{Template:Material_BehindTextureScale}}", "The UV scale of <code>BehindTexture</code>."},
        {"{{Template:Material_BehindTextureOffset}}", "The UV offset of <code>BehindTexture</code>."},
        {"{{Template:Material_FrontTexture}}", "The texture to use when the material is in front of an object."},
        {"{{Template:Material_FrontTextureScale}}", "The UV scale of <code>FrontTexture</code>."},
        {"{{Template:Material_FrontTextureOffset}}", "The UV offset of <code>FrontTexture</code>."},
        {"{{Template:Material_Texture0Scale}}", "The size of the <code>Texture0</code> on the surface."},
        {"{{Template:Material_Texture0Offset}}", "The offset of the <code>Texture0</code> on the surface."},
        {"{{Template:Material_Texture1Scale}}", "The size of the <code>Texture1</code> on the surface."},
        {"{{Template:Material_Texture1Offset}}", "The offset of the <code>Texture1</code> on the surface."},
        {"{{Template:Material_AlbedoColor0}}", "The color tint of <code>Texture0</code>."},
        {"{{Template:Material_AlbedoColor1}}", "The color tint of <code>Texture1</code>."},
        {"{{Template:Material_AlbedoTexture0}}", "Texture 0 for albedo."},
        {"{{Template:Material_AlbedoTexture1}}", "Texture 1 for albedo."},
        {"{{Template:Material_EmissiveColor0}}", "The emissive texture tint for texture 0."},
        {"{{Template:Material_EmissiveColor1}}", "The emissive texture tint for texture 1."},
        {"{{Template:Material_EmissiveMap0}}", "The texture 0 for emissive."},
        {"{{Template:Material_EmissiveMap1}}", "The texture 1 for emissive."},
        {"{{Template:Material_NormalScale0}}", "The normal scale for normal 0."},
        {"{{Template:Material_NormalScale1}}", "The normal scale for normal 1."},
        {"{{Template:Material_OcclusionMap0}}", "The occlusion map for texture 0."},
        {"{{Template:Material_OcclusionMap1}}", "The occlusion map for texture 1."},
        {"{{Template:Material_Metallic0}}", "The metallicness for set 0 when a texture is not provided for set 0."},
        {"{{Template:Material_Metallic1}}", "The metallicness for set 1 when a texture is not provided for set 1."},
        {"{{Template:Material_Smoothness0}}", "The smoothness for set 0 when a texture is not provided for set 0."},
        {"{{Template:Material_Smoothness1}}", "The smoothness for set 1 when a texture is not provided for set 1."},
        {"{{Template:Material_MetallicMap0}}", "The Metallic map for texture set 0."},
        {"{{Template:Material_MetallicMap1}}", "The Metallic map for texture set 1."},
        {"{{Template:Material_SpecularColor0}}", "The color for the Specular for texture set 0."},
        {"{{Template:Material_SpecularColor1}}", "The color for the Specular for texture set 1."},
        {"{{Template:Material_SpecularMap0}}", "The map for the Specular for texture set 0."},
        {"{{Template:Material_SpecularMap1}}", "The map for the Specular for texture set 1."},
        {"{{Template:Material_AlbedoColor2}}", "The color to use for spots where B is on <code>ColorMask</code>"},
        {"{{Template:Material_AlbedoColor3}}", "The color to use for spots where A is on <code>ColorMask</code>"},
        {"{{Template:Material_AlbedoTexture}}", "The texture to use on the surface."},
        {"{{Template:Material_EmissiveColor2}}", "The color to use for spots where B is on <code>AlbedoTexture</code>"},
        {"{{Template:Material_EmissiveColor3}}", "The color to use for spots where A is on <code>AlbedoTexture</code>"},
        {"{{Template:Material_EmissiveMap}}", "The map to use to control the intensity of emissions."},
        {"{{Template:Material_OcclusionMap}}", "The map to use for lighting effectiveness or Occlusion."},
        {"{{Template:Material_Transparent}}", "Whether this should render transparent (BROKEN Right now)"},
        {"{{Template:Material_ForceZWrite}}", "Whether to enforce writing to the Z-buffer."},
        {"{{Template:Material_MetallicMap}}", "[[Component:PBS_Metallic#Metallic Maps|Metallic Maps]]"},
        {"{{Template:Material_SpecularMap}}", "[[Component:PBS_Specular#Specular Maps|Specular Maps]]"},
        {"{{Template:Material_ColorMap}}", "The color splat map to specify what textures go where."},
        {"{{Template:Material_ColorMapScale}}", "The scale of the color splat map."},
        {"{{Template:Material_ColorMapOffset}}", "The offset of the color splat map."},
        {"{{Template:Material_PackedHeightMap}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_HeightTransitionRange}}", "From 0 to this number is the encoding of the height map data."},
        {"{{Template:Material_AlbedoTexture2}}", "Albedo map 2."},
        {"{{Template:Material_AlbedoTexture3}}", "Albedo map 3."},
        {"{{Template:Material_EmissiveMap2}}", "The color map for emissive 2."},
        {"{{Template:Material_EmissiveMap3}}", "The color map for emissive 3."},
        {"{{Template:Material_PackedEmissionMap}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_PackedNormalMap01}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_PackedNormalMap23}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_NormalScale2}}", "The scaling of normal map 2."},
        {"{{Template:Material_NormalScale3}}", "The scaling of normal map 3."},
        {"{{Template:Material_Metallic2}}", "The Metallicness of map 2 in absence of <code>MetallicMap23</code>."},
        {"{{Template:Material_Metallic3}}", "The Metallicness of map 3 in absence of <code>MetallicMap23</code>."},
        {"{{Template:Material_Smoothness2}}", "The Smoothness of map 2 in absence of <code>MetallicMap23</code>."},
        {"{{Template:Material_Smoothness3}}", "The Smoothness of map 3 in absence of <code>MetallicMap23</code>."},
        {"{{Template:Material_MetallicMap01}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_MetallicMap23}}", "See [[#Packed Textures Channel Mappings]]."},
        {"{{Template:Material_SpecularColor2}}", "The color for the Specular 2 if <code>SpecularMap2</code> is null."},
        {"{{Template:Material_SpecularColor3}}", "The color for the Specular 3 if <code>SpecularMap3</code> is null."},
        {"{{Template:Material_SpecularMap2}}", "The Specular map plus color for Specular 2."},
        {"{{Template:Material_SpecularMap3}}", "The Specular map plus color for Specular 3."},
        {"{{Template:Material_AlbedoColor}}", "The color to multiply the texture of the albedo (base color) with. Basically a tint. Default white."},
        {"{{Template:Material_EmissiveColor}}", "The color to multiply the texture of the emission (glowly color) with. Basically a tint. Default white."},
        {"{{Template:Material_VertexDisplaceMap}}", "See [[#VertexDisplaceMap|VertexDisplaceMap]]."},
        {"{{Template:Material_VertexDisplaceMagnitude}}", "See [[#VertexDisplaceMap|VertexDisplaceMap]]."},
        {"{{Template:Material_VertexDisplaceBias}}", "See [[#VertexDisplaceMap|VertexDisplaceMap]]."},
        {"{{Template:Material_VertexDisplaceMapScale}}", "See [[#VertexDisplaceMap|VertexDisplaceMap]]."},
        {"{{Template:Material_VertexDisplaceMapOffset}}", "See [[#VertexDisplaceMap|VertexDisplaceMap]]."},
        {"{{Template:Material_UVDisplaceMap}}", "A map to displace the UV coordinates of the mesh for every other texture."},
        {"{{Template:Material_UVDisplaceMagnitude}}", "How strong the UV displace effect is."},
        {"{{Template:Material_UVDisplaceBias}}", "The amount to add to the uv displacement."},
        {"{{Template:Material_UVDisplaceMapScale}}", "The UV scale of the displacement map for UVs on the original surface."},
        {"{{Template:Material_UVDisplaceMapOffset}}", "The UV offset of the displacement map for UVs on the original surface."},
        {"{{Template:Material_WorldspaceVertexOffsetMap}}", "See [[#WorldSpaceVertexOffsetMap|WorldSpaceVertexOffsetMap]]."},
        {"{{Template:Material_WorldspaceOffsetMagnitude}}", "See [[#WorldSpaceVertexOffsetMap|WorldSpaceVertexOffsetMap]]."},
        {"{{Template:Material_WorldspaceVertexOffsetMapScale}}", "See [[#WorldSpaceVertexOffsetMap|WorldSpaceVertexOffsetMap]]."},
        {"{{Template:Material_WorldspaceVertexOffsetMapOffset}}", "See [[#WorldSpaceVertexOffsetMap|WorldSpaceVertexOffsetMap]]."},
        {"{{Template:Material_WorldspaceOffsetPerVertex}}", "See [[#WorldSpaceVertexOffsetMap|WorldSpaceVertexOffsetMap]]."},
        {"{{Template:Material_AlphaHandling}}", "How to handle alpha values in pixels on the <code>AlbedoTexture</code>."},
        {"{{Template:Material_AlphaClip}}", "Any alpha value below this amount is not rendered for any given pixel when cutout is enabled."},
        {"{{Template:Material_Points}}", "A list of (global) positions and tints. The tint is applied to mesh vertices more or less depending on the mesh vertex's distance to the given point: larger distances means less tint."},
        {"{{Template:Material__useAlphaClip}}", "Whether to respect the value for <code>AlphaClip</code>."},
        {"{{Template:Material_Resolution}}", "The resolution of the pixel effect."},
        {"{{Template:Material_ResolutionMagnitudeTexture}}", "The texture to determine the resolution of certain areas of the surface."},
        {"{{Template:Material_ResolutionTextureScale}}", "How big to scale the <code>ResolutionMagnitudeTexture</code> along the surface."},
        {"{{Template:Material_ResolutionTextureOffset}}", "How much to offset the <code>ResolutionMagnitudeTexture</code> along the surface."},
        {"{{Template:Material_Levels}}", "How many different levels of color there should be per color channel."},
        {"{{Template:Material_SunQuality}}", "The quality of the sun visual in the sky if any."},
        {"{{Template:Material_SunSize}}", "The radius of the sun in the sky."},
        {"{{Template:Material_Sun}}", "The light being used for the sun position and color."},
        {"{{Template:Material_AtmosphereThickness}}", "The thickness of the atmosphere, which determines how high up the \"blue\" refraction effect seen in atmospheres. if this value is low, the blue of the sky becomes a thin band on the horizon that is faint. this would give the illusion of a thin atmosphere."},
        {"{{Template:Material_SkyTint}}", "The color of the atmosphere in the sky. for earth this is blue."},
        {"{{Template:Material_GroundColor}}", "The color of the sky below the horizon."},
        {"{{Template:Material_Exposure}}", "How much exposure or brightness should be given to the sky."},
        {"{{Template:Material_ReflectionTexture}}", "A texture usually generated by a [[Component:CameraPortal|Camera Portal]] or previously rendered by a Camera Portal."},
        {"{{Template:Material_Distort}}", "How much effect the normal map should have on the reflection."},
        {"{{Template:Material_TintColor}}", "What color to tint the projected image."},
        {"{{Template:Material_DepthBias}}", "The bias for the depth buffer affecting the refraction strength."},
        {"{{Template:Material_FontAtlas}}", "The font to render."},
        {"{{Template:Material_BackgroundColor}}", "The color of behind each text glyph"},
        {"{{Template:Material_AutoBackgroundColor}}", "Whether to automatically generate the background text color."},
        {"{{Template:Material_GlyphRenderMethod}}", "How to render each glyph or text character."},
        {"{{Template:Material_PixelRange}}", "Sets the distance field range in output pixels."},
        {"{{Template:Material_FaceDilate}}", "How fat to make the letters like bolding them."},
        {"{{Template:Material_OutlineThickness}}", "The thickness of the outline on text for colored outlines."},
        {"{{Template:Material_FaceSoftness}}", "How much blurring to do on the edges of the text."},
        {"{{Template:Material_Texture}}", "The texture to debug data for."},
        {"{{Template:Material_TextureChannel}}", "The channel to display from <code>Texture</code> as the surface."},
        {"{{Template:Material_Threshold}}", "How much brightness is needed to pass the filter."},
        {"{{Template:Material_Transition}}", "How much of the color to transition across at the threshold limit."},
        {"{{Template:Material_FillTint}}", "The center color of the circle arc"},
        {"{{Template:Material_Overlay}}", "Whether this material should render on top of everything"},
        {"{{Template:Material_OverlayTint}}", "what to tint the entire material as."},
        {"{{Template:Material_Tint}}", "The color to multiply or tint by for the entire material."},
        {"{{Template:Material_TextureMode}}", "How to use <code>Texture</code>."},
        {"{{Template:Material_Rect}}", "The rectangle to render on the  material surface."},
        {"{{Template:Material_OuterColor}}", "The color for the rest of the material."},
        {"{{Template:Material_InnerColor}}", "The color inside the rectangle."},
        {"{{Template:Material_LocalSpace}}", "Whether to calculate <code>Distance</code> and <code>Point</code> in local space."},
        {"{{Template:Material_Point}}", "The point to measure from."},
        {"{{Template:Material_Distance}}", "The distance where the transition from near to far starts."},
        {"{{Template:Material_TransitionRange}}", "How much distance from near, far is."},
        {"{{Template:Material_OffsetTexture}}", "The texture used to offset the positions of pixels."},
        {"{{Template:Material_OffsetMagnitude}}", "How much to amplify the offset effect."},
        {"{{Template:Material_OffsetTextureScale}}", "How much to scale the detail of the <code>OffsetTexture</code>"},
        {"{{Template:Material_OffsetTextureOffset}}", "How much to offset the detail of the <code>OffsetTexture</code>"},
        {"{{Template:Material_StereoTextureTransform}}", "Whether a texture on the surface should appear different in the right eye."},
        {"{{Template:Material_RightEyeTextureScale}}", "The override for texture in the right eye for scale."},
        {"{{Template:Material_RightEyeTextureOffset}}", "The override for texture in the right eye for offset."},
        {"{{Template:Material_DecodeAsNormalMap}}", "Whether to decode the textures as a normal map."},
        {"{{Template:Material_UseBillboardGeometry}}", "Whether to not render the surface, and instead render each vertex like a Particle with this material and texture."},
        {"{{Template:Material_UsePerBillboardScale}}", "Whether scaling of vertex points should be used for Billboard geometry positioning."},
        {"{{Template:Material_UsePerBillboardRotation}}", "Whether rotation of vertex points should be used for Billboard geometry positioning."},
        {"{{Template:Material_UsePerBillboardUV}}", "Whether uv data of vertex points should be used for Billboard geometry positioning."},
        {"{{Template:Material_BillboardSize}}", "The base size for Billboard geometry."},
        {"{{Template:Material_Mode}}", "How to display the texture in 3D space."},
        {"{{Template:Material_Volume}}", "The 3D texture this material should display."},
        {"{{Template:Material_StepSize}}", "How many cell details there should be."},
        {"{{Template:Material_Gain}}", "How much to gain up the color."},
        {"{{Template:Material_Exp}}", "What exponent to raise colors to."},
        {"{{Template:Material_AccumulationCutoff}}", "if color goes beyond this brightness, don't render it."},
        {"{{Template:Material_HitThreshold}}", "tells what value to consider a hit. Any value above this is drawn, and any other value below this is culled."},
        {"{{Template:Material_InputRange}}", "What range duration within 0<->1 do the colors in the 3d image have? Useful for isolating particular ranges for densities in a CT scan."},
        {"{{Template:Material_InputOffset}}", "at what point does the range start?"},
        {"{{Template:Material_UseAlphaChannel}}", "Whether to use the alpha channel in the pixel data of <code>Volume</code>."},
        {"{{Template:Material_Slices}}", "Slice planes that cut the volume along local positions and directions."},
        {"{{Template:Material_Highlights}}", "A list of planes with offsets which define a region of the volume to override the color of."},
        {"{{Template:Material_Thickness}}", "the thickness of the lines in the polygons"},
        {"{{Template:Material_ScreenSpace}}", "whether the thickness should stay constant in width on the screen regardless of distance"},
        {"{{Template:Material_LineColor}}", "the color of this material's lines"},
        {"{{Template:Material_FillColor}}", "the color that should fill the center of each polygon."},
        {"{{Template:Material_InnerLineColor}}", "The color of the lines on the inner frensel."},
        {"{{Template:Material_InnerFillColor}}", "The color of the fill in the inner frensel."},
        {"{{Template:Material_UseFresnel}}", "Whether to use the frensel effect with the normal and inner line and fill colors."},
        {"{{Template:Material_LineFarColor}}", "what the color of the lines should be when this material is rendered further away zbuffer wise."},
        {"{{Template:Material_FillFarColor}}", "what the color of the fill should be when this material is rendered further away zbuffer wise."},
        {"{{Template:Material_InnerLineFarColor}}", "what the color of the lines on the inner frensel should be when this material is rendered further away zbuffer wise."},
        {"{{Template:Material_InnerFillFarColor}}", "what the color of the fill on the inner frensel should be when this material is rendered further away zbuffer wise."},
        {"{{Template:Material_DoubleSided}}", "Whether to render this material on both sides."},
        {"{{Template:Material_Saturation}}", "The saturation multiplier of <code>MainTexture</code>."},
        {"{{Template:Material_Metallic}}", "The metallicness of the material in the absence of <code>MetallicGlossMap</code>."},
        {"{{Template:Material_Glossiness}}", "The glossiness of the material in the absence of <code>MetallicGlossMap</code>."},
        {"{{Template:Material_Reflectivity}}", "The reflectivity of the material in the absence of <code>MetallicGlossMap</code>."},
        {"{{Template:Material_MetallicGlossMap}}", "A packed channel texture for Metallic, glossyness, and reflectivity."},
        {"{{Template:Material_MetallicGlossMapScale}}", "The UV scaling of the <code>MetallicGlossMap</code>."},
        {"{{Template:Material_MetallicGlossMapOffset}}", "The UV offset of the <code>MetallicGlossMap</code>."},
        {"{{Template:Material_RimAlbedoTint}}", "How much the rim lighting effects albedo tint."},
        {"{{Template:Material_RimAttenuationEffect}}", "The strength of the rim light"},
        {"{{Template:Material_RimIntensity}}", "Brightness of rim lighting"},
        {"{{Template:Material_RimRange}}", "Size of light area of the shadowramp"},
        {"{{Template:Material_RimThreshold}}", "Size of rim light area"},
        {"{{Template:Material_RimSharpness}}", "Definition of the shadowramp"},
        {"{{Template:Material_MatcapTint}}", "The color tint to apply to <code>Matcap</code>."},
        {"{{Template:Material_OcclusionMapScale}}", "The UV scale of <code>OcclusionMap</code>"},
        {"{{Template:Material_OcclusionMapOffset}}", "The UV offset of <code>OcclusionMap</code>."},
        {"{{Template:Material_OcclusionColor}}", "The color of the occlusion effect."},
        {"{{Template:Material_OutlineAlbedoTint}}", "Whether the outline is tinted by albedo."},
        {"{{Template:Material_OutlineMask}}", "Grayscale mask that determines relative thickeness of outline. Black is no outline. White is 100% of the OutlineWidth value."},
        {"{{Template:Material_ShadowRamp}}", "The shadow ramp used to influence what shadows look like on a model."},
        {"{{Template:Material_ShadowRampMask}}", "Grayscale mask that selects which pixel row of the Shadow Ramp texture to use for a given texel."},
        {"{{Template:Material_ShadowRampMaskScale}}", "The UV scale of <code>ShadowRampMask</code>."},
        {"{{Template:Material_ShadowRampMaskOffset}}", "The UV offset of <code>ShadowRampMask</code>."},
        {"{{Template:Material_ShadowRim}}", "Color of the dark rim light"},
        {"{{Template:Material_ShadowSharpness}}", "Definition of all shadows applied to the material"},
        {"{{Template:Material_ShadowRimRange}}", "Size of the dark section of the shadowramp"},
        {"{{Template:Material_ShadowRimThreshold}}", "Size of the dark section of the shadowramp"},
        {"{{Template:Material_ShadowRimSharpness}}", "Definition of the dark section of the shadowramp"},
        {"{{Template:Material_ShadowRimAlbedoTint}}", "How much the albedo color influences the dark section of the shadowramp"},
        {"{{Template:Material_ThicknessMap}}", "The thickness map of the surface. Used to make subsurface effects."},
        {"{{Template:Material_ThicknessMapScale}}", "The UV scale of <code>ThicknessMap</code>."},
        {"{{Template:Material_ThicknessMapOffset}}", "The UV offset of <code>ThicknessMap</code>"},
        {"{{Template:Material_SubsurfaceColor}}", "The color of the subsurface effect."},
        {"{{Template:Material_SubsurfaceDistortion}}", "Strength of subsurface color"},
        {"{{Template:Material_SubsurfacePower}}", "Strength of subsurface color"},
        {"{{Template:Material_SubsurfaceScale}}", "Strength of subsurface color"},
        {"{{Template:Material_AlbedoUV}}", "The UV map index Albedo map should use."},
        {"{{Template:Material_NormalUV}}", "The UV map index normal map should use."},
        {"{{Template:Material_MetallicUV}}", "The UV map index metallic map should use."},
        {"{{Template:Material_ThicknessUV}}", "The UV map index Thickness map should use."},
        {"{{Template:Material_OcclusionUV}}", "The UV map index occlusion map should use."},
        {"{{Template:Material_EmissionUV}}", "The UV map index emission map should use."},
    };
    public async Task<int> Run(WorkContext context, string[] args)
    {
        var db = context.DbConnection;
        await using var transaction = await db.BeginTransactionAsync();

        foreach (var page in CreateTemplates)
        {
            db.Execute("INSERT INTO wiki_page_create_queue(title, text) VALUES (@Title, @Text)",
            new
            {
                Title = page.Key.Replace("{{", "").Replace("}}", ""),
                Text = page.Value
            });
        }

        transaction.Commit();

        return 0;
    }
}
