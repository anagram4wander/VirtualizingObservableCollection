using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
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
            get { return _ActionOnBeforeReset; }
            set { _ActionOnBeforeReset = value; }
        }


        Func<T, Task<int>> _FuncIndexOfAsync = null;

        public Func<T, Task<int>> FuncIndexOfAsync
        {
            get { return _FuncIndexOfAsync; }
            set { _FuncIndexOfAsync = value; }
        }

        private Func<int, int, Task<PagedSourceItemsPacket<T>>> _FuncGetItemsAtAsync = null;

        public Func<int, int, Task<PagedSourceItemsPacket<T>>> FuncGetItemsAtAsync
        {
            get { return _FuncGetItemsAtAsync; }
            set { _FuncGetItemsAtAsync = value; }
        }

        private Func<int, int, int, T> _FuncGetPlaceHolder = null;

        public Func<int, int, int, T> FuncGetPlaceHolder
        {
            get { return _FuncGetPlaceHolder; }
            set { _FuncGetPlaceHolder = value; }
        }

        private Func<Task<int>> _FuncGetCountAsync = null;

        public Func<Task<int>> FuncGetCountAsync
        {
            get { return _FuncGetCountAsync; }
            set { _FuncGetCountAsync = value; }
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

        public virtual Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int pageoffset, int count, bool usePlaceholder)
        {
            if (_FuncGetItemsAtAsync != null) 
                return _FuncGetItemsAtAsync.Invoke(pageoffset, count);

            return null;
        }

        public virtual T GetPlaceHolder(int index, int page, int offset)
        {
            T ret = default(T);

            if (_FuncGetPlaceHolder != null) ret = _FuncGetPlaceHolder.Invoke(index, page, offset);

            return ret;
        }

        public virtual Task<int> GetCountAsync()
        {
            Task<int> ret = null;

            if (_FuncGetCountAsync != null)
            {
                ret = _FuncGetCountAsync.Invoke();
            }

            return ret;
        }

        public PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count, bool usePlaceholder)
        {
            PagedSourceItemsPacket<T> ret = null;

            return Task.Run( () => GetItemsAtAsync(pageoffset, count, usePlaceholder)).Result;
                
        }

        public int Count
        {
            get { return Task.Run( () => GetCountAsync()).Result; }
        }

        public virtual int IndexOf(T item)
        {
            int ret = -1;

            if (_FuncIndexOf != null)
            {
                ret = _FuncIndexOf.Invoke(item);
            }
            else if (_FuncIndexOfAsync != null)
            {
                ret = Task.Run( () => _FuncIndexOfAsync.Invoke(item)).Result;
            } 
            else
            {
                ret = Task.Run(() => IndexOfAsync(item)).Result;
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
            if (_ActionOnReset != null) _ActionOnReset.Invoke(count);
        }
    }
}
