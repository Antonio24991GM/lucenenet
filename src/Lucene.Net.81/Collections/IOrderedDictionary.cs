using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net._81.Collections
{
    public interface IOrderedDictionary : IDictionary
    {
        new IDictionaryEnumerator GetEnumerator();
        void Insert(int idx, object key, object value);
        void RemoveAt(int idx);

        object this[int idx]
        {
            get; set;
        }
    }
}
