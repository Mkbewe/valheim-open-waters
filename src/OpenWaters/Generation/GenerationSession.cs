namespace OpenWaters
{
    internal enum GenerationStage
    {
        Idle,
        Calibrating,
        Analyzing,
        Ready
    }

    internal sealed class GenerationSession
    {
        public static GenerationSession Current { get; private set; } = new GenerationSession(null);
        private static bool _deferNextSession;

        public readonly World World;
        public float Offset0;
        public float Offset1;
        public float MinMountainDistance = 1000f;
        public float HeightOffset;
        public bool Active;
        public bool UsesGlobalNoise;
        public NoiseField Field;
        public GenerationStage Stage { get; private set; } = GenerationStage.Idle;
        public bool DeferIslandAnalysis { get; set; }

        public GenerationSession(World world)
        {
            World = world;
        }

        public static void DeferNextSession()
        {
            _deferNextSession = true;
        }

        public static void ClearDeferredSession()
        {
            _deferNextSession = false;
        }

        public static void Begin(World world)
        {
            Current = new GenerationSession(world)
            {
                DeferIslandAnalysis = _deferNextSession
            };
            _deferNextSession = false;
            IslandAnalysis.Clear();
        }

        public void BeginBuild()
        {
            Stage = GenerationStage.Calibrating;
            Active = false;
            HeightOffset = 0f;
            Field = null;
        }

        public void BeginAnalysis()
        {
            Stage = GenerationStage.Analyzing;
        }

        public void MarkReady()
        {
            Stage = GenerationStage.Ready;
            Active = true;
        }

        public void Cancel()
        {
            Stage = GenerationStage.Idle;
            Active = false;
            HeightOffset = 0f;
            UsesGlobalNoise = false;
            Field = null;
            DeferIslandAnalysis = false;
            IslandAnalysis.Clear();
        }
    }
}
