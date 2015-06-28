using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net._81.Collections
{
    interface IHashCodeProvider
    {
        int GetHashCode(object obj);
    }
}
