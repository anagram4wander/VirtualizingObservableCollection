using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public interface IPageExpiryComparer
    {
        bool IsUpdateValid(object pageUpdateAt, object updateAt);
    }
}
