using System;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.VirtualizingCollection.Pageing
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
            get { return this._FuncGetItemsAt; }
            set { this._FuncGetItemsAt = value; }
        }
        private Func<int> _FuncGetCount = null;

        public Func<int> FuncGetCount
        {
            get { return this._FuncGetCount; }
            set { this._FuncGetCount = value; }
        }
        private Func<T, int> _FuncIndexOf = null;

        public Func<T, int> FuncIndexOf
        {
            get { return this._FuncIndexOf; }
            set { this._FuncIndexOf = value; }
        }
        private Action<int> _ActionOnReset = null;

        public Action<int> ActionOnReset
        {
            get { return this._ActionOnReset; }
            set { this._ActionOnReset = value; }
        }

        public virtual PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder)
        {
            if (this._FuncGetItemsAt != null) return this._FuncGetItemsAt.Invoke(pageoffset, count);

            return null;
        }

        public virtual int Count
        {
            get
            {
                int ret = 0;

                if (this._FuncGetCount != null) ret = this._FuncGetCount.Invoke();

                return ret;
            }
        }

        public virtual int IndexOf(T item)
        {
            int ret = -1;

            if (this._FuncIndexOf != null) ret = this._FuncIndexOf.Invoke(item);

            return ret;
        }

        public virtual void OnReset(int count)
        {
            if (this._ActionOnReset != null) this._ActionOnReset.Invoke(count);
        }
    }
}
