using System;

namespace AlphaChiTech.VirtualizingCollection.Actions
{
    public class PlaceholderReplaceWA<T> : BaseActionVirtualization
    {
        private T _OldValue;
        private T _NewValue;
        private int _Index;

        WeakReference _VOC;

        public PlaceholderReplaceWA(VirtualizingObservableCollection<T> voc, T oldValue, T newValue, int index)
            : base(VirtualActionThreadModelEnum.UseUIThread)
        {
            this._VOC = new WeakReference(voc);
            this._OldValue = oldValue;
            this._NewValue = newValue;
            this._Index = index;
        }

        public override void DoAction()
        {
            var voc = (VirtualizingObservableCollection<T>)this._VOC.Target;

            if (voc != null && this._VOC.IsAlive)
            {
                voc.ReplaceAt(this._Index, this._OldValue, this._NewValue, null);
            }
        }
    }

}
