using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public static class TaskExtension
    {
        public static Task Run(this TaskFactory factory, Action action)
        {
            return Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static Task<T> Run<T>(this TaskFactory factory, Func<T> func)
        {
            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        public static Task<T> Run<T>(this TaskFactory factory, Func<Task<T>> func)
        {
            return Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap();
        }
    }
}
