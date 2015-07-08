﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.AssemblyProcessor
{
    public class SerializationProcessor : IAssemblyDefinitionProcessor
    {
        public string SignKeyFile { get; private set; }

        public List<string> SerializatonProjectReferencePaths { get; private set; }

        public ILogger Log { get; private set; }

        public SerializationProcessor(string signKeyFile, List<string> serializatonProjectReferencePaths, ILogger log)
        {
            SignKeyFile = signKeyFile;
            SerializatonProjectReferencePaths = serializatonProjectReferencePaths;
            Log = log;
        }

        public bool Process(AssemblyProcessorContext context)
        {
            // Generate serialization assembly
            var serializationAssemblyFilepath = ComplexSerializerGenerator.GenerateSerializationAssemblyLocation(context.Assembly.MainModule.FullyQualifiedName);
            context.Assembly = ComplexSerializerGenerator.GenerateSerializationAssembly(context.Platform, context.AssemblyResolver, context.Assembly, serializationAssemblyFilepath, SignKeyFile, SerializatonProjectReferencePaths, Log);

            return true;
        }
    }
}