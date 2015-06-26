using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Util
{
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

    /*
     * Some of this code came from the excellent Unicode
     * conversion examples from:
     *
     *   http://www.unicode.org/Public/PROGRAMS/CVTUTF
     *
     * Full Copyright for that code follows:
    */

    /*
     * Copyright 2001-2004 Unicode, Inc.
     *
     * Disclaimer
     *
     * this source code is provided as is by Unicode, Inc. No claims are
     * made as to fitness for any particular purpose. No warranties of any
     * kind are expressed or implied. The recipient agrees to determine
     * applicability of information provided. If this file has been
     * purchased on magnetic or optical media from Unicode, Inc., the
     * sole remedy for any claim will be exchange of defective media
     * within 90 days of receipt.
     *
     * Limitations on Rights to Redistribute this Code
     *
     * Unicode, Inc. hereby grants the right to freely use the information
     * supplied in this file in the creation of products supporting the
     * Unicode Standard, and to make copies of this file in any form
     * for internal or external distribution as long as this notice
     * remains attached.
     */

    /*
     * Additional code came from the IBM ICU library.
     *
     *  http://www.icu-project.org
     *
     * Full Copyright for that code follows.
     */

    /*
     * Copyright (C) 1999-2010, International Business Machines
     * Corporation and others.  All Rights Reserved.
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, and/or sell copies of the
     * Software, and to permit persons to whom the Software is furnished to do so,
     * provided that the above copyright notice(s) and this permission notice appear
     * in all copies of the Software and that both the above copyright notice(s) and
     * this permission notice appear in supporting documentation.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS.
     * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN this NOTICE BE
     * LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR
     * ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
     * IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
     * OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF this SOFTWARE.
     *
     * Except as contained in this notice, the name of a copyright holder shall not
     * be used in advertising or otherwise to promote the sale, use or other
     * dealings in this Software without prior written authorization of the
     * copyright holder.
     */

    /// <summary>
    /// Class to encode java's UTF16 char[] into UTF8 byte[]
    /// without always allocating a new byte[] as
    /// String.getBytes(StandardCharsets.UTF_8) does.
    ///
    /// @lucene.internal
    /// </summary>

    public static class UnicodeUtil
    {
        /// <summary>
        /// A binary term consisting of a number of 0xff bytes, likely to be bigger than other terms
        ///  (e.g. collation keys) one would normally encounter, and definitely bigger than any UTF-8 terms.
        ///  <p>
        ///  WARNING: this is not a valid UTF8 Term
        ///
        /// </summary>
        public static readonly BytesRef BIG_TERM = new BytesRef(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }); // TODO this is unrelated here find a better place for it

        public const int UNI_SUR_HIGH_START = 0xD800;
        public const int UNI_SUR_HIGH_END = 0xDBFF;
        public const int UNI_SUR_LOW_START = 0xDC00;
        public const int UNI_SUR_LOW_END = 0xDFFF;
        public const int UNI_REPLACEMENT_CHAR = 0xFFFD;
        private const int MIN_SUPPLEMENTARY_CODE_POINT = 0x10000;

        private const long UNI_MAX_BMP = 0x0000FFFF;

        private const long HALF_SHIFT = 10;
        private const long HALF_MASK = 0x3FFL;

        private const int SURROGATE_OFFSET = MIN_SUPPLEMENTARY_CODE_POINT - (UNI_SUR_HIGH_START << (int) HALF_SHIFT) - UNI_SUR_LOW_START;

        /// <summary>
        /// Encode characters from a char[] source, starting at
        ///  offset for length chars. After encoding, result.offset will always be 0.
        /// </summary>
        // TODO: broken if incoming result.offset != 0
        public static void UTF16toUTF8(CharsRef source, int offset, int length, BytesRef result)
        {
            int upto = 0;
            int i = offset;
            int end = offset + length;
            var @out = result.Bytes;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (@out.Length < maxLen)
            {
                @out = result.Bytes = new byte[maxLen];
            }
            result.Offset = 0;

            while (i < end)
            {
                int code = (int)source.CharAt(i++);

                if (code < 0x80)
                {
                    @out[upto++] = (byte)code;
                }
                else if (code < 0x800)
                {
                    @out[upto++] = (byte)(0xC0 | (code >> 6));
                    @out[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    @out[upto++] = (byte)(0xE0 | (code >> 12));
                    @out[upto++] = (byte)(0x80 | ((code >> 6) & 0x3F));
                    @out[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && i < end)
                    {
                        var utf32 = (int)source.CharAt(i);
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            @out[upto++] = (byte)(0xF0 | (utf32 >> 18));
                            @out[upto++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            @out[upto++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            @out[upto++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    @out[upto++] = unchecked((byte)0xEF);
                    @out[upto++] = unchecked((byte)0xBF);
                    @out[upto++] = unchecked((byte)0xBD);
                }
            }
            //assert matches(source, offset, length, out, upto);
            result.Length = upto;
        }

        /// <summary>
        /// Encode characters from this String, starting at offset
        ///  for length characters. After encoding, result.offset will always be 0.
        /// </summary>
        // TODO: broken if incoming result.offset != 0
        public static void UTF16toUTF8(char[] s, int offset, int length, BytesRef result)
        {
            int end = offset + length;

            var @out = result.Bytes;
            result.Offset = 0;
            // Pre-allocate for worst case 4-for-1
            int maxLen = length * 4;
            if (@out.Length < maxLen)
            {
                @out = result.Bytes = new byte[maxLen];
            }

            int upto = 0;
            for (int i = offset; i < end; i++)
            {
                var code = (int)s[i];
                if (code < 0x80)
                {
                    @out[upto++] = (byte)code;
                }
                else if (code < 0x800)
                {
                    @out[upto++] = (byte)(0xC0 | (code >> 6));
                    @out[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else if (code < 0xD800 || code > 0xDFFF)
                {
                    @out[upto++] = (byte)(0xE0 | (code >> 12));
                    @out[upto++] = (byte)(0x80 | ((code >> 6) & 0x3F));
                    @out[upto++] = (byte)(0x80 | (code & 0x3F));
                }
                else
                {
                    // surrogate pair
                    // confirm valid high surrogate
                    if (code < 0xDC00 && (i < end - 1))
                    {
                        int utf32 = (int)s[i + 1];
                        // confirm valid low surrogate and write pair
                        if (utf32 >= 0xDC00 && utf32 <= 0xDFFF)
                        {
                            utf32 = (code << 10) + utf32 + SURROGATE_OFFSET;
                            i++;
                            @out[upto++] = (byte)(0xF0 | (utf32 >> 18));
                            @out[upto++] = (byte)(0x80 | ((utf32 >> 12) & 0x3F));
                            @out[upto++] = (byte)(0x80 | ((utf32 >> 6) & 0x3F));
                            @out[upto++] = (byte)(0x80 | (utf32 & 0x3F));
                            continue;
                        }
                    }
                    // replace unpaired surrogate or out-of-order low surrogate
                    // with substitution character
                    @out[upto++] = unchecked((byte)0xEF);
                    @out[upto++] = unchecked((byte)0xBF);
                    @out[upto++] = unchecked((byte)0xBD);
                }
            }
            //assert matches(s, offset, length, out, upto);
            result.Length = upto;
        }

        // Only called from assert
        /*
        private static boolean matches(char[] source, int offset, int length, byte[] result, int upto) {
          try {
            String s1 = new String(source, offset, length);
            String s2 = new String(result, 0, upto, StandardCharsets.UTF_8);
            if (!s1.equals(s2)) {
              //System.out.println("DIFF: s1 len=" + s1.length());
              //for(int i=0;i<s1.length();i++)
              //  System.out.println("    " + i + ": " + (int) s1.charAt(i));
              //System.out.println("s2 len=" + s2.length());
              //for(int i=0;i<s2.length();i++)
              //  System.out.println("    " + i + ": " + (int) s2.charAt(i));

              // If the input string was invalid, then the
              // difference is OK
              if (!validUTF16String(s1))
                return true;

              return false;
            }
            return s1.equals(s2);
          } catch (UnsupportedEncodingException uee) {
            return false;
          }
        }

        // Only called from assert
        private static boolean matches(String source, int offset, int length, byte[] result, int upto) {
          try {
            String s1 = source.substring(offset, offset+length);
            String s2 = new String(result, 0, upto, StandardCharsets.UTF_8);
            if (!s1.equals(s2)) {
              // Allow a difference if s1 is not valid UTF-16

              //System.out.println("DIFF: s1 len=" + s1.length());
              //for(int i=0;i<s1.length();i++)
              //  System.out.println("    " + i + ": " + (int) s1.charAt(i));
              //System.out.println("  s2 len=" + s2.length());
              //for(int i=0;i<s2.length();i++)
              //  System.out.println("    " + i + ": " + (int) s2.charAt(i));

              // If the input string was invalid, then the
              // difference is OK
              if (!validUTF16String(s1))
                return true;

              return false;
            }
            return s1.equals(s2);
          } catch (UnsupportedEncodingException uee) {
            return false;
          }
        }
        */

        public static bool ValidUTF16String(char[] s)
        {
            int size = s.Length;
            for (int i = 0; i < size; i++)
            {
                char ch = s[i];
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s[i];
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                        // Unmatched high surrogate
                        {
                            return false;
                        }
                    }
                    else
                    // Unmatched high surrogate
                    {
                        return false;
                    }
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                // Unmatched low surrogate
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidUTF16String(char[] s, int size)
        {
            for (int i = 0; i < size; i++)
            {
                char ch = s[i];
                if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                {
                    if (i < size - 1)
                    {
                        i++;
                        char nextCH = s[i];
                        if (nextCH >= UNI_SUR_LOW_START && nextCH <= UNI_SUR_LOW_END)
                        {
                            // Valid surrogate pair
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                // Unmatched low surrogate
                {
                    return false;
                }
            }

            return true;
        }

        // Borrowed from Python's 3.1.2 sources,
        // Objects/unicodeobject.c, and modified (see commented
        // out section, and the -1s) to disallow the reserved for
        // future (RFC 3629) 5/6 byte sequence characters, and
        // invalid 0xFE and 0xFF bytes.

        /* Map UTF-8 encoded prefix byte to sequence length.  -1 (0xFF)
         * means illegal prefix.  see RFC 2279 for details */
        internal static readonly int[] Utf8CodeLength;

        static UnicodeUtil()
        {
            int v = int.MinValue;
            Utf8CodeLength = new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4 };
        }

        /// <summary>
        /// Returns the number of code points in this UTF8 sequence.
        ///
        /// <p>this method assumes valid UTF8 input. this method
        /// <strong>does not perform</strong> full UTF8 validation, it will check only the
        /// first byte of each codepoint (for multi-byte sequences any bytes after
        /// the head are skipped).
        /// </summary>
        /// <exception cref="IllegalArgumentException"> If invalid codepoint header byte occurs or the
        ///    content is prematurely truncated. </exception>
        public static int CodePointCount(BytesRef utf8)
        {
            int pos = utf8.Offset;
            int limit = pos + utf8.Length;
            var bytes = utf8.Bytes;

            int codePointCount = 0;
            for (; pos < limit; codePointCount++)
            {
                int v = bytes[pos] & 0xFF;
                if (v <   /* 0xxx xxxx */ 0x80) { pos += 1; continue; }
                if (v >=  /* 110x xxxx */ 0xc0)
                {
                    if (v < /* 111x xxxx */ 0xe0) { pos += 2; continue; }
                    if (v < /* 1111 xxxx */ 0xf0) { pos += 3; continue; }
                    if (v < /* 1111 1xxx */ 0xf8) { pos += 4; continue; }
                    // fallthrough, consider 5 and 6 byte sequences invalid. 
                }

                // Anything not covered above is invalid UTF8.
                throw new ArgumentException();
            }

            // Check if we didn't go over the limit on the last character.
            if (pos > limit) throw new ArgumentException();

            return codePointCount;
        }

        /// <summary>
        /// <p>this method assumes valid UTF8 input. this method
        /// <strong>does not perform</strong> full UTF8 validation, it will check only the
        /// first byte of each codepoint (for multi-byte sequences any bytes after
        /// the head are skipped).
        /// </summary>
        /// <exception cref="IllegalArgumentException"> If invalid codepoint header byte occurs or the
        ///    content is prematurely truncated. </exception>
        public static void UTF8toUTF32(BytesRef utf8, IntsRef utf32)
        {
            // TODO: broken if incoming result.offset != 0
            // pre-alloc for worst case
            // TODO: ints cannot be null, should be an assert
            if (utf32.Ints == null || utf32.Ints.Length < utf8.Length)
            {
                utf32.Ints = new int[utf8.Length];
            }
            int utf32Count = 0;
            int utf8Upto = utf8.Offset;
            int[] ints = utf32.Ints;
            var bytes = utf8.Bytes;
            int utf8Limit = utf8.Offset + utf8.Length;
            while (utf8Upto < utf8Limit)
            {
                int numBytes = Utf8CodeLength[bytes[utf8Upto] & 0xFF];
                int v = 0;
                switch (numBytes)
                {
                    case 1:
                        ints[utf32Count++] = bytes[utf8Upto++];
                        continue;
                    case 2:
                        // 5 useful bits
                        v = bytes[utf8Upto++] & 31;
                        break;

                    case 3:
                        // 4 useful bits
                        v = bytes[utf8Upto++] & 15;
                        break;

                    case 4:
                        // 3 useful bits
                        v = bytes[utf8Upto++] & 7;
                        break;

                    default:
                        throw new System.ArgumentException("invalid utf8");
                }

                // TODO: this may read past utf8's limit.
                int limit = utf8Upto + numBytes - 1;
                while (utf8Upto < limit)
                {
                    v = v << 6 | bytes[utf8Upto++] & 63;
                }
                ints[utf32Count++] = v;
            }

            utf32.Offset = 0;
            utf32.Length = utf32Count;
        }

        /// <summary>
        /// Shift value for lead surrogate to form a supplementary character. </summary>
        private const int LEAD_SURROGATE_SHIFT_ = 10;

        /// <summary>
        /// Mask to retrieve the significant value from a trail surrogate. </summary>
        private const int TRAIL_SURROGATE_MASK_ = 0x3FF;

        /// <summary>
        /// Trail surrogate minimum value </summary>
        private const int TRAIL_SURROGATE_MIN_VALUE = 0xDC00;

        /// <summary>
        /// Lead surrogate minimum value </summary>
        private const int LEAD_SURROGATE_MIN_VALUE = 0xD800;

        /// <summary>
        /// The minimum value for Supplementary code points </summary>
        private const int SUPPLEMENTARY_MIN_VALUE = 0x10000;

        /// <summary>
        /// Value that all lead surrogate starts with </summary>
        private static readonly int LEAD_SURROGATE_OFFSET_ = LEAD_SURROGATE_MIN_VALUE - (SUPPLEMENTARY_MIN_VALUE >> LEAD_SURROGATE_SHIFT_);

        /// <summary>
        /// Cover JDK 1.5 API. Create a String from an array of codePoints.
        /// </summary>
        /// <param name="codePoints"> The code array </param>
        /// <param name="offset"> The start of the text in the code point array </param>
        /// <param name="count"> The number of code points </param>
        /// <returns> a String representing the code points between offset and count </returns>
        /// <exception cref="IllegalArgumentException"> If an invalid code point is encountered </exception>
        /// <exception cref="IndexOutOfBoundsException"> If the offset or count are out of bounds. </exception>
        public static string NewString(int[] codePoints, int offset, int count)
        {
            var chars = ToCharArray(codePoints, offset, count);
            return new string(chars);
        }

        /// <summary>
        /// Generates char array that represents the provided input code points
        /// </summary>
        /// <param name="codePoints"> The code array </param>
        /// <param name="offset"> The start of the text in the code point array </param>
        /// <param name="count"> The number of code points </param>
        /// <returns> a char array representing the code points between offset and count </returns>
        public static char[] ToCharArray(int[] codePoints, int offset, int count)
        {
            if (count < 0)
            {
                throw new System.ArgumentException();
            }
            char[] chars = new char[count];
            int w = 0;
            for (int r = offset, e = offset + count; r < e; ++r)
            {
                int cp = codePoints[r];
                if (cp < 0 || cp > 0x10ffff)
                {
                    throw new System.ArgumentException();
                }
                while (true)
                {
                    try
                    {
                        if (cp < 0x010000)
                        {
                            chars[w] = (char)cp;
                            w++;
                        }
                        else
                        {
                            chars[w] = (char)(LEAD_SURROGATE_OFFSET_ + (cp >> LEAD_SURROGATE_SHIFT_));
                            chars[w + 1] = (char)(TRAIL_SURROGATE_MIN_VALUE + (cp & TRAIL_SURROGATE_MASK_));
                            w += 2;
                        }
                        break;
                    }
                    catch (System.IndexOutOfRangeException)
                    {
                        int newlen = (int)(Math.Ceiling((double)codePoints.Length * (w + 2) / (r - offset + 1)));
                        char[] temp = new char[newlen];
                        Array.Copy(chars, 0, temp, 0, w);
                        chars = temp;
                    }
                }
            }

            var result = new char[w];
            Array.Copy(chars, result, w);
            return result;
        }

        // for debugging
        public static string ToHexString(string s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (i > 0)
                {
                    sb.Append(' ');
                }
                if (ch < 128)
                {
                    sb.Append(ch);
                }
                else
                {
                    if (ch >= UNI_SUR_HIGH_START && ch <= UNI_SUR_HIGH_END)
                    {
                        sb.Append("H:");
                    }
                    else if (ch >= UNI_SUR_LOW_START && ch <= UNI_SUR_LOW_END)
                    {
                        sb.Append("L:");
                    }
                    else if (ch > UNI_SUR_LOW_END)
                    {
                        if (ch == 0xffff)
                        {
                            sb.Append("F:");
                        }
                        else
                        {
                            sb.Append("E:");
                        }
                    }

                    sb.Append("0x" + ((short)ch).ToString("X"));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Interprets the given byte array as UTF-8 and converts to UTF-16. The <seealso cref="CharsRef"/> will be extended if
        /// it doesn't provide enough space to hold the worst case of each byte becoming a UTF-16 codepoint.
        /// <p>
        /// NOTE: Full characters are read, even if this reads past the length passed (and
        /// can result in an ArrayOutOfBoundsException if invalid UTF-8 is passed).
        /// Explicit checks for valid UTF-8 are not performed.
        /// </summary>
        // TODO: broken if chars.offset != 0
        public static void UTF8toUTF16(byte[] utf8, int offset, int length, CharsRef chars)
        {
            int out_offset = chars.Offset = 0;
            char[] @out = chars.Chars = ArrayUtil.Grow(chars.Chars, length);
            int limit = offset + length;
            while (offset < limit)
            {
                int b = ((sbyte)utf8[offset++]) & 0xff;
                if (b < 0xc0)
                {
                    Debug.Assert(b < 0x80);
                    @out[out_offset++] = (char)b;
                }
                else if (b < 0xe0)
                {
                    @out[out_offset++] = (char)(((b & 0x1f) << 6) + (utf8[offset++] & 0x3f));
                }
                else if (b < 0xf0)
                {
                    @out[out_offset++] = (char)(((b & 0xf) << 12) + ((utf8[offset] & 0x3f) << 6) + (utf8[offset + 1] & 0x3f));
                    offset += 2;
                }
                else
                {
                    Debug.Assert(b < 0xf8, "b = 0x" + b.ToString("x"));
                    int ch = ((b & 0x7) << 18) + ((utf8[offset] & 0x3f) << 12) + ((utf8[offset + 1] & 0x3f) << 6) + (utf8[offset + 2] & 0x3f);
                    offset += 3;
                    if (ch < UNI_MAX_BMP)
                    {
                        @out[out_offset++] = (char)ch;
                    }
                    else
                    {
                        int chHalf = ch - 0x0010000;
                        @out[out_offset++] = (char)((chHalf >> 10) + 0xD800);
                        @out[out_offset++] = (char)((chHalf & HALF_MASK) + 0xDC00);
                    }
                }
            }
            chars.Length = out_offset - chars.Offset;
        }

        /// <summary>
        /// Utility method for <seealso cref="#UTF8toUTF16(byte[], int, int, CharsRef)"/> </summary>
        /// <seealso cref= #UTF8toUTF16(byte[], int, int, CharsRef) </seealso>
        public static void UTF8toUTF16(BytesRef bytesRef, CharsRef chars)
        {
            UTF8toUTF16(bytesRef.Bytes, bytesRef.Offset, bytesRef.Length, chars);
        }
    }
}