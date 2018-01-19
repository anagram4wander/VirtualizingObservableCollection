using System;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.VirtualizingCollection.Actions
{
    /// <summary>
    /// Base class there the Action repeats on a periodic basis (the RepeatingSchedule) like BaseActionVirtualization
    /// simply implement the DoAction method.
    /// </summary>
    public abstract class BaseRepeatableActionVirtualization : BaseActionVirtualization, IRepeatingVirtualizationAction
    {
        protected DateTime _LastRun = DateTime.MinValue;
        private TimeSpan _RepeatingSchedule = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepeatableActionVirtualization"/> class.
        /// </summary>
        /// <param name="threadModel">The thread model.</param>
        /// <param name="isRepeating">if set to <c>true</c> [is repeating].</param>
        /// <param name="repeatingSchedule">The repeating schedule.</param>
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

        /// <summary>
        /// Gets or sets the repeating schedule.
        /// </summary>
        /// <value>
        /// The repeating schedule.
        /// </value>
        public TimeSpan RepeatingSchedule
        {
            get { return this._RepeatingSchedule; }
            set { this._RepeatingSchedule = value; }
        }

        private bool _IsRepeating = false;

        /// <summary>
        /// Gets or sets a value indicating whether [is repeating].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [is repeating]; otherwise, <c>false</c>.
        /// </value>
        protected bool IsRepeating
        {
            get { return this._IsRepeating; }
            set { this._IsRepeating = value; }
        }

        /// <summary>
        /// check to see if the action should be kept.
        /// </summary>
        /// <returns></returns>
        public virtual bool KeepInActionsList()
        {
            return this.IsRepeating;
        }

        /// <summary>
        /// Determines whether [is due to run].
        /// </summary>
        /// <returns></returns>
        public virtual bool IsDueToRun()
        {

            if (DateTime.Now >= this._LastRun.Add(this.RepeatingSchedule))
            {
                return true;
            }

            return false;
        }

    }

}
