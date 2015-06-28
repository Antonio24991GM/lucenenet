using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net._81.Interfaces
{
    public interface IDeflater
    {
        void SetLevel(int level);

        void SetInput(byte[] input, int offset, int count);

        void Finish();

        bool IsFinished {get; }

        int Deflate(byte[] output);

        void Reset();

        bool NeedsInput { get; }

        int Deflate(byte[] output, int offset, int length);
    }
}
