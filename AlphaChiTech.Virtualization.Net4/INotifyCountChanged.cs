using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public delegate void OnCountChanged(object sender, CountChangedEventArgs args);

    public class CountChangedEventArgs : EventArgs
    {
        public bool NeedsReset { get; set; }
        public int Count { get; set; }
    }

    public interface INotifyCountChanged
    {
        event OnCountChanged CountChanged;
    }
}
