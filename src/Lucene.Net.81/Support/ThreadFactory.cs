using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public abstract class ThreadFactory
    {
        public abstract Task NewThread(IThreadRunnable r);
    }
}