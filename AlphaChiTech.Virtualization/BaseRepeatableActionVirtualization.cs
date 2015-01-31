using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public abstract class BaseRepeatableActionVirtualization : BaseActionVirtualization, IRepeatingVirtualizationAction
    {
        protected DateTime _LastRun = DateTime.MinValue;
        private TimeSpan _RepeatingSchedule = TimeSpan.FromSeconds(1);

        public BaseRepeatableActionVirtualization(VirtualActionThreadModelEnum threadModel = VirtualActionThreadModelEnum.UseUIThread,
            bool isRepeating = false, TimeSpan? repeatingSchedule = null)
            : base(threadModel)
        {
            this.IsRepeating = isRepeating;
            if (repeatingSchedule.HasValue)
            {
                this.RepeatingSchedule = repeatingSchedule.Value;
            }
        }

        public TimeSpan RepeatingSchedule
        {
            get { return _RepeatingSchedule; }
            set { _RepeatingSchedule = value; }
        }

        private bool _IsRepeating = false;

        protected bool IsRepeating
        {
            get { return _IsRepeating; }
            set { _IsRepeating = value; }
        }

        public virtual bool KeepInActionsList()
        {
            return this.IsRepeating;
        }

        public virtual bool IsDueToRun()
        {

            if (DateTime.Now >= _LastRun.Add(this.RepeatingSchedule))
            {
                return true;
            }

            return false;
        }

    }

}
