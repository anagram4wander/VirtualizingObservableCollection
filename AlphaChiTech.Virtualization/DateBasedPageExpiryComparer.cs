using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    public class DateBasedPageExpiryComparer : IPageExpiryComparer
    {

        private static DateBasedPageExpiryComparer _Instance = new DateBasedPageExpiryComparer();

        public static DateBasedPageExpiryComparer DefaultInstance
        {
            get
            {
                return _Instance;
            }
        }

        public bool IsUpdateValid(object pageUpdateAt, object updateAt)
        {
            bool isStillValid = true;

            if (pageUpdateAt is DateTime && updateAt is DateTime)
            {
                if (((DateTime)pageUpdateAt) >= ((DateTime)updateAt))
                {
                    isStillValid = false;
                }
            }

            return isStillValid;
        }
    }
}
