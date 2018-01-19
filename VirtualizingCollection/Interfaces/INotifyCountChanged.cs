using System;

namespace AlphaChiTech.VirtualizingCollection.Interfaces
{
    public delegate void OnCountChanged(object sender, CountChangedEventArgs args);

    public class CountChangedEventArgs : EventArgs
    {
        public int Count { get; set; }
        public bool NeedsReset { get; set; }
    }

    public interface INotifyCountChanged
    {
        event OnCountChanged CountChanged;
    }
}