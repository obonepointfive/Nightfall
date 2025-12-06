using HelixToolkit.Wpf.SharpDX;
using SharpDX;


namespace Nightfall.Visuals
{
    /// <summary>
    /// Controls the subtle pulsing / color shift of the HUD based on mood.
    /// </summary>
    public sealed class AuraEngine
    {
        private readonly PhongMaterial _material;
        private float _time;
        private float _warmth = 0.5f;
        private float _intensity = 0.5f;
        private float _focus = 0.5f;

        public AuraEngine(PhongMaterial material)
        {
            _material = material;
        }

        public void ApplyMood(float warmth, float intensity, float focus)
        {
            _warmth = warmth;
            _intensity = intensity;
            _focus = focus;
        }

        public void Update()
        {
            _time += 0.016f; // ~60fps, fine for now

            // Base cyan
            float baseR = 0.0f + 0.4f * _warmth;
            float baseG = 0.7f + 0.2f * _warmth;
            float baseB = 1.0f;

            // Pulse intensity
            float pulse = 0.3f + 0.3f * _intensity * (float)Math.Sin(_time * (1.0f + 2.0f * _focus));

            var color = new Color4(
                baseR * (0.7f + pulse),
                baseG * (0.7f + pulse),
                baseB * (0.7f + pulse),
                1.0f);

            _material.EmissiveColor = color;
        }
    }
}
