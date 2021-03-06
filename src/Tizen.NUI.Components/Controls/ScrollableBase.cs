/* Copyright (c) 2020 Samsung Electronics Co., Ltd.
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
using Tizen.NUI.BaseComponents;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace Tizen.NUI.Components
{
    /// <summary>
    /// ScrollEventArgs is a class to record scroll event arguments which will sent to user.
    /// </summary>
    /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ScrollEventArgs : EventArgs
    {
        Position position;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="position">Current scroll position</param>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        public ScrollEventArgs(Position position)
        {
            this.position = position;
        }

        /// <summary>
        /// [Draft] Current scroll position.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Position Position
        {
            get
            {
                return position;
            }
        }
    }

    /// <summary>
    /// [Draft] This class provides a View that can scroll a single View with a layout. This View can be a nest of Views.
    /// </summary>
    /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ScrollableBase : Control
    {
        static bool LayoutDebugScrollableBase = false; // Debug flag
        private Direction mScrollingDirection = Direction.Vertical;
        private bool mScrollEnabled = true;
        private int mPageWidth = 0;

        private class ScrollableBaseCustomLayout : LayoutGroup
        {
            protected override void OnMeasure(MeasureSpecification widthMeasureSpec, MeasureSpecification heightMeasureSpec)
            {
                Extents padding = Padding;
                float totalHeight = padding.Top + padding.Bottom;
                float totalWidth = padding.Start + padding.End;

                MeasuredSize.StateType childWidthState = MeasuredSize.StateType.MeasuredSizeOK;
                MeasuredSize.StateType childHeightState = MeasuredSize.StateType.MeasuredSizeOK;

                Direction scrollingDirection = Direction.Vertical;
                ScrollableBase scrollableBase = this.Owner as ScrollableBase;
                if (scrollableBase)
                {
                    scrollingDirection = scrollableBase.ScrollingDirection;
                }

                // measure child, should be a single scrolling child
                foreach (LayoutItem childLayout in LayoutChildren)
                {
                    if (childLayout != null)
                    {
                        // Get size of child
                        // Use an Unspecified MeasureSpecification mode so scrolling child is not restricted to it's parents size in Height (for vertical scrolling)
                        // or Width for horizontal scrolling
                        MeasureSpecification unrestrictedMeasureSpec = new MeasureSpecification(heightMeasureSpec.Size, MeasureSpecification.ModeType.Unspecified);

                        if (scrollingDirection == Direction.Vertical)
                        {
                            MeasureChildWithMargins(childLayout, widthMeasureSpec, new LayoutLength(0), unrestrictedMeasureSpec, new LayoutLength(0));  // Height unrestricted by parent
                        }
                        else
                        {
                            MeasureChildWithMargins(childLayout, unrestrictedMeasureSpec, new LayoutLength(0), heightMeasureSpec, new LayoutLength(0));  // Width unrestricted by parent
                        }

                        float childWidth = childLayout.MeasuredWidth.Size.AsDecimal();
                        float childHeight = childLayout.MeasuredHeight.Size.AsDecimal();

                        // Determine the width and height needed by the children using their given position and size.
                        // Children could overlap so find the left most and right most child.
                        Position2D childPosition = childLayout.Owner.Position2D;
                        float childLeft = childPosition.X;
                        float childTop = childPosition.Y;

                        // Store current width and height needed to contain all children.
                        Extents childMargin = childLayout.Margin;
                        totalWidth = childWidth + childMargin.Start + childMargin.End;
                        totalHeight = childHeight + childMargin.Top + childMargin.Bottom;

                        if (childLayout.MeasuredWidth.State == MeasuredSize.StateType.MeasuredSizeTooSmall)
                        {
                            childWidthState = MeasuredSize.StateType.MeasuredSizeTooSmall;
                        }
                        if (childLayout.MeasuredHeight.State == MeasuredSize.StateType.MeasuredSizeTooSmall)
                        {
                            childHeightState = MeasuredSize.StateType.MeasuredSizeTooSmall;
                        }
                    }
                }


                MeasuredSize widthSizeAndState = ResolveSizeAndState(new LayoutLength(totalWidth + Padding.Start + Padding.End), widthMeasureSpec, MeasuredSize.StateType.MeasuredSizeOK);
                MeasuredSize heightSizeAndState = ResolveSizeAndState(new LayoutLength(totalHeight + Padding.Top + Padding.Bottom), heightMeasureSpec, MeasuredSize.StateType.MeasuredSizeOK);
                totalWidth = widthSizeAndState.Size.AsDecimal();
                totalHeight = heightSizeAndState.Size.AsDecimal();

                // Ensure layout respects it's given minimum size
                totalWidth = Math.Max(totalWidth, SuggestedMinimumWidth.AsDecimal());
                totalHeight = Math.Max(totalHeight, SuggestedMinimumHeight.AsDecimal());

                widthSizeAndState.State = childWidthState;
                heightSizeAndState.State = childHeightState;

                SetMeasuredDimensions(ResolveSizeAndState(new LayoutLength(totalWidth + Padding.Start + Padding.End), widthMeasureSpec, childWidthState),
                                       ResolveSizeAndState(new LayoutLength(totalHeight + Padding.Top + Padding.Bottom), heightMeasureSpec, childHeightState));

                // Size of ScrollableBase is changed. Change Page width too.
                scrollableBase.mPageWidth = (int)MeasuredWidth.Size.AsRoundedValue();
            }

            protected override void OnLayout(bool changed, LayoutLength left, LayoutLength top, LayoutLength right, LayoutLength bottom)
            {
                foreach (LayoutItem childLayout in LayoutChildren)
                {
                    if (childLayout != null)
                    {
                        LayoutLength childWidth = childLayout.MeasuredWidth.Size;
                        LayoutLength childHeight = childLayout.MeasuredHeight.Size;

                        Position2D childPosition = childLayout.Owner.Position2D;
                        Extents padding = Padding;
                        Extents childMargin = childLayout.Margin;

                        LayoutLength childLeft = new LayoutLength(childPosition.X + childMargin.Start + padding.Start);
                        LayoutLength childTop = new LayoutLength(childPosition.Y + childMargin.Top + padding.Top);

                        childLayout.Layout(childLeft, childTop, childLeft + childWidth, childTop + childHeight);
                    }
                }
            }
        } //  ScrollableBaseCustomLayout

        /// <summary>
        /// The direction axis to scroll.
        /// </summary>
        /// <since_tizen> 6 </since_tizen>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public enum Direction
        {
            /// <summary>
            /// Horizontal axis.
            /// </summary>
            /// <since_tizen> 6 </since_tizen>
            Horizontal,

            /// <summary>
            /// Vertical axis.
            /// </summary>
            /// <since_tizen> 6 </since_tizen>
            Vertical
        }

        /// <summary>
        /// [Draft] Configurable speed threshold that register the gestures as a flick.
        /// If the flick speed less than the threshold then will not be considered a flick.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float FlickThreshold { get; set; } = 0.2f;

        /// <summary>
        /// [Draft] Configurable duration modifer for the flick animation.
        /// Determines the speed of the scroll, large value results in a longer flick animation. Range (0.1 - 1.0)
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public float FlickAnimationSpeed { get; set; } = 0.4f;

        /// <summary>
        /// [Draft] Configurable modifer for the distance to be scrolled when flicked detected.
        /// It a ratio of the ScrollableBase's length. (not child's length).
        /// First value is the ratio of the distance to scroll with the weakest flick.
        /// Second value is the ratio of the distance to scroll with the strongest flick.
        /// Second > First.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Vector2 FlickDistanceMultiplierRange { get; set; } = new Vector2(0.6f, 1.8f);

        /// <summary>
        /// [Draft] Scrolling direction mode.
        /// Default is Vertical scrolling.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Direction ScrollingDirection
        {
            get
            {
                return mScrollingDirection;
            }
            set
            {
                if (value != mScrollingDirection)
                {
                    mScrollingDirection = value;
                    mPanGestureDetector.RemoveDirection(value == Direction.Horizontal ?
                        PanGestureDetector.DirectionVertical : PanGestureDetector.DirectionHorizontal);
                    mPanGestureDetector.AddDirection(value == Direction.Horizontal ?
                        PanGestureDetector.DirectionHorizontal : PanGestureDetector.DirectionVertical);

                    ContentContainer.WidthSpecification = mScrollingDirection == Direction.Vertical ?
                        LayoutParamPolicies.MatchParent : LayoutParamPolicies.WrapContent;
                    ContentContainer.HeightSpecification = mScrollingDirection == Direction.Vertical ?
                        LayoutParamPolicies.WrapContent : LayoutParamPolicies.MatchParent;
                }
            }
        }

        /// <summary>
        /// [Draft] Enable or disable scrolling.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ScrollEnabled
        {
            get
            {
                return mScrollEnabled;
            }
            set
            {
                if (value != mScrollEnabled)
                {
                    mScrollEnabled = value;
                    if (mScrollEnabled)
                    {
                        mPanGestureDetector.Detected += OnPanGestureDetected;
                        mTapGestureDetector.Detected += OnTapGestureDetected;
                    }
                    else
                    {
                        mPanGestureDetector.Detected -= OnPanGestureDetected;
                        mTapGestureDetector.Detected -= OnTapGestureDetected;
                    }
                }
            }
        }

        /// <summary>
        /// [Draft] Pages mode, enables moving to the next or return to current page depending on pan displacement.
        /// Default is false.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool SnapToPage { set; get; } = false;

        /// <summary>
        /// [Draft] Get current page.
        /// Working propery with SnapToPage property.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int CurrentPage { get; private set; } = 0;

        /// <summary>
        /// [Draft] Duration of scroll animation.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]

        public int ScrollDuration { set; get; } = 125;
        /// <summary>
        /// [Draft] Scroll Available area.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Vector2 ScrollAvailableArea { set; get; }

        /// <summary>
        /// An event emitted when user starts dragging ScrollableBase, user can subscribe or unsubscribe to this event handler.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<ScrollEventArgs> ScrollDragStarted;

        /// <summary>
        /// An event emitted when user stops dragging ScrollableBase, user can subscribe or unsubscribe to this event handler.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<ScrollEventArgs> ScrollDragEnded;


        /// <summary>
        /// An event emitted when the scrolling slide animation starts, user can subscribe or unsubscribe to this event handler.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<ScrollEventArgs> ScrollAnimationStarted;

        /// <summary>
        /// An event emitted when the scrolling slide animation ends, user can subscribe or unsubscribe to this event handler.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<ScrollEventArgs> ScrollAnimationEnded;


        /// <summary>
        /// An event emitted when scrolling, user can subscribe or unsubscribe to this event handler.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<ScrollEventArgs> Scrolling;


        /// <summary>
        /// Scrollbar for ScrollableBase.<br />
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ScrollbarBase Scrollbar
        {
            get
            {
                return scrollBar;
            }
            set
            {
                if (scrollBar)
                {
                    scrollBar.Unparent();
                }

                scrollBar = value;
                scrollBar.Name = "ScrollBar";
                base.Add(scrollBar);

                if (hideScrollbar)
                {
                    scrollBar.Hide();
                }
                else
                {
                    scrollBar.Show();
                }

                SetScrollbar();
            }
        }

        /// <summary>
        /// [Draft] Always hide Scrollbar.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool HideScrollBar
        {
            get
            {
                return hideScrollbar;
            }
            set
            {
                hideScrollbar = value;

                if (scrollBar)
                {
                    if (value)
                    {
                        scrollBar.Hide();
                    }
                    else
                    {
                        scrollBar.Show();
                    }
                }
            }
        }

        private bool hideScrollbar = true;
        private Animation scrollAnimation;
        private float maxScrollDistance;
        private float childTargetPosition = 0.0f;
        private PanGestureDetector mPanGestureDetector;
        private TapGestureDetector mTapGestureDetector;
        private View mInterruptTouchingChild;
        private ScrollbarBase scrollBar;
        private float multiplier = 1.0f;
        private bool scrolling = false;
        private float ratioOfScreenWidthToCompleteScroll = 0.5f;
        private float totalDisplacementForPan = 0.0f;
        private Size previousContainerSize = new Size();

        // If false then can only flick pages when the current animation/scroll as ended.
        private bool flickWhenAnimating = false;
        private PropertyNotification propertyNotification;

        // Let's consider more whether this needs to be set as protected.
        private float finalTargetPosition;

        /// <summary>
        /// [Draft] Constructor
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ScrollableBase() : base()
        {
            base.Layout = new ScrollableBaseCustomLayout();
            mPanGestureDetector = new PanGestureDetector();
            mPanGestureDetector.Attach(this);
            mPanGestureDetector.AddDirection(PanGestureDetector.DirectionVertical);
            mPanGestureDetector.Detected += OnPanGestureDetected;

            mTapGestureDetector = new TapGestureDetector();
            mTapGestureDetector.Attach(this);
            mTapGestureDetector.Detected += OnTapGestureDetected;

            ClippingMode = ClippingModeType.ClipChildren;

            //Default Scrolling child
            ContentContainer = new View()
            {
                WidthSpecification = ScrollingDirection == Direction.Vertical ? LayoutParamPolicies.MatchParent : LayoutParamPolicies.WrapContent,
                HeightSpecification = ScrollingDirection == Direction.Vertical ? LayoutParamPolicies.WrapContent : LayoutParamPolicies.MatchParent,
                Layout = new AbsoluteLayout(){SetPositionByLayout = false},
            };
            ContentContainer.Relayout += OnScrollingChildRelayout;
            propertyNotification = ContentContainer.AddPropertyNotification("position", PropertyCondition.Step(1.0f));
            propertyNotification.Notified += OnPropertyChanged;
            base.Add(ContentContainer);

            //Interrupt touching when panning is started
            mInterruptTouchingChild = new View()
            {
                Size = new Size(Window.Instance.WindowSize),
                BackgroundColor = Color.Transparent,
            };
            mInterruptTouchingChild.TouchEvent += OnIterruptTouchingChildTouched;

            Scrollbar = new Scrollbar();
        }

        /// <summary>
        /// Container which has content of ScrollableBase.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public View ContentContainer { get; private set; }

        /// <summary>
        /// Set the layout on this View. Replaces any existing Layout.
        /// </summary>
        public new LayoutItem Layout
        {
            get
            {
                return ContentContainer.Layout;
            }
            set
            {
                ContentContainer.Layout = value;
                if(ContentContainer.Layout != null)
                {
                    ContentContainer.Layout.SetPositionByLayout = false;
                }
            }
        }

        /// <summary>
        /// List of children of Container.
        /// </summary>
        public new List<View> Children
        {
            get
            {
                return ContentContainer.Children;
            }
        }

        private bool OnIterruptTouchingChildTouched(object source, View.TouchEventArgs args)
        {
            return true;
        }

        private void OnPropertyChanged(object source, PropertyNotification.NotifyEventArgs args)
        {
            OnScroll();
        }

        /// <summary>
        /// Called after a child has been added to the owning view.
        /// </summary>
        /// <param name="view">The child which has been added.</param>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Add(View view)
        {
            ContentContainer.Add(view);
        }

        /// <summary>
        /// Called after a child has been removed from the owning view.
        /// </summary>
        /// <param name="view">The child which has been removed.</param>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Remove(View view)
        {
            if(SnapToPage && CurrentPage == Children.IndexOf(view) &&  CurrentPage == Children.Count -1)
            {
                // Target View is current page and also last child.
                // CurrentPage should be changed to previous page.
                CurrentPage = Math.Max(0, CurrentPage-1);
                ScrollToIndex(CurrentPage);
            }

            ContentContainer.Remove(view);
        }

        private void OnScrollingChildRelayout(object source, EventArgs args)
        {
            // Size is changed. Calculate maxScrollDistance.
            bool isSizeChanged = previousContainerSize.Width != ContentContainer.Size.Width || previousContainerSize.Height != ContentContainer.Size.Height;

            if (isSizeChanged)
            {
                maxScrollDistance = CalculateMaximumScrollDistance();
                SetScrollbar();
            }

            previousContainerSize = ContentContainer.Size;
        }

        /// <summary>
        /// The composition of a Scrollbar can vary depending on how you use ScrollableBase. 
        /// Set the composition that will go into the ScrollableBase according to your ScrollableBase.
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void SetScrollbar()
        {
            if (Scrollbar)
            {
                bool isHorizontal = ScrollingDirection == Direction.Horizontal;
                float contentLength = isHorizontal ? ContentContainer.Size.Width : ContentContainer.Size.Height;
                float viewportLength = isHorizontal ? Size.Width : Size.Height;
                float currentPosition = isHorizontal ? ContentContainer.CurrentPosition.X : ContentContainer.CurrentPosition.Y;
                Scrollbar.Initialize(contentLength, viewportLength, currentPosition, isHorizontal);
            }
        }

        /// <summary>
        /// Scrolls to the item at the specified index.
        /// </summary>
        /// <param name="index">Index of item.</param>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ScrollToIndex(int index)
        {
            if (ContentContainer.ChildCount - 1 < index || index < 0)
            {
                return;
            }

            if (SnapToPage)
            {
                CurrentPage = index;
            }

            float targetPosition = Math.Min(ScrollingDirection == Direction.Vertical ? Children[index].Position.Y : Children[index].Position.X, maxScrollDistance);
            AnimateChildTo(ScrollDuration, -targetPosition);
        }

        private void OnScrollDragStarted()
        {
            ScrollEventArgs eventArgs = new ScrollEventArgs(ContentContainer.CurrentPosition);
            ScrollDragStarted?.Invoke(this, eventArgs);
        }

        private void OnScrollDragEnded()
        {
            ScrollEventArgs eventArgs = new ScrollEventArgs(ContentContainer.CurrentPosition);
            ScrollDragEnded?.Invoke(this, eventArgs);
        }

        private void OnScrollAnimationStarted()
        {
            ScrollEventArgs eventArgs = new ScrollEventArgs(ContentContainer.CurrentPosition);
            ScrollAnimationStarted?.Invoke(this, eventArgs);
        }

        private void OnScrollAnimationEnded()
        {
            ScrollEventArgs eventArgs = new ScrollEventArgs(ContentContainer.CurrentPosition);
            ScrollAnimationEnded?.Invoke(this, eventArgs);
        }

        private bool readyToNotice = false;

        private float noticeAnimationEndBeforePosition = 0.0f;
        // Let's consider more whether this needs to be set as protected.
        public float NoticeAnimationEndBeforePosition { get => noticeAnimationEndBeforePosition; set => noticeAnimationEndBeforePosition = value; }

        private void OnScroll()
        {
            ScrollEventArgs eventArgs = new ScrollEventArgs(ContentContainer.CurrentPosition);
            Scrolling?.Invoke(this, eventArgs);

            bool isHorizontal = ScrollingDirection == Direction.Horizontal;
            float contentLength = isHorizontal ? ContentContainer.Size.Width : ContentContainer.Size.Height;
            float currentPosition = isHorizontal ? ContentContainer.CurrentPosition.X : ContentContainer.CurrentPosition.Y;

            scrollBar.Update(contentLength, Math.Abs(currentPosition));
            CheckPreReachedTargetPosition();
        }

        private void CheckPreReachedTargetPosition()
        {
            // Check whether we reached pre-reached target position
            if (readyToNotice &&
                ContentContainer.CurrentPosition.Y <= finalTargetPosition + NoticeAnimationEndBeforePosition &&
                ContentContainer.CurrentPosition.Y >= finalTargetPosition - NoticeAnimationEndBeforePosition)
            {
                //Notice first
                readyToNotice = false;
                OnPreReachedTargetPosition(finalTargetPosition);
            }
        }

        /// <summary>
        /// This helps developer who wants to know before scroll is reaching target position.
        /// </summary>
        /// <param name="targetPosition">Index of item.</param>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void OnPreReachedTargetPosition(float targetPosition)
        {

        }

        private void StopScroll()
        {
            if (scrollAnimation != null)
            {
                if (scrollAnimation.State == Animation.States.Playing)
                {
                    Debug.WriteLineIf(LayoutDebugScrollableBase, "StopScroll Animation Playing");
                    scrollAnimation.Stop(Animation.EndActions.Cancel);
                    OnScrollAnimationEnded();
                }
                scrollAnimation.Clear();
            }
        }

        // static constructor registers the control type
        static ScrollableBase()
        {
            // ViewRegistry registers control type with DALi type registry
            // also uses introspection to find any properties that need to be registered with type registry
            CustomViewRegistry.Instance.Register(CreateInstance, typeof(ScrollableBase));
        }

        internal static CustomView CreateInstance()
        {
            return new ScrollableBase();
        }

        private void AnimateChildTo(int duration, float axisPosition)
        {
            Debug.WriteLineIf(LayoutDebugScrollableBase, "AnimationTo Animation Duration:" + duration + " Destination:" + axisPosition);
            finalTargetPosition = axisPosition;

            StopScroll(); // Will replace previous animation so will stop existing one.

            if (scrollAnimation == null)
            {
                scrollAnimation = new Animation();
                scrollAnimation.Finished += ScrollAnimationFinished;
            }

            scrollAnimation.Duration = duration;
            scrollAnimation.DefaultAlphaFunction = new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseOutSine);
            scrollAnimation.AnimateTo(ContentContainer, (ScrollingDirection == Direction.Horizontal) ? "PositionX" : "PositionY", axisPosition);
            scrolling = true;
            OnScrollAnimationStarted();
            scrollAnimation.Play();
        }

        /// <summary>
        /// Scroll to specific position with or without animation.
        /// </summary>
        /// <param name="position">Destination.</param>
        /// <param name="animate">Scroll with or without animation</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ScrollTo(float position, bool animate)
        {
            float currentPositionX = ContentContainer.CurrentPosition.X != 0 ? ContentContainer.CurrentPosition.X : ContentContainer.Position.X;
            float currentPositionY = ContentContainer.CurrentPosition.Y != 0 ? ContentContainer.CurrentPosition.Y : ContentContainer.Position.Y;
            float delta = ScrollingDirection == Direction.Horizontal ? currentPositionX : currentPositionY;
            // The argument position is the new pan position. So the new position of ScrollableBase becomes (-position).
            // To move ScrollableBase's position to (-position), it moves by (-position - currentPosition).
            delta = -position - delta;

            ScrollBy(delta, animate);
        }

        private float BoundScrollPosition(float targetPosition)
        {
            if (ScrollAvailableArea != null)
            {
                float minScrollPosition = ScrollAvailableArea.X;
                float maxScrollPosition = ScrollAvailableArea.Y;

                targetPosition = Math.Min(-minScrollPosition, targetPosition);
                targetPosition = Math.Max(-maxScrollPosition, targetPosition);
            }
            else
            {
                targetPosition = Math.Min(0, targetPosition);
                targetPosition = Math.Max(-maxScrollDistance, targetPosition);
            }

            return targetPosition;
        }

        private void ScrollBy(float displacement, bool animate)
        {
            if (GetChildCount() == 0 || maxScrollDistance < 0)
            {
                return;
            }

            float childCurrentPosition = (ScrollingDirection == Direction.Horizontal) ? ContentContainer.PositionX : ContentContainer.PositionY;

            Debug.WriteLineIf(LayoutDebugScrollableBase, "ScrollBy childCurrentPosition:" + childCurrentPosition +
                                                   " displacement:" + displacement,
                                                   " maxScrollDistance:" + maxScrollDistance);

            childTargetPosition = childCurrentPosition + displacement; // child current position + gesture displacement


            Debug.WriteLineIf(LayoutDebugScrollableBase, "ScrollBy currentAxisPosition:" + childCurrentPosition + "childTargetPosition:" + childTargetPosition);

            if (animate)
            {
                // Calculate scroll animaton duration
                float scrollDistance = Math.Abs(displacement);
                int duration = (int)((320 * FlickAnimationSpeed) + (scrollDistance * FlickAnimationSpeed));
                Debug.WriteLineIf(LayoutDebugScrollableBase, "Scroll Animation Duration:" + duration + " Distance:" + scrollDistance);

                readyToNotice = true;

                AnimateChildTo(duration, BoundScrollPosition(AdjustTargetPositionOfScrollAnimation(BoundScrollPosition(childTargetPosition))));
            }
            else
            {
                finalTargetPosition = BoundScrollPosition(childTargetPosition);

                // Set position of scrolling child without an animation
                if (ScrollingDirection == Direction.Horizontal)
                {
                    ContentContainer.PositionX = finalTargetPosition;
                }
                else
                {
                    ContentContainer.PositionY = finalTargetPosition;
                }

            }
        }

        /// <summary>
        /// you can override it to clean-up your own resources.
        /// </summary>
        /// <param name="type">DisposeTypes</param>
        /// This will be public opened in tizen_5.5 after ACR done. Before ACR, need to be hidden as inhouse API.
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void Dispose(DisposeTypes type)
        {
            if (disposed)
            {
                return;
            }

            if (type == DisposeTypes.Explicit)
            {
                StopScroll();

                if (mPanGestureDetector != null)
                {
                    mPanGestureDetector.Detected -= OnPanGestureDetected;
                    mPanGestureDetector.Dispose();
                    mPanGestureDetector = null;
                }

                if (mTapGestureDetector != null)
                {
                    mTapGestureDetector.Detected -= OnTapGestureDetected;
                    mTapGestureDetector.Dispose();
                    mTapGestureDetector = null;
                }
            }
            base.Dispose(type);
        }

        private float CalculateDisplacementFromVelocity(float axisVelocity)
        {
            // Map: flick speed of range (2.0 - 6.0) to flick multiplier of range (0.7 - 1.6)
            float speedMinimum = FlickThreshold;
            float speedMaximum = FlickThreshold + 6.0f;
            float multiplierMinimum = FlickDistanceMultiplierRange.X;
            float multiplierMaximum = FlickDistanceMultiplierRange.Y;

            float flickDisplacement = 0.0f;

            float speed = Math.Min(4.0f, Math.Abs(axisVelocity));

            Debug.WriteLineIf(LayoutDebugScrollableBase, "ScrollableBase Candidate Flick speed:" + speed);

            if (speed > FlickThreshold)
            {
                // Flick length is the length of the ScrollableBase.
                float flickLength = (ScrollingDirection == Direction.Horizontal) ? CurrentSize.Width : CurrentSize.Height;

                // Calculate multiplier by mapping speed between the multiplier minimum and maximum.
                multiplier = ((speed - speedMinimum) / ((speedMaximum - speedMinimum) * (multiplierMaximum - multiplierMinimum))) + multiplierMinimum;

                // flick displacement is the product of the flick length and multiplier
                flickDisplacement = ((flickLength * multiplier) * speed) / axisVelocity;  // *speed and /velocity to perserve sign.

                Debug.WriteLineIf(LayoutDebugScrollableBase, "Calculated FlickDisplacement[" + flickDisplacement + "] from speed[" + speed + "] multiplier:"
                                                        + multiplier);
            }
            return flickDisplacement;
        }

        private float CalculateMaximumScrollDistance()
        {
            float scrollingChildLength = 0;
            float scrollerLength = 0;
            if (ScrollingDirection == Direction.Horizontal)
            {
                Debug.WriteLineIf(LayoutDebugScrollableBase, "Horizontal");

                scrollingChildLength = ContentContainer.Size.Width;
                scrollerLength = Size.Width;
            }
            else
            {
                Debug.WriteLineIf(LayoutDebugScrollableBase, "Vertical");
                scrollingChildLength = ContentContainer.Size.Height;
                scrollerLength = Size.Height;
            }

            Debug.WriteLineIf(LayoutDebugScrollableBase, "ScrollBy maxScrollDistance:" + (scrollingChildLength - scrollerLength) +
                                                   " parent length:" + scrollerLength +
                                                   " scrolling child length:" + scrollingChildLength);

            return Math.Max(scrollingChildLength - scrollerLength, 0);
        }

        private void PageSnap()
        {
            Debug.WriteLineIf(LayoutDebugScrollableBase, "PageSnap with pan candidate totalDisplacement:" + totalDisplacementForPan +
                                                                " currentPage[" + CurrentPage + "]");

            //Increment current page if total displacement enough to warrant a page change.
            if (Math.Abs(totalDisplacementForPan) > (mPageWidth * ratioOfScreenWidthToCompleteScroll))
            {
                if (totalDisplacementForPan < 0)
                {
                    CurrentPage = Math.Min(Math.Max(Children.Count - 1, 0), ++CurrentPage);
                }
                else
                {
                    CurrentPage = Math.Max(0, --CurrentPage);
                }
            }

            // Animate to new page or reposition to current page
            float destinationX = -(Children[CurrentPage].Position.X + Children[CurrentPage].CurrentSize.Width / 2 - CurrentSize.Width / 2); // set to middle of current page
            Debug.WriteLineIf(LayoutDebugScrollableBase, "Snapping to page[" + CurrentPage + "] to:" + destinationX + " from:" + ContentContainer.PositionX);
            AnimateChildTo(ScrollDuration, destinationX);
        }

        private void Flick(float flickDisplacement)
        {
            if (SnapToPage && Children.Count > 0)
            {
                if ((flickWhenAnimating && scrolling == true) || (scrolling == false))
                {
                    if (flickDisplacement < 0)
                    {
                        CurrentPage = Math.Min(Math.Max(Children.Count - 1, 0), CurrentPage + 1);
                        Debug.WriteLineIf(LayoutDebugScrollableBase, "Snap - to page:" + CurrentPage);
                    }
                    else
                    {
                        CurrentPage = Math.Max(0, CurrentPage - 1);
                        Debug.WriteLineIf(LayoutDebugScrollableBase, "Snap + to page:" + CurrentPage);
                    }

                    float destinationX = -(Children[CurrentPage].Position.X + Children[CurrentPage].CurrentSize.Width / 2.0f - CurrentSize.Width / 2.0f); // set to middle of current page
                    Debug.WriteLineIf(LayoutDebugScrollableBase, "Snapping to :" + destinationX);
                    AnimateChildTo(ScrollDuration, destinationX);
                }
            }
            else
            {
                ScrollBy(flickDisplacement, true); // Animate flickDisplacement.
            }
        }

        private void OnPanGestureDetected(object source, PanGestureDetector.DetectedEventArgs e)
        {
            if (e.PanGesture.State == Gesture.StateType.Started)
            {
                base.Add(mInterruptTouchingChild);
                Debug.WriteLineIf(LayoutDebugScrollableBase, "Gesture Start");
                if (scrolling && !SnapToPage)
                {
                    StopScroll();
                }
                totalDisplacementForPan = 0.0f;
                OnScrollDragStarted();
            }
            else if (e.PanGesture.State == Gesture.StateType.Continuing)
            {
                if (ScrollingDirection == Direction.Horizontal)
                {
                    ScrollBy(e.PanGesture.Displacement.X, false);
                    totalDisplacementForPan += e.PanGesture.Displacement.X;
                }
                else
                {
                    ScrollBy(e.PanGesture.Displacement.Y, false);
                    totalDisplacementForPan += e.PanGesture.Displacement.Y;
                }
                Debug.WriteLineIf(LayoutDebugScrollableBase, "OnPanGestureDetected Continue totalDisplacementForPan:" + totalDisplacementForPan);
            }
            else if (e.PanGesture.State == Gesture.StateType.Finished)
            {
                float axisVelocity = (ScrollingDirection == Direction.Horizontal) ? e.PanGesture.Velocity.X : e.PanGesture.Velocity.Y;
                float flickDisplacement = CalculateDisplacementFromVelocity(axisVelocity);

                Debug.WriteLineIf(LayoutDebugScrollableBase, "FlickDisplacement:" + flickDisplacement + "TotalDisplacementForPan:" + totalDisplacementForPan);
                OnScrollDragEnded();

                if (flickDisplacement > 0 | flickDisplacement < 0)// Flick detected
                {
                    Flick(flickDisplacement);
                }
                else
                {
                    // End of panning gesture but was not a flick
                    if (SnapToPage && Children.Count > 0)
                    {
                        PageSnap();
                    }
                    else
                    {
                        ScrollBy(0, true);
                    }
                }
                totalDisplacementForPan = 0;

                base.Remove(mInterruptTouchingChild);
            }
        }

        private new void OnTapGestureDetected(object source, TapGestureDetector.DetectedEventArgs e)
        {
            if (e.TapGesture.Type == Gesture.GestureType.Tap)
            {
                // Stop scrolling if tap detected (press then relase).
                // Unless in Pages mode, do not want a page change to stop part way.
                if (scrolling && !SnapToPage)
                {
                    StopScroll();
                }
            }
        }

        private void ScrollAnimationFinished(object sender, EventArgs e)
        {
            scrolling = false;
            CheckPreReachedTargetPosition();
            OnScrollAnimationEnded();
        }

        /// <summary>
        /// Adjust scrolling position by own scrolling rules.
        /// Override this function when developer wants to change destination of flicking.(e.g. always snap to center of item)
        /// </summary>
        /// This may be public opened in tizen_6.0 after ACR done. Before ACR, need to be hidden as inhouse API
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual float AdjustTargetPositionOfScrollAnimation(float position)
        {
            return position;
        }

    }

} // namespace
