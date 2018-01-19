namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IItemSourceProvider<T> : IBaseSourceProvider<T>
    {
        T GetAt(int index, object voc, bool usePlaceholder);
        int GetCount(bool asyncOk);

        int IndexOf(T item);
        bool Contains(T item);
    }
}
