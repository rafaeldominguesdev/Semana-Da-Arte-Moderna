namespace MuseumModerna
{
    /// <summary>Estado único da ficha. Independente de input e de Unity para testes determinísticos.</summary>
    public sealed class GuideFocusState
    {
        public PaintingInfo Current { get; private set; }
        public bool Pinned { get; private set; }
        public bool Enabled { get; private set; } = true;
        private PaintingInfo dismissed;
        private float timeWithoutTarget;

        public void Observe(PaintingInfo target, float delta, float linger)
        {
            if (target != dismissed) dismissed = null;
            if (!Enabled || Pinned) return;
            if (target != null && target != dismissed)
            {
                Current = target;
                timeWithoutTarget = 0;
            }
            else
            {
                timeWithoutTarget += delta;
                if (timeWithoutTarget >= linger) Current = null;
            }
        }

        public void TogglePin() { if (Current != null) Pinned = !Pinned; }
        public void Pin(PaintingInfo target)
        {
            if (!Enabled || target == null) return;
            dismissed = null;
            Current = target;
            Pinned = true;
        }
        public void Dismiss(PaintingInfo lookedAt)
        {
            dismissed = lookedAt;
            Current = null;
            Pinned = false;
            timeWithoutTarget = 0;
        }
        public void SetEnabled(bool value)
        {
            Enabled = value;
            if (!value) Dismiss(null);
        }
    }
}
