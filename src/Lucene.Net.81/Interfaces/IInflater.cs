using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net._81.Interfaces
{
    public interface IInflater
    {
        void SetInput(byte[] buffer);

        bool IsFinished { get; }

        int Inflate(byte[] buffer);

        void Reset();

        void SetInput(byte[] buffer, int index, int count);

        int Inflate(byte[] buffer, int offset, int count);
    }
}
