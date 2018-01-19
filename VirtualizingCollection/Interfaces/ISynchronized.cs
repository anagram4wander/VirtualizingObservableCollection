namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface ISynchronized
    {
        object SyncRoot { get; }
        bool IsSynchronized { get; }
    }
}