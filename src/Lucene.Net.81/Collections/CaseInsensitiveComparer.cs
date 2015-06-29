using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net._81.Collections
{
    class CaseInsensitiveComparer : IComparer
    {
        private static CaseInsensitiveComparer defaultComparer = new CaseInsensitiveComparer();
        private static CaseInsensitiveComparer defaultInvariantComparer = new CaseInsensitiveComparer(true);

        private CultureInfo culture;

        // Public instance constructor
        public CaseInsensitiveComparer()
        {
            //LAMESPEC: This seems to be encoded while the object is created while Comparer does this at runtime.
            culture = CultureInfo.CurrentCulture;
        }

        private CaseInsensitiveComparer(bool invariant)
        {
            // leave culture == null
        }

        public CaseInsensitiveComparer(CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");

            if (culture.TwoLetterISOLanguageName != CultureInfo.InvariantCulture.TwoLetterISOLanguageName)
                this.culture = culture;
            // else leave culture == null
        }

        //
        // Public static properties
        //
        public static CaseInsensitiveComparer Default
        {
            get
            {
                return defaultComparer;
            }
        }

        public static CaseInsensitiveComparer DefaultInvariant
        {
            get
            {
                return defaultInvariantComparer;
            }
        }

        //
        // IComparer
        //
        public int Compare(object a, object b)
        {
            string sa = a as string;
            string sb = b as string;

            if ((sa != null) && (sb != null))
            {
                if (culture != null)
                    return culture.CompareInfo.Compare(sa, sb, CompareOptions.IgnoreCase);
                else
                    // FIXME: We should call directly into an invariant compare once available in string
                    return CultureInfo.InvariantCulture.CompareInfo.Compare(sa, sb, CompareOptions.IgnoreCase);
            }
            else
                return Comparer<object>.Default.Compare(a, b);
        }
    }
}
