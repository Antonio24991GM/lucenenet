using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lucene.Net.Index
{
    using System.Threading.Tasks;
    /*
* Licensed to the Apache Software Foundation (ASF) under one or more
* contributor license agreements. See the NOTICE file distributed with
* this work for additional information regarding copyright ownership.
* The ASF licenses this file to You under the Apache License, Version 2.0
* (the "License"); you may not use this file except in compliance with
* the License. You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

    using ThreadState = Lucene.Net.Index.DocumentsWriterPerThreadPool.ThreadState; //javadoc

    /// <summary>
    /// A <seealso cref="DocumentsWriterPerThreadPool"/> implementation that tries to assign an
    /// indexing thread to the same <seealso cref="ThreadState"/> each time the thread tries to
    /// obtain a <seealso cref="ThreadState"/>. Once a new <seealso cref="ThreadState"/> is created it is
    /// associated with the creating thread. Subsequently, if the threads associated
    /// <seealso cref="ThreadState"/> is not in use it will be associated with the requesting
    /// thread. Otherwise, if the <seealso cref="ThreadState"/> is used by another thread
    /// <seealso cref="ThreadAffinityDocumentsWriterThreadPool"/> tries to find the currently
    /// minimal contended <seealso cref="ThreadState"/>.
    /// </summary>
    public class ThreadAffinityDocumentsWriterThreadPool : DocumentsWriterPerThreadPool
    {
        private IDictionary<Task, ThreadState> ThreadBindings = new ConcurrentDictionary<Task, ThreadState>();

        /// <summary>
        /// Creates a new <seealso cref="ThreadAffinityDocumentsWriterThreadPool"/> with a given maximum of <seealso cref="ThreadState"/>s.
        /// </summary>
        public ThreadAffinityDocumentsWriterThreadPool(int maxNumPerThreads)
            : base(maxNumPerThreads)
        {
            Debug.Assert(MaxThreadStates >= 1);
        }

        public override object Clone()
        {
            ThreadAffinityDocumentsWriterThreadPool clone = (ThreadAffinityDocumentsWriterThreadPool)base.Clone();
            clone.ThreadBindings = new ConcurrentDictionary<Task, ThreadState>();
            return clone;
        }
    }
}