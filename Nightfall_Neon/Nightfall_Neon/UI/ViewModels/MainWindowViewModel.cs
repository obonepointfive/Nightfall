using HelixToolkit.Wpf.SharpDX;
using Nightfall.Visuals;
using SharpDX;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HelixToolkit.Wpf.SharpDX;

namespace Nightfall.UI.ViewModels
{
    public sealed class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Exposed to XAML
        public PerspectiveCamera HudCamera { get; }
        public EffectsManager EffectsManager { get; }
        public MeshGeometry3D FullscreenQuad { get; }
        public PhongMaterial HudMaterial { get; }

        public ObservableCollection<SystemMessageVM> SystemMessages { get; } = new();
        private string _hudModeLabel = "ACTIVE TRAFFIC";
        private string _emotionLabel = "Mood: Neutral • Focus: 0.5";

        public string HudModeLabel
        {
            get => _hudModeLabel;
            set { _hudModeLabel = value; OnPropertyChanged(); }
        }

        public string EmotionLabel
        {
            get => _emotionLabel;
            set { _emotionLabel = value; OnPropertyChanged(); }
        }

        private readonly AuraEngine _aura;

        public MainWindowViewModel()
        {
            EffectsManager = new DefaultEffectsManager();

            // Camera looking at origin
            HudCamera = new PerspectiveCamera
            {
                Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5),
                LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -5),
                UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0),
                FieldOfView = 45
            };

            // Fullscreen quad in XY plane, Z=0
            FullscreenQuad = HudRenderer.CreateFullscreenQuad();
            System.Diagnostics.Debug.WriteLine("UV Count = " + FullscreenQuad.TextureCoordinates?.Count);

            // Base HUD material (texture + emissive tint)
            HudMaterial = HudRenderer.CreateHudMaterial(EffectsManager, "Assets/HUD/aethera_cognitive_cortex.png");
            _aura = new AuraEngine(HudMaterial);

              System.Diagnostics.Debug.WriteLine(
                "TextureModel loaded from: " + typeof(HelixToolkit.Wpf.SharpDX.TextureModel).Assembly.FullName
            );

            // Hook render tick to animate aura
            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            _aura.Update(); // ticks time + updates emissive color etc.
        }

        public void UpdateFromTelemetry(HudTelemetry t)
        {
            HudModeLabel = t.ModeLabel;
            EmotionLabel = t.EmotionLabel;

            _aura.ApplyMood(t.MoodWarmth, t.MoodIntensity, t.MoodFocus);
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class SystemMessageVM
    {
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public sealed class HudTelemetry
    {
        public string ModeLabel { get; set; } = "ACTIVE TRAFFIC";
        public string EmotionLabel { get; set; } = "Mood: Neutral";
        public float MoodWarmth { get; set; } = 0.5f;
        public float MoodIntensity { get; set; } = 0.5f;
        public float MoodFocus { get; set; } = 0.5f;
    }
}
