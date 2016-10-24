using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface IVirtualizationAction
    {
        VirtualActionThreadModelEnum ThreadModel { get; }

        void DoAction();
    }
}
