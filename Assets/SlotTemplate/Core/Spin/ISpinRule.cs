namespace SlotTemplate.Core.Spin
{
    /// <summary>
    /// One step of spin evaluation: pays, scatters, feature triggers. Rules run in order and must be
    /// deterministic for a given grid, so a saved spin can be replayed exactly.
    /// </summary>
    public interface ISpinRule
    {
        void Apply(SpinContext context);
    }
}
