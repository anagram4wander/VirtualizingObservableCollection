using System;
using System.Collections.Generic;
using System.Linq;
using AlphaChiTech.VirtualizingCollection.Interfaces;

namespace AlphaChiTech.Virtualization.Pageing
{
    /// <summary>
    ///     PageReclainOnTouched is a Page Reclaimer implementation that releases pages based on when
    ///     they where last touched.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageReclaimOnTouched<T> : IPageReclaimer<T>
    {
        /// <summary>
        ///     Makes the page.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public ISourcePage<T> MakePage(int page, int size)
        {
            return new SourcePage<T> {Page = page, ItemsPerPage = size};
        }

        /// <summary>
        ///     Called when [page released].
        /// </summary>
        /// <param name="page">The page.</param>
        public void OnPageReleased(ISourcePage<T> page)
        {
        }

        /// <summary>
        ///     Called when [page touched].
        /// </summary>
        /// <param name="page">The page.</param>
        public void OnPageTouched(ISourcePage<T> page)
        {
            page.LastTouch = DateTime.Now;
        }

        /// <summary>
        ///     Reclaims the pages.
        /// </summary>
        /// <param name="pages">The pages.</param>
        /// <param name="pagesNeeded">The pages needed.</param>
        /// <param name="sectionContext">The section context.</param>
        /// <returns></returns>
        public IEnumerable<ISourcePage<T>> ReclaimPages(IEnumerable<ISourcePage<T>> pages, int pagesNeeded,
            string sectionContext)
        {
            var ret = new List<ISourcePage<T>>();

            var candiadates = (from p in pages where p.CanReclaimPage orderby p.LastTouch select p).Take(pagesNeeded);

            foreach (var c in candiadates) ret.Add(c);

            return ret;
        }
    }
}