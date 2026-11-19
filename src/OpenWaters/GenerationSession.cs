namespace OpenWaters
{
    internal sealed class GenerationSession
    {
        public static GenerationSession Current { get; private set; } = new GenerationSession(null);

        public readonly World World;
        public float Offset0;
        public float Offset1;
        public float MinMountainDistance = 1000f;
        public float HeightOffset;
        public bool Active;
        public bool UsesGlobalNoise;

        public GenerationSession(World world)
        {
            World = world;
        }

        public static void Begin(World world)
        {
            Current = new GenerationSession(world);
            IslandAnalysis.Clear();
        }

        public void Cancel()
        {
            Active = false;
            HeightOffset = 0f;
            UsesGlobalNoise = false;
            IslandAnalysis.Clear();
        }
    }
}
