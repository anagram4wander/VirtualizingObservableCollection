namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IRepeatingVirtualizationAction
    {
        bool KeepInActionsList();
        bool IsDueToRun();
    }
}
