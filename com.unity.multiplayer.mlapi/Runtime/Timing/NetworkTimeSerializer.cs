using System;
using MLAPI.Serialization;

namespace MLAPI.Timing
{
    /// <summary>
    /// Helper class for serializing network ticks.
    /// </summary>
    public static class NetworkTimeSerializer
    {
        /// <summary>
        /// The amounts of bits used when serializing a network tick. Lowering this value will increases the chance of a total desync. A ushort (16 bits) should be safe.
        /// </summary>
        private const int k_TickSerializeBits = 16;

        /// <summary>
        /// The period on which to wrap ticks based on the amount of bits used to serialize the tick. The max value is kept free to allow for the `NoTick`
        /// </summary>
        private const int k_TickPeriod = (1 << k_TickSerializeBits) - 1;

        /// <summary>
        /// Special value to indicate "No tick information" when serializing the tick. This is needed because we need to map a negative value of the regular NoTick into a positive range.
        /// </summary>
        private const int k_NoTick = (1 << k_TickSerializeBits);

        /// <summary>
        /// Serializes a tick by only sending a value inside a sliding window.
        /// </summary>
        /// <param name="serializer">The serializer used to serialize the value.</param>
        /// <param name="currentTick">A tick used as an anchor point for the sliding window. This can be any up-to-date tick value.</param>
        /// <param name="value">The value to serialize.</param>
        public static void SerializeTick(NetworkSerializer serializer, int currentTick, ref int value)
        {
            if (serializer.IsReading)
            {
                int rawValueRead = (int)serializer.Reader.ReadBits(k_TickSerializeBits);

                if (rawValueRead == k_NoTick)
                {
                    value = NetworkTimeSystem.NoTick;
                    return;
                }

                var quotient = Math.DivRem(currentTick, k_TickPeriod, out int remainder);

                value = quotient + rawValueRead;

                if (remainder > (k_TickPeriod / 2) && rawValueRead < (k_TickPeriod / 2))
                {
                    // This happens when the sending side just passed the sliding window. In that case we need to jump to the next period.
                    value += k_TickPeriod;
                }
            }
            else
            {
                // If no tick convert to NoTick else we need to take remainder
                ulong rawValueWrite = value == NetworkTimeSystem.NoTick ? k_NoTick : (ulong)(value % k_TickPeriod);

                serializer.Writer.WriteBits(rawValueWrite, k_TickSerializeBits);
            }
        }
    }
}
