using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DataGridAsyncDemoMVVM.filtersort
{
    internal static class ArgumentVerificationExtensions
    {
        public static void TestNotEmptyString(this string parameter, string parameterName)
        {
            if (string.IsNullOrEmpty(parameter))
                throw new ArgumentException($@"The parameter '{parameterName}' should not be empty string.",
                    parameterName);
        }

        public static void TestNotNull(this object parameter, string parameterName)
        {
            if (parameter == null)
                throw new ArgumentNullException(parameterName);
        }
    }

    /// <summary>
    ///     Contains extension methods for enumerating the parents of an element.
    /// </summary>
    public static class ParentOfTypeExtensions
    {
        /// <summary>
        ///     This recurses the visual tree for ancestors of a specific type.
        /// </summary>
        public static IEnumerable<T> GetAncestors<T>(this DependencyObject element) where T : class
        {
            return element.GetParents().OfType<T>();
        }

        /// <summary>
        ///     This recurses the visual tree for a parent of a specific type.
        /// </summary>
        public static T GetParent<T>(this DependencyObject element) where T : FrameworkElement
        {
            return element.ParentOfType<T>();
        }

        /// <summary>
        ///     Enumerates through element's parents in the visual tree.
        /// </summary>
        public static IEnumerable<DependencyObject> GetParents(this DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            while ((element = element.GetParent()) != null)
                yield return element;
        }

        /// <summary>
        ///     Searches up in the visual tree for parent element of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the parent that will be searched up in the visual object hierarchy.
        ///     The type should be <see cref="DependencyObject" />.
        /// </typeparam>
        /// <param name="element">
        ///     The target <see cref="DependencyObject" /> which visual parents will be traversed.
        /// </param>
        /// <returns>Visual parent of the specified type if there is any, otherwise null.</returns>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter")]
        public static T GetVisualParent<T>(this DependencyObject element) where T : DependencyObject
        {
            return element.ParentOfType<T>();
        }


        /// <summary>
        ///     Determines whether the element is an ancestor of the descendant.
        /// </summary>
        /// <returns>true if the visual object is an ancestor of descendant; otherwise, false.</returns>
        public static bool IsAncestorOf(this DependencyObject element, DependencyObject descendant)
        {
            element.TestNotNull("element");
            descendant.TestNotNull("descendant");

            return Equals(descendant, element) || descendant.GetParents().Contains(element);
        }

        /// <summary>
        ///     Gets the parent element from the visual tree by given type.
        /// </summary>
        public static T ParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            return element?.GetParents().OfType<T>().FirstOrDefault();
        }

        private static DependencyObject GetParent(this DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
                if (element is FrameworkElement frameworkElement)
                    parent = frameworkElement.Parent;
            return parent;
        }
    }
}