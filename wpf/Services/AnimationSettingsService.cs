namespace HyperBoostX.Services
{
    public sealed class AnimationSettingsService
    {
        public bool EnableAnimations { get; set; } = true;
        public bool ReduceMotion { get; set; }
        public bool UseMotionEffects => EnableAnimations && !ReduceMotion;
    }
}
