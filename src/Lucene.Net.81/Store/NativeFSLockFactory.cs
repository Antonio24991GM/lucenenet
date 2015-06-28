using System;
using System.Collections.Concurrent;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Store
{
    using System.IO;
    using Windows.Storage;


    /*
                 * Licensed to the Apache Software Foundation (ASF) under one or more
                 * contributor license agreements.  See the NOTICE file distributed with
                 * this work for additional information regarding copyright ownership.
                 * The ASF licenses this file to You under the Apache License, Version 2.0
                 * (the "License"); you may not use this file except in compliance with
                 * the License.  You may obtain a copy of the License at
                 *
                 *     http://www.apache.org/licenses/LICENSE-2.0
                 *
                 * Unless required by applicable law or agreed to in writing, software
                 * distributed under the License is distributed on an "AS IS" BASIS,
                 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
                 * See the License for the specific language governing permissions and
                 * limitations under the License.
                 */

    /// <summary>
    /// <p>Implements <seealso cref="LockFactory"/> using native OS file
    /// locks.  Note that because this LockFactory relies on
    /// java.nio.* APIs for locking, any problems with those APIs
    /// will cause locking to fail.  Specifically, on certain NFS
    /// environments the java.nio.* locks will fail (the lock can
    /// incorrectly be double acquired) whereas {@link
    /// SimpleFSLockFactory} worked perfectly in those same
    /// environments.  For NFS based access to an index, it's
    /// recommended that you try <seealso cref="SimpleFSLockFactory"/>
    /// first and work around the one limitation that a lock file
    /// could be left when the JVM exits abnormally.</p>
    ///
    /// <p>The primary benefit of <seealso cref="NativeFSLockFactory"/> is
    /// that locks (not the lock file itsself) will be properly
    /// removed (by the OS) if the JVM has an abnormal exit.</p>
    ///
    /// <p>Note that, unlike <seealso cref="SimpleFSLockFactory"/>, the existence of
    /// leftover lock files in the filesystem is fine because the OS
    /// will free the locks held against these files even though the
    /// files still remain. Lucene will never actively remove the lock
    /// files, so although you see them, the index may not be locked.</p>
    ///
    /// <p>Special care needs to be taken if you change the locking
    /// implementation: First be certain that no writer is in fact
    /// writing to the index otherwise you can easily corrupt
    /// your index. Be sure to do the LockFactory change on all Lucene
    /// instances and clean up all leftover lock files before starting
    /// the new configuration for the first time. Different implementations
    /// can not work together!</p>
    ///
    /// <p>If you suspect that this or any other LockFactory is
    /// not working properly in your environment, you can easily
    /// test it by using <seealso cref="VerifyingLockFactory"/>, {@link
    /// LockVerifyServer} and <seealso cref="LockStressTest"/>.</p>
    /// </summary>
    /// <seealso cref= LockFactory </seealso>

    public class NativeFSLockFactory : FSLockFactory
    {
        /// <summary>
        /// Create a NativeFSLockFactory instance, with null (unset)
        /// lock directory. When you pass this factory to a <seealso cref="FSDirectory"/>
        /// subclass, the lock directory is automatically set to the
        /// directory itself. Be sure to create one instance for each directory
        /// your create!
        /// </summary>
        public NativeFSLockFactory()
            : this((string)null)
        {
        }

        /// <summary>
        /// Create a NativeFSLockFactory instance, storing lock
        /// files into the specified lockDirName:
        /// </summary>
        /// <param name="lockDirName"> where lock files are created. </param>
        public NativeFSLockFactory(string lockDirName)
        {
            var task = ApplicationData.Current.LocalFolder.CreateFolderAsync(lockDirName).AsTask();
            task.Wait();       
            LockDir = task.Result;
        }

        ///// <summary>
        ///// Create a NativeFSLockFactory instance, storing lock
        ///// files into the specified lockDir:
        ///// </summary>
        ///// <param name="lockDir"> where lock files are created. </param>
        //public NativeFSLockFactory(string lockDir)
        //{
            
        //}

        // LUCENENET NativeFSLocks in Java are infact singletons; this is how we mimick that to track instances and make sure
        // IW.Unlock and IW.IsLocked works correctly
        internal readonly ConcurrentDictionary<string, NativeFSLock> _locks = new ConcurrentDictionary<string, NativeFSLock>(); 

        public override Lock MakeLock(string lockName)
        {
            if (LockPrefix_Renamed != null)
            {
                lockName = LockPrefix_Renamed + "-" + lockName;
            }

            return _locks.GetOrAdd(lockName, (s) => new NativeFSLock(this, LockDir_Renamed, s));
        }

        public override void ClearLock(string lockName)
        {
            MakeLock(lockName).Release();
        }
    }

    internal class NativeFSLock : Lock
    {
        private Stream Channel;
        private readonly StorageFile Path;
        private readonly NativeFSLockFactory _creatingInstance;
        private readonly StorageFolder LockDir;

        public NativeFSLock(NativeFSLockFactory creatingInstance, StorageFolder lockDir, string lockFileName)
        {
            _creatingInstance = creatingInstance;
            this.LockDir = lockDir;
            var task = LockDir.CreateFileAsync(lockFileName).AsTask();
            task.Wait();
            Path = task.Result;
        }

        public override bool Obtain()
        {
            lock (this)
            {
                FailureReason = null;

                if (Channel != null)
                {
                    // Our instance is already locked:
                    return false;
                }
                var success = false;
                try
                {
                    var task = Path.OpenAsync(FileAccessMode.ReadWrite).AsTask();
                    task.Wait();                    
                    Channel = task.Result.AsStream();
                    //I cant look this stream is posible an error on ejecution
                    //Channel. Lock(0, Channel.Length);
                    success = true;
                }
                catch (IOException e)
                {
                    FailureReason = e;
                    IOUtils.CloseWhileHandlingException(Channel);
                    Channel = null;
                }
                // LUCENENET: UnauthorizedAccessException does not derive from IOException like in java
                catch (UnauthorizedAccessException e)
                {
                    // On Windows, we can get intermittent "Access
                    // Denied" here.  So, we treat this as failure to
                    // acquire the lock, but, store the reason in case
                    // there is in fact a real error case.
                    FailureReason = e;
                    IOUtils.CloseWhileHandlingException(Channel);
                    Channel = null;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(Channel);
                        Channel = null;
                    }
                }

                return Channel != null;
            }
        }

        public override void Release()
        {
            lock (this)
            {
                if (Channel != null)
                {
                    try
                    {
                        Channel.Dispose();

                        NativeFSLock _;
                        _creatingInstance._locks.TryRemove(Path.Path, out _);
                    }
                    finally
                    {
                        IOUtils.CloseWhileHandlingException(Channel);
                        Channel = null;
                    }

                    bool tmpBool;
                    if (Path!=null)
                    {
                        Path.DeleteAsync().AsTask().Wait();
                        tmpBool = true;
                    }
                    //else if (System.IO.Directory.Exists(Path.FullName))
                    //{
                    //    System.IO.Directory.Delete(Path.FullName);
                    //    tmpBool = true;
                    //}
                    else
                    {
                        tmpBool = false;
                    }
                    if (!tmpBool)
                    {
                        throw new LockReleaseFailedException("failed to delete " + Path);
                    }
                }
            }
        }

        public override bool Locked
        {
            get
            {
                lock (this)
                {
                    // The test for is isLocked is not directly possible with native file locks:

                    // First a shortcut, if a lock reference in this instance is available
                    if (Channel != null)
                    {
                        return true;
                    }

                    // Look if lock file is present; if not, there can definitely be no lock!
                    bool tmpBool;
                    if (Path!=null)
                        tmpBool = true;
                    else
                        tmpBool = false;
                    if (!tmpBool)
                        return false;

                    // Try to obtain and release (if was locked) the lock
                    try
                    {
                        bool obtained = Obtain();
                        if (obtained)
                        {
                            Release();
                        }
                        return !obtained;
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "NativeFSLock@" + Path;
        }
    }
}