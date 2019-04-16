/*
   BigInteger Class Version 1.03
   
   Copyright (c) 2002 Chew Keong TAN All rights reserved.
   
   Permission is hereby granted, free of charge, to any person obtaining a copy of
   this software and associated documentation files (the "Software"), to deal in
   the Software without restriction, including without limitation the rights to
   use, copy, modify, merge, publish, distribute, and/or sell copies of the
   Software, and to permit persons to whom the Software is furnished to do so,
   provided that the above copyright notice(s) and this permission notice appear in
   all copies of the Software and that both the above copyright notice(s) and this
   permission notice appear in supporting documentation.
   
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
   FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS. IN NO EVENT
   SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY
   CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF
   CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION
   WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#if !DISABLE_CRYPTOGRAPHY
using System;
using System.Security.Cryptography;

namespace MLAPI.Security
{
    /// <summary>
    /// This is a BigInteger class. Holds integer that is more than 64-bit (long).
    /// </summary>
    /// <remarks>
    /// This class contains overloaded arithmetic operators(+, -, *, /, %), bitwise operators(&amp;, |) and other
    /// operations that can be done with normal integer. It also contains implementation of various prime test.
    /// This class also contains methods dealing with cryptography such as generating prime number, generating
    /// a coprime number.
    /// </remarks>

    internal class BigInteger
    {
        // maximum length of the BigInteger in uint (4 bytes)
        // change this to suit the required level of precision.
        private const int MaxLength = 70;

        // primes smaller than 2000 to test the generated prime number
        public static readonly int[] PrimesBelow2000 =
        {
            2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
            73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
            179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
            283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409,
            419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541,
            547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659,
            661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797, 809,
            811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941,
            947, 953, 967, 971, 977, 983, 991, 997, 1009, 1013, 1019, 1021, 1031, 1033, 1039, 1049, 1051, 1061, 1063,
            1069,
            1087, 1091, 1093, 1097, 1103, 1109, 1117, 1123, 1129, 1151, 1153, 1163, 1171, 1181, 1187, 1193, 1201, 1213,
            1217, 1223,
            1229, 1231, 1237, 1249, 1259, 1277, 1279, 1283, 1289, 1291, 1297, 1301, 1303, 1307, 1319, 1321, 1327, 1361,
            1367, 1373,
            1381, 1399, 1409, 1423, 1427, 1429, 1433, 1439, 1447, 1451, 1453, 1459, 1471, 1481, 1483, 1487, 1489, 1493,
            1499, 1511,
            1523, 1531, 1543, 1549, 1553, 1559, 1567, 1571, 1579, 1583, 1597, 1601, 1607, 1609, 1613, 1619, 1621, 1627,
            1637, 1657,
            1663, 1667, 1669, 1693, 1697, 1699, 1709, 1721, 1723, 1733, 1741, 1747, 1753, 1759, 1777, 1783, 1787, 1789,
            1801, 1811,
            1823, 1831, 1847, 1861, 1867, 1871, 1873, 1877, 1879, 1889, 1901, 1907, 1913, 1931, 1933, 1949, 1951, 1973,
            1979, 1987,
            1993, 1997, 1999
        };

        private uint[] _data = null; // stores bytes from the Big Integer
        public int DataLength; // number of actual chars used

        /// <summary>
        /// Default constructor for BigInteger of value 0
        /// </summary>
        public BigInteger()
        {
            _data = new uint[MaxLength];
            DataLength = 1;
        }

        /// <summary>
        /// Constructor (Default value provided by long)
        /// </summary>
        /// <param name="value">Turn the long value into BigInteger type</param>
        public BigInteger(long value)
        {
            _data = new uint[MaxLength];
            long tempVal = value;

            // copy bytes from long to BigInteger without any assumption of
            // the length of the long datatype
            DataLength = 0;
            while (value != 0 && DataLength < MaxLength)
            {
                _data[DataLength] = (uint)(value & 0xFFFFFFFF);
                value >>= 32;
                DataLength++;
            }

            if (tempVal > 0) // overflow check for +ve value
            {
                if (value != 0 || (_data[MaxLength - 1] & 0x80000000) != 0)
                    throw (new ArithmeticException("Positive overflow in constructor."));
            }
            else if (tempVal < 0) // underflow check for -ve value
            {
                if (value != -1 || (_data[DataLength - 1] & 0x80000000) == 0)
                    throw (new ArithmeticException("Negative underflow in constructor."));
            }

            if (DataLength == 0)
                DataLength = 1;
        }

        /// <summary>
        /// Constructor (Default value provided by ulong)
        /// </summary>
        /// <param name="value">Turn the unsigned long value into BigInteger type</param>
        public BigInteger(ulong value)
        {
            _data = new uint[MaxLength];

            // copy bytes from ulong to BigInteger without any assumption of
            // the length of the ulong datatype
            DataLength = 0;
            while (value != 0 && DataLength < MaxLength)
            {
                _data[DataLength] = (uint)(value & 0xFFFFFFFF);
                value >>= 32;
                DataLength++;
            }

            if (value != 0 || (_data[MaxLength - 1] & 0x80000000) != 0)
                throw (new ArithmeticException("Positive overflow in constructor."));

            if (DataLength == 0)
                DataLength = 1;
        }

        /// <summary>
        /// Constructor (Default value provided by BigInteger)
        /// </summary>
        /// <param name="bi"></param>
        public BigInteger(BigInteger bi)
        {
            _data = new uint[MaxLength];

            DataLength = bi.DataLength;

            for (int i = 0; i < DataLength; i++)
                _data[i] = bi._data[i];
        }

        /// <summary>
        /// Constructor (Default value provided by a string of digits of the specified base)
        /// </summary>
        /// <example>
        /// To initialize "a" with the default value of 1234 in base 10:
        ///      BigInteger a = new BigInteger("1234", 10)
        /// To initialize "a" with the default value of -0x1D4F in base 16:
        ///      BigInteger a = new BigInteger("-1D4F", 16)
        /// </example>
        ///
        /// <param name="value">String value in the format of [sign][magnitude]</param>
        /// <param name="radix">The base of value</param>
        public BigInteger(string value, int radix = 10)
        {
            BigInteger multiplier = new BigInteger(1);
            BigInteger result = new BigInteger();
            value = (value.ToUpper()).Trim();
            int limit = 0;

            if (value[0] == '-')
                limit = 1;

            for (int i = value.Length - 1; i >= limit; i--)
            {
                int posVal = value[i];

                if (posVal >= '0' && posVal <= '9')
                    posVal -= '0';
                else if (posVal >= 'A' && posVal <= 'Z')
                    posVal = (posVal - 'A') + 10;
                else
                    posVal = 9999999; // arbitrary large

                if (posVal >= radix)
                    throw (new ArithmeticException("Invalid string in constructor."));
                else
                {
                    if (value[0] == '-')
                        posVal = -posVal;

                    result = result + (multiplier * posVal);

                    if ((i - 1) >= limit)
                        multiplier = multiplier * radix;
                }
            }

            if (value[0] == '-') // negative values
            {
                if ((result._data[MaxLength - 1] & 0x80000000) == 0)
                    throw (new ArithmeticException("Negative underflow in constructor."));
            }
            else // positive values
            {
                if ((result._data[MaxLength - 1] & 0x80000000) != 0)
                    throw (new ArithmeticException("Positive overflow in constructor."));
            }

            _data = new uint[MaxLength];
            for (int i = 0; i < result.DataLength; i++)
                _data[i] = result._data[i];

            DataLength = result.DataLength;
        }

        //***********************************************************************
        // Constructor (Default value provided by an array of bytes)
        //
        // The lowest index of the input byte array (i.e [0]) should contain the
        // most significant byte of the number, and the highest index should
        // contain the least significant byte.
        //
        // E.g.
        // To initialize "a" with the default value of 0x1D4F in base 16
        //      byte[] temp = { 0x1D, 0x4F };
        //      BigInteger a = new BigInteger(temp)
        //
        // Note that this method of initialization does not allow the
        // sign to be specified.
        //
        //***********************************************************************
        /*public BigInteger(byte[] inData)
        {
            dataLength = inData.Length >> 2;

            int leftOver = inData.Length & 0x3;
            if (leftOver != 0)         // length not multiples of 4
                dataLength++;

            if (dataLength > maxLength)
                throw (new ArithmeticException("Byte overflow in constructor."));

            data = new uint[maxLength];

            for (int i = inData.Length - 1, j = 0; i >= 3; i -= 4, j++)
            {
                data[j] = ((uint)(inData[i - 3]) << 24) + ((uint)(inData[i - 2]) << 16) +
                          ((uint)(inData[i - 1] << 8))  + ((uint)(inData[i]));
            }

            if (leftOver == 1)
                data[dataLength - 1] = (uint)inData[0];
            else if (leftOver == 2)
                data[dataLength - 1] = (uint)((inData[0] << 8) + inData[1]);
            else if (leftOver == 3)
                data[dataLength - 1] = (uint)((inData[0] << 16) + (inData[1] << 8) + inData[2]);

            while (dataLength > 1 && data[dataLength - 1] == 0)
                dataLength--;
        }*/

        public BigInteger(byte[] inData) : this((System.Collections.Generic.IList<byte>)inData)
        {
        }

        /// <summary>
        /// Constructor (Default value provided by an array of bytes of the specified length.)
        /// </summary>
        /// <param name="inData">A list of byte values</param>
        /// <param name="length">Default -1</param>
        /// <param name="offset">Default 0</param>
        public BigInteger(System.Collections.Generic.IList<byte> inData, int length = -1, int offset = 0)
        {
            var inLen = length == -1 ? inData.Count - offset : length;

            DataLength = inLen >> 2;

            int leftOver = inLen & 0x3;
            if (leftOver != 0) // length not multiples of 4
                DataLength++;

            if (DataLength > MaxLength || inLen > inData.Count - offset)
                throw (new ArithmeticException("Byte overflow in constructor."));

            _data = new uint[MaxLength];

            for (int i = inLen - 1, j = 0; i >= 3; i -= 4, j++)
            {
                _data[j] = (uint)((inData[offset + i - 3] << 24) + (inData[offset + i - 2] << 16) +
                                  (inData[offset + i - 1] << 8) + inData[offset + i]);
            }

            if (leftOver == 1)
                _data[DataLength - 1] = inData[offset + 0];
            else if (leftOver == 2)
                _data[DataLength - 1] = (uint)((inData[offset + 0] << 8) + inData[offset + 1]);
            else if (leftOver == 3)
                _data[DataLength - 1] =
                    (uint)((inData[offset + 0] << 16) + (inData[offset + 1] << 8) + inData[offset + 2]);

            if (DataLength == 0)
                DataLength = 1;

            while (DataLength > 1 && _data[DataLength - 1] == 0)
                DataLength--;
        }

        /// <summary>
        ///  Constructor (Default value provided by an array of unsigned integers)
        /// </summary>
        /// <param name="inData">Array of unsigned integer</param>
        public BigInteger(uint[] inData)
        {
            DataLength = inData.Length;

            if (DataLength > MaxLength)
                throw (new ArithmeticException("Byte overflow in constructor."));

            _data = new uint[MaxLength];

            for (int i = DataLength - 1, j = 0; i >= 0; i--, j++)
                _data[j] = inData[i];

            while (DataLength > 1 && _data[DataLength - 1] == 0)
                DataLength--;
        }

        /// <summary>
        /// Cast a type long value to type BigInteger value
        /// </summary>
        /// <param name="value">A long value</param>
        public static implicit operator BigInteger(long value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        /// Cast a type ulong value to type BigInteger value
        /// </summary>
        /// <param name="value">An unsigned long value</param>
        public static implicit operator BigInteger(ulong value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        /// Cast a type int value to type BigInteger value
        /// </summary>
        /// <param name="value">An int value</param>
        public static implicit operator BigInteger(int value)
        {
            return (new BigInteger(value));
        }

        /// <summary>
        /// Cast a type uint value to type BigInteger value
        /// </summary>
        /// <param name="value">An unsigned int value</param>
        public static implicit operator BigInteger(uint value)
        {
            return (new BigInteger((ulong)value));
        }

        /// <summary>
        /// Overloading of addition operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Result of the addition of 2 BigIntegers</returns>
        public static BigInteger operator +(BigInteger bi1, BigInteger bi2)
        {
            BigInteger result = new BigInteger()
            {
                DataLength = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength
            };

            long carry = 0;
            for (int i = 0; i < result.DataLength; i++)
            {
                long sum = bi1._data[i] + (long)bi2._data[i] + carry;
                carry = sum >> 32;
                result._data[i] = (uint)(sum & 0xFFFFFFFF);
            }

            if (carry != 0 && result.DataLength < MaxLength)
            {
                result._data[result.DataLength] = (uint)(carry);
                result.DataLength++;
            }

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            // overflow check
            int lastPos = MaxLength - 1;
            if ((bi1._data[lastPos] & 0x80000000) == (bi2._data[lastPos] & 0x80000000) &&
                (result._data[lastPos] & 0x80000000) != (bi1._data[lastPos] & 0x80000000))
            {
                throw (new ArithmeticException());
            }

            return result;
        }

        /// <summary>
        /// Overloading of the unary ++ operator, which increments BigInteger by 1
        /// </summary>
        /// <param name="bi1">A BigInteger</param>
        /// <returns>Incremented BigInteger</returns>
        public static BigInteger operator ++(BigInteger bi1)
        {
            BigInteger result = new BigInteger(bi1);

            long val, carry = 1;
            int index = 0;

            while (carry != 0 && index < MaxLength)
            {
                val = result._data[index];
                val++;

                result._data[index] = (uint)(val & 0xFFFFFFFF);
                carry = val >> 32;

                index++;
            }

            if (index > result.DataLength)
                result.DataLength = index;
            else
            {
                while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                    result.DataLength--;
            }

            // overflow check
            int lastPos = MaxLength - 1;

            // overflow if initial value was +ve but ++ caused a sign
            // change to negative.

            if ((bi1._data[lastPos] & 0x80000000) == 0 &&
                (result._data[lastPos] & 0x80000000) != (bi1._data[lastPos] & 0x80000000))
            {
                throw (new ArithmeticException("Overflow in ++."));
            }

            return result;
        }

        /// <summary>
        /// Overloading of subtraction operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Result of the subtraction of 2 BigIntegers</returns>
        public static BigInteger operator -(BigInteger bi1, BigInteger bi2)
        {
            BigInteger result = new BigInteger()
            {
                DataLength = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength
            };

            long carryIn = 0;
            for (int i = 0; i < result.DataLength; i++)
            {
                long diff;

                diff = bi1._data[i] - (long)bi2._data[i] - carryIn;
                result._data[i] = (uint)(diff & 0xFFFFFFFF);

                if (diff < 0)
                    carryIn = 1;
                else
                    carryIn = 0;
            }

            // roll over to negative
            if (carryIn != 0)
            {
                for (int i = result.DataLength; i < MaxLength; i++)
                    result._data[i] = 0xFFFFFFFF;
                result.DataLength = MaxLength;
            }

            // fixed in v1.03 to give correct datalength for a - (-b)
            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            // overflow check

            int lastPos = MaxLength - 1;
            if ((bi1._data[lastPos] & 0x80000000) != (bi2._data[lastPos] & 0x80000000) &&
                (result._data[lastPos] & 0x80000000) != (bi1._data[lastPos] & 0x80000000))
            {
                throw (new ArithmeticException());
            }

            return result;
        }

        /// <summary>
        /// Overloading of the unary -- operator, decrements BigInteger by 1
        /// </summary>
        /// <param name="bi1">A BigInteger</param>
        /// <returns>Decremented BigInteger</returns>
        public static BigInteger operator --(BigInteger bi1)
        {
            BigInteger result = new BigInteger(bi1);

            long val;
            bool carryIn = true;
            int index = 0;

            while (carryIn && index < MaxLength)
            {
                val = result._data[index];
                val--;

                result._data[index] = (uint)(val & 0xFFFFFFFF);

                if (val >= 0)
                    carryIn = false;

                index++;
            }

            if (index > result.DataLength)
                result.DataLength = index;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            // overflow check
            int lastPos = MaxLength - 1;

            // overflow if initial value was -ve but -- caused a sign
            // change to positive.

            if ((bi1._data[lastPos] & 0x80000000) != 0 &&
                (result._data[lastPos] & 0x80000000) != (bi1._data[lastPos] & 0x80000000))
            {
                throw (new ArithmeticException("Underflow in --."));
            }

            return result;
        }

        /// <summary>
        /// Overloading of multiplication operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Result of the multiplication of 2 BigIntegers</returns>
        public static BigInteger operator *(BigInteger bi1, BigInteger bi2)
        {
            int lastPos = MaxLength - 1;
            bool bi1Neg = false, bi2Neg = false;

            // take the absolute value of the inputs
            try
            {
                if ((bi1._data[lastPos] & 0x80000000) != 0) // bi1 negative
                {
                    bi1Neg = true;
                    bi1 = -bi1;
                }

                if ((bi2._data[lastPos] & 0x80000000) != 0) // bi2 negative
                {
                    bi2Neg = true;
                    bi2 = -bi2;
                }
            }
            catch (Exception)
            {
            }

            BigInteger result = new BigInteger();

            // multiply the absolute values
            try
            {
                for (int i = 0; i < bi1.DataLength; i++)
                {
                    if (bi1._data[i] == 0) continue;

                    ulong mcarry = 0;
                    for (int j = 0, k = i; j < bi2.DataLength; j++, k++)
                    {
                        // k = i + j
                        ulong val = (bi1._data[i] * (ulong)bi2._data[j]) +
                                     result._data[k] + mcarry;

                        result._data[k] = (uint)(val & 0xFFFFFFFF);
                        mcarry = (val >> 32);
                    }

                    if (mcarry != 0)
                        result._data[i + bi2.DataLength] = (uint)mcarry;
                }
            }
            catch (Exception)
            {
                throw (new ArithmeticException("Multiplication overflow."));
            }

            result.DataLength = bi1.DataLength + bi2.DataLength;
            if (result.DataLength > MaxLength)
                result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            // overflow check (result is -ve)
            if ((result._data[lastPos] & 0x80000000) != 0)
            {
                if (bi1Neg != bi2Neg && result._data[lastPos] == 0x80000000) // different sign
                {
                    // handle the special case where multiplication produces
                    // a max negative number in 2's complement.

                    if (result.DataLength == 1)
                        return result;
                    else
                    {
                        bool isMaxNeg = true;
                        for (int i = 0; i < result.DataLength - 1 && isMaxNeg; i++)
                        {
                            if (result._data[i] != 0)
                                isMaxNeg = false;
                        }

                        if (isMaxNeg)
                            return result;
                    }
                }

                throw (new ArithmeticException("Multiplication overflow."));
            }

            // if input has different signs, then result is -ve
            if (bi1Neg != bi2Neg)
                return -result;

            return result;
        }

        /// <summary>
        /// Overloading of the unary &lt;&lt; operator (left shift)
        /// </summary>
        /// <remarks>
        /// Shifting by a negative number is an undefined behaviour (UB).
        /// </remarks>
        /// <param name="bi1">A BigInteger</param>
        /// <param name="shiftVal">Left shift by shiftVal bit</param>
        /// <returns>Left-shifted BigInteger</returns>
        public static BigInteger operator <<(BigInteger bi1, int shiftVal)
        {
            BigInteger result = new BigInteger(bi1);
            result.DataLength = ShiftLeft(result._data, shiftVal);

            return result;
        }

        // least significant bits at lower part of buffer
        private static int ShiftLeft(uint[] buffer, int shiftVal)
        {
            int shiftAmount = 32;
            int bufLen = buffer.Length;

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            for (int count = shiftVal; count > 0;)
            {
                if (count < shiftAmount)
                    shiftAmount = count;

                ulong carry = 0;
                for (int i = 0; i < bufLen; i++)
                {
                    ulong val = ((ulong)buffer[i]) << shiftAmount;
                    val |= carry;

                    buffer[i] = (uint)(val & 0xFFFFFFFF);
                    carry = val >> 32;
                }

                if (carry != 0)
                {
                    if (bufLen + 1 <= buffer.Length)
                    {
                        buffer[bufLen] = (uint)carry;
                        bufLen++;
                    }
                }

                count -= shiftAmount;
            }

            return bufLen;
        }

        /// <summary>
        /// Overloading of the unary &gt;&gt; operator (right shift)
        /// </summary>
        /// <remarks>
        /// Shifting by a negative number is an undefined behaviour (UB).
        /// </remarks>
        /// <param name="bi1">A BigInteger</param>
        /// <param name="shiftVal">Right shift by shiftVal bit</param>
        /// <returns>Right-shifted BigInteger</returns>
        public static BigInteger operator >>(BigInteger bi1, int shiftVal)
        {
            BigInteger result = new BigInteger(bi1);
            result.DataLength = ShiftRight(result._data, shiftVal);

            if ((bi1._data[MaxLength - 1] & 0x80000000) != 0) // negative
            {
                for (int i = MaxLength - 1; i >= result.DataLength; i--)
                    result._data[i] = 0xFFFFFFFF;

                uint mask = 0x80000000;
                for (int i = 0; i < 32; i++)
                {
                    if ((result._data[result.DataLength - 1] & mask) != 0)
                        break;

                    result._data[result.DataLength - 1] |= mask;
                    mask >>= 1;
                }

                result.DataLength = MaxLength;
            }

            return result;
        }

        private static int ShiftRight(uint[] buffer, int shiftVal)
        {
            int shiftAmount = 32;
            int invShift = 0;
            int bufLen = buffer.Length;

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            for (int count = shiftVal; count > 0;)
            {
                if (count < shiftAmount)
                {
                    shiftAmount = count;
                    invShift = 32 - shiftAmount;
                }

                ulong carry = 0;
                for (int i = bufLen - 1; i >= 0; i--)
                {
                    ulong val = ((ulong)buffer[i]) >> shiftAmount;
                    val |= carry;

                    carry = (((ulong)buffer[i]) << invShift) & 0xFFFFFFFF;
                    buffer[i] = (uint)(val);
                }

                count -= shiftAmount;
            }

            while (bufLen > 1 && buffer[bufLen - 1] == 0)
                bufLen--;

            return bufLen;
        }

        /// <summary>
        /// Overloading of the bit-wise NOT operator (1's complement)
        /// </summary>
        /// <param name="bi1">A BigInteger</param>
        /// <returns>Complemented BigInteger</returns>
        public static BigInteger operator ~(BigInteger bi1)
        {
            BigInteger result = new BigInteger(bi1);

            for (int i = 0; i < MaxLength; i++)
                result._data[i] = ~(bi1._data[i]);

            result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            return result;
        }

        /// <summary>
        /// Overloading of the NEGATE operator (2's complement)
        /// </summary>
        /// <param name="bi1">A BigInteger</param>
        /// <returns>Negated BigInteger or default BigInteger value if bi1 is 0</returns>
        public static BigInteger operator -(BigInteger bi1)
        {
            // handle neg of zero separately since it'll cause an overflow
            // if we proceed.

            if (bi1.DataLength == 1 && bi1._data[0] == 0)
                return (new BigInteger());

            BigInteger result = new BigInteger(bi1);

            // 1's complement
            for (int i = 0; i < MaxLength; i++)
                result._data[i] = ~(bi1._data[i]);

            // add one to result of 1's complement
            long val, carry = 1;
            int index = 0;

            while (carry != 0 && index < MaxLength)
            {
                val = result._data[index];
                val++;

                result._data[index] = (uint)(val & 0xFFFFFFFF);
                carry = val >> 32;

                index++;
            }

            if ((bi1._data[MaxLength - 1] & 0x80000000) == (result._data[MaxLength - 1] & 0x80000000))
                throw (new ArithmeticException("Overflow in negation.\n"));

            result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;
            return result;
        }

        /// <summary>
        /// Overloading of equality operator, allows comparing 2 BigIntegers with == operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator ==(BigInteger bi1, BigInteger bi2)
        {
            return object.ReferenceEquals(bi1, bi2) || bi1.Equals(bi2);
        }

        /// <summary>
        /// Overloading of not equal operator, allows comparing 2 BigIntegers with != operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator !=(BigInteger bi1, BigInteger bi2)
        {
            return !object.ReferenceEquals(bi1, bi2) && !(bi1.Equals(bi2));
        }

        /// <summary>
        /// Overriding of Equals method, allows comparing BigInteger with an arbitary object
        /// </summary>
        /// <param name="o">Input object, to be casted into BigInteger type for comparison</param>
        /// <returns>Boolean result of the comparison</returns>
        public override bool Equals(object o)
        {
            if (object.ReferenceEquals(null, o))
            {
                return false;
            }

            BigInteger bi = (BigInteger) o;



            if (this.DataLength != bi.DataLength)
                return false;

            for (int i = 0; i < this.DataLength; i++)
            {
                if (this._data[i] != bi._data[i])
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Overloading of greater than operator, allows comparing 2 BigIntegers with &gt; operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator >(BigInteger bi1, BigInteger bi2)
        {
            int pos = MaxLength - 1;

            // bi1 is negative, bi2 is positive
            if ((bi1._data[pos] & 0x80000000) != 0 && (bi2._data[pos] & 0x80000000) == 0)
                return false;

            // bi1 is positive, bi2 is negative
            else if ((bi1._data[pos] & 0x80000000) == 0 && (bi2._data[pos] & 0x80000000) != 0)
                return true;

            // same sign
            int len = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength;
            for (pos = len - 1; pos >= 0 && bi1._data[pos] == bi2._data[pos]; pos--) ;

            if (pos >= 0)
            {
                if (bi1._data[pos] > bi2._data[pos])
                    return true;
                return false;
            }

            return false;
        }

        /// <summary>
        /// Overloading of greater than operator, allows comparing 2 BigIntegers with &lt; operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator <(BigInteger bi1, BigInteger bi2)
        {
            int pos = MaxLength - 1;

            // bi1 is negative, bi2 is positive
            if ((bi1._data[pos] & 0x80000000) != 0 && (bi2._data[pos] & 0x80000000) == 0)
                return true;

            // bi1 is positive, bi2 is negative
            else if ((bi1._data[pos] & 0x80000000) == 0 && (bi2._data[pos] & 0x80000000) != 0)
                return false;

            // same sign
            int len = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength;
            for (pos = len - 1; pos >= 0 && bi1._data[pos] == bi2._data[pos]; pos--) ;

            if (pos >= 0)
            {
                if (bi1._data[pos] < bi2._data[pos])
                    return true;
                return false;
            }

            return false;
        }

        /// <summary>
        /// Overloading of greater than or equal to operator, allows comparing 2 BigIntegers with &gt;= operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator >=(BigInteger bi1, BigInteger bi2)
        {
            return (bi1 == bi2 || bi1 > bi2);
        }

        /// <summary>
        /// Overloading of less than or equal to operator, allows comparing 2 BigIntegers with &lt;= operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>Boolean result of the comparison</returns>
        public static bool operator <=(BigInteger bi1, BigInteger bi2)
        {
            return (bi1 == bi2 || bi1 < bi2);
        }

        //***********************************************************************
        // Private function that supports the division of two numbers with
        // a divisor that has more than 1 digit.
        //
        // Algorithm taken from [1]
        //***********************************************************************
        private static void MultiByteDivide(BigInteger bi1, BigInteger bi2,
            BigInteger outQuotient, BigInteger outRemainder)
        {
            uint[] result = new uint[MaxLength];

            int remainderLen = bi1.DataLength + 1;
            uint[] remainder = new uint[remainderLen];

            uint mask = 0x80000000;
            uint val = bi2._data[bi2.DataLength - 1];
            int shift = 0, resultPos = 0;

            while (mask != 0 && (val & mask) == 0)
            {
                shift++;
                mask >>= 1;
            }

            for (int i = 0; i < bi1.DataLength; i++)
                remainder[i] = bi1._data[i];
            ShiftLeft(remainder, shift);
            bi2 = bi2 << shift;

            int j = remainderLen - bi2.DataLength;
            int pos = remainderLen - 1;

            ulong firstDivisorByte = bi2._data[bi2.DataLength - 1];
            ulong secondDivisorByte = bi2._data[bi2.DataLength - 2];

            int divisorLen = bi2.DataLength + 1;
            uint[] dividendPart = new uint[divisorLen];

            while (j > 0)
            {
                ulong dividend = ((ulong)remainder[pos] << 32) + remainder[pos - 1];

                ulong qHat = dividend / firstDivisorByte;
                ulong rHat = dividend % firstDivisorByte;

                bool done = false;
                while (!done)
                {
                    done = true;

                    if (qHat == 0x100000000 ||
                        (qHat * secondDivisorByte) > ((rHat << 32) + remainder[pos - 2]))
                    {
                        qHat--;
                        rHat += firstDivisorByte;

                        if (rHat < 0x100000000)
                            done = false;
                    }
                }

                for (int h = 0; h < divisorLen; h++)
                    dividendPart[h] = remainder[pos - h];

                BigInteger kk = new BigInteger(dividendPart);
                BigInteger ss = bi2 * (long)qHat;

                while (ss > kk)
                {
                    qHat--;
                    ss -= bi2;
                }

                BigInteger yy = kk - ss;

                for (int h = 0; h < divisorLen; h++)
                    remainder[pos - h] = yy._data[bi2.DataLength - h];

                result[resultPos++] = (uint)qHat;

                pos--;
                j--;
            }

            outQuotient.DataLength = resultPos;
            int y = 0;
            for (int x = outQuotient.DataLength - 1; x >= 0; x--, y++)
                outQuotient._data[y] = result[x];
            for (; y < MaxLength; y++)
                outQuotient._data[y] = 0;

            while (outQuotient.DataLength > 1 && outQuotient._data[outQuotient.DataLength - 1] == 0)
                outQuotient.DataLength--;

            if (outQuotient.DataLength == 0)
                outQuotient.DataLength = 1;

            outRemainder.DataLength = ShiftRight(remainder, shift);

            for (y = 0; y < outRemainder.DataLength; y++)
                outRemainder._data[y] = remainder[y];
            for (; y < MaxLength; y++)
                outRemainder._data[y] = 0;
        }

        //***********************************************************************
        // Private function that supports the division of two numbers with
        // a divisor that has only 1 digit.
        //***********************************************************************
        private static void SingleByteDivide(BigInteger bi1, BigInteger bi2,
            BigInteger outQuotient, BigInteger outRemainder)
        {
            uint[] result = new uint[MaxLength];
            int resultPos = 0;

            // copy dividend to reminder
            for (int i = 0; i < MaxLength; i++)
                outRemainder._data[i] = bi1._data[i];
            outRemainder.DataLength = bi1.DataLength;

            while (outRemainder.DataLength > 1 && outRemainder._data[outRemainder.DataLength - 1] == 0)
                outRemainder.DataLength--;

            ulong divisor = bi2._data[0];
            int pos = outRemainder.DataLength - 1;
            ulong dividend = outRemainder._data[pos];

            if (dividend >= divisor)
            {
                ulong quotient = dividend / divisor;
                result[resultPos++] = (uint)quotient;

                outRemainder._data[pos] = (uint)(dividend % divisor);
            }

            pos--;

            while (pos >= 0)
            {
                dividend = ((ulong)outRemainder._data[pos + 1] << 32) + outRemainder._data[pos];
                ulong quotient = dividend / divisor;
                result[resultPos++] = (uint)quotient;

                outRemainder._data[pos + 1] = 0;
                outRemainder._data[pos--] = (uint)(dividend % divisor);
            }

            outQuotient.DataLength = resultPos;
            int j = 0;
            for (int i = outQuotient.DataLength - 1; i >= 0; i--, j++)
                outQuotient._data[j] = result[i];
            for (; j < MaxLength; j++)
                outQuotient._data[j] = 0;

            while (outQuotient.DataLength > 1 && outQuotient._data[outQuotient.DataLength - 1] == 0)
                outQuotient.DataLength--;

            if (outQuotient.DataLength == 0)
                outQuotient.DataLength = 1;

            while (outRemainder.DataLength > 1 && outRemainder._data[outRemainder.DataLength - 1] == 0)
                outRemainder.DataLength--;
        }

        /// <summary>
        /// Overloading of division operator
        /// </summary>
        /// <remarks>The dataLength of the divisor's absolute value must be less than maxLength</remarks>
        /// <param name="bi1">Dividend</param>
        /// <param name="bi2">Divisor</param>
        /// <returns>Quotient of the division</returns>
        public static BigInteger operator /(BigInteger bi1, BigInteger bi2)
        {
            BigInteger quotient = new BigInteger();
            BigInteger remainder = new BigInteger();

            int lastPos = MaxLength - 1;
            bool divisorNeg = false, dividendNeg = false;

            if ((bi1._data[lastPos] & 0x80000000) != 0) // bi1 negative
            {
                bi1 = -bi1;
                dividendNeg = true;
            }

            if ((bi2._data[lastPos] & 0x80000000) != 0) // bi2 negative
            {
                bi2 = -bi2;
                divisorNeg = true;
            }

            if (bi1 < bi2)
            {
                return quotient;
            }
            else
            {
                if (bi2.DataLength == 1)
                    SingleByteDivide(bi1, bi2, quotient, remainder);
                else
                    MultiByteDivide(bi1, bi2, quotient, remainder);

                if (dividendNeg != divisorNeg)
                    return -quotient;

                return quotient;
            }
        }

        /// <summary>
        /// Overloading of modulus operator
        /// </summary>
        /// <remarks>The dataLength of the divisor's absolute value must be less than maxLength</remarks>
        /// <param name="bi1">Dividend</param>
        /// <param name="bi2">Divisor</param>
        /// <returns>Remainder of the division</returns>
        public static BigInteger operator %(BigInteger bi1, BigInteger bi2)
        {
            BigInteger quotient = new BigInteger();
            BigInteger remainder = new BigInteger(bi1);

            int lastPos = MaxLength - 1;
            bool dividendNeg = false;

            if ((bi1._data[lastPos] & 0x80000000) != 0) // bi1 negative
            {
                bi1 = -bi1;
                dividendNeg = true;
            }

            if ((bi2._data[lastPos] & 0x80000000) != 0) // bi2 negative
                bi2 = -bi2;

            if (bi1 < bi2)
            {
                return remainder;
            }
            else
            {
                if (bi2.DataLength == 1)
                    SingleByteDivide(bi1, bi2, quotient, remainder);
                else
                    MultiByteDivide(bi1, bi2, quotient, remainder);

                if (dividendNeg)
                    return -remainder;

                return remainder;
            }
        }

        /// <summary>
        /// Overloading of bitwise AND operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>BigInteger result after performing &amp; operation</returns>
        public static BigInteger operator &(BigInteger bi1, BigInteger bi2)
        {
            BigInteger result = new BigInteger();

            int len = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength;

            for (int i = 0; i < len; i++)
            {
                uint sum = bi1._data[i] & bi2._data[i];
                result._data[i] = sum;
            }

            result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            return result;
        }

        /// <summary>
        /// Overloading of bitwise OR operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>BigInteger result after performing | operation</returns>
        public static BigInteger operator |(BigInteger bi1, BigInteger bi2)
        {
            BigInteger result = new BigInteger();

            int len = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength;

            for (int i = 0; i < len; i++)
            {
                uint sum = bi1._data[i] | bi2._data[i];
                result._data[i] = sum;
            }

            result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            return result;
        }

        /// <summary>
        /// Overloading of bitwise XOR operator
        /// </summary>
        /// <param name="bi1">First BigInteger</param>
        /// <param name="bi2">Second BigInteger</param>
        /// <returns>BigInteger result after performing ^ operation</returns>
        public static BigInteger operator ^(BigInteger bi1, BigInteger bi2)
        {
            BigInteger result = new BigInteger();

            int len = (bi1.DataLength > bi2.DataLength) ? bi1.DataLength : bi2.DataLength;

            for (int i = 0; i < len; i++)
            {
                uint sum = bi1._data[i] ^ bi2._data[i];
                result._data[i] = sum;
            }

            result.DataLength = MaxLength;

            while (result.DataLength > 1 && result._data[result.DataLength - 1] == 0)
                result.DataLength--;

            return result;
        }

        /// <summary>
        /// Compare this and a BigInteger and find the maximum one
        /// </summary>
        /// <param name="bi">BigInteger to be compared with this</param>
        /// <returns>The bigger value of this and bi</returns>
        public BigInteger Max(BigInteger bi)
        {
            if (this > bi)
                return (new BigInteger(this));
            else
                return (new BigInteger(bi));
        }

        /// <summary>
        /// Compare this and a BigInteger and find the minimum one
        /// </summary>
        /// <param name="bi">BigInteger to be compared with this</param>
        /// <returns>The smaller value of this and bi</returns>
        public BigInteger Min(BigInteger bi)
        {
            if (this < bi)
                return (new BigInteger(this));
            else
                return (new BigInteger(bi));
        }

        /// <summary>
        /// Returns the absolute value
        /// </summary>
        /// <returns>Absolute value of this</returns>
        public BigInteger Abs()
        {
            if ((this._data[MaxLength - 1] & 0x80000000) != 0)
                return (-this);
            else
                return (new BigInteger(this));
        }

        /// <summary>
        /// Returns a string representing the BigInteger in base 10
        /// </summary>
        /// <returns>string representation of the BigInteger</returns>
        public override string ToString()
        {
            return ToString(10);
        }

        /// <summary>
        /// Returns a string representing the BigInteger in [sign][magnitude] format in the specified radix
        /// </summary>
        /// <example>If the value of BigInteger is -255 in base 10, then ToString(16) returns "-FF"</example>
        /// <param name="radix">Base</param>
        /// <returns>string representation of the BigInteger in [sign][magnitude] format</returns>
        public string ToString(int radix)
        {
            if (radix < 2 || radix > 36)
                throw (new ArgumentException("Radix must be >= 2 and <= 36"));

            string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string result = "";

            BigInteger a = this;

            bool negative = false;
            if ((a._data[MaxLength - 1] & 0x80000000) != 0)
            {
                negative = true;
                try
                {
                    a = -a;
                }
                catch (Exception)
                {
                }
            }

            BigInteger quotient = new BigInteger();
            BigInteger remainder = new BigInteger();
            BigInteger biRadix = new BigInteger(radix);

            if (a.DataLength == 1 && a._data[0] == 0)
                result = "0";
            else
            {
                while (a.DataLength > 1 || (a.DataLength == 1 && a._data[0] != 0))
                {
                    SingleByteDivide(a, biRadix, quotient, remainder);

                    if (remainder._data[0] < 10)
                        result = remainder._data[0] + result;
                    else
                        result = charSet[(int)remainder._data[0] - 10] + result;

                    a = quotient;
                }

                if (negative)
                    result = "-" + result;
            }

            return result;
        }

        /// <summary>
        /// Returns a hex string showing the contains of the BigInteger
        /// </summary>
        /// <example>
        /// 1) If the value of BigInteger is 255 in base 10, then ToHexString() returns "FF"
        /// 2) If the value of BigInteger is -255 in base 10, thenToHexString() returns ".....FFFFFFFFFF01", which is the 2's complement representation of -255.
        /// </example>
        /// <returns></returns>
        public string ToHexString()
        {
            string result = _data[DataLength - 1].ToString("X");

            for (int i = DataLength - 2; i >= 0; i--)
            {
                result += _data[i].ToString("X8");
            }

            return result;
        }

        /// <summary>
        /// Modulo Exponentiation
        /// </summary>
        /// <param name="exp">Exponential</param>
        /// <param name="n">Modulo</param>
        /// <returns>BigInteger result of raising this to the power of exp and then modulo n </returns>
        public BigInteger ModPow(BigInteger exp, BigInteger n)
        {
            if ((exp._data[MaxLength - 1] & 0x80000000) != 0)
                throw (new ArithmeticException("Positive exponents only."));

            BigInteger resultNum = 1;
            BigInteger tempNum;
            bool thisNegative = false;

            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative this
            {
                tempNum = -this % n;
                thisNegative = true;
            }
            else
                tempNum = this % n; // ensures (tempNum * tempNum) < b^(2k)

            if ((n._data[MaxLength - 1] & 0x80000000) != 0) // negative n
                n = -n;

            // calculate constant = b^(2k) / m
            BigInteger constant = new BigInteger();

            int i = n.DataLength << 1;
            constant._data[i] = 0x00000001;
            constant.DataLength = i + 1;

            constant = constant / n;
            int totalBits = exp.BitCount();
            int count = 0;

            // perform squaring and multiply exponentiation
            for (int pos = 0; pos < exp.DataLength; pos++)
            {
                uint mask = 0x01;

                for (int index = 0; index < 32; index++)
                {
                    if ((exp._data[pos] & mask) != 0)
                        resultNum = BarrettReduction(resultNum * tempNum, n, constant);

                    mask <<= 1;

                    tempNum = BarrettReduction(tempNum * tempNum, n, constant);

                    if (tempNum.DataLength == 1 && tempNum._data[0] == 1)
                    {
                        if (thisNegative && (exp._data[0] & 0x1) != 0) //odd exp
                            return -resultNum;
                        return resultNum;
                    }

                    count++;
                    if (count == totalBits)
                        break;
                }
            }

            if (thisNegative && (exp._data[0] & 0x1) != 0) //odd exp
                return -resultNum;

            return resultNum;
        }

        /// <summary>
        /// Fast calculation of modular reduction using Barrett's reduction
        /// </summary>
        /// <remarks>
        /// Requires x &lt; b^(2k), where b is the base.  In this case, base is 2^32 (uint).
        ///
        /// Reference [4]
        /// </remarks>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <param name="constant"></param>
        /// <returns></returns>
        private BigInteger BarrettReduction(BigInteger x, BigInteger n, BigInteger constant)
        {
            int k = n.DataLength,
                kPlusOne = k + 1,
                kMinusOne = k - 1;

            BigInteger q1 = new BigInteger();

            // q1 = x / b^(k-1)
            for (int i = kMinusOne, j = 0; i < x.DataLength; i++, j++)
                q1._data[j] = x._data[i];
            q1.DataLength = x.DataLength - kMinusOne;
            if (q1.DataLength <= 0)
                q1.DataLength = 1;

            BigInteger q2 = q1 * constant;
            BigInteger q3 = new BigInteger();

            // q3 = q2 / b^(k+1)
            for (int i = kPlusOne, j = 0; i < q2.DataLength; i++, j++)
                q3._data[j] = q2._data[i];
            q3.DataLength = q2.DataLength - kPlusOne;
            if (q3.DataLength <= 0)
                q3.DataLength = 1;

            // r1 = x mod b^(k+1)
            // i.e. keep the lowest (k+1) words
            BigInteger r1 = new BigInteger();
            int lengthToCopy = (x.DataLength > kPlusOne) ? kPlusOne : x.DataLength;
            for (int i = 0; i < lengthToCopy; i++)
                r1._data[i] = x._data[i];
            r1.DataLength = lengthToCopy;

            // r2 = (q3 * n) mod b^(k+1)
            // partial multiplication of q3 and n

            BigInteger r2 = new BigInteger();
            for (int i = 0; i < q3.DataLength; i++)
            {
                if (q3._data[i] == 0) continue;

                ulong mcarry = 0;
                int t = i;
                for (int j = 0; j < n.DataLength && t < kPlusOne; j++, t++)
                {
                    // t = i + j
                    ulong val = (q3._data[i] * (ulong)n._data[j]) +
                                 r2._data[t] + mcarry;

                    r2._data[t] = (uint)(val & 0xFFFFFFFF);
                    mcarry = (val >> 32);
                }

                if (t < kPlusOne)
                    r2._data[t] = (uint)mcarry;
            }

            r2.DataLength = kPlusOne;
            while (r2.DataLength > 1 && r2._data[r2.DataLength - 1] == 0)
                r2.DataLength--;

            r1 -= r2;
            if ((r1._data[MaxLength - 1] & 0x80000000) != 0) // negative
            {
                BigInteger val = new BigInteger();
                val._data[kPlusOne] = 0x00000001;
                val.DataLength = kPlusOne + 1;
                r1 += val;
            }

            while (r1 >= n)
                r1 -= n;

            return r1;
        }

        /// <summary>
        /// Returns gcd(this, bi)
        /// </summary>
        /// <param name="bi"></param>
        /// <returns>Greatest Common Divisor of this and bi</returns>
        public BigInteger Gcd(BigInteger bi)
        {
            BigInteger x;
            BigInteger y;

            if ((_data[MaxLength - 1] & 0x80000000) != 0) // negative
                x = -this;
            else
                x = this;

            if ((bi._data[MaxLength - 1] & 0x80000000) != 0) // negative
                y = -bi;
            else
                y = bi;

            BigInteger g = y;

            while (x.DataLength > 1 || (x.DataLength == 1 && x._data[0] != 0))
            {
                g = x;
                x = y % x;
                y = g;
            }

            return g;
        }

        /// <summary>
        /// Populates "this" with the specified amount of random bits
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="rand"></param>
        public void GenRandomBits(int bits, Random rand)
        {
            int dwords = bits >> 5;
            int remBits = bits & 0x1F;

            if (remBits != 0)
                dwords++;

            if (dwords > MaxLength || bits <= 0)
                throw (new ArithmeticException("Number of required bits is not valid."));

            byte[] randBytes = new byte[dwords * 4];
            rand.NextBytes(randBytes);

            for (int i = 0; i < dwords; i++)
                _data[i] = BitConverter.ToUInt32(randBytes, i * 4);

            for (int i = dwords; i < MaxLength; i++)
                _data[i] = 0;

            if (remBits != 0)
            {
                uint mask;

                if (bits != 1)
                {
                    mask = (uint)(0x01 << (remBits - 1));
                    _data[dwords - 1] |= mask;
                }

                mask = 0xFFFFFFFF >> (32 - remBits);
                _data[dwords - 1] &= mask;
            }
            else
                _data[dwords - 1] |= 0x80000000;

            DataLength = dwords;

            if (DataLength == 0)
                DataLength = 1;
        }

        /// <summary>
        /// Populates "this" with the specified amount of random bits (secured version)
        /// </summary>
        /// <param name="bits"></param>
        /// <param name="rng"></param>
        public void GenRandomBits(int bits, RNGCryptoServiceProvider rng)
        {
            int dwords = bits >> 5;
            int remBits = bits & 0x1F;

            if (remBits != 0)
                dwords++;

            if (dwords > MaxLength || bits <= 0)
                throw (new ArithmeticException("Number of required bits is not valid."));

            byte[] randomBytes = new byte[dwords * 4];
            rng.GetBytes(randomBytes);

            for (int i = 0; i < dwords; i++)
                _data[i] = BitConverter.ToUInt32(randomBytes, i * 4);

            for (int i = dwords; i < MaxLength; i++)
                _data[i] = 0;

            if (remBits != 0)
            {
                uint mask;

                if (bits != 1)
                {
                    mask = (uint)(0x01 << (remBits - 1));
                    _data[dwords - 1] |= mask;
                }

                mask = 0xFFFFFFFF >> (32 - remBits);
                _data[dwords - 1] &= mask;
            }
            else
                _data[dwords - 1] |= 0x80000000;

            DataLength = dwords;

            if (DataLength == 0)
                DataLength = 1;
        }

        /// <summary>
        /// Returns the position of the most significant bit in the BigInteger
        /// </summary>
        /// <example>
        /// 1) The result is 1, if the value of BigInteger is 0...0000 0000
        /// 2) The result is 1, if the value of BigInteger is 0...0000 0001
        /// 3) The result is 2, if the value of BigInteger is 0...0000 0010
        /// 4) The result is 2, if the value of BigInteger is 0...0000 0011
        /// 5) The result is 5, if the value of BigInteger is 0...0001 0011
        /// </example>
        /// <returns></returns>
        public int BitCount()
        {
            while (DataLength > 1 && _data[DataLength - 1] == 0)
                DataLength--;

            uint value = _data[DataLength - 1];
            uint mask = 0x80000000;
            int bits = 32;

            while (bits > 0 && (value & mask) == 0)
            {
                bits--;
                mask >>= 1;
            }

            bits += ((DataLength - 1) << 5);

            return bits == 0 ? 1 : bits;
        }

        /// <summary>
        /// Probabilistic prime test based on Fermat's little theorem
        /// </summary>
        /// <remarks>
        /// for any a &lt; p (p does not divide a) if
        ///      a^(p-1) mod p != 1 then p is not prime.
        ///
        /// Otherwise, p is probably prime (pseudoprime to the chosen base).
        ///
        /// This method is fast but fails for Carmichael numbers when the randomly chosen base is a factor of the number.
        /// </remarks>
        /// <param name="confidence">Number of chosen bases</param>
        /// <returns>True if this is a pseudoprime to randomly chosen bases</returns>
        public bool FermatLittleTest(int confidence)
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.DataLength == 1)
            {
                // test small numbers
                if (thisVal._data[0] == 0 || thisVal._data[0] == 1)
                    return false;
                else if (thisVal._data[0] == 2 || thisVal._data[0] == 3)
                    return true;
            }

            if ((thisVal._data[0] & 0x1) == 0) // even numbers
                return false;

            int bits = thisVal.BitCount();
            BigInteger a = new BigInteger();
            BigInteger pSub1 = thisVal - (new BigInteger(1));
            Random rand = new Random();

            for (int round = 0; round < confidence; round++)
            {
                bool done = false;

                while (!done) // generate a < n
                {
                    int testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int)(rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    int byteLen = a.DataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || (byteLen == 1 && a._data[0] != 1))
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                BigInteger gcdTest = a.Gcd(thisVal);
                if (gcdTest.DataLength == 1 && gcdTest._data[0] != 1)
                    return false;

                // calculate a^(p-1) mod p
                BigInteger expResult = a.ModPow(pSub1, thisVal);

                int resultLen = expResult.DataLength;

                // is NOT prime is a^(p-1) mod p != 1

                if (resultLen > 1 || (resultLen == 1 && expResult._data[0] != 1))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Probabilistic prime test based on Rabin-Miller's
        /// </summary>
        /// <remarks>
        /// for any p &gt; 0 with p - 1 = 2^s * t
        ///
        /// p is probably prime (strong pseudoprime) if for any a &lt; p,
        /// 1) a^t mod p = 1 or
        /// 2) a^((2^j)*t) mod p = p-1 for some 0 &lt;= j &lt;= s-1
        ///
        /// Otherwise, p is composite.
        /// </remarks>
        /// <param name="confidence">Number of chosen bases</param>
        /// <returns>True if this is a strong pseudoprime to randomly chosen bases</returns>
        public bool RabinMillerTest(int confidence)
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.DataLength == 1)
            {
                // test small numbers
                if (thisVal._data[0] == 0 || thisVal._data[0] == 1)
                    return false;
                else if (thisVal._data[0] == 2 || thisVal._data[0] == 3)
                    return true;
            }

            if ((thisVal._data[0] & 0x1) == 0) // even numbers
                return false;

            // calculate values of s and t
            BigInteger pSub1 = thisVal - (new BigInteger(1));
            int s = 0;

            for (int index = 0; index < pSub1.DataLength; index++)
            {
                uint mask = 0x01;

                for (int i = 0; i < 32; i++)
                {
                    if ((pSub1._data[index] & mask) != 0)
                    {
                        index = pSub1.DataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            BigInteger t = pSub1 >> s;

            int bits = thisVal.BitCount();
            BigInteger a = new BigInteger();
            Random rand = new Random();

            for (int round = 0; round < confidence; round++)
            {
                bool done = false;

                while (!done) // generate a < n
                {
                    int testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int)(rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    int byteLen = a.DataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || (byteLen == 1 && a._data[0] != 1))
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                BigInteger gcdTest = a.Gcd(thisVal);
                if (gcdTest.DataLength == 1 && gcdTest._data[0] != 1)
                    return false;

                BigInteger b = a.ModPow(t, thisVal);

                bool result = false;

                if (b.DataLength == 1 && b._data[0] == 1) // a^t mod p = 1
                    result = true;

                for (int j = 0; result == false && j < s; j++)
                {
                    if (b == pSub1) // a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
                    {
                        result = true;
                        break;
                    }

                    b = (b * b) % thisVal;
                }

                if (result == false)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Probabilistic prime test based on Solovay-Strassen (Euler Criterion)
        /// </summary>
        /// <remarks>
        ///  p is probably prime if for any a &lt; p (a is not multiple of p),
        /// a^((p-1)/2) mod p = J(a, p)
        ///
        /// where J is the Jacobi symbol.
        ///
        /// Otherwise, p is composite.
        /// </remarks>
        /// <param name="confidence">Number of chosen bases</param>
        /// <returns>True if this is a Euler pseudoprime to randomly chosen bases</returns>
        public bool SolovayStrassenTest(int confidence)
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.DataLength == 1)
            {
                // test small numbers
                if (thisVal._data[0] == 0 || thisVal._data[0] == 1)
                    return false;
                else if (thisVal._data[0] == 2 || thisVal._data[0] == 3)
                    return true;
            }

            if ((thisVal._data[0] & 0x1) == 0) // even numbers
                return false;

            int bits = thisVal.BitCount();
            BigInteger a = new BigInteger();
            BigInteger pSub1 = thisVal - 1;
            BigInteger pSub1Shift = pSub1 >> 1;

            Random rand = new Random();

            for (int round = 0; round < confidence; round++)
            {
                bool done = false;

                while (!done) // generate a < n
                {
                    int testBits = 0;

                    // make sure "a" has at least 2 bits
                    while (testBits < 2)
                        testBits = (int)(rand.NextDouble() * bits);

                    a.GenRandomBits(testBits, rand);

                    int byteLen = a.DataLength;

                    // make sure "a" is not 0
                    if (byteLen > 1 || (byteLen == 1 && a._data[0] != 1))
                        done = true;
                }

                // check whether a factor exists (fix for version 1.03)
                BigInteger gcdTest = a.Gcd(thisVal);
                if (gcdTest.DataLength == 1 && gcdTest._data[0] != 1)
                    return false;

                // calculate a^((p-1)/2) mod p

                BigInteger expResult = a.ModPow(pSub1Shift, thisVal);
                if (expResult == pSub1)
                    expResult = -1;

                // calculate Jacobi symbol
                BigInteger jacob = Jacobi(a, thisVal);

                // if they are different then it is not prime
                if (expResult != jacob)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Implementation of the Lucas Strong Pseudo Prime test
        /// </summary>
        /// <remarks>
        /// Let n be an odd number with gcd(n,D) = 1, and n - J(D, n) = 2^s * d
        /// with d odd and s >= 0.
        ///
        /// If Ud mod n = 0 or V2^r*d mod n = 0 for some 0 &lt;= r &lt; s, then n
        /// is a strong Lucas pseudoprime with parameters (P, Q).  We select
        /// P and Q based on Selfridge.
        /// </remarks>
        /// <returns>True if number is a strong Lucus pseudo prime</returns>
        public bool LucasStrongTest()
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.DataLength == 1)
            {
                // test small numbers
                if (thisVal._data[0] == 0 || thisVal._data[0] == 1)
                    return false;
                else if (thisVal._data[0] == 2 || thisVal._data[0] == 3)
                    return true;
            }

            if ((thisVal._data[0] & 0x1) == 0) // even numbers
                return false;

            return LucasStrongTestHelper(thisVal);
        }

        private bool LucasStrongTestHelper(BigInteger thisVal)
        {
            // Do the test (selects D based on Selfridge)
            // Let D be the first element of the sequence
            // 5, -7, 9, -11, 13, ... for which J(D,n) = -1
            // Let P = 1, Q = (1-D) / 4

            long d = 5, sign = -1, dCount = 0;
            bool done = false;

            while (!done)
            {
                int jresult = BigInteger.Jacobi(d, thisVal);

                if (jresult == -1)
                    done = true; // J(D, this) = 1
                else
                {
                    if (jresult == 0 && Math.Abs(d) < thisVal) // divisor found
                        return false;

                    if (dCount == 20)
                    {
                        // check for square
                        BigInteger root = thisVal.Sqrt();
                        if (root * root == thisVal)
                            return false;
                    }

                    d = (Math.Abs(d) + 2) * sign;
                    sign = -sign;
                }

                dCount++;
            }

            long q = (1 - d) >> 2;

            BigInteger pAdd1 = thisVal + 1;
            int s = 0;

            for (int index = 0; index < pAdd1.DataLength; index++)
            {
                uint mask = 0x01;

                for (int i = 0; i < 32; i++)
                {
                    if ((pAdd1._data[index] & mask) != 0)
                    {
                        index = pAdd1.DataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            BigInteger t = pAdd1 >> s;

            // calculate constant = b^(2k) / m
            // for Barrett Reduction
            BigInteger constant = new BigInteger();

            int nLen = thisVal.DataLength << 1;
            constant._data[nLen] = 0x00000001;
            constant.DataLength = nLen + 1;

            constant = constant / thisVal;

            BigInteger[] lucas = LucasSequenceHelper(1, q, t, thisVal, constant, 0);
            bool isPrime = false;

            if ((lucas[0].DataLength == 1 && lucas[0]._data[0] == 0) ||
                (lucas[1].DataLength == 1 && lucas[1]._data[0] == 0))
            {
                // u(t) = 0 or V(t) = 0
                isPrime = true;
            }

            for (int i = 1; i < s; i++)
            {
                if (!isPrime)
                {
                    // doubling of index
                    lucas[1] = thisVal.BarrettReduction(lucas[1] * lucas[1], thisVal, constant);
                    lucas[1] = (lucas[1] - (lucas[2] << 1)) % thisVal;

                    if ((lucas[1].DataLength == 1 && lucas[1]._data[0] == 0))
                        isPrime = true;
                }

                lucas[2] = thisVal.BarrettReduction(lucas[2] * lucas[2], thisVal, constant); //Q^k
            }

            if (isPrime) // additional checks for composite numbers
            {
                // If n is prime and gcd(n, Q) == 1, then
                // Q^((n+1)/2) = Q * Q^((n-1)/2) is congruent to (Q * J(Q, n)) mod n

                BigInteger g = thisVal.Gcd(q);
                if (g.DataLength == 1 && g._data[0] == 1) // gcd(this, Q) == 1
                {
                    if ((lucas[2]._data[MaxLength - 1] & 0x80000000) != 0)
                        lucas[2] += thisVal;

                    BigInteger temp = (q * BigInteger.Jacobi(q, thisVal)) % thisVal;
                    if ((temp._data[MaxLength - 1] & 0x80000000) != 0)
                        temp += thisVal;

                    if (lucas[2] != temp)
                        isPrime = false;
                }
            }

            return isPrime;
        }

        /// <summary>
        /// Determines whether a number is probably prime using the Rabin-Miller's test
        /// </summary>
        /// <remarks>
        /// Before applying the test, the number is tested for divisibility by primes &lt; 2000
        /// </remarks>
        /// <param name="confidence">Number of chosen bases</param>
        /// <returns>True if this is probably prime</returns>
        public bool IsProbablePrime(int confidence)
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            // test for divisibility by primes < 2000
            for (int p = 0; p < PrimesBelow2000.Length; p++)
            {
                BigInteger divisor = PrimesBelow2000[p];

                if (divisor >= thisVal)
                    break;

                BigInteger resultNum = thisVal % divisor;
                if (resultNum.IntValue() == 0)
                    return false;
            }

            if (thisVal.RabinMillerTest(confidence))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Determines whether this BigInteger is probably prime using a combination of base 2 strong pseudoprime test and Lucas strong pseudoprime test
        /// </summary>
        /// <remarks>
        /// The sequence of the primality test is as follows,
        ///
        /// 1) Trial divisions are carried out using prime numbers below 2000.
        ///    if any of the primes divides this BigInteger, then it is not prime.
        ///
        /// 2) Perform base 2 strong pseudoprime test.  If this BigInteger is a
        ///    base 2 strong pseudoprime, proceed on to the next step.
        ///
        /// 3) Perform strong Lucas pseudoprime test.
        ///
        /// For a detailed discussion of this primality test, see [6].
        /// </remarks>
        /// <returns>True if this is probably prime</returns>
        public bool IsProbablePrime()
        {
            BigInteger thisVal;
            if ((this._data[MaxLength - 1] & 0x80000000) != 0) // negative
                thisVal = -this;
            else
                thisVal = this;

            if (thisVal.DataLength == 1)
            {
                // test small numbers
                if (thisVal._data[0] == 0 || thisVal._data[0] == 1)
                    return false;
                else if (thisVal._data[0] == 2 || thisVal._data[0] == 3)
                    return true;
            }

            if ((thisVal._data[0] & 0x1) == 0) // even numbers
                return false;

            // test for divisibility by primes < 2000
            for (int p = 0; p < PrimesBelow2000.Length; p++)
            {
                BigInteger divisor = PrimesBelow2000[p];

                if (divisor >= thisVal)
                    break;

                BigInteger resultNum = thisVal % divisor;
                if (resultNum.IntValue() == 0)
                    return false;
            }

            // Perform BASE 2 Rabin-Miller Test

            // calculate values of s and t
            BigInteger pSub1 = thisVal - (new BigInteger(1));
            int s = 0;

            for (int index = 0; index < pSub1.DataLength; index++)
            {
                uint mask = 0x01;

                for (int i = 0; i < 32; i++)
                {
                    if ((pSub1._data[index] & mask) != 0)
                    {
                        index = pSub1.DataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            BigInteger t = pSub1 >> s;

            int bits = thisVal.BitCount();
            BigInteger a = 2;

            // b = a^t mod p
            BigInteger b = a.ModPow(t, thisVal);
            bool result = false;

            if (b.DataLength == 1 && b._data[0] == 1) // a^t mod p = 1
                result = true;

            for (int j = 0; result == false && j < s; j++)
            {
                if (b == pSub1) // a^((2^j)*t) mod p = p-1 for some 0 <= j <= s-1
                {
                    result = true;
                    break;
                }

                b = (b * b) % thisVal;
            }

            // if number is strong pseudoprime to base 2, then do a strong lucas test
            if (result)
                result = LucasStrongTestHelper(thisVal);

            return result;
        }

        /// <summary>
        /// Returns the lowest 4 bytes of the BigInteger as an int
        /// </summary>
        /// <returns>Lowest 4 bytes as integer</returns>
        public int IntValue()
        {
            return (int)_data[0];
        }

        /// <summary>
        /// Returns the lowest 8 bytes of the BigInteger as a long
        /// </summary>
        /// <returns>Lowest 8 bytes as long</returns>
        public long LongValue()
        {
            long val = 0;

            val = _data[0];
            try
            {
                // exception if maxLength = 1
                val |= (long)_data[1] << 32;
            }
            catch (Exception)
            {
                if ((_data[0] & 0x80000000) != 0) // negative
                    val = (int)_data[0];
            }

            return val;
        }

        /// <summary>
        /// Computes the Jacobi Symbol for 2 BigInteger a and b
        /// </summary>
        /// <remarks>
        /// Algorithm adapted from [3] and [4] with some optimizations
        /// </remarks>
        /// <param name="a">Any BigInteger</param>
        /// <param name="b">Odd BigInteger</param>
        /// <returns>Jacobi Symbol</returns>
        public static int Jacobi(BigInteger a, BigInteger b)
        {
            // Jacobi defined only for odd integers
            if ((b._data[0] & 0x1) == 0)
                throw (new ArgumentException("Jacobi defined only for odd integers."));

            if (a >= b) a %= b;
            if (a.DataLength == 1 && a._data[0] == 0) return 0; // a == 0
            if (a.DataLength == 1 && a._data[0] == 1) return 1; // a == 1

            if (a < 0)
            {
                if ((((b - 1)._data[0]) & 0x2) == 0) //if( (((b-1) >> 1).data[0] & 0x1) == 0)
                    return Jacobi(-a, b);
                else
                    return -Jacobi(-a, b);
            }

            int e = 0;
            for (int index = 0; index < a.DataLength; index++)
            {
                uint mask = 0x01;

                for (int i = 0; i < 32; i++)
                {
                    if ((a._data[index] & mask) != 0)
                    {
                        index = a.DataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    e++;
                }
            }

            BigInteger a1 = a >> e;

            int s = 1;
            if ((e & 0x1) != 0 && ((b._data[0] & 0x7) == 3 || (b._data[0] & 0x7) == 5))
                s = -1;

            if ((b._data[0] & 0x3) == 3 && (a1._data[0] & 0x3) == 3)
                s = -s;

            if (a1.DataLength == 1 && a1._data[0] == 1)
                return s;
            else
                return (s * Jacobi(b % a1, a1));
        }

        /// <summary>
        /// Generates a positive BigInteger that is probably prime
        /// </summary>
        /// <param name="bits">Number of bit</param>
        /// <param name="confidence">Number of chosen bases</param>
        /// <param name="rand">Random object</param>
        /// <returns>A probably prime number</returns>
        public static BigInteger GenPseudoPrime(int bits, int confidence, Random rand)
        {
            BigInteger result = new BigInteger();
            bool done = false;

            while (!done)
            {
                result.GenRandomBits(bits, rand);
                result._data[0] |= 0x01; // make it odd

                // prime test
                done = result.IsProbablePrime(confidence);
            }

            return result;
        }

        /// <summary>
        /// Generates a positive BigInteger that is probably prime (secured version)
        /// </summary>
        /// <param name="bits">Number of bit</param>
        /// <param name="confidence">Number of chosen bases</param>
        /// <param name="rand">RNGCryptoServiceProvider object</param>
        /// <returns>A probably prime number</returns>
        public static BigInteger GenPseudoPrime(int bits, int confidence, RNGCryptoServiceProvider rand)
        {
            BigInteger result = new BigInteger();
            bool done = false;

            while (!done)
            {
                result.GenRandomBits(bits, rand);
                result._data[0] |= 0x01; // make it odd

                // prime test
                done = result.IsProbablePrime(confidence);
            }

            return result;
        }

        /// <summary>
        /// Generates a random number with the specified number of bits such that gcd(number, this) = 1
        /// </summary>
        /// <remarks>
        /// The number of bits must be greater than 0 and less than 2209
        /// </remarks>
        /// <param name="bits">Number of bit</param>
        /// <param name="rand">Random object</param>
        /// <returns>Relatively prime number of this</returns>
        public BigInteger GenCoPrime(int bits, Random rand)
        {
            bool done = false;
            BigInteger result = new BigInteger();

            while (!done)
            {
                result.GenRandomBits(bits, rand);

                // gcd test
                BigInteger g = result.Gcd(this);
                if (g.DataLength == 1 && g._data[0] == 1)
                    done = true;
            }

            return result;
        }

        /// <summary>
        /// Generates a random number with the specified number of bits such that gcd(number, this) = 1 (secured)
        /// </summary>
        /// <param name="bits">Number of bit</param>
        /// <param name="rand">Random object</param>
        /// <returns>Relatively prime number of this</returns>
        public BigInteger GenCoPrime(int bits, RNGCryptoServiceProvider rand)
        {
            bool done = false;
            BigInteger result = new BigInteger();

            while (!done)
            {
                result.GenRandomBits(bits, rand);

                // gcd test
                BigInteger g = result.Gcd(this);
                if (g.DataLength == 1 && g._data[0] == 1)
                    done = true;
            }

            return result;
        }

        /// <summary>
        /// Returns the modulo inverse of this
        /// </summary>
        /// <remarks>
        /// Throws ArithmeticException if the inverse does not exist.  (i.e. gcd(this, modulus) != 1)
        /// </remarks>
        /// <param name="modulus"></param>
        /// <returns>Modulo inverse of this</returns>
        public BigInteger ModInverse(BigInteger modulus)
        {
            BigInteger[] p = { 0, 1 };
            BigInteger[] q = new BigInteger[2]; // quotients
            BigInteger[] r = { 0, 0 }; // remainders

            int step = 0;

            BigInteger a = modulus;
            BigInteger b = this;

            while (b.DataLength > 1 || (b.DataLength == 1 && b._data[0] != 0))
            {
                BigInteger quotient = new BigInteger();
                BigInteger remainder = new BigInteger();

                if (step > 1)
                {
                    BigInteger pval = (p[0] - (p[1] * q[0])) % modulus;
                    p[0] = p[1];
                    p[1] = pval;
                }

                if (b.DataLength == 1)
                    SingleByteDivide(a, b, quotient, remainder);
                else
                    MultiByteDivide(a, b, quotient, remainder);

                q[0] = q[1];
                r[0] = r[1];
                q[1] = quotient;
                r[1] = remainder;

                a = b;
                b = remainder;

                step++;
            }

            if (r[0].DataLength > 1 || (r[0].DataLength == 1 && r[0]._data[0] != 1))
                throw (new ArithmeticException("No inverse!"));

            BigInteger result = ((p[0] - (p[1] * q[0])) % modulus);

            if ((result._data[MaxLength - 1] & 0x80000000) != 0)
                result += modulus; // get the least positive modulus

            return result;
        }

        /// <summary>
        /// Returns the value of the BigInteger as a byte array
        /// </summary>
        /// <remarks>
        /// The lowest index contains the MSB
        /// </remarks>
        /// <returns>Byte array containing value of the BigInteger</returns>
        public byte[] GetBytes()
        {
            int numBits = BitCount();

            int numBytes = numBits >> 3;
            if ((numBits & 0x7) != 0)
                numBytes++;

            byte[] result = new byte[numBytes];

            int pos = 0;
            uint tempVal, val = _data[DataLength - 1];

            if ((tempVal = (val >> 24 & 0xFF)) != 0)
                result[pos++] = (byte)tempVal;

            if ((tempVal = (val >> 16 & 0xFF)) != 0)
                result[pos++] = (byte)tempVal;
            else if (pos > 0)
                pos++;

            if ((tempVal = (val >> 8 & 0xFF)) != 0)
                result[pos++] = (byte)tempVal;
            else if (pos > 0)
                pos++;

            if ((tempVal = (val & 0xFF)) != 0)
                result[pos++] = (byte)tempVal;
            else if (pos > 0)
                pos++;

            for (int i = DataLength - 2; i >= 0; i--, pos += 4)
            {
                val = _data[i];
                result[pos + 3] = (byte)(val & 0xFF);
                val >>= 8;
                result[pos + 2] = (byte)(val & 0xFF);
                val >>= 8;
                result[pos + 1] = (byte)(val & 0xFF);
                val >>= 8;
                result[pos] = (byte)(val & 0xFF);
            }

            return result;
        }

        public uint[] GetInternalState()
        {
            uint[] result = new UInt32[_data.Length];
            _data.CopyTo(result, 0);
            return result;
        }

        /// <summary>
        /// Sets the value of the specified bit to 1
        /// </summary>
        /// <remarks>
        /// The Least Significant Bit position is 0
        /// </remarks>
        /// <param name="bitNum">The position of bit to be changed</param>
        public void SetBit(uint bitNum)
        {
            uint bytePos = bitNum >> 5; // divide by 32
            byte bitPos = (byte)(bitNum & 0x1F); // get the lowest 5 bits

            uint mask = (uint)1 << bitPos;
            this._data[bytePos] |= mask;

            if (bytePos >= this.DataLength)
                this.DataLength = (int)bytePos + 1;
        }

        /// <summary>
        /// Sets the value of the specified bit to 0
        /// </summary>
        /// <remarks>
        /// The Least Significant Bit position is 0
        /// </remarks>
        /// <param name="bitNum">The position of bit to be changed</param>
        public void UnsetBit(uint bitNum)
        {
            uint bytePos = bitNum >> 5;

            if (bytePos < this.DataLength)
            {
                byte bitPos = (byte)(bitNum & 0x1F);

                uint mask = (uint)1 << bitPos;
                uint mask2 = 0xFFFFFFFF ^ mask;

                this._data[bytePos] &= mask2;

                if (this.DataLength > 1 && this._data[this.DataLength - 1] == 0)
                    this.DataLength--;
            }
        }

        /// <summary>
        /// Returns a value that is equivalent to the integer square root of this
        /// </summary>
        /// <remarks>
        /// The integer square root of "this" is defined as the largest integer n, such that (n * n) &lt;= this.
        /// Square root of negative integer is an undefined behaviour (UB).
        /// </remarks>
        /// <returns>Integer square root of this</returns>
        public BigInteger Sqrt()
        {
            uint numBits = (uint)this.BitCount();

            if ((numBits & 0x1) != 0) // odd number of bits
                numBits = (numBits >> 1) + 1;
            else
                numBits = (numBits >> 1);

            uint bytePos = numBits >> 5;
            byte bitPos = (byte)(numBits & 0x1F);

            uint mask;

            BigInteger result = new BigInteger();
            if (bitPos == 0)
                mask = 0x80000000;
            else
            {
                mask = (uint)1 << bitPos;
                bytePos++;
            }

            result.DataLength = (int)bytePos;

            for (int i = (int)bytePos - 1; i >= 0; i--)
            {
                while (mask != 0)
                {
                    // guess
                    result._data[i] ^= mask;

                    // undo the guess if its square is larger than this
                    if ((result * result) > this)
                        result._data[i] ^= mask;

                    mask >>= 1;
                }

                mask = 0x80000000;
            }

            return result;
        }

        /// <summary>
        /// Returns the k_th number in the Lucas Sequence reduced modulo n
        /// </summary>
        /// <remarks>
        /// Uses index doubling to speed up the process.  For example, to calculate V(k),
        /// we maintain two numbers in the sequence V(n) and V(n+1).
        ///
        /// To obtain V(2n), we use the identity
        ///      V(2n) = (V(n) * V(n)) - (2 * Q^n)
        /// To obtain V(2n+1), we first write it as
        ///      V(2n+1) = V((n+1) + n)
        /// and use the identity
        ///      V(m+n) = V(m) * V(n) - Q * V(m-n)
        /// Hence,
        ///      V((n+1) + n) = V(n+1) * V(n) - Q^n * V((n+1) - n)
        ///                   = V(n+1) * V(n) - Q^n * V(1)
        ///                   = V(n+1) * V(n) - Q^n * P
        ///
        /// We use k in its binary expansion and perform index doubling for each
        /// bit position.  For each bit position that is set, we perform an
        /// index doubling followed by an index addition.  This means that for V(n),
        /// we need to update it to V(2n+1).  For V(n+1), we need to update it to
        /// V((2n+1)+1) = V(2*(n+1))
        ///
        /// This function returns
        /// [0] = U(k)
        /// [1] = V(k)
        /// [2] = Q^n
        ///
        /// Where U(0) = 0 % n, U(1) = 1 % n
        ///       V(0) = 2 % n, V(1) = P % n
        /// </remarks>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <param name="k"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static BigInteger[] LucasSequence(BigInteger p, BigInteger q,
            BigInteger k, BigInteger n)
        {
            if (k.DataLength == 1 && k._data[0] == 0)
            {
                BigInteger[] result = new BigInteger[3];

                result[0] = 0;
                result[1] = 2 % n;
                result[2] = 1 % n;
                return result;
            }

            // calculate constant = b^(2k) / m
            // for Barrett Reduction
            BigInteger constant = new BigInteger();

            int nLen = n.DataLength << 1;
            constant._data[nLen] = 0x00000001;
            constant.DataLength = nLen + 1;

            constant = constant / n;

            // calculate values of s and t
            int s = 0;

            for (int index = 0; index < k.DataLength; index++)
            {
                uint mask = 0x01;

                for (int i = 0; i < 32; i++)
                {
                    if ((k._data[index] & mask) != 0)
                    {
                        index = k.DataLength; // to break the outer loop
                        break;
                    }

                    mask <<= 1;
                    s++;
                }
            }

            BigInteger t = k >> s;

            return LucasSequenceHelper(p, q, t, n, constant, s);
        }

        //***********************************************************************
        // Performs the calculation of the kth term in the Lucas Sequence.
        // For details of the algorithm, see reference [9].
        //
        // k must be odd.  i.e LSB == 1
        //***********************************************************************
        private static BigInteger[] LucasSequenceHelper(BigInteger p, BigInteger q,
            BigInteger k, BigInteger n,
            BigInteger constant, int s)
        {
            BigInteger[] result = new BigInteger[3];

            if ((k._data[0] & 0x00000001) == 0)
                throw (new ArgumentException("Argument k must be odd."));

            int numbits = k.BitCount();
            uint mask = (uint)0x1 << ((numbits & 0x1F) - 1);

            // v = v0, v1 = v1, u1 = u1, Q_k = Q^0

            BigInteger v = 2 % n,
                qK = 1 % n,
                v1 = p % n,
                u1 = qK;
            bool flag = true;

            for (int i = k.DataLength - 1; i >= 0; i--) // iterate on the binary expansion of k
            {
                while (mask != 0)
                {
                    if (i == 0 && mask == 0x00000001) // last bit
                        break;

                    if ((k._data[i] & mask) != 0) // bit is set
                    {
                        // index doubling with addition

                        u1 = (u1 * v1) % n;

                        v = ((v * v1) - (p * qK)) % n;
                        v1 = n.BarrettReduction(v1 * v1, n, constant);
                        v1 = (v1 - ((qK * q) << 1)) % n;

                        if (flag)
                            flag = false;
                        else
                            qK = n.BarrettReduction(qK * qK, n, constant);

                        qK = (qK * q) % n;
                    }
                    else
                    {
                        // index doubling
                        u1 = ((u1 * v) - qK) % n;

                        v1 = ((v * v1) - (p * qK)) % n;
                        v = n.BarrettReduction(v * v, n, constant);
                        v = (v - (qK << 1)) % n;

                        if (flag)
                        {
                            qK = q % n;
                            flag = false;
                        }
                        else
                            qK = n.BarrettReduction(qK * qK, n, constant);
                    }

                    mask >>= 1;
                }

                mask = 0x80000000;
            }

            // at this point u1 = u(n+1) and v = v(n)
            // since the last bit always 1, we need to transform u1 to u(2n+1) and v to v(2n+1)

            u1 = ((u1 * v) - qK) % n;
            v = ((v * v1) - (p * qK)) % n;
            if (flag)
                flag = false;
            else
                qK = n.BarrettReduction(qK * qK, n, constant);

            qK = (qK * q) % n;

            for (int i = 0; i < s; i++)
            {
                // index doubling
                u1 = (u1 * v) % n;
                v = ((v * v) - (qK << 1)) % n;

                if (flag)
                {
                    qK = q % n;
                    flag = false;
                }
                else
                    qK = n.BarrettReduction(qK * qK, n, constant);
            }

            result[0] = u1;
            result[1] = v;
            result[2] = qK;

            return result;
        }
    }
}
#endif