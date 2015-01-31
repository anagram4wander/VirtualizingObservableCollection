using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class ActionVirtualizationWrapper : BaseRepeatableActionVirtualization
    {
        private Action _Action = null;

        public ActionVirtualizationWrapper(Action action,
            VirtualActionThreadModelEnum threadModel = VirtualActionThreadModelEnum.UseUIThread,
            bool isRepeating = false, TimeSpan? repeatingSchedule = null)
            : base(threadModel, isRepeating, repeatingSchedule)
        {
            _Action = action;
        }

        public override void DoAction()
        {
            var a = _Action;
            _LastRun = DateTime.Now;

            if (a != null)
            {
                a.Invoke();
            }
        }

    }

}
