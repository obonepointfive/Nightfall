using HelixToolkit.Geometry;
using HelixToolkit.Maths;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using System;
using System.IO;

namespace Nightfall.Visuals
{
    public static class HudRenderer
    {
        //----------------------------------------------------
        // FULLSCREEN QUAD (DX11 coordinates)
        //----------------------------------------------------
        public static MeshGeometry3D CreateFullscreenQuad()
        {
            var builder = new MeshBuilder();

            builder.AddQuad(
                new Vector3(-1, -1, 0.1f),
                new Vector3(1, -1, 0.1f),
                new Vector3(1, 1, 0.1f),
                new Vector3(-1, 1, 0.1f)
            );

            return builder.ToMeshGeometry3D();
        }

        //----------------------------------------------------
        // PNG / DDS MATERIAL LOADER — WORKS IN 3.1.1
        //----------------------------------------------------
        public static PhongMaterial CreateHudMaterial(IEffectsManager effects, string relativePath)
        {
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"HUD texture not found: {fullPath}");

            using var fs = File.OpenRead(fullPath);

            // HelixToolkit 3.1.1: Correct DX11 texture loader
            var texture = TextureModel.Create(effects.Device, fs);

            return new PhongMaterial
            {
                DiffuseMap = texture,
                DiffuseColor = Color.White,
                EmissiveColor = new Color4(0f, 0.8f, 1f, 1f),
                RenderDiffuseMap = true,
                RenderDiffuseAlphaMap = false,
            };
        }
    }
}
