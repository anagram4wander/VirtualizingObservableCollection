using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaChiTech.Virtualization
{
    public class VirtualizationManager
    {
        private static VirtualizationManager _Instance = new VirtualizationManager();

        private List<IVirtualizationAction> _Actions = new List<IVirtualizationAction>();
        private object _ActionLock = new object();

        private static bool _IsInitialized = false;

        public static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        public static VirtualizationManager Instance
        {
            get
            {
                return _Instance;
            }
        }

        bool _Processing = false;

        private Action<Action> _UIThreadExcecuteAction = null;

        public Action<Action> UIThreadExcecuteAction
        {
            get { return _UIThreadExcecuteAction; }
            set
            {
                _UIThreadExcecuteAction = value;
                _IsInitialized = true;
            }
        }

        public void ProcessActions()
        {
            if (_Processing) return;

            _Processing = true;

            List<IVirtualizationAction> lst;
            lock (_ActionLock)
            {
                lst = _Actions.ToList();
            }

            foreach (var action in lst)
            {
                bool bdo = true;

                if (action is IRepeatingVirtualizationAction)
                {
                    bdo = (action as IRepeatingVirtualizationAction).IsDueToRun();
                }

                if (bdo)
                {
                    switch (action.ThreadModel)
                    {
                        case VirtualActionThreadModelEnum.UseUIThread:
                            UIThreadExcecuteAction.Invoke(() => action.DoAction());
                            break;
                        case VirtualActionThreadModelEnum.Background:
                            Task.Run(() => action.DoAction());
                            break;
                    }

                    if (action is IRepeatingVirtualizationAction)
                    {
                        if (!(action as IRepeatingVirtualizationAction).KeepInActionsList())
                        {
                            lock (_ActionLock)
                            {
                                _Actions.Remove(action);
                            }
                        }
                    }
                    else
                    {
                        lock (_ActionLock)
                        {
                            _Actions.Remove(action);
                        }
                    }
                }
            }

            _Processing = false;
        }
        public void AddAction(IVirtualizationAction action)
        {
            lock (_ActionLock)
            {
                _Actions.Add(action);
            }
        }

        public void AddAction(Action action)
        {
            AddAction(new ActionVirtualizationWrapper(action));
        }

        public void RunOnUI(IVirtualizationAction action)
        {
            UIThreadExcecuteAction.Invoke(() => action.DoAction());
        }

        public void RunOnUI(Action action)
        {
            RunOnUI(new ActionVirtualizationWrapper(action));
        }
    }
}
