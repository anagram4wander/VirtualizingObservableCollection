using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class ExecuteResetWA<T> : BaseActionVirtualization
    {
        WeakReference _VOC;

        public ExecuteResetWA(VirtualizingObservableCollection<T> voc)
            : base(VirtualActionThreadModelEnum.UseUIThread)
        {
            _VOC = new WeakReference(voc);
        }

        public override void DoAction()
        {
            var voc = (VirtualizingObservableCollection<T>)_VOC.Target;

            if (voc != null && _VOC.IsAlive)
            {
                voc.RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

    }
}
