using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Ror2Meeeee
{
    // Amostrador da thread principal para a vigia: numa travada, diz QUAL método C# está rodando (o perf de fora só
    // vê endereços de código do JIT). Só Windows x64 (o jogo roda assim, inclusive no Proton).
    //  - Init na thread principal: guarda um handle dela.
    //  - Sample na thread da vigia: suspende a principal por microssegundos, copia RIP e um pedaço da pilha para buffers
    //    já alocados (nada de alocação ou lock com ela parada: o GC ou um lock dela travaria a vigia), solta e só então
    //    traduz os endereços em métodos pelo próprio Mono (mono_jit_info_table_find).
    // A cadeia é heurística (varredura da pilha por endereços que caem em código do JIT), mas o topo é confiável.
    internal static class MainThreadSampler
    {
        private const int Samples = 20;
        private const int SampleIntervalMs = 50;
        private const int StackBytes = 2048;
        private const int ChainDepth = 4;

        private const uint ThreadSuspendResume = 0x0002, ThreadGetContext = 0x0008, ThreadQueryInformation = 0x0040;
        private const uint ContextControl = 0x00100001; // CONTEXT_AMD64 | CONTEXT_CONTROL (Rip, Rsp)
        private const int ContextSize = 0x4D0, ContextFlagsOffset = 0x30, RspOffset = 0x98, RipOffset = 0xF8;

        private static IntPtr _thread;
        private static IntPtr _contextRaw, _context;
        private static IntPtr _domain;
        private static readonly ulong[] Rips = new ulong[Samples];
        private static readonly byte[] Stacks = new byte[Samples * StackBytes];
        private static readonly bool[] Valid = new bool[Samples];
        private static readonly Dictionary<IntPtr, string> Names = new Dictionary<IntPtr, string>();
        private static string _disabled;

        public static void Init()
        {
            try
            {
                if (IntPtr.Size != 8) { _disabled = "só em 64 bits"; return; }
                _thread = OpenThread(ThreadSuspendResume | ThreadGetContext | ThreadQueryInformation, false, GetCurrentThreadId());
                if (_thread == IntPtr.Zero) { _disabled = "OpenThread falhou"; return; }
                _contextRaw = Marshal.AllocHGlobal(ContextSize + 16);
                _context = new IntPtr((_contextRaw.ToInt64() + 15) & ~15L); // GetThreadContext exige alinhamento de 16
                _domain = mono_get_root_domain();
            }
            catch (Exception e) { _disabled = e.GetType().Name; }
        }

        // Chamar só na thread da vigia, com a principal travada. Devolve a linha para o log.
        public static string Sample()
        {
            if (_disabled != null) return $"amostrador desligado ({_disabled})";
            try
            {
                for (var i = 0; i < Samples; i++)
                {
                    Valid[i] = Capture(i);
                    Thread.Sleep(SampleIntervalMs);
                }
                return Summarize();
            }
            catch (Exception e)
            {
                _disabled = e.GetType().Name; // ex. função do Mono ausente nesta versão: não tenta de novo
                return $"amostrador falhou: {e.Message}";
            }
        }

        // Entre Suspend e Resume: só chamadas nativas e cópia para buffer existente.
        private static bool Capture(int i)
        {
            if (SuspendThread(_thread) == uint.MaxValue) return false;
            var ok = false;
            try
            {
                Marshal.WriteInt32(_context, ContextFlagsOffset, unchecked((int)ContextControl));
                if (GetThreadContext(_thread, _context))
                {
                    Rips[i] = (ulong)Marshal.ReadInt64(_context, RipOffset);
                    var rsp = new IntPtr(Marshal.ReadInt64(_context, RspOffset));
                    Marshal.Copy(rsp, Stacks, i * StackBytes, StackBytes);
                    ok = true;
                }
            }
            finally { ResumeThread(_thread); }
            return ok;
        }

        private static string Summarize()
        {
            var counts = new Dictionary<string, int>();
            var valid = 0;
            var frames = new List<string>(ChainDepth);
            for (var i = 0; i < Samples; i++)
            {
                if (!Valid[i]) continue;
                valid++;
                frames.Clear();
                AddFrame(frames, Rips[i]);
                for (var off = 0; off + 8 <= StackBytes && frames.Count < ChainDepth; off += 8)
                    AddFrame(frames, BitConverter.ToUInt64(Stacks, i * StackBytes + off));
                var chain = frames.Count > 0 ? string.Join(" ← ", frames) : "(fora de código C#: nativo do Unity/Mono)";
                counts.TryGetValue(chain, out var c);
                counts[chain] = c + 1;
            }
            if (valid == 0) return "nenhuma amostra da thread principal";
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort((a, b) => b.Value.CompareTo(a.Value));
            var sb = new StringBuilder($"thread principal ({valid} amostras): ");
            for (var i = 0; i < list.Count && i < 3; i++)
                sb.Append(i == 0 ? "" : " | ").Append(list[i].Value).Append("x ").Append(list[i].Key);
            return sb.ToString();
        }

        // Endereço que cai em código do JIT vira "Tipo:Método"; repetido seguido (recursão/laço) não entra de novo.
        private static void AddFrame(List<string> frames, ulong address)
        {
            if (address < 0x10000) return;
            var ji = mono_jit_info_table_find(_domain, new IntPtr((long)address));
            if (ji == IntPtr.Zero) return;
            var method = mono_jit_info_get_method(ji);
            if (method == IntPtr.Zero) return;
            if (!Names.TryGetValue(method, out var name))
            {
                var p = mono_method_full_name(method, 0);
                name = p != IntPtr.Zero ? Marshal.PtrToStringAnsi(p) : "?";
                if (p != IntPtr.Zero) mono_free(p);
                Names[method] = name;
            }
            if (frames.Count == 0 || frames[frames.Count - 1] != name) frames.Add(name);
        }

        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenThread(uint access, bool inherit, uint threadId);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint SuspendThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(IntPtr thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetThreadContext(IntPtr thread, IntPtr context);

        private const string Mono = "mono-2.0-bdwgc";
        [DllImport(Mono)] private static extern IntPtr mono_get_root_domain();
        [DllImport(Mono)] private static extern IntPtr mono_jit_info_table_find(IntPtr domain, IntPtr address);
        [DllImport(Mono)] private static extern IntPtr mono_jit_info_get_method(IntPtr jitInfo);
        [DllImport(Mono)] private static extern IntPtr mono_method_full_name(IntPtr method, int signature);
        [DllImport(Mono)] private static extern void mono_free(IntPtr p);
    }
}
