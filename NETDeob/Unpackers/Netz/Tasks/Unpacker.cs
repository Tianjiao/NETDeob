﻿using System.IO;
using System.Resources;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NETDeob.Core.Engine.Utils;
using NETDeob.Core.Engine.Utils.Extensions;
using NETDeob.Core.Misc;
using NETDeob.Deobfuscators;
using NETDeob.Misc;
using NETDeob.Misc.Structs__Enums___Interfaces.Deobfuscation;
using NETDeob.Misc.Structs__Enums___Interfaces.Tasks.Base;

namespace NETDeob.Core.Unpackers.Netz.Tasks
{
    public class Unpacker : UnpackingTask
    {
        public Unpacker(AssemblyDefinition asmDef)
            : base(asmDef)
        {
            RoutineDescription = "Unpacking";
        }

        #region phases

        [DeobfuscationPhase(1, "Finding resource")]
        public static bool Phase1()
        {
            var target = AsmDef.EntryPoint;

            target = target.Body.Instructions.FirstOfOpCode((op => op == OpCodes.Call), 3).Operand as MethodDefinition;
            PhaseParam = target.Body.Instructions.FirstOfOpCode(OpCodes.Ldstr).Operand as string;

            if(PhaseParam == null || PhaseParam == "")
            {
                PhaseError = new PhaseError
                                 {
                                     Level = PhaseError.ErrorLevel.Critical,
                                     Message = "Could not locate compressed resource!"
                                 };
                return false;
            }

            Logger.VSLog(string.Format("Found compressed resource: {0}...", PhaseParam));
            return true;
        }

        [DeobfuscationPhase(2, "Retrieving resource data")]
        public static bool Phase2()
        {
            var resName = PhaseParam;

            var resReader = new ResourceReader(AsmDef.FindResource(res => res.Name == "app.resources").GetResourceStream());
            var en = resReader.GetEnumerator();
            byte[] resData = null;

            while (en.MoveNext())
            {
                if (en.Key.ToString() == resName)
                    resData = en.Value as byte[];
            }

            if(resData == null)
            {
                PhaseError = new PhaseError
                                 {
                                     Level = PhaseError.ErrorLevel.Critical,
                                     Message = "Could not read resource data!"
                                 };
            }

            PhaseParam = resData;
            return true;
        }

        [DeobfuscationPhase(3, "Decompressing & writing output")]
        public static bool Phase3()
        {
            var resData = PhaseParam;

            File.WriteAllBytes(DeobfuscatorContext.OutPath, GetAssemblyData(resData));
            Logger.VSLog("Writing decompressed payload to disk...");

            return true;
        }

        #endregion

        #region Reversed methods

        private static byte[] GetAssemblyData(byte[] data)
        {
            Logger.VSLog("Decompressing payload (" + data.Length + " bytes)...");

            MemoryStream stream = UnZip(data);
            stream.Seek(0L, SeekOrigin.Begin);

            return stream.ToArray();
        }

        private static MemoryStream UnZip(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            MemoryStream baseInputStream = null;
            MemoryStream stream2;
            InflaterInputStream stream3 = null;
            try
            {
                baseInputStream = new MemoryStream(data);
                stream2 = new MemoryStream();
                stream3 = new InflaterInputStream(baseInputStream);
                byte[] buffer = new byte[data.Length];
                while (true)
                {
                    int count = stream3.Read(buffer, 0, buffer.Length);
                    if (count <= 0)
                    {
                        break;
                    }
                    stream2.Write(buffer, 0, count);
                }
                stream2.Flush();
                stream2.Seek(0L, SeekOrigin.Begin);
            }
            finally
            {
                if (baseInputStream != null)
                {
                    baseInputStream.Close();
                }
                if (stream3 != null)
                {
                    stream3.Close();
                }
            }
            return stream2;
        }

        #endregion

    }
}
