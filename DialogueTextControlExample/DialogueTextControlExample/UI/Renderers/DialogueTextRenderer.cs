using DialogueTextControlExample.UI.Controls;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.UI;
using Stride.UI.Renderers;
using System;

namespace DialogueTextControlExample.UI.Renderers
{
    internal class DialogueTextRenderer : ElementRenderer
    {
        public DialogueTextRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            var textControl = (DialogueText)element;
            if (textControl.Font == null || (textControl.TextGlyphs?.Count ?? 0) == 0)
            {
                return;
            }

            if (textControl.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();

                Batch.BeginCustom(context.GraphicsContext, 1);
            }

            DrawText(textControl, context);

            if (textControl.Font.FontType == SpriteFontType.SDF)
            {
                Batch.End();

                Batch.BeginCustom(context.GraphicsContext, 0);
            }
        }

        private void DrawText(DialogueText textControl, UIRenderingContext context)
        {
            // Note the majority of this code is adapted from Stride's existing code
            // TextBlock.cs, UIBatch.cs & SpriteFont.cs
            // The biggest difference is that TextBlock/SpriteFont takes some (internal) shortcuts
            // and calculates things World-View-Projection space, whereas we must work in World space

            var spriteFont = textControl.Font;
            var commandList = context.GraphicsContext.CommandList;
            var layoutingContext = textControl.GetLayoutingContext();

            var drawCommand = new UIDrawCommand
            {
                RenderOpacity = textControl.RenderOpacity,
                DepthBias = context.DepthBias,
                RealVirtualResolutionRatio = layoutingContext.RealVirtualResolutionRatio,
                RequestedFontSizeVirtual = textControl.ActualTextSize,
                //Batch = Batch,
                SnapText = context.ShouldSnapText && !textControl.DoNotSnapText,
                //WorldMatrix = textControl.WorldMatrix,    // Calculated down below
                Alignment = textControl.TextAlignment,      // Not implemented, refer to SpriteFont.ForEachGlyph<T> to adapt the rest!
                TextBoxSize = new Vector2(textControl.ActualWidth, textControl.ActualHeight),
                Swizzle = StrideInternalExtensions.StrideSpriteFont.GetSwizzleField(spriteFont)
            };

            // shift the string position so that it is written from the left/top corner of the element
            var textBoxSize = new Vector2(textControl.ActualWidth, textControl.ActualHeight);
            var leftTopCornerOffset = textBoxSize / 2;

            var worldMatrix = textControl.WorldMatrix;
            worldMatrix.M41 -= worldMatrix.M11 * leftTopCornerOffset.X + worldMatrix.M21 * leftTopCornerOffset.Y;
            worldMatrix.M42 -= worldMatrix.M12 * leftTopCornerOffset.X + worldMatrix.M22 * leftTopCornerOffset.Y;
            worldMatrix.M43 -= worldMatrix.M13 * leftTopCornerOffset.X + worldMatrix.M23 * leftTopCornerOffset.Y;
            drawCommand.WorldMatrix = worldMatrix;

            switch (spriteFont.FontType)
            {
                case SpriteFontType.SDF:
                    {
                        drawCommand.SnapText = false;
                        float scaling = textControl.ActualTextSize / spriteFont.Size;
                        drawCommand.RealVirtualResolutionRatio = 1 / new Vector2(scaling, scaling);
                    }
                    break;
                case SpriteFontType.Static:
                    {
                        drawCommand.RealVirtualResolutionRatio = Vector2.One; // ensure that static font are not scaled internally
                    }
                    break;
                case SpriteFontType.Dynamic:
                    {
                        // Dynamic: if we're not displaying in a situation where we can snap text, we're probably in 3D.
                        // Let's use virtual resolution (otherwise requested size might change on every camera move)
                        // TODO: some step function to have LOD without regenerating on every small change?
                        if (!drawCommand.SnapText)
                        {
                            drawCommand.RealVirtualResolutionRatio = Vector2.One;
                        }
                    }
                    break;
            }

            var textGlyphs = textControl.TextGlyphs;
            var textGlyphRenderInfos = textControl.TextGlyphRenderInfos;

            // We don't want to have letters with non uniform ratio
            var fontSizeReal = new Vector2(textControl.ActualTextSize * drawCommand.RealVirtualResolutionRatio.Y);
            // Round due to low float precision
            fontSizeReal.X = MathF.Round(fontSizeReal.X, 4);
            fontSizeReal.Y = MathF.Round(fontSizeReal.Y, 4);

            var defaultTextColor = textControl.RenderOpacity * textControl.TextColor;
            float horizontalExtraSpacing = spriteFont.GetExtraSpacing(fontSizeReal.X);
            float lineSpacingDistance = spriteFont.GetTotalLineSpacing(fontSizeReal.Y);
            int kerningKey = 0;
            float posX = 0;
            float posY = 0;
            int curLineIndex = 0;
            for (int i = 0; i < textGlyphRenderInfos.Count; i++)
            {
                var textGlyphRenderInfo = textGlyphRenderInfos[i];
                if (textGlyphRenderInfo.IgnoreRender)
                {
                    continue;
                }
                var textGlyph = textGlyphRenderInfo.TextGlyph;
                if (curLineIndex != textGlyphRenderInfo.LineIndex)
                {
                    posY += lineSpacingDistance;
                    posX = 0;
                    curLineIndex = textGlyphRenderInfo.LineIndex;
                }

                char ch = textGlyph.Character;
                const bool uploadGpuResources = true;
                textGlyphRenderInfo.SpriteFont ??= spriteFont;  // Ensure sprite font is set
                var glyphSpriteFont = textGlyphRenderInfo.SpriteFont ?? spriteFont;
                var glyph = StrideInternalExtensions.StrideSpriteFont.GetGlyphMethod(glyphSpriteFont, commandList, ch, fontSizeReal, uploadGpuResources, out Vector2 auxiliaryScaling);
                if (glyph == null && !glyphSpriteFont.IgnoreUnkownCharacters && glyphSpriteFont.DefaultCharacter.HasValue)
                {
                    glyph = StrideInternalExtensions.StrideSpriteFont.GetGlyphMethod(glyphSpriteFont, commandList, glyphSpriteFont.DefaultCharacter.Value, fontSizeReal, uploadGpuResources, out auxiliaryScaling);
                }
                if (glyph != null)
                {
                    var dx = glyph.Offset.X;

                    var kerningMap = StrideInternalExtensions.StrideSpriteFont.GetKerningMapField(glyphSpriteFont);
                    if (kerningMap != null && kerningMap.TryGetValue(kerningKey, out float kerningOffset))
                    {
                        dx += kerningOffset;
                    }

                    float nextX = posX + (glyph.XAdvance + horizontalExtraSpacing) * auxiliaryScaling.X;
                    float drawPosX = posX + dx * auxiliaryScaling.X;
                    float drawPosY = posY;
                    textGlyphRenderInfo.PositionX = drawPosX;
                    textGlyphRenderInfo.PositionY = drawPosY;
                    textGlyphRenderInfo.AuxiliaryScaling = auxiliaryScaling;
                    textGlyphRenderInfo.SpriteFontGlyph = glyph;
                    posX = nextX;
                }

                // Shift the kerning key
                kerningKey = (kerningKey << 16);
            }
            textControl.UpdateTextEffects(context.Time);
            for (int i = 0; i < textGlyphRenderInfos.Count; i++)
            {
                var textGlyphRenderInfo = textGlyphRenderInfos[i];
                if (textGlyphRenderInfo.IsVisible && !textGlyphRenderInfo.IsWhitespace)
                {
                    DrawGlyph(ref drawCommand, textGlyphRenderInfo, fontSizeReal);
                }
            }
        }

        private void DrawGlyph(
            ref UIDrawCommand drawCommand, Dialogue.DialogueTextGlyphRenderInfo textGlyphRenderInfo,
            Vector2 requestedFontSizeReal)
        {
            var spriteFont = textGlyphRenderInfo.SpriteFont;
            var glyph = textGlyphRenderInfo.SpriteFontGlyph;
            var texture = spriteFont.Textures[glyph.BitmapIndex];
            RectangleF sourceRectangle = glyph.Subrect;
            Vector4 borderSize = default;   // Unused
            Color textColor = textGlyphRenderInfo.TextColor * drawCommand.RenderOpacity;
            int depthBias = drawCommand.DepthBias;
            var imageOrientation = ImageOrientation.AsIs;
            var swizzle = drawCommand.Swizzle;
            bool snapImage = drawCommand.SnapText;

            var realVirtualResolutionRatio = requestedFontSizeReal / drawCommand.RequestedFontSizeVirtual;
            var auxiliaryScaling = textGlyphRenderInfo.AuxiliaryScaling;

            // Skip items with null size
            var elementSize = new Vector3(
                auxiliaryScaling.X * glyph.Subrect.Width / realVirtualResolutionRatio.X,
                auxiliaryScaling.Y * glyph.Subrect.Height / realVirtualResolutionRatio.Y,
                z: 1);
            if (elementSize.LengthSquared() < MathUtil.ZeroTolerance)
            {
                return;
            }

            float posX = textGlyphRenderInfo.PositionX + textGlyphRenderInfo.PositionOffsetX;
            float posY = textGlyphRenderInfo.PositionY + textGlyphRenderInfo.PositionOffsetY;

            float baseOffsetY = StrideInternalExtensions.StrideSpriteFont.GetBaseOffsetYMethod(spriteFont, requestedFontSizeReal.Y);
            float srcOffsetX = sourceRectangle.Width * 0.5f;
            float srcOffsetY = sourceRectangle.Height * 0.5f;
            float xShift = posX + srcOffsetX;
            float yShift = posY + (baseOffsetY + glyph.Offset.Y * auxiliaryScaling.Y) + srcOffsetY;

            var xScaledShift = xShift / realVirtualResolutionRatio.X;
            var yScaledShift = yShift / realVirtualResolutionRatio.Y;

            var worldMatrix = drawCommand.WorldMatrix;

            worldMatrix.M41 += worldMatrix.M11 * xScaledShift + worldMatrix.M21 * yScaledShift;
            worldMatrix.M42 += worldMatrix.M12 * xScaledShift + worldMatrix.M22 * yScaledShift;
            worldMatrix.M43 += worldMatrix.M13 * xScaledShift + worldMatrix.M23 * yScaledShift;
            worldMatrix.M44 += worldMatrix.M14 * xScaledShift + worldMatrix.M24 * yScaledShift;

            Batch.DrawImage(
                texture, ref worldMatrix, ref sourceRectangle, ref elementSize, ref borderSize,
                ref textColor, depthBias, imageOrientation, swizzle, snapImage);
        }

        private struct UIDrawCommand
        {
            public float RenderOpacity;
            public int DepthBias;
            public Vector2 RealVirtualResolutionRatio;
            /// <summary>
            /// Font size in virtual resolution.
            /// </summary>
            public float RequestedFontSizeVirtual;
            //public UIBatch Batch;
            public bool SnapText;
            public Matrix WorldMatrix;
            public TextAlignment Alignment;
            public Vector2 TextBoxSize;
            public SwizzleMode Swizzle;
        }
    }
}
