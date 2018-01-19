using AlphaChiTech.VirtualizingCollection.Pageing;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IPagedSourceProvider<T> : IBaseSourceProvider
    {
        PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder);

        int Count { get; }

        int IndexOf(T item);
    }
}
