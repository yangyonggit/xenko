﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Particles.Sorters;
using SiliconStudio.Xenko.Particles.VertexLayouts;

namespace SiliconStudio.Xenko.Particles.Materials
{
    [DataContract("UVBuilderScroll")]
    [Display("Scrolling")]
    public class UVBuilderScroll : UVBuilderBase
    {
        [DataMember(200)]
        [Display("Start frame")]
        public Vector4 StartFrame { get; set; } = new Vector4(0, 0, 1, 1);

        [DataMember(240)]
        [Display("End frame")]
        public Vector4 EndFrame { get; set; } = new Vector4(0, 1, 1, 2);

        public unsafe override void BuildUVCoordinates(ParticleVertexBuffer vtxBuilder, ParticleSorter sorter)
        {
            var lifeField = sorter.GetField(ParticleFields.RemainingLife);

            if (!lifeField.IsValid())
                return;

            var texAttribute = vtxBuilder.GetAccessor(new AttributeDescription("TEXCOORD"));

            foreach (var particle in sorter)
            {
                var normalizedTimeline = 1f - *(float*)(particle[lifeField]); ;

                var uvTransform = Vector4.Lerp(StartFrame, EndFrame, normalizedTimeline);
                uvTransform.Z -= uvTransform.X;
                uvTransform.W -= uvTransform.Y;

                ParticleVertexBuffer.TransformAttributeDelegate<Vector2> transformCoords =
                    (ref Vector2 value) =>
                    {
                        value.X = uvTransform.X + uvTransform.Z * value.X;
                        value.Y = uvTransform.Y + uvTransform.W * value.Y;
                    };

                vtxBuilder.TransformAttributePerParticle(texAttribute, transformCoords);

                vtxBuilder.NextParticle();
            }
        }
    }
}
