/*
 * Copyright(c) 2020 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
using System;
using System.ComponentModel;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Binding;
using Tizen.NUI.Components;

namespace Tizen.NUI.Wearable
{
    /// <summary>
    /// The CircualrScrollbar is a wearable NUI component that can be linked to the scrollable objects
    /// indicating the current scroll position of the scrollable object.<br />
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CircularScrollbar : ScrollbarBase
    {
        #region Fields

        /// <summary>Bindable property of Thickness</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly BindableProperty ThicknessProperty = BindableProperty.Create(nameof(Thickness), typeof(float), typeof(CircularScrollbar), default(float), propertyChanged: (bindable, oldValue, newValue) =>
        {
            var instance = ((CircularScrollbar)bindable);
            var thickness = (float?)newValue;

            instance.Style.Thickness = thickness;
            instance.UpdateVisualThickness(thickness ?? 0);
        },
        defaultValueCreator: (bindable) =>
        {
            return ((CircularScrollbar)bindable).Style.Thickness ?? 0;
        });

        /// <summary>Bindable property of TrackSweepAngle</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly BindableProperty TrackSweepAngleProperty = BindableProperty.Create(nameof(TrackSweepAngle), typeof(float), typeof(CircularScrollbar), default(float), propertyChanged: (bindable, oldValue, newValue) =>
        {
            var instance = ((CircularScrollbar)bindable);
            var angle = (float?)newValue;

            instance.Style.TrackSweepAngle = angle;
            instance.UpdateTrackVisualSweepAngle(angle ?? 0);
        },
        defaultValueCreator: (bindable) =>
        {
            return ((CircularScrollbar)bindable).Style.TrackSweepAngle ?? 0;
        });

        /// <summary>Bindable property of TrackColor</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(nameof(TrackColor), typeof(Color), typeof(CircularScrollbar), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var instance = ((CircularScrollbar)bindable);
            var color = (Color)newValue;

            instance.Style.TrackColor = color;
            instance.UpdateTrackVisualColor(color);
        },
        defaultValueCreator: (bindable) =>
        {
            return ((CircularScrollbar)bindable).Style.TrackColor;
        });

        /// <summary>Bindable property of ThumbColor</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly BindableProperty ThumbColorProperty = BindableProperty.Create(nameof(ThumbColor), typeof(Color), typeof(CircularScrollbar), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            var instance = ((CircularScrollbar)bindable);
            var color = (Color)newValue;

            instance.Style.ThumbColor = color;
            instance.UpdateThumbVisualColor(color);
        },
        defaultValueCreator: (bindable) =>
        {
            return ((CircularScrollbar)bindable).Style.ThumbColor;
        });

        private ArcVisual trackVisual;
        private ArcVisual thumbVisual;
        private float contentLength;
        private float visibleLength;
        private float currentPosition;
        private float directionAlpha;
        private Size containerSize = new Size(0, 0);
        private Animation thumbStartAngleAnimation;
        private Animation thumbSweepAngleAnimation;

        #endregion Fields


        #region Constructors

        /// <summary>
        /// Create an empty CircularScrollbar.
        /// </summary>
        public CircularScrollbar() : base(new CircularScrollbarStyle())
        {
        }

        /// <summary>
        /// Create a CircularScrollbar and initialize with properties.
        /// </summary>
        /// <param name="contentLength">The length of the scrollable content area.</param>
        /// <param name="viewportLength">The length of the viewport representing the amount of visible content.</param>
        /// <param name="currentPosition">The current position of the viewport in scrollable content area. This is the viewport's top position if the scroller is vertical, otherwise, left.</param>
        /// <param name="isHorizontal">Whether the direction of scrolling is horizontal or not. It is vertical by default.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CircularScrollbar(float contentLength, float viewportLength, float currentPosition, bool isHorizontal = false) : base(new CircularScrollbarStyle())
        {
            Initialize(contentLength, viewportLength, currentPosition, isHorizontal);
        }

        /// <summary>
        /// Create an empty CircularScrollbar with a CircularScrollbarStyle instance to set style properties.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CircularScrollbar(CircularScrollbarStyle style) : base(style)
        {
        }

        /// <summary>
        /// Static constructor to initialize bindable properties when loading.
        /// </summary>
        static CircularScrollbar()
        {
        }

        #endregion Constructors


        #region Properties

        /// <summary>
        /// Return a copied Style instance of CircularScrollbar
        /// </summary>
        /// <remarks>
        /// It returns copied Style instance and changing it does not effect to the CircularScrollbar.
        /// Style setting is possible by using constructor or the function of ApplyStyle(ViewStyle viewStyle)
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new CircularScrollbarStyle Style => ViewStyle as CircularScrollbarStyle;

        /// <summary>
        /// The thickness of the scrollbar and track.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float Thickness
        {
            get => (float)GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        /// <summary>
        /// The sweep angle of track area in degrees.
        /// </summary>
        /// <remarks>
        /// Values below 6 degrees are treated as 6 degrees.
        /// Values exceeding 180 degrees are treated as 180 degrees.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float TrackSweepAngle
        {
            get => (float)GetValue(TrackSweepAngleProperty);
            set => SetValue(TrackSweepAngleProperty, value);
        }

        /// <summary>
        /// The color of the track part.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color TrackColor
        {
            get => (Color)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }

        /// <summary>
        /// The color of the thumb part.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Color ThumbColor
        {
            get => (Color)GetValue(ThumbColorProperty);
            set => SetValue(ThumbColorProperty, value);
        }

        #endregion Properties


        #region Methods

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Initialize(float contentLength, float viewportLenth, float currentPosition, bool isHorizontal = false)
        {
            this.contentLength = contentLength > 0.0f ? contentLength : 0.0f;
            this.visibleLength = viewportLenth;
            this.currentPosition = currentPosition;
            this.directionAlpha = isHorizontal ? 270.0f : 0.0f;

            thumbStartAngleAnimation?.Stop();
            thumbStartAngleAnimation = null;

            thumbSweepAngleAnimation?.Stop();
            thumbSweepAngleAnimation = null;


            float trackSweepAngle = CalculateTrackSweepAngle(TrackSweepAngle);
            float trackStartAngle = CalculateTrackStartAngle(trackSweepAngle);
            float thumbSweepAngle = CalculateThumbSweepAngle(TrackSweepAngle);
            float thumbStartAngle = CalculateThumbStartAngle(currentPosition, trackStartAngle, trackSweepAngle, thumbSweepAngle);

            if (trackVisual == null)
            {
                trackVisual = new ArcVisual
                {
                    SuppressUpdateVisual = true,
                    Thickness = this.Thickness,
                    Cap = ArcVisual.CapType.Round,
                    MixColor = TrackColor,
                    Size = containerSize - new Size(2, 2),
                    SizePolicy = VisualTransformPolicyType.Absolute,
                    SweepAngle = trackSweepAngle,
                    StartAngle = trackStartAngle,
                };

                AddVisual("Track", trackVisual);
            }
            else
            {
                trackVisual.SweepAngle = trackSweepAngle;
                trackVisual.StartAngle = trackStartAngle;
                trackVisual.UpdateVisual(true);
            }

            if (thumbVisual == null)
            {
                thumbVisual = new ArcVisual
                {
                    SuppressUpdateVisual = true,
                    Thickness = trackVisual.Thickness,
                    Cap = ArcVisual.CapType.Round,
                    MixColor = ThumbColor,
                    Size = containerSize - new Size(2, 2),
                    SizePolicy = VisualTransformPolicyType.Absolute,
                    SweepAngle = thumbSweepAngle,
                    StartAngle = thumbStartAngle,
                    Opacity = CalculateThumbVisibility() ? 1.0f : 0.0f,
                };

                AddVisual("Thumb", thumbVisual);
            }
            else
            {
                thumbVisual.SweepAngle = thumbSweepAngle;
                thumbVisual.StartAngle = thumbStartAngle;
                thumbVisual.UpdateVisual(true);
            }
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Update(float contentLength, float position, uint durationMs = 0, AlphaFunction alphaFunction = null)
        {
            this.currentPosition = position;
            this.contentLength = contentLength > 0.0f ? contentLength : 0.0f;

            if (thumbVisual == null)
            {
                return;
            }

            thumbVisual.SweepAngle = CalculateThumbSweepAngle(TrackSweepAngle);
            thumbVisual.StartAngle = CalculateThumbStartAngle(position, trackVisual.StartAngle, trackVisual.SweepAngle, thumbVisual.SweepAngle);
            thumbVisual.Opacity = CalculateThumbVisibility() ? 1.0f : 0.0f;

            if (durationMs == 0)
            {
                thumbVisual.UpdateVisual(true);

                return;
            }

            // TODO Support non built-in alpha function for visual trainsition in DALi.
            AlphaFunction.BuiltinFunctions builtinAlphaFunction = alphaFunction?.GetBuiltinFunction() ?? AlphaFunction.BuiltinFunctions.Default;

            thumbStartAngleAnimation?.Stop();
            thumbStartAngleAnimation = AnimateVisual(thumbVisual, "startAngle", thumbVisual.StartAngle, 0, (int)durationMs, builtinAlphaFunction);
            thumbStartAngleAnimation.Play();

            thumbSweepAngleAnimation?.Stop();
            thumbSweepAngleAnimation = AnimateVisual(thumbVisual, "sweepAngle", thumbVisual.SweepAngle, 0, (int)durationMs, builtinAlphaFunction);
            thumbSweepAngleAnimation.Play();
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ScrollTo(float position, uint durationMs = 0, AlphaFunction alphaFunction = null)
        {
            currentPosition = position;

            if (thumbVisual == null)
            {
                return;
            }

            var oldThumbStartAngle = thumbVisual.StartAngle;

            thumbVisual.StartAngle = CalculateThumbStartAngle(position, trackVisual.StartAngle, trackVisual.SweepAngle, thumbVisual.SweepAngle);

            if (durationMs == 0)
            {
                thumbVisual.UpdateVisual(true);

                return;
            }

            // TODO Support non built-in alpha function for visual trainsition in DALi.
            AlphaFunction.BuiltinFunctions builtinAlphaFunction = alphaFunction?.GetBuiltinFunction() ?? AlphaFunction.BuiltinFunctions.Default;

            thumbStartAngleAnimation?.Stop();
            thumbStartAngleAnimation = AnimateVisual(thumbVisual, "startAngle", thumbVisual.StartAngle, 0, (int)durationMs, builtinAlphaFunction);
            thumbStartAngleAnimation.Play();
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void OnRelayout(Vector2 size, RelayoutContainer container)
        {
            base.OnRelayout(size, container);

            if (size.Width == containerSize?.Width && size.Height == containerSize.Height)
            {
                return;
            }

            containerSize = new Size(size.Width, size.Height);

            if (trackVisual == null)
            {
                return;
            }

            trackVisual.Size = containerSize - new Size(2, 2);
            thumbVisual.Size = containerSize - new Size(2, 2);
            
            trackVisual.UpdateVisual(true);
            thumbVisual.UpdateVisual(true);
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override ViewStyle CreateViewStyle()
        {
            return new CircularScrollbarStyle();
        }

        private float CalculateTrackStartAngle(float currentTrackSweepAngle)
        {
            return ((180.0f - currentTrackSweepAngle) / 2.0f) + directionAlpha;
        }

        private float CalculateTrackSweepAngle(float inputTrackSweepAngle)
        {
            return Math.Min(Math.Max(inputTrackSweepAngle, 3), 180);
        }

        private float CalculateThumbStartAngle(float position, float trackStartAngle, float trackSweepAngle, float thumbSweepAngle)
        {
            float minAngle = trackStartAngle;
            float maxAngle = trackStartAngle + trackSweepAngle - thumbSweepAngle;
            float resultAngle = trackStartAngle + (trackSweepAngle * (position < 0.0f ? 0.0f : position) / contentLength);

            return Math.Min(Math.Max(resultAngle, minAngle), maxAngle);
        }

        private float CalculateThumbSweepAngle(float trackSweepAngle)
        {
            return trackSweepAngle * visibleLength / contentLength;
        }

        private bool CalculateThumbVisibility()
        {
            return contentLength > visibleLength;
        }

        private void UpdateVisualThickness(float thickness)
        {
            if (trackVisual == null)
            {
                return;
            }

            trackVisual.Thickness = thickness;
            thumbVisual.Thickness = thickness;

            trackVisual.UpdateVisual(true);
            thumbVisual.UpdateVisual(true);
        }

        private void UpdateTrackVisualSweepAngle(float trackSweepAngle)
        {
            if (trackVisual == null || thumbVisual == null)
            {
                return;
            }

            trackVisual.SweepAngle = CalculateTrackSweepAngle(trackSweepAngle);
            trackVisual.StartAngle = CalculateTrackStartAngle(trackVisual.SweepAngle);

            thumbVisual.SweepAngle = CalculateThumbSweepAngle(TrackSweepAngle);
            thumbVisual.StartAngle = CalculateThumbStartAngle(currentPosition, trackVisual.StartAngle, trackVisual.SweepAngle, thumbVisual.SweepAngle);

            trackVisual.UpdateVisual(true);
            thumbVisual.UpdateVisual(true);
        }

        private void UpdateTrackVisualColor(Color trackColor)
        {
            if (trackVisual == null)
            {
                return;
            }

            trackVisual.MixColor = trackColor;
            trackVisual.UpdateVisual(true);
        }

        private void UpdateThumbVisualColor(Color thumbColor)
        {
            if (thumbVisual == null)
            {
                return;
            }

            thumbVisual.MixColor = thumbColor;
            thumbVisual.UpdateVisual(true);
        }

        #endregion Methods
    }
}
