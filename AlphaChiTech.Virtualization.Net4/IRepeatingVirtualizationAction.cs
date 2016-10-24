using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface IRepeatingVirtualizationAction
    {
        bool KeepInActionsList();
        bool IsDueToRun();
    }
}
