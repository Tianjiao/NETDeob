﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NETDeob.Misc.Structs__Enums___Interfaces.Signatures
{
    public interface ISignature
    {
        string Name { get; }
        Version Ver { get; }
        Type DeObfuscator { get; }
    }
}
