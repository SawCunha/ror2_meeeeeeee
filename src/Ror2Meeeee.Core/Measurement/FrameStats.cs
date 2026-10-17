using System;

namespace Ror2Meeeee.Core.Measurement
{
    // Média e p95 (nearest-rank) de samples[0..count). `scratch` evita alocação e é sobrescrito.
    public static class FrameStats
    {
        public static void Compute(float[] samples, int count, float[] scratch, out float mean, out float p95)
        {
            if (count <= 0)
            {
                mean = 0f;
                p95 = 0f;
                return;
            }

            double sum = 0;
            for (var i = 0; i < count; i++) sum += samples[i];
            mean = (float)(sum / count);

            Array.Copy(samples, scratch, count);
            Array.Sort(scratch, 0, count);
            var rank = (int)Math.Ceiling(0.95 * count); // 1-based
            p95 = scratch[rank - 1];
        }
    }
}
