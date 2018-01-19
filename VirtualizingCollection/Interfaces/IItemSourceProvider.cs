namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IItemSourceProvider<T> : IBaseSourceProvider
    {
        T GetAt(int index, object voc, bool usePlaceholder);
        int GetCount(bool asyncOK);

        int IndexOf(T item);
    }
}
