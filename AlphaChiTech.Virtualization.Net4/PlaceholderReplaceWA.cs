using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
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
            _VOC = new WeakReference(voc);
            _OldValue = oldValue;
            _NewValue = newValue;
            _Index = index;
        }

        public override void DoAction()
        {
            var voc = (VirtualizingObservableCollection<T>)_VOC.Target;

            if (voc != null && _VOC.IsAlive)
            {
                voc.ReplaceAt(_Index, _OldValue, _NewValue, null);
            }
        }
    }

}
