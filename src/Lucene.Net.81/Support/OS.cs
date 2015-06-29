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

using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Provides platform infos.
    /// </summary>
    public class OS
    {
        private static bool isUnix;
        private static bool isWindows;

        static OS()
        {
            isUnix = false;
        }

        /// <summary>
        /// Whether we run under a Unix platform.
        /// </summary>
        public static bool IsUnix
        {
            get { return isUnix; }
        }

        /// <summary>
        /// Whether we run under a supported Windows platform.
        /// </summary>
        public static bool IsWindows
        {
            get { return isWindows; }
        }
    }
}