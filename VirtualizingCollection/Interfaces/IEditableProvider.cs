namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public interface IEditableProvider<in T>
    {
        int OnAppend(T item, object timestamp);
        void OnInsert(int index, T item, object timestamp);
        void OnReplace(int index, T oldItem, T newItem, object timestamp);
    }

    public interface IEditableProviderIndexBased<T> : IEditableProvider<T>
    {
        T OnRemove(int index, object timestamp);
        T OnReplace(int index,  T newItem, object timestamp);
    }

    public interface IEditableProviderItemBased<in T> : IEditableProvider<T>
    {
        int OnRemove(T item, object timestamp);
        int OnReplace(T oldItem, T newItem, object timestamp);
    }
}
