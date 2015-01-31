using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class ReclaimPagesWA : BaseRepeatableActionVirtualization
    {
        public ReclaimPagesWA(IReclaimableService provider, string sectionContext)
            : base(VirtualActionThreadModelEnum.Background, true, TimeSpan.FromMinutes(1))
        {
            _WRProvider = new WeakReference(provider);
        }

        WeakReference _WRProvider = null;

        string _SectionContext = "";

        public override void DoAction()
        {
            _LastRun = DateTime.Now;

            var reclaimer = _WRProvider.Target as IReclaimableService;

            if (reclaimer != null)
            {
                reclaimer.RunClaim(_SectionContext);
            }
        }

        public override bool KeepInActionsList()
        {
            bool ret = base.KeepInActionsList();

            if (!_WRProvider.IsAlive) ret = false;

            return ret;
        }
    }

}
