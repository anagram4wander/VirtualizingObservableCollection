using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public class BasePagedSourceProvider<T> : IPagedSourceProvider<T>
    {
        public BasePagedSourceProvider()
        {
        }

        public BasePagedSourceProvider(
            Func<int, int, PagedSourceItemsPacket<T>> funcGetItemsAt = null,
            Func<int> funcGetCount = null,
            Func<T, int> funcIndexOf = null,
            Action<int> actionOnReset = null
            )
        {
            this.FuncGetItemsAt = funcGetItemsAt;
            this.FuncGetCount = funcGetCount;
            this.FuncIndexOf = funcIndexOf;
            this.ActionOnReset = actionOnReset;
        }

        private Func<int, int, PagedSourceItemsPacket<T>> _FuncGetItemsAt = null;

        public Func<int, int, PagedSourceItemsPacket<T>> FuncGetItemsAt
        {
            get { return _FuncGetItemsAt; }
            set { _FuncGetItemsAt = value; }
        }
        private Func<int> _FuncGetCount = null;

        public Func<int> FuncGetCount
        {
            get { return _FuncGetCount; }
            set { _FuncGetCount = value; }
        }
        private Func<T, int> _FuncIndexOf = null;

        public Func<T, int> FuncIndexOf
        {
            get { return _FuncIndexOf; }
            set { _FuncIndexOf = value; }
        }
        private Action<int> _ActionOnReset = null;

        public Action<int> ActionOnReset
        {
            get { return _ActionOnReset; }
            set { _ActionOnReset = value; }
        }

        public virtual PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder)
        {
            if (_FuncGetItemsAt != null) return _FuncGetItemsAt.Invoke(pageoffset, count);

            return null;
        }

        public virtual int Count
        {
            get
            {
                int ret = 0;

                if (_FuncGetCount != null) ret = _FuncGetCount.Invoke();

                return ret;
            }
        }

        public virtual int IndexOf(T item)
        {
            int ret = -1;

            if (_FuncIndexOf != null) ret = _FuncIndexOf.Invoke(item);

            return ret;
        }

        public virtual void OnReset(int count)
        {
            if (_ActionOnReset != null) _ActionOnReset.Invoke(count);
        }
    }
}
