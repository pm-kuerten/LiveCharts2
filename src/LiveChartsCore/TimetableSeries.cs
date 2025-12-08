// The MIT License(MIT)
//
// Copyright(c) 2021 Alberto Rodriguez Orozco & LiveCharts Contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Drawing;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.VisualElements;

namespace LiveChartsCore;

/// <summary>
/// Defines a scatter series.
/// </summary>
/// <typeparam name="TModel">The type of the model.</typeparam>
/// <typeparam name="TVisual">The type of the visual.</typeparam>
/// <typeparam name="TLabel">The type of the label.</typeparam>
/// <seealso cref="CartesianSeries{TModel, TVisual, TLabel}" />
public class TimetableSeries<TModel, TVisual, TLabel>
    : StrokeAndFillCartesianSeries<TModel, TVisual, TLabel>, ITimetableSeries
        where TVisual : BoundedDrawnGeometry, new()
        where TLabel : BaseLabelGeometry, new()
{
    private double _minGeometrySize = 6d;
    private double _geometrySize = 24d;
    private double _rx;
    private double _ry;
    private double _barOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoreScatterSeries{TModel, TVisual, TLabel, TErrorGeometry}"/> class.
    /// </summary>
    /// <param name="values">The values.</param>
    public TimetableSeries(IReadOnlyCollection<TModel>? values)
        : base(GetProperties(), values)
    {
        DataPadding = new LvcPoint(1, 1);

        DataLabelsFormatter = (point) => $"{point.Coordinate.PrimaryValue}";
        YToolTipLabelFormatter = point =>
        {
            //var series = (TimetableSeries<TModel, TVisual, TLabel>)point.Context.Series;
            var c = point.Coordinate;
            return $"X = {c.SecondaryValue}{Environment.NewLine}" +
                   $"Length = {c.TertiaryValue}";
        };
    }

    /// <summary>
    /// Gets or sets the minimum size of the geometry.
    /// </summary>
    /// <value>
    /// The minimum size of the geometry.
    /// </value>
    public double MinGeometrySize { get => _minGeometrySize; set => SetProperty(ref _minGeometrySize, value); }
    /// <summary>
    /// Gets or sets the size of the geometry.
    /// </summary>
    /// <value>
    /// The size of the geometry.
    /// </value>
    public double GeometrySize { get => _geometrySize; set => SetProperty(ref _geometrySize, value); }

    /// <inheritdoc cref="ITimetableSeries.Rx"/>
    public double Rx { get => _rx; set => SetProperty(ref _rx, value); }

    /// <inheritdoc cref="ITimetableSeries.Ry"/>
    public double Ry { get => _ry; set => SetProperty(ref _ry, value); }

    /// <inheritdoc />
    public double BarOffset { get => _barOffset; set => SetProperty(ref _barOffset, value); }

    /// <inheritdoc cref="ChartElement.Invalidate(Chart)"/>
    public override void Invalidate(Chart chart)
    {
        var cartesianChart = (CartesianChartEngine)chart;
        var primaryAxis = cartesianChart.YAxes[ScalesYAt];
        var secondaryAxis = cartesianChart.XAxes[ScalesXAt];

        var drawLocation = cartesianChart.DrawMarginLocation;
        var drawMarginSize = cartesianChart.DrawMarginSize;
        var xScale = new Scaler(drawLocation, drawMarginSize, secondaryAxis);
        var yScale = new Scaler(drawLocation, drawMarginSize, primaryAxis);

        var actualZIndex = ZIndex == 0 ? ((ISeries)this).SeriesId : ZIndex;
        var clipping = GetClipRectangle(cartesianChart);

        if (Fill is not null)
        {
            Fill.ZIndex = actualZIndex + 0.1;
            Fill.SetClipRectangle(cartesianChart.Canvas, clipping);
            cartesianChart.Canvas.AddDrawableTask(Fill);
        }
        if (Stroke is not null)
        {
            Stroke.ZIndex = actualZIndex + 0.2;
            Stroke.SetClipRectangle(cartesianChart.Canvas, clipping);
            cartesianChart.Canvas.AddDrawableTask(Stroke);
        }
        if (DataLabelsPaint is not null)
        {
            DataLabelsPaint.ZIndex = actualZIndex + 0.4;
            DataLabelsPaint.SetClipRectangle(cartesianChart.Canvas, clipping);
            cartesianChart.Canvas.AddDrawableTask(DataLabelsPaint);
        }

        var rx = (float)Rx;
        var ry = (float)Ry;

        var dls = (float)DataLabelsSize;
        var pointsCleanup = ChartPointCleanupContext.For(everFetched);

        var geometryHeight = (float)GeometrySize;
        var halfGeometryHeight = geometryHeight / 2f;

        var hasSvg = this.HasVariableSvgGeometry();

        var isFirstDraw = !chart.IsDrawn(((ISeries)this).SeriesId);
        var barOffset = (float)BarOffset;

        foreach (var point in Fetch(cartesianChart))
        {
            var coordinate = point.Coordinate;
            var visual = (TVisual?)point.Context.Visual;

            var x = xScale.ToPixels(coordinate.SecondaryValue) - barOffset;
            var y = yScale.ToPixels(coordinate.PrimaryValue);

            float geometryWidth;
            if (coordinate.TertiaryValue == 0)
            {
                geometryWidth = geometryHeight;
                x -= halfGeometryHeight;
            }
            else geometryWidth = xScale.ToPixels(coordinate.TertiaryValue) - xScale.ToPixels(pivot) + barOffset * 2;

            if (point.IsEmpty || !IsVisible)
            {
                if (visual is not null)
                {
                    visual.X = x;
                    visual.Y = y - halfGeometryHeight;
                    visual.Width = 0;
                    visual.Height = 0;
                    visual.RemoveOnCompleted = true;
                    point.Context.Visual = null;
                }

                if (point.Context.Label is not null)
                {
                    var label = (TLabel)point.Context.Label;

                    label.X = x;
                    label.Y = y - halfGeometryHeight;
                    label.Opacity = 0;
                    label.RemoveOnCompleted = true;

                    point.Context.Label = null;
                }

                pointsCleanup.Clean(point);

                continue;
            }


            if (visual is null)
            {
                var r = new TVisual
                {
                    X = x,
                    Y = y,
                    Width = 0,
                    Height = 0
                };

                visual = r;
                point.Context.Visual = visual;
                OnPointCreated(point);

                _ = everFetched.Add(point);

                if (r is BaseRoundedRectangleGeometry rg)
                    rg.BorderRadius = new LvcPoint(rx, ry);
            }

            if (hasSvg)
            {
                var svgVisual = (IVariableSvgPath)visual;
                if (_geometrySvgChanged || svgVisual.SVGPath is null)
                    svgVisual.SVGPath = GeometrySvg ?? throw new Exception("svg path is not defined");
            }

            Fill?.AddGeometryToPaintTask(cartesianChart.Canvas, visual);
            Stroke?.AddGeometryToPaintTask(cartesianChart.Canvas, visual);

            var sizedGeometry = visual;

            sizedGeometry.X = x;
            sizedGeometry.Y = y - halfGeometryHeight;
            sizedGeometry.Width = geometryWidth;
            sizedGeometry.Height = geometryHeight;

            if (sizedGeometry is BaseRoundedRectangleGeometry rrg)
                rrg.BorderRadius = new LvcPoint(rx, ry);

            sizedGeometry.RemoveOnCompleted = false;

            if (point.Context.HoverArea is not RectangleHoverArea ha)
                point.Context.HoverArea = ha = new RectangleHoverArea();
            _ = ha.SetDimensions(x, y - halfGeometryHeight, geometryWidth, geometryHeight).CenterXToolTip().CenterYToolTip();

            pointsCleanup.Clean(point);

            if (DataLabelsPaint is not null)
            {
                if (point.Context.Label is not TLabel label)
                {
                    var l = new TLabel
                    {
                        X = x,
                        Y = y - halfGeometryHeight,
                        RotateTransform = (float)DataLabelsRotation,
                        MaxWidth = (float)DataLabelsMaxWidth
                    };
                    l.Animate(EasingFunction ?? cartesianChart.EasingFunction, AnimationsSpeed ?? cartesianChart.AnimationsSpeed);
                    label = l;
                    point.Context.Label = l;
                }

                DataLabelsPaint.AddGeometryToPaintTask(cartesianChart.Canvas, label);
                label.Text = DataLabelsFormatter(new ChartPoint<TModel, TVisual, TLabel>(point));
                label.TextSize = dls;
                label.Padding = DataLabelsPadding;
                label.Paint = DataLabelsPaint;

                if (isFirstDraw)
                    label.CompleteTransition(
                        nameof(label.TextSize), nameof(label.X), nameof(label.Y), nameof(label.RotateTransform));

                var m = label.Measure();
                var labelPosition = GetLabelPosition(
                    x, y - halfGeometryHeight, geometryWidth, geometryHeight, m, DataLabelsPosition,
                    SeriesProperties, coordinate.PrimaryValue > 0, drawLocation, drawMarginSize);
                if (DataLabelsTranslate is not null) label.TranslateTransform =
                        new LvcPoint(m.Width * DataLabelsTranslate.Value.X, m.Height * DataLabelsTranslate.Value.Y);
                label.X = labelPosition.X;
                label.Y = labelPosition.Y;
            }

            OnPointMeasured(point);
        }

        pointsCleanup.CollectPoints(everFetched, cartesianChart.View, yScale, xScale, SoftDeleteOrDisposePoint);
        _geometrySvgChanged = false;
    }

    /// <inheritdoc cref="CartesianSeries{TModel, TVisual, TLabel}.GetBounds(Chart, ICartesianAxis, ICartesianAxis)"/>
    public override SeriesBounds GetBounds(
        Chart chart, ICartesianAxis secondaryAxis, ICartesianAxis primaryAxis)
    {
        var rawBounds = DataFactory.GetTimetableBounds(chart, this, secondaryAxis, primaryAxis);
        if (rawBounds.HasData) return rawBounds;

        var rawBaseBounds = rawBounds.Bounds;

        var tickPrimary = primaryAxis.GetTick(chart.ControlSize, rawBaseBounds.VisiblePrimaryBounds);
        var tickSecondary = secondaryAxis.GetTick(chart.ControlSize, rawBaseBounds.VisibleSecondaryBounds);

        var ts = tickSecondary.Value * DataPadding.X;
        var tp = tickPrimary.Value * DataPadding.Y;

        var rgs = GetRequestedGeometrySize();
        var rso = GetRequestedSecondaryOffset();
        var rpo = GetRequestedPrimaryOffset();

        var dimensionalBounds = new DimensionalBounds
        {
            SecondaryBounds = new Bounds
            {
                Max = rawBaseBounds.SecondaryBounds.Max + rso * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.SecondaryBounds.Min - rso * secondaryAxis.UnitWidth,
                MinDelta = rawBaseBounds.SecondaryBounds.MinDelta,
                PaddingMax = ts,
                PaddingMin = ts,
                RequestedGeometrySize = rgs
            },
            PrimaryBounds = new Bounds
            {
                Max = rawBaseBounds.PrimaryBounds.Max + rpo * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.PrimaryBounds.Min - rpo * secondaryAxis.UnitWidth,
                MinDelta = rawBaseBounds.PrimaryBounds.MinDelta,
                PaddingMax = tp,
                PaddingMin = tp,
                RequestedGeometrySize = rgs
            },
            VisibleSecondaryBounds = new Bounds
            {
                Max = rawBaseBounds.VisibleSecondaryBounds.Max + rso * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.VisibleSecondaryBounds.Min - rso * secondaryAxis.UnitWidth,
            },
            VisiblePrimaryBounds = new Bounds
            {
                Max = rawBaseBounds.VisiblePrimaryBounds.Max + rpo * secondaryAxis.UnitWidth,
                Min = rawBaseBounds.VisiblePrimaryBounds.Min - rpo * secondaryAxis.UnitWidth
            },
            TertiaryBounds = rawBaseBounds.TertiaryBounds,
            VisibleTertiaryBounds = rawBaseBounds.VisibleTertiaryBounds
        };

        return new SeriesBounds(dimensionalBounds, false);
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.GetMiniaturesSketch"/>
    [Obsolete($"Replaced by ${nameof(GetMiniatureGeometry)}")]
    public override Sketch GetMiniaturesSketch()
    {
        var schedules = new List<PaintSchedule>();

        if (Fill is not null) schedules.Add(BuildMiniatureSchedule(Fill, new TVisual()));
        if (Stroke is not null) schedules.Add(BuildMiniatureSchedule(Stroke, new TVisual()));

        return new Sketch(MiniatureShapeSize, MiniatureShapeSize, GeometrySvg)
        {
            PaintSchedules = schedules
        };
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.GetMiniature"/>"/>
    [Obsolete($"Replaced by ${nameof(GetMiniatureGeometry)}")]
    public override IChartElement GetMiniature(ChartPoint? point, int zindex)
    {
        var typedPoint = point is null ? null : ConvertToTypedChartPoint(point);

        return new GeometryVisual<TVisual, TLabel>
        {
            Fill = GetMiniatureFill(point, zindex + 1),
            Stroke = GetMiniatureStroke(point, zindex + 2),
            Width = MiniatureShapeSize,
            Height = MiniatureShapeSize,
            Rotation = typedPoint?.Visual?.RotateTransform ?? 0,
            Svg = GeometrySvg,
            ClippingMode = ClipMode.None
        };
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.GetMiniatureGeometry(ChartPoint)"/>
    public override IDrawnElement GetMiniatureGeometry(ChartPoint? point)
    {
        var typedPoint = point is null ? null : ConvertToTypedChartPoint(point);

        var m = new TVisual
        {
            Fill = GetMiniatureFill(point, 0),
            Stroke = GetMiniatureStroke(point, 0),
            Width = (float)MiniatureShapeSize,
            Height = (float)MiniatureShapeSize,
            RotateTransform = typedPoint?.Visual?.RotateTransform ?? 0,
        };

        if (m is IVariableSvgPath svg) svg.SVGPath = GeometrySvg;

        return m;
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.OnPointerEnter(ChartPoint)"/>
    protected override void OnPointerEnter(ChartPoint point)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        visual.Opacity = 0.8f;

        base.OnPointerEnter(point);
    }

    /// <inheritdoc cref="Series{TModel, TVisual, TLabel}.OnPointerLeft(ChartPoint)"/>
    protected override void OnPointerLeft(ChartPoint point)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        visual.Opacity = 1;

        base.OnPointerLeft(point);
    }

    /// <inheritdoc cref="SetDefaultPointTransitions(ChartPoint)"/>
    protected override void SetDefaultPointTransitions(ChartPoint chartPoint)
    {
        var visual = (TVisual?)chartPoint.Context.Visual;
        var chart = chartPoint.Context.Chart;

        if (visual is null) throw new Exception("Unable to initialize the point instance.");

        var easing = EasingFunction ?? chart.EasingFunction;
        var speed = AnimationsSpeed ?? chart.AnimationsSpeed;

        visual.Animate(easing, speed);
    }

    /// <inheritdoc cref="SoftDeleteOrDisposePoint(ChartPoint, Scaler, Scaler)"/>
    protected internal override void SoftDeleteOrDisposePoint(ChartPoint point, Scaler primaryScale, Scaler secondaryScale)
    {
        var visual = (TVisual?)point.Context.Visual;
        if (visual is null) return;
        if (DataFactory is null) throw new Exception("Data provider not found");

        var c = point.Coordinate;

        var x = secondaryScale.ToPixels(c.SecondaryValue);
        var y = primaryScale.ToPixels(c.PrimaryValue);

        visual.X = x;
        visual.Y = y;
        visual.Height = 0;
        visual.Width = 0;
        visual.RemoveOnCompleted = true;

        DataFactory.DisposePoint(point);

        var label = (TLabel?)point.Context.Label;
        if (label is null) return;

        label.TextSize = 1;
        label.RemoveOnCompleted = true;
    }

    private static SeriesProperties GetProperties() =>
        SeriesProperties.Timetable | SeriesProperties.Solid | SeriesProperties.PrefersXYStrategyTooltips |
        SeriesProperties.PrimaryAxisHorizontalOrientation;
}
