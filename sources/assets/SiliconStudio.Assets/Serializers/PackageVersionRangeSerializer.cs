﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SharpYaml;
using SharpYaml.Events;
using SharpYaml.Serialization;
using SiliconStudio.Core;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Assets.Serializers
{
    /// <summary>
    /// A Yaml serializer for <see cref="PackageVersionRange"/>
    /// </summary>
    [YamlSerializerFactory]
    internal class PackageVersionRangeSerializer : AssetScalarSerializerBase
    {
        public override bool CanVisit(Type type)
        {
            return typeof(PackageVersionRange).IsAssignableFrom(type);
        }

        public override object ConvertFrom(ref ObjectContext context, Scalar fromScalar)
        {
            PackageVersionRange packageVersion;
            if (!PackageVersionRange.TryParse(fromScalar.Value, out packageVersion))
            {
                throw new YamlException(fromScalar.Start, fromScalar.End, "Invalid version dependency format. Unable to decode [{0}]".ToFormat(fromScalar.Value));
            }
            return packageVersion;
        }

        public override string ConvertTo(ref ObjectContext objectContext)
        {
            return objectContext.Instance.ToString();
        }
    }
}