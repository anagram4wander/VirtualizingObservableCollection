using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public class PagedSourceProviderMakeAsync<T> : BasePagedSourceProvider<T>, IPagedSourceProviderAsync<T>, IProviderPreReset
    {
      private Func<T, Task<int>> _FuncIndexOfAsync = null;
      private Func<int, int, int, T> _FuncGetPlaceHolder = null;

      public PagedSourceProviderMakeAsync()
        {
        }

        public PagedSourceProviderMakeAsync(
            Func<int, int, PagedSourceItemsPacket<T>> funcGetItemsAt = null,
            Func<int> funcGetCount = null,
            Func<T, Task<int>> funcIndexOfAsync = null,
            Action<int> actionOnReset = null,
            Func<int, int, int, T> funcGetPlaceHolder = null,
            Action actionOnBeforeReset = null
            )
            : base(funcGetItemsAt, funcGetCount, null, actionOnReset)
            //: base(funcGetItemsAt, funcGetCount, funcIndexOf, actionOnReset)
        {
            this.FuncGetPlaceHolder = funcGetPlaceHolder;
            this.ActionOnBeforeReset = actionOnBeforeReset;
          _FuncIndexOfAsync = funcIndexOfAsync;
        }

        public virtual void OnBeforeReset()
        {
            if (this.ActionOnBeforeReset != null)
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

        public Func<int, int, int, T> FuncGetPlaceHolder
        {
            get { return _FuncGetPlaceHolder; }
            set { _FuncGetPlaceHolder = value; }
        }

        public Func<T, Task<int>> FuncIndexOfAsync
        {
          get { return _FuncIndexOfAsync; }
          set { _FuncIndexOfAsync = value; }
        }

        public Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int pageoffset, int count, bool usePlaceholder)
        {
            var tcs = new TaskCompletionSource<PagedSourceItemsPacket<T>>();

            try
            {
                tcs.SetResult(this.GetItemsAt(pageoffset, count, usePlaceholder));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        public virtual T GetPlaceHolder(int index, int page, int offset)
        {
            T ret = default(T);

            if (_FuncGetPlaceHolder != null)
                ret = _FuncGetPlaceHolder.Invoke(index, page, offset);

            return ret;
        }

        public Task<int> GetCountAsync()
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                tcs.SetResult(this.Count);
            } catch(Exception e)
            {
                tcs.SetException(e);
            }

            return tcs.Task;
        }

      public Task<int> IndexOfAsync( T item )
      {
        var ret = default( Task<int> );

        if (_FuncIndexOfAsync != null)
          ret = _FuncIndexOfAsync.Invoke( item );

        return ret;
      }
    }
}
