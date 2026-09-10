namespace MuseumModerna
{
    /// <summary>Acionamento único por permanência, rearmado somente ao trocar/sair do alvo.</summary>
    public sealed class GazeActivation
    {
        private object target;
        private float elapsed;
        private bool fired;
        public float Progress { get; private set; }
        public bool Step(object value, float delta, float duration)
        {
            if (!ReferenceEquals(target, value)) { target = value; elapsed = 0; fired = false; }
            if (value == null) { Progress = 0; return false; }
            if (fired) { Progress = 1; return false; }
            elapsed += delta;
            Progress = duration <= 0 ? 1 : System.Math.Min(1, elapsed / duration);
            if (Progress < 1) return false;
            fired = true;
            return true;
        }
        public void Reset() { target = null; elapsed = 0; fired = false; Progress = 0; }
    }
}
