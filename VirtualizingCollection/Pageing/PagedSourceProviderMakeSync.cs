using System;
using System.Threading.Tasks;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.VirtualizingCollection.Pageing
{
    public class PagedSourceProviderMakeSync<T> : IPagedSourceProviderAsync<T>, IProviderPreReset
    {
        public PagedSourceProviderMakeSync()
        {
        }

        public PagedSourceProviderMakeSync(
            Func<int, int, Task<PagedSourceItemsPacket<T>>> funcGetItemsAtAsync = null,
            Func<Task<int>> funcGetCountAsync = null,
            Func<T, int> funcIndexOf = null,
            Func<T, Task<int>> funcIndexOfAsync = null,
            Action<int> actionOnReset = null,
            Func<int, int, int, T> funcGetPlaceHolder = null,
            Action actionOnBeforeReset = null
            )
        {
            this.FuncGetItemsAtAsync = funcGetItemsAtAsync;
            this.FuncGetCountAsync = funcGetCountAsync;
            this.FuncIndexOf = funcIndexOf;
            this.FuncIndexOfAsync = funcIndexOfAsync;
            this.ActionOnReset = actionOnReset;
            this.FuncGetPlaceHolder = funcGetPlaceHolder;
            this.ActionOnBeforeReset = actionOnBeforeReset;
        }

        public virtual void OnBeforeReset()
        {
            if(this.ActionOnBeforeReset != null)
            {
                this.ActionOnBeforeReset.Invoke();
            }
        }

        Action _ActionOnBeforeReset = null;

        public Action ActionOnBeforeReset
        {
            get { return this._ActionOnBeforeReset; }
            set { this._ActionOnBeforeReset = value; }
        }


        Func<T, Task<int>> _FuncIndexOfAsync = null;

        public Func<T, Task<int>> FuncIndexOfAsync
        {
            get { return this._FuncIndexOfAsync; }
            set { this._FuncIndexOfAsync = value; }
        }

        private Func<int, int, Task<PagedSourceItemsPacket<T>>> _FuncGetItemsAtAsync = null;

        public Func<int, int, Task<PagedSourceItemsPacket<T>>> FuncGetItemsAtAsync
        {
            get { return this._FuncGetItemsAtAsync; }
            set { this._FuncGetItemsAtAsync = value; }
        }

        private Func<int, int, int, T> _FuncGetPlaceHolder = null;

        public Func<int, int, int, T> FuncGetPlaceHolder
        {
            get { return this._FuncGetPlaceHolder; }
            set { this._FuncGetPlaceHolder = value; }
        }

        private Func<Task<int>> _FuncGetCountAsync = null;

        public Func<Task<int>> FuncGetCountAsync
        {
            get { return this._FuncGetCountAsync; }
            set { this._FuncGetCountAsync = value; }
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

        public virtual Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int pageoffset, int count, bool usePlaceholder)
        {
            if (this._FuncGetItemsAtAsync != null) 
                return this._FuncGetItemsAtAsync.Invoke(pageoffset, count);

            return null;
        }

        public virtual T GetPlaceHolder(int index, int page, int offset)
        {
            T ret = default(T);

            if (this._FuncGetPlaceHolder != null) ret = this._FuncGetPlaceHolder.Invoke(index, page, offset);

            return ret;
        }

        public virtual Task<int> GetCountAsync()
        {
            Task<int> ret = null;

            if (this._FuncGetCountAsync != null)
            {
                ret = this._FuncGetCountAsync.Invoke();
            }

            return ret;
        }

        public PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder)
        {
            PagedSourceItemsPacket<T> ret = null;

            return Task.Run( () => this.GetItemsAtAsync(pageoffset, count, usePlaceholder)).Result;
                
        }

        public int Count
        {
            get { return Task.Run( () => this.GetCountAsync()).Result; }
        }

        public virtual int IndexOf(T item)
        {
            int ret = -1;

            if (this._FuncIndexOf != null)
            {
                ret = this._FuncIndexOf.Invoke(item);
            }
            else if (this._FuncIndexOfAsync != null)
            {
                ret = Task.Run( () => this._FuncIndexOfAsync.Invoke(item)).Result;
            } 
            else
            {
                ret = Task.Run(() => this.IndexOfAsync(item)).Result;
            }

            return ret;
        }

        public virtual async Task<int> IndexOfAsync(T item)
        {
            int ret = -1;

            return ret;
        }

        public virtual void OnReset(int count)
        {
            if (this._ActionOnReset != null) this._ActionOnReset.Invoke(count);
        }
    }
}
