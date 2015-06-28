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
using System.Reflection;
using System.Linq;
using Lucene.Net._81.Interfaces;

namespace Lucene.Net.Support
{
    public class Deflater : IDeflater
    {
        private IDeflater _deflater;

        public const int BEST_COMPRESSION = 9;

        internal Deflater(IDeflater deflaterInstance)
        {
            _deflater = deflaterInstance;
        }

        public void SetLevel(int level)
        {
            _deflater.SetLevel(level);
        }

        public void SetInput(byte[] input, int offset, int count)
        {
            _deflater.SetInput(input, offset, count);
        }

        public void Finish()
        {
            _deflater.Finish();
        }

        public bool IsFinished
        {
            get { return _deflater.IsFinished; }
        }

        public int Deflate(byte[] output)
        {
            return _deflater.Deflate(output);
        }

        public int Deflate(byte[] output, int offset, int length)
        {
            return _deflater.Deflate(output, offset, length);
        }

        public void Reset()
        {
            _deflater.Reset();
        }

        public bool NeedsInput
        {
            get { return _deflater.NeedsInput; }
        }
    }
}