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

using Lucene.Net._81.Interfaces;
using System;
using System.Linq;

namespace Lucene.Net.Support
{
    public class Inflater
    {
        private IInflater _inflater;

        internal Inflater(IInflater inflater)
        {
            _inflater = inflater;
        }

        public void SetInput(byte[] buffer)
        {
            _inflater.SetInput(buffer);
        }

        public void SetInput(byte[] buffer, int index, int count)
        {
            _inflater.SetInput(buffer, index, count);
        }

        public bool IsFinished
        {
            get { return _inflater.IsFinished; }
        }

        public int Inflate(byte[] buffer)
        {
            return _inflater.Inflate(buffer);
        }

        public int Inflate(byte[] buffer, int offset, int count)
        {
            return _inflater.Inflate(buffer, offset, count);
        }

        public void Reset()
        {
            _inflater.Reset();
        }
    }
}