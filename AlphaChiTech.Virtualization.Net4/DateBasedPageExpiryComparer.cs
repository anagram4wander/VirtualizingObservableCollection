using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlphaChiTech.Virtualization
{
    /// <summary>
    /// An implementation of a IPageExiryComparer that uses DateTime to see if the update should be applied
    /// </summary>
    public class DateBasedPageExpiryComparer : IPageExpiryComparer
    {

        private static DateBasedPageExpiryComparer _Instance = new DateBasedPageExpiryComparer();

        /// <summary>
        /// Gets the default instance.
        /// </summary>
        /// <value>
        /// The default instance.
        /// </value>
        public static DateBasedPageExpiryComparer DefaultInstance
        {
            get
            {
                return _Instance;
            }
        }

        /// <summary>
        /// Determines whether [is update valid] [the specified page based on the updateAt].
        /// </summary>
        /// <param name="pageUpdateAt">The page update at - null or a DateTime.</param>
        /// <param name="updateAt">The update at - null or a DateTime.</param>
        /// <returns></returns>
        public bool IsUpdateValid(object pageUpdateAt, object updateAt)
        {
            bool isStillValid = true;

            if (pageUpdateAt != null && updateAt != null && pageUpdateAt is DateTime && updateAt is DateTime)
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
