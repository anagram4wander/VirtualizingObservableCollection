using System;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.VirtualizingCollection.Actions
{
    public class ReclaimPagesWA : BaseRepeatableActionVirtualization
    {
        public ReclaimPagesWA(IReclaimableService provider, string sectionContext)
            : base(VirtualActionThreadModelEnum.Background, true, TimeSpan.FromMinutes(1))
        {
            this._WRProvider = new WeakReference(provider);
        }

        WeakReference _WRProvider = null;

        string _SectionContext = "";

        public override void DoAction()
        {
            this._LastRun = DateTime.Now;

            var reclaimer = this._WRProvider.Target as IReclaimableService;

            if (reclaimer != null)
            {
                reclaimer.RunClaim(this._SectionContext);
            }
        }

        public override bool KeepInActionsList()
        {
            bool ret = base.KeepInActionsList();

            if (!this._WRProvider.IsAlive) ret = false;

            return ret;
        }
    }

}
