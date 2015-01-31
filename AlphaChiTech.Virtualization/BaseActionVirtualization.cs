using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public abstract class BaseActionVirtualization : IVirtualizationAction
    {
        public VirtualActionThreadModelEnum ThreadModel { get; set; }

        public BaseActionVirtualization(VirtualActionThreadModelEnum threadModel)
        {
            this.ThreadModel = threadModel;
        }

        public abstract void DoAction();
    }
}
