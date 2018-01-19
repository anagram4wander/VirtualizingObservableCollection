using System;
using System.Collections.Specialized;

namespace AlphaChiTech.VirtualizingCollection.Actions
{
    public class ExecuteResetWA<T> : BaseActionVirtualization
    {
        WeakReference _VOC;

        public ExecuteResetWA(VirtualizingObservableCollection<T> voc)
            : base(VirtualActionThreadModelEnum.UseUIThread)
        {
            this._VOC = new WeakReference(voc);
        }

        public override void DoAction()
        {
            var voc = (VirtualizingObservableCollection<T>)this._VOC.Target;

            if (voc != null && this._VOC.IsAlive)
            {
                voc.RaiseCollectionChangedEvent(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

    }
}
