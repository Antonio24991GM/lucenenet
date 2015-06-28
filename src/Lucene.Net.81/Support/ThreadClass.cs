/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net._81.Threads;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Support class used to handle threads
    /// </summary>
    public class ThreadClass : IThreadRunnable
    {
        /// <summary>
        /// The instance of System.Threading.Thread
        /// </summary>
        private Task _threadField;
        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationSource;

        /// <summary>
        /// Initializes a new instance of the ThreadClass class
        /// </summary>
        public ThreadClass()
        {
            _cancellationSource = new CancellationTokenSource();
            _cancellationToken = _cancellationSource.Token;
            _threadField = new Task(Run, _cancellationToken);
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="name">The name of the thread</param>
        public ThreadClass(System.String name) : this()
        {
        }

        /// <summary>
        /// This method has no functionality unless the method is overridden
        /// </summary>
        public virtual void Run()
        {
        }

        /// <summary>
        /// Causes the operating system to change the state of the current thread instance to ThreadState.Running
        /// </summary>
        public virtual void Start()
        {
            _threadField.Start();
        }

        /// <summary>
        /// Interrupts a thread that is in the WaitSleepJoin thread state
        /// </summary>
        public virtual void Interrupt()
        {
            _cancellationSource.Cancel();
        }

        /// <summary>
        /// Gets the current thread instance
        /// </summary>
        public Task Instance
        {
            get
            {
                return _threadField;
            }
            set
            {
                _threadField = value;
            }
        }

        /// <summary>
        /// Gets a value indicating the execution status of the current thread
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return _threadField.IsCanceled;
            }
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates
        /// </summary>
        public void Join()
        {
            _threadField.Wait();
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="MiliSeconds">Time of wait in milliseconds</param>
        public void Join(long MiliSeconds)
        {
            _threadField.Wait(new System.TimeSpan(MiliSeconds * 10000));
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="MiliSeconds">Time of wait in milliseconds</param>
        /// <param name="NanoSeconds">Time of wait in nanoseconds</param>
        public void Join(long MiliSeconds, int NanoSeconds)
        {
            _threadField.Wait(new System.TimeSpan(MiliSeconds * 10000 + NanoSeconds * 100));
        }

        /// <summary>
        /// Resumes a thread that has been suspended
        /// </summary>
        public void Resume()
        {
            Monitor.PulseAll(_threadField);
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked,
        /// to begin the process of terminating the thread. Calling this method
        /// usually terminates the thread
        /// </summary>
        public void Abort()
        {
            _cancellationSource.Cancel();
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked,
        /// to begin the process of terminating the thread while also providing
        /// exception information about the thread termination.
        /// Calling this method usually terminates the thread.
        /// </summary>
        /// <param name="stateInfo">An object that contains application-specific information, such as state, which can be used by the thread being aborted</param>
        public void Abort(object stateInfo)
        {
            _cancellationSource.Cancel();
        }

        /// <summary>
        /// Suspends the thread, if the thread is already suspended it has no effect
        /// </summary>
        public void Suspend()
        {
            Monitor.Wait(_threadField);
        }

        /// <summary>
        /// Obtain a String that represents the current object
        /// </summary>
        /// <returns>A String that represents the current object</returns>
        public override System.String ToString()
        {
            return "Thread[" + _threadField.Id + "]";
        }

        [ThreadStatic]
        private static ThreadClass This = null;

        // named as the Java version
        public static ThreadClass CurrentThread()
        {
            return Current();
        }

        public static void Sleep(long ms)
        {
            // casting long ms to int ms could lose resolution, however unlikely
            // that someone would want to sleep for that long...
            Task.Delay((int)ms);
        }

        /// <summary>
        /// Gets the currently running thread
        /// </summary>
        /// <returns>The currently running thread</returns>
        public static ThreadClass Current()
        {
            if (This == null)
            {
                This = new ThreadClass();
            }
            return This;
        }

        public static bool operator ==(ThreadClass t1, object t2)
        {
            if (((object)t1) == null) return t2 == null;
            return t1.Equals(t2);
        }

        public static bool operator !=(ThreadClass t1, object t2)
        {
            return !(t1 == t2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is ThreadClass) return this._threadField.Equals(((ThreadClass)obj)._threadField);
            return false;
        }

        public override int GetHashCode()
        {
            return this._threadField.GetHashCode();
        }
    }
}