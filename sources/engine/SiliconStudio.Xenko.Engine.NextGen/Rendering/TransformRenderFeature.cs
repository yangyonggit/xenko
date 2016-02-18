using System.Collections.Generic;
using System.Runtime.InteropServices;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;

namespace SiliconStudio.Xenko.Rendering
{
    /// <summary>
    /// Computes and upload World, View and Projection matrices for each views and for each objects.
    /// </summary>
    public class TransformRenderFeature : SubRenderFeature
    {
        private ObjectPropertyKey<RenderModelFrameInfo> renderModelObjectInfoKey;
        private ViewObjectPropertyKey<RenderModelViewInfo> renderModelViewInfoKey;

        private ConstantBufferOffsetReference view;
        private ConstantBufferOffsetReference world;

        struct RenderModelFrameInfo
        {
            // Copied during Extract
            public Matrix World;
        }

        struct RenderModelViewInfo
        {
            // Copied during Extract
            public Matrix WorldViewProjection, WorldView;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            renderModelObjectInfoKey = RootRenderFeature.CreateObjectKey<RenderModelFrameInfo>();
            renderModelViewInfoKey = RootRenderFeature.CreateViewObjectKey<RenderModelViewInfo>();

            view = ((RootEffectRenderFeature)RootRenderFeature).CreateViewCBufferOffsetSlot(TransformationKeys.View.Name);
            world = ((RootEffectRenderFeature)RootRenderFeature).CreateDrawCBufferOffsetSlot(TransformationKeys.World.Name);
        }

        /// <inheritdoc/>
        public override void Extract()
        {
            var renderModelObjectInfo = RootRenderFeature.GetData(renderModelObjectInfoKey);

            foreach (var objectNodeReference in RootRenderFeature.ObjectNodeReferences)
            {
                var objectNode = RootRenderFeature.GetObjectNode(objectNodeReference);
                var renderMesh = objectNode.RenderObject as RenderMesh;
                // TODO: Extract world
                var world = (renderMesh != null) ? renderMesh.World : Matrix.Identity;

                renderModelObjectInfo[objectNodeReference] = new RenderModelFrameInfo { World = world };
            }
        }

        /// <param name="context"></param>
        /// <inheritdoc/>
        public unsafe override void Prepare(RenderContext context)
        {
            // Compute WorldView, WorldViewProj
            var renderModelObjectInfoData = RootRenderFeature.GetData(renderModelObjectInfoKey);
            var renderModelViewInfoData = RootRenderFeature.GetData(renderModelViewInfoKey);

            for (int index = 0; index < RenderSystem.Views.Count; index++)
            {
                var view = RenderSystem.Views[index];
                var viewFeature = view.Features[RootRenderFeature.Index];

                Matrix.Multiply(ref view.View, ref view.Projection, out view.ViewProjection);

                // Compute WorldView and WorldViewProjection
                foreach (var renderPerViewNodeReference in viewFeature.ViewObjectNodes)
                {
                    var renderPerViewNode = RootRenderFeature.GetViewObjectNode(renderPerViewNodeReference);
                    var renderModelFrameInfo = renderModelObjectInfoData[renderPerViewNode.ObjectNode];

                    var renderModelViewInfo = new RenderModelViewInfo();
                    Matrix.Multiply(ref renderModelFrameInfo.World, ref view.View, out renderModelViewInfo.WorldView);
                    Matrix.Multiply(ref renderModelFrameInfo.World, ref view.ViewProjection, out renderModelViewInfo.WorldViewProjection);

                    // TODO: Use ref locals or Utilities instead, to avoid double copy
                    renderModelViewInfoData[renderPerViewNodeReference] = renderModelViewInfo;
                }

                // Copy ViewProjection to PerFrame cbuffer
                foreach (var viewLayout in viewFeature.Layouts)
                {
                    var viewProjectionOffset = viewLayout.GetConstantBufferOffset(this.view);
                    if (viewProjectionOffset == -1)
                        continue;

                    var resourceGroup = viewLayout.Entries[view.Index].Resources;
                    var mappedCB = resourceGroup.ConstantBuffer.Data;

                    var perView = (PerView*)((byte*)mappedCB + viewProjectionOffset);

                    // Fill PerView
                    perView->View = view.View;
                    Matrix.Invert(ref view.View, out perView->ViewInverse);
                    perView->Projection = view.Projection;
                    Matrix.Invert(ref view.Projection, out perView->ProjectionInverse);
                    perView->ViewProjection = view.ViewProjection;
                    perView->ProjScreenRay = new Vector2(-1.0f / view.Projection.M11, 1.0f / view.Projection.M22);
                    // TODO GRAPHICS REFACTOR avoid cbuffer read
                    perView->Eye = new Vector4(perView->ViewInverse.M41, perView->ViewInverse.M42, perView->ViewInverse.M43, 1.0f);
                }
            }

            // Copy Entity.World to PerDraw cbuffer
            // TODO: Have a PerObject cbuffer?
            foreach (var renderNode in ((RootEffectRenderFeature)RootRenderFeature).RenderNodes)
            {
                var worldOffset = renderNode.RenderEffect.Reflection.PerDrawLayout.GetConstantBufferOffset(this.world);
                if (worldOffset == -1)
                    continue;

                var renderModelObjectInfo = renderModelObjectInfoData[renderNode.RenderObject.ObjectNode];
                var renderModelViewInfo = renderModelViewInfoData[renderNode.ViewObjectNode];

                var mappedCB = renderNode.Resources.ConstantBuffer.Data;
                var perDraw = (PerDraw*)((byte*)mappedCB + worldOffset);


                // Fill PerDraw
                perDraw->World = renderModelObjectInfo.World;
                Matrix.Invert(ref renderModelObjectInfo.World, out perDraw->WorldInverse);
                // TODO GRAPHICS REFACTOR avoid cbuffer read
                Matrix.Transpose(ref perDraw->WorldInverse, out perDraw->WorldInverseTranspose);
                perDraw->WorldView = renderModelViewInfo.WorldView;
                Matrix.Invert(ref renderModelViewInfo.WorldView, out perDraw->WorldViewInverse);
                perDraw->WorldViewProjection = renderModelViewInfo.WorldViewProjection;
                perDraw->WorldScale = new Vector3(
                    ((Vector3)renderModelObjectInfo.World.Row1).Length(),
                    ((Vector3)renderModelObjectInfo.World.Row2).Length(),
                    ((Vector3)renderModelObjectInfo.World.Row3).Length());
                // TODO GRAPHICS REFACTOR avoid cbuffer read
                perDraw->EyeMS = new Vector4(perDraw->WorldViewInverse.M41, perDraw->WorldViewInverse.M42, perDraw->WorldViewInverse.M43, 1.0f);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerView
        {
            public Matrix View;
            public Matrix ViewInverse;
            public Matrix Projection;
            public Matrix ProjectionInverse;
            public Matrix ViewProjection;
            public Vector2 ProjScreenRay;
            private Vector2 padding1;
            public Vector4 Eye;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerDraw
        {
            public Matrix World;
            public Matrix WorldInverse;
            public Matrix WorldInverseTranspose;
            public Matrix WorldView;
            public Matrix WorldViewInverse;
            public Matrix WorldViewProjection;
            public Vector3 WorldScale;
            private float padding1;
            public Vector4 EyeMS;
        }
    }
}